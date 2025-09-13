using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SacksDataLayer.Data;
using System.Data;
using System.Text;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.ComponentModel; // Needed for ListSortDirection
using Microsoft.Data.SqlClient; // Added for parameterized SQL execution
using System.Text.Json; // For state persistence

namespace SacksApp
{
    public partial class SqlQueryForm : Form
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<SqlQueryForm> _logger;
        private readonly SacksDbContext _dbContext;
        private readonly List<string> _hiddenColumns = new();
        private readonly List<FilterCondition> _filters = new();
        private List<string> _availableColumns = new();
        private const string StateFileName = "SqlQueryForm.UserState.json";
        private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
        private List<string>? _pendingColumnOrder; // loaded order to apply after data bind
        private (string column, ListSortDirection direction)? _pendingSort; // loaded sort to apply after data bind
        private Dictionary<string, float>? _pendingFillWeights; // loaded fill weights to apply after data bind

        private static readonly Type ViewType = typeof(SacksDataLayer.Entities.ProductOffersView);
        private static readonly string[] EntityPropertyNames = ViewType
            .GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
            .Select(p => p.Name)
            .OrderBy(n => n)
            .ToArray();

        private static readonly HashSet<string> ProductColumns = new(StringComparer.OrdinalIgnoreCase)
        {
            // Core product + product dynamic properties (columns we blank on non-lowest offer)
            "EAN","Name","Category","Brand","Line","Gender","Concentration","Size","Type","Decoded","COO","Units","Ref"
        };
        private static readonly HashSet<string> OfferColumns = new(StringComparer.OrdinalIgnoreCase)
        {
            // Offer-level & per-offer metrics (always shown)
            "Price","Currency","Quantity","Description", // op level
            "Supplier Name","Offer Name","Date Offer",     // supplier/offer metadata (aliases with spaces)
            "OfferRank","TotalOffers"                       // ranking + count (needed for ordering / context)
        };

        public SqlQueryForm(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = _serviceProvider.GetRequiredService<ILogger<SqlQueryForm>>();
            _dbContext = _serviceProvider.GetRequiredService<SacksDbContext>();

            InitializeComponent();
            SetupControls();
            InitializeQueryDesigner();
        }

        #region Setup / Initialization
        private void InitializeQueryDesigner()
        {
            try
            {
                LoadAvailableColumns();

                //ProductOffersView is the only table/view available

                columnsCheckedListBox.Items.Clear();
                columnsCheckedListBox.Items.AddRange(_availableColumns.ToArray());
                for (int i = 0; i < columnsCheckedListBox.Items.Count; i++)
                {
                    columnsCheckedListBox.SetItemChecked(i, true);
                }

                filterColumnComboBox.Items.Clear();
                filterColumnComboBox.Items.AddRange(_availableColumns.ToArray());
                if (filterColumnComboBox.Items.Count > 0) filterColumnComboBox.SelectedIndex = 0;

                filterOperatorComboBox.Items.Clear();
                filterColumnComboBox.SelectedIndexChanged += (s, e) => RefreshOperatorList();
                RefreshOperatorList();

                addFilterButton.Click += (s, e) => AddFilter();
                removeFilterButton.Click += (s, e) => RemoveSelectedFilter();
                buildButton.Click += BuildButton_Click; // wire build/run button

                // also react when collapse mode toggles to update grid behavior
                if (collapseProductsCheckBox != null)
                {
                    collapseProductsCheckBox.CheckedChanged += (s, e) => UpdateGridSortability();
                }

                // Load persisted state after controls are populated
                LoadUserState();

                UpdateStatus("Ready - Design your query and click Run");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not initialize ProductOffersView query designer");
            }
        }

        private void LoadAvailableColumns()
        {
            try
            {
                var connString = _dbContext.Database.GetConnectionString();
                if (string.IsNullOrWhiteSpace(connString)) throw new InvalidOperationException("Missing connection string");
                using var conn = new SqlConnection(connString);
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT TOP (0) * FROM [ProductOffersView]"; // schema only
                conn.Open();
                using var reader = cmd.ExecuteReader(CommandBehavior.SchemaOnly);
                var schema = reader.GetSchemaTable();
                var cols = new List<string>();
                if (schema != null)
                {
                    foreach (DataRow row in schema.Rows)
                    {
                        var name = row["ColumnName"]?.ToString();
                        if (!string.IsNullOrWhiteSpace(name)) cols.Add(name);
                    }
                }
                if (cols.Count > 0)
                {
                    _availableColumns = cols.Distinct().OrderBy(c => c).ToList();
                    return;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to read schema for ProductOffersView, falling back to entity properties");
            }
            // fallback
            _availableColumns = EntityPropertyNames.ToList();
        }
        #endregion

        #region Filter Infrastructure
        private enum FilterOperator
        {
            Equals,
            NotEquals,
            GreaterThan,
            GreaterThanOrEqual,
            LessThan,
            LessThanOrEqual,
            Contains,
            StartsWith,
            EndsWith,
            IsEmpty,
            IsNotEmpty
        }

        private sealed class FilterCondition
        {
            public string PropertyName { get; init; } = string.Empty;
            public FilterOperator Operator { get; init; }
            public string? RawValue { get; init; }
            public Type PropertyType { get; init; } = typeof(string);
            public override string ToString()
            {
                var opText = Operator switch
                {
                    FilterOperator.Equals => "=",
                    FilterOperator.NotEquals => "!=",
                    FilterOperator.GreaterThan => ">",
                    FilterOperator.GreaterThanOrEqual => ">=",
                    FilterOperator.LessThan => "<",
                    FilterOperator.LessThanOrEqual => "<=",
                    FilterOperator.Contains => "contains",
                    FilterOperator.StartsWith => "starts with",
                    FilterOperator.EndsWith => "ends with",
                    FilterOperator.IsEmpty => "is empty",
                    FilterOperator.IsNotEmpty => "is not empty",
                    _ => "?"

                };
                return Operator is FilterOperator.IsEmpty or FilterOperator.IsNotEmpty
                    ? $"{PropertyName} {opText}"
                    : $"{PropertyName} {opText} '{RawValue}'";
            }
        }

        private void RefreshOperatorList()
        {
            filterOperatorComboBox.Items.Clear();
            if (filterColumnComboBox.SelectedItem is not string col) return;
            // Try to infer type from entity property name if it exists; else default to string
            var propInfo = ViewType.GetProperty(col);
            var type = propInfo != null ? (Nullable.GetUnderlyingType(propInfo.PropertyType) ?? propInfo.PropertyType) : typeof(string);
            IEnumerable<FilterOperator> ops;
            if (type == typeof(string))
            {
                ops = new[]
                {
                    FilterOperator.Contains,
                    FilterOperator.StartsWith,
                    FilterOperator.EndsWith,
                    FilterOperator.Equals,
                    FilterOperator.NotEquals,
                    FilterOperator.IsEmpty,
                    FilterOperator.IsNotEmpty
                };
            }
            else if (type == typeof(int) || type == typeof(decimal) || type == typeof(double) || type == typeof(DateTime))
            {
                ops = new[]
                {
                    FilterOperator.Equals,
                    FilterOperator.NotEquals,
                    FilterOperator.GreaterThan,
                    FilterOperator.GreaterThanOrEqual,
                    FilterOperator.LessThan,
                    FilterOperator.LessThanOrEqual
                };
            }
            else
            {
                ops = new[] { FilterOperator.Equals, FilterOperator.NotEquals };
            }
            foreach (var op in ops) filterOperatorComboBox.Items.Add(op);
            if (filterOperatorComboBox.Items.Count > 0) filterOperatorComboBox.SelectedIndex = 0;
        }

        private void AddFilter()
        {
            if (filterColumnComboBox.SelectedItem is not string col) return;
            if (filterOperatorComboBox.SelectedItem is not FilterOperator op) return;
            // Map to entity property if names differ? If not found default to string
            var propInfo = ViewType.GetProperty(col);
            var type = propInfo != null ? (Nullable.GetUnderlyingType(propInfo.PropertyType) ?? propInfo.PropertyType) : typeof(string);

            string? raw = filterValueTextBox.Text.Trim();
            if (op is FilterOperator.IsEmpty or FilterOperator.IsNotEmpty)
            {
                raw = null; // value not needed
            }
            else if (string.IsNullOrWhiteSpace(raw))
            {
                return; // require value
            }

            if (raw != null && type != typeof(string))
            {
                try { _ = ConvertToType(raw, type); }
                catch (Exception ex)
                {
                    MessageBox.Show($"Value '{raw}' is not valid for {col} ({type.Name}): {ex.Message}", "Invalid Value", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }

            var condition = new FilterCondition
            {
                PropertyName = col,
                Operator = op,
                RawValue = raw,
                PropertyType = type
            };
            _filters.Add(condition);
            filtersListBox.Items.Add(condition.ToString());
            filterValueTextBox.Clear();
        }

        private void RemoveSelectedFilter()
        {
            var idx = filtersListBox.SelectedIndex;
            if (idx >= 0 && idx < _filters.Count)
            {
                _filters.RemoveAt(idx);
                filtersListBox.Items.RemoveAt(idx);
            }
        }
        #endregion

        #region Query Execution (SQL Building)
        private static object? ConvertToType(string raw, Type target)
        {
            if (target == typeof(string)) return raw;
            if (target == typeof(int)) return int.Parse(raw, System.Globalization.CultureInfo.InvariantCulture);
            if (target == typeof(decimal)) return decimal.Parse(raw, System.Globalization.CultureInfo.InvariantCulture);
            if (target == typeof(double)) return double.Parse(raw, System.Globalization.CultureInfo.InvariantCulture);
            if (target == typeof(DateTime)) return DateTime.Parse(raw, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AssumeLocal);
            return Convert.ChangeType(raw, target, System.Globalization.CultureInfo.InvariantCulture);
        }

        // Helper: escape SQL identifiers like column names
        private static string EscapeIdentifier(string name) => "[" + name.Replace("]", "]]" ) + "]";

        // Helper: builds a single predicate for a filter condition against a given table alias
        private string BuildPredicate(string alias, FilterCondition f, ref int paramIndex, List<SqlParameter> parameters)
        {
            var prefix = string.IsNullOrEmpty(alias) ? string.Empty : alias + ".";
            var colRef = prefix + EscapeIdentifier(f.PropertyName);
            if (f.Operator is FilterOperator.IsEmpty or FilterOperator.IsNotEmpty)
            {
                if (f.PropertyType == typeof(string))
                {
                    return f.Operator == FilterOperator.IsEmpty
                        ? $"({colRef} IS NULL OR {colRef} = '')"
                        : $"({colRef} IS NOT NULL AND {colRef} <> '')";
                }
                else
                {
                    return f.Operator == FilterOperator.IsEmpty ? $"{colRef} IS NULL" : $"{colRef} IS NOT NULL";
                }
            }
            else
            {
                var paramName = $"@p{paramIndex++}";
                object? valueObj = f.RawValue;
                if (valueObj != null && f.PropertyType != typeof(string))
                {
                    valueObj = ConvertToType(f.RawValue!, f.PropertyType);
                }
                string opSql = f.Operator switch
                {
                    FilterOperator.Contains => $"{colRef} LIKE {paramName}",
                    FilterOperator.StartsWith => $"{colRef} LIKE {paramName}",
                    FilterOperator.EndsWith => $"{colRef} LIKE {paramName}",
                    FilterOperator.Equals => $"{colRef} = {paramName}",
                    FilterOperator.NotEquals => $"{colRef} <> {paramName}",
                    FilterOperator.GreaterThan => $"{colRef} > {paramName}",
                    FilterOperator.GreaterThanOrEqual => $"{colRef} >= {paramName}",
                    FilterOperator.LessThan => $"{colRef} < {paramName}",
                    FilterOperator.LessThanOrEqual => $"{colRef} <= {paramName}",
                    _ => string.Empty
                };

                // add parameter with potential wildcard adjustments for LIKE
                object? paramVal = valueObj;
                switch (f.Operator)
                {
                    case FilterOperator.Contains: paramVal = $"%{valueObj}%"; break;
                    case FilterOperator.StartsWith: paramVal = $"{valueObj}%"; break;
                    case FilterOperator.EndsWith: paramVal = $"%{valueObj}"; break;
                }
                parameters.Add(new SqlParameter(paramName, paramVal ?? DBNull.Value));
                return opSql;
            }
        }

        // Helper: build WHERE clause for base view without aliasing (simple mode)
        private string BuildBaseWhere(List<SqlParameter> parameters)
        {
            if (_filters.Count == 0) return string.Empty;
            var whereParts = new List<string>();
            int paramIndex = 0;
            foreach (var f in _filters)
            {
                if (!_availableColumns.Contains(f.PropertyName, StringComparer.OrdinalIgnoreCase)) continue;
                var part = BuildPredicate(string.Empty, f, ref paramIndex, parameters);
                if (!string.IsNullOrWhiteSpace(part)) whereParts.Add(part);
            }
            return whereParts.Count > 0 ? " WHERE " + string.Join(" AND ", whereParts) : string.Empty;
        }

        // Helper: build WHERE for collapsed view usage (alias c); product filters via EXISTS to base view (alias v)
        private string BuildCollapsedViewWhere(List<SqlParameter> parameters)
        {
            if (_filters.Count == 0) return string.Empty;
            var whereParts = new List<string>();
            int paramIndex = 0;
            foreach (var f in _filters)
            {
                if (!_availableColumns.Contains(f.PropertyName, StringComparer.OrdinalIgnoreCase)) continue;
                if (ProductColumns.Contains(f.PropertyName) && !OfferColumns.Contains(f.PropertyName))
                {
                    var innerPred = BuildPredicate("v", f, ref paramIndex, parameters);
                    if (!string.IsNullOrWhiteSpace(innerPred))
                    {
                        whereParts.Add($"EXISTS (SELECT 1 FROM [ProductOffersView] AS v WHERE v.[EAN] = c.[EANKey] AND {innerPred})");
                    }
                }
                else
                {
                    var direct = BuildPredicate("c", f, ref paramIndex, parameters);
                    if (!string.IsNullOrWhiteSpace(direct)) whereParts.Add(direct);
                }
            }
            return whereParts.Count > 0 ? " WHERE " + string.Join(" AND ", whereParts) : string.Empty;
        }

        // Helper: build WHERE against base view alias v (for dynamic collapse path)
        private string BuildDynamicInnerWhere(List<SqlParameter> parameters)
        {
            if (_filters.Count == 0) return string.Empty;
            var whereParts = new List<string>();
            int paramIndex = 0;
            foreach (var f in _filters)
            {
                if (!_availableColumns.Contains(f.PropertyName, StringComparer.OrdinalIgnoreCase)) continue;
                var pred = BuildPredicate("v", f, ref paramIndex, parameters);
                if (!string.IsNullOrWhiteSpace(pred)) whereParts.Add(pred);
            }
            return whereParts.Count > 0 ? " WHERE " + string.Join(" AND ", whereParts) : string.Empty;
        }

        // Helper: build SELECT column list
        private static string BuildSelectList(IEnumerable<string> columns)
            => string.Join(", ", columns.Select(EscapeIdentifier));

        // Simple query from base view
        private string BuildSimpleQuery(List<string> selectedColumns, string where, bool applyTop)
        {
            var sb = new StringBuilder();
            var cols = BuildSelectList(selectedColumns);
            if (applyTop) sb.Append($"SELECT TOP ({(int)topNumericUpDown.Value}) {cols} FROM [ProductOffersView]");
            else sb.Append($"SELECT {cols} FROM [ProductOffersView]");
            sb.Append(where);
            var orderCols = new List<string>();
            if (selectedColumns.Contains("EAN", StringComparer.OrdinalIgnoreCase)) orderCols.Add("[EAN]");
            if (selectedColumns.Contains("OfferRank", StringComparer.OrdinalIgnoreCase)) orderCols.Add("[OfferRank]");
            else if (selectedColumns.Contains("Price", StringComparer.OrdinalIgnoreCase)) orderCols.Add("[Price]");
            if (orderCols.Count > 0) sb.Append(" ORDER BY " + string.Join(", ", orderCols));
            return sb.ToString();
        }

        // Collapsed query using dedicated collapsed view
        private string BuildCollapsedUsingView(List<string> selectedColumns, string where, bool applyTop)
        {
            var sb = new StringBuilder();
            var cols = BuildSelectList(selectedColumns);
            if (applyTop) sb.Append($"SELECT TOP ({(int)topNumericUpDown.Value}) {cols} FROM [ProductOffersViewCollapse] AS c");
            else sb.Append($"SELECT {cols} FROM [ProductOffersViewCollapse] AS c");
            sb.Append(where);
            sb.Append(" ORDER BY [EANKey], [OfferRank]");
            return sb.ToString();
        }

        // Dynamic collapsed query: filter base view then recompute collapse using ROW_NUMBER
        private string BuildDynamicCollapsed(List<string> selectedColumns, string innerWhere, bool applyTop)
        {
            var inner = new StringBuilder();
            inner.Append("SELECT v.*, ROW_NUMBER() OVER (PARTITION BY v.[EAN] ORDER BY v.[OfferRank]) AS rn, COUNT(*) OVER (PARTITION BY v.[EAN]) AS cnt FROM [ProductOffersView] AS v");
            inner.Append(innerWhere);

            var projected = new List<string>();
            foreach (var col in selectedColumns)
            {
                if (ProductColumns.Contains(col) && !OfferColumns.Contains(col))
                    projected.Add($"CASE WHEN rn = 1 THEN {EscapeIdentifier(col)} ELSE NULL END AS {EscapeIdentifier(col)}");
                else
                    projected.Add(EscapeIdentifier(col));
            }

            var sb = new StringBuilder();
            if (applyTop) sb.Append($"SELECT TOP ({(int)topNumericUpDown.Value}) "); else sb.Append("SELECT ");
            sb.Append(string.Join(", ", projected));
            sb.Append(" FROM (");
            sb.Append(inner);
            sb.Append(") AS x WHERE x.[cnt] > 1 ORDER BY x.[EAN], x.[OfferRank]");
            return sb.ToString();
        }

        private (string sql, List<SqlParameter> parameters) BuildSqlQuery()
        {
            var selectedColumns = columnsCheckedListBox.CheckedItems.Cast<string>().ToList();
            if (selectedColumns.Count == 0) selectedColumns = _availableColumns.ToList();

            var parameters = new List<SqlParameter>();
            bool collapse = collapseProductsCheckBox?.Checked == true;
            bool applyTop = radioButtonTop.Checked; // only apply TOP when radio selected

            if (!collapse)
            {
                var where = BuildBaseWhere(parameters);
                var sql = BuildSimpleQuery(selectedColumns, where, applyTop);
                return (sql, parameters);
            }

            // collapse mode
            bool hasOfferFilters = _filters.Any(f => OfferColumns.Contains(f.PropertyName));
            if (!hasOfferFilters)
            {
                var where = BuildCollapsedViewWhere(parameters);
                var sql = BuildCollapsedUsingView(selectedColumns, where, applyTop);
                return (sql, parameters);
            }
            else
            {
                var innerWhere = BuildDynamicInnerWhere(parameters);
                var sql = BuildDynamicCollapsed(selectedColumns, innerWhere, applyTop);
                return (sql, parameters);
            }
        }

        private async Task ExecuteSqlAsync(string sql, List<SqlParameter> parameters, CancellationToken cancellationToken)
        {
            SqlConnection? connection = null;
            try
            {
                SetExecuting(true);
                var sw = System.Diagnostics.Stopwatch.StartNew();
                var connString = _dbContext.Database.GetConnectionString();
                if (string.IsNullOrWhiteSpace(connString)) throw new InvalidOperationException("Connection string unavailable");
                connection = new SqlConnection(connString);
                await using (connection)
                {
                    await connection.OpenAsync(cancellationToken);
                    await using var cmd = connection.CreateCommand();
#pragma warning disable CA2100 // Query is user-composed but identifiers are constrained & values parameterized
                    cmd.CommandText = sql;
#pragma warning restore CA2100
                    cmd.CommandTimeout = 30;
                    foreach (var p in parameters) cmd.Parameters.Add(p);
                    await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
                    var dt = new DataTable();
                    dt.Load(reader);
                    resultsGrid.DataSource = dt;
                    UpdateGridSortability();
                    TryApplyColumnOrder();
                    TryApplySavedColumnFillWeights();
                    TryApplySavedSort();
                    UpdateStatus($"{dt.Rows.Count:N0} rows - {dt.Columns.Count} columns ({sw.ElapsedMilliseconds}ms)");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing generated query");
                MessageBox.Show($"Error executing query: {ex.Message}", "Query Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                UpdateStatus("Error");
            }
            finally
            {
                SetExecuting(false);
            }
        }

        private void SetExecuting(bool executing)
        {
            progressBar.Visible = executing;
            buildButton.Enabled = !executing;
            addFilterButton.Enabled = !executing;
            removeFilterButton.Enabled = !executing;
            columnsCheckedListBox.Enabled = !executing;
        }
        #endregion

        #region State Persistence
        private sealed class QueryFormState
        {
            public List<string> SelectedColumns { get; set; } = new();
            public List<FilterState> Filters { get; set; } = new();
            public string? OrderBy { get; set; }
            public string? OrderByDir { get; set; }
            public int Top { get; set; }
            public List<string> ColumnOrder { get; set; } = new();
            public bool CollapseProducts { get; set; } // new
            public Dictionary<string, float> ColumnFillWeights { get; set; } = new();
        }
        private sealed class FilterState
        {
            public string Property { get; set; } = string.Empty;
            public string Operator { get; set; } = string.Empty;
            public string? Value { get; set; }
        }

        private string GetStateFilePath()
        {
            try
            {
                // Store beside main form window state file (AppContext.BaseDirectory) for simplicity
                return Path.Combine(AppContext.BaseDirectory, StateFileName);
            }
            catch
            {
                return StateFileName;
            }
        }

        private void LoadUserState()
        {
            try
            {
                var path = GetStateFilePath();
                if (!File.Exists(path)) return;
                var json = File.ReadAllText(path);
                var state = JsonSerializer.Deserialize<QueryFormState>(json);
                if (state == null) return;

                // Restore top
                if (state.Top >= topNumericUpDown.Minimum && state.Top <= topNumericUpDown.Maximum)
                    topNumericUpDown.Value = state.Top;

                // Restore columns
                for (int i = 0; i < columnsCheckedListBox.Items.Count; i++)
                    columnsCheckedListBox.SetItemChecked(i, false);
                foreach (var col in state.SelectedColumns)
                {
                    var idx = columnsCheckedListBox.Items.IndexOf(col);
                    if (idx >= 0) columnsCheckedListBox.SetItemChecked(idx, true);
                }

                // Restore filters
                _filters.Clear();
                filtersListBox.Items.Clear();
                foreach (var f in state.Filters)
                {
                    if (!_availableColumns.Contains(f.Property, StringComparer.OrdinalIgnoreCase)) continue;
                    if (!TryParseOperator(f.Operator, out var op)) continue;
                    var propInfo = ViewType.GetProperty(f.Property);
                    var type = propInfo != null ? (Nullable.GetUnderlyingType(propInfo.PropertyType) ?? propInfo.PropertyType) : typeof(string);
                    var cond = new FilterCondition
                    {
                        PropertyName = f.Property,
                        Operator = op,
                        RawValue = f.Value,
                        PropertyType = type
                    };
                    _filters.Add(cond);
                    filtersListBox.Items.Add(cond.ToString());
                }

                // Column order (defer until data bound)
                if (state.ColumnOrder is { Count: > 0 })
                {
                    _pendingColumnOrder = state.ColumnOrder;
                    TryApplyColumnOrder(); // in case grid already has data
                }
                if (collapseProductsCheckBox != null)
                    collapseProductsCheckBox.Checked = state.CollapseProducts;

                // Saved sort (defer until data bound)
                if (!string.IsNullOrWhiteSpace(state.OrderBy) && !string.IsNullOrWhiteSpace(state.OrderByDir))
                {
                    var dir = state.OrderByDir.Equals("Descending", StringComparison.OrdinalIgnoreCase)
                        ? ListSortDirection.Descending
                        : ListSortDirection.Ascending;
                    _pendingSort = (state.OrderBy, dir);
                    TryApplySavedSort();
                }

                // Saved fill weights (defer until data bound)
                if (state.ColumnFillWeights is { Count: > 0 })
                {
                    _pendingFillWeights = state.ColumnFillWeights;
                    TryApplySavedColumnFillWeights();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load SqlQueryForm user state");
            }
        }

        private void SaveUserState()
        {
            try
            {
                var (orderBy, orderByDir) = GetCurrentGridSortOrPending();
                var state = new QueryFormState
                {
                    Top = (int)topNumericUpDown.Value,
                    SelectedColumns = columnsCheckedListBox.CheckedItems.Cast<string>().ToList(),
                    Filters = _filters.Select(f => new FilterState
                    {
                        Property = f.PropertyName,
                        Operator = f.Operator.ToString(),
                        Value = f.RawValue
                    }).ToList(),
                    ColumnOrder = GetCurrentGridColumnOrder(),
                    CollapseProducts = collapseProductsCheckBox?.Checked == true,
                    OrderBy = orderBy,
                    OrderByDir = orderByDir,
                    ColumnFillWeights = GetCurrentColumnFillWeights()
                };
                var json = JsonSerializer.Serialize(state, JsonOptions);
                File.WriteAllText(GetStateFilePath(), json, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to save SqlQueryForm user state");
            }
        }

        private (string? orderBy, string? dir) GetCurrentGridSortOrPending()
        {
            try
            {
                // If grid has an active sort, use it
                if (resultsGrid.SortedColumn != null && resultsGrid.SortOrder != System.Windows.Forms.SortOrder.None)
                {
                    var dir = resultsGrid.SortOrder == System.Windows.Forms.SortOrder.Descending ? "Descending" : "Ascending";
                    return (resultsGrid.SortedColumn.HeaderText, dir);
                }
                // Else if collapse is active but we have a pending sort from state, persist that
                if (_pendingSort != null)
                {
                    var (col, d) = _pendingSort.Value;
                    return (col, d == ListSortDirection.Descending ? "Descending" : "Ascending");
                }
            }
            catch { }
            return (null, null);
        }

        private Dictionary<string, float> GetCurrentColumnFillWeights()
        {
            var map = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
            try
            {
                foreach (DataGridViewColumn col in resultsGrid.Columns)
                {
                    map[col.HeaderText] = col.FillWeight;
                }
            }
            catch { }
            return map;
        }

        private static bool TryParseOperator(string opString, out FilterOperator op)
        {
            return Enum.TryParse(opString, ignoreCase: true, out op);
        }
        #endregion

        #region Existing UI Logic (Adjusted)
        private void SetupControls()
        {
            resultsGrid.AllowUserToAddRows = false;
            resultsGrid.AllowUserToDeleteRows = false;
            resultsGrid.ReadOnly = true;
            resultsGrid.AllowUserToOrderColumns = true;
            resultsGrid.AllowUserToResizeColumns = true;
            resultsGrid.AllowUserToResizeRows = true;
            resultsGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            resultsGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            resultsGrid.MultiSelect = true;
            resultsGrid.ClipboardCopyMode = DataGridViewClipboardCopyMode.EnableWithAutoHeaderText;
            resultsGrid.ColumnHeaderMouseClick += ResultsGrid_ColumnHeaderMouseClick;
            resultsGrid.ColumnDisplayIndexChanged += ResultsGrid_ColumnDisplayIndexChanged;
            resultsGrid.ColumnWidthChanged += ResultsGrid_ColumnWidthChanged;
            resultsGrid.Sorted += ResultsGrid_Sorted;
        }

        private void ResultsGrid_ColumnWidthChanged(object? sender, DataGridViewColumnEventArgs e)
        {
            try
            {
                // Update pending fill weights whenever a user resizes a column
                _pendingFillWeights = GetCurrentColumnFillWeights();
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to capture column width change");
            }
        }

        private void ResultsGrid_Sorted(object? sender, EventArgs e)
        {
            try
            {
                if (resultsGrid.SortedColumn == null) return;
                var dir = resultsGrid.SortOrder == System.Windows.Forms.SortOrder.Descending
                    ? ListSortDirection.Descending
                    : ListSortDirection.Ascending;
                _pendingSort = (resultsGrid.SortedColumn.HeaderText, dir);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to capture sorted state");
            }
        }

        private void ResultsGrid_ColumnDisplayIndexChanged(object? sender, DataGridViewColumnEventArgs e)
        {
            try
            {
                _pendingColumnOrder = GetCurrentGridColumnOrder();
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to capture column order change");
            }
        }

        private ContextMenuStrip CreateColumnContextMenu(DataGridViewColumn column)
        {
#pragma warning disable CA2000
            var menu = new ContextMenuStrip();
#pragma warning restore CA2000
            var hideItem = new ToolStripMenuItem($"Hide '{column.HeaderText}'");
            hideItem.Click += (s, e) => HideColumn(column);
            menu.Items.Add(hideItem);

            var sortAscItem = new ToolStripMenuItem("Sort Ascending");
            sortAscItem.Click += (s, e) => SortColumn(column, ListSortDirection.Ascending);
            var sortDescItem = new ToolStripMenuItem("Sort Descending");
            sortDescItem.Click += (s, e) => SortColumn(column, ListSortDirection.Descending);

            bool collapseActive = collapseProductsCheckBox?.Checked == true;
            sortAscItem.Enabled = !collapseActive;
            sortDescItem.Enabled = !collapseActive;

            menu.Items.Add(sortAscItem);
            menu.Items.Add(sortDescItem);

            menu.Items.Add(new ToolStripSeparator());
            var autoSizeItem = new ToolStripMenuItem("Auto-size Column");
            autoSizeItem.Click += (s, e) => column.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            menu.Items.Add(autoSizeItem);
            return menu;
        }

        private void HideColumn(DataGridViewColumn column)
        {
            var key = GetColumnKey(column);
            if (!_hiddenColumns.Contains(key)) _hiddenColumns.Add(key);
            column.Visible = false;
        }

        private static string GetColumnKey(DataGridViewColumn column)
            => !string.IsNullOrEmpty(column.Name) ? column.Name : column.HeaderText ?? string.Empty;

        private void SortColumn(DataGridViewColumn column, ListSortDirection direction)
        {
            // Disable manual sort in collapse mode
            if (collapseProductsCheckBox?.Checked == true) return;
            resultsGrid.Sort(column, direction);
        }

        private void UpdateGridSortability()
        {
            try
            {
                bool collapseActive = collapseProductsCheckBox?.Checked == true;

                // Clear any existing sort if collapse turned on
                if (collapseActive)
                {
                    if (resultsGrid.DataSource is DataTable dt)
                    {
                        dt.DefaultView.Sort = string.Empty;
                    }
                    if (resultsGrid.SortedColumn != null)
                    {
                        resultsGrid.SortedColumn.HeaderCell.SortGlyphDirection = System.Windows.Forms.SortOrder.None;
                    }
                }

                foreach (DataGridViewColumn col in resultsGrid.Columns)
                {
                    col.SortMode = collapseActive ? DataGridViewColumnSortMode.NotSortable : DataGridViewColumnSortMode.Automatic;
                }

                // If collapse is off, try to reapply saved sort
                if (!collapseActive)
                {
                    TryApplySavedSort();
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to update grid sortability state");
            }
        }

        private void TryApplySavedSort()
        {
            try
            {
                if (collapseProductsCheckBox?.Checked == true) return;
                if (_pendingSort == null || resultsGrid.Columns.Count == 0) return;
                var (name, dir) = _pendingSort.Value;
                var col = resultsGrid.Columns.Cast<DataGridViewColumn>().FirstOrDefault(c => c.HeaderText == name);
                if (col != null)
                {
                    col.SortMode = DataGridViewColumnSortMode.Automatic;
                    resultsGrid.Sort(col, dir);
                    _pendingSort = null; // applied
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to apply saved sort");
            }
        }

        private void TryApplySavedColumnFillWeights()
        {
            try
            {
                if (_pendingFillWeights == null || resultsGrid.Columns.Count == 0) return;
                foreach (DataGridViewColumn col in resultsGrid.Columns)
                {
                    if (_pendingFillWeights.TryGetValue(col.HeaderText, out var weight))
                    {
                        if (weight > 0)
                        {
                            col.FillWeight = weight;
                        }
                    }
                }
                _pendingFillWeights = null; // applied
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to apply saved column fill weights");
            }
        }

        private void UpdateStatus(string message) => statusLabel.Text = message;

        private void ResultsGrid_ColumnHeaderMouseClick(object? sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                var column = resultsGrid.Columns[e.ColumnIndex];
                using var menu = CreateColumnContextMenu(column);
                var location = resultsGrid.PointToScreen(new Point(e.X, e.Y));
                menu.Show(location);
            }
        }
        #endregion

        private async void BuildButton_Click(object? sender, EventArgs e)
        {
            var (sql, parameters) = BuildSqlQuery();
            sqlParametersList  = parameters;
            _logger.LogDebug("Executing generated query: {SqlPreview}", sql.Length > 200 ? sql[..200] + "..." : sql);
            sqlLabel.Text = sql; // still hidden, for diagnostics
            await ExecuteSqlAsync(sql, parameters, CancellationToken.None);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            SaveUserState();
            base.OnFormClosing(e);
        }

        private List<string> GetCurrentGridColumnOrder()
        {
            try
            {
                if (resultsGrid.Columns.Count == 0)
                    return columnsCheckedListBox.CheckedItems.Cast<string>().ToList();
                return resultsGrid.Columns
                    .Cast<DataGridViewColumn>()
                    .OrderBy(c => c.DisplayIndex)
                    .Select(c => c.HeaderText)
                    .ToList();
            }
            catch { return new(); }
        }

        private void TryApplyColumnOrder()
        {
            if (_pendingColumnOrder == null || resultsGrid.Columns.Count == 0) return;
            try
            {
                var map = resultsGrid.Columns.Cast<DataGridViewColumn>().ToDictionary(c => c.HeaderText, c => c);
                int displayIndex = 0;
                foreach (var name in _pendingColumnOrder)
                {
                    if (map.TryGetValue(name, out var col))
                    {
                        col.DisplayIndex = displayIndex++;
                    }
                }
                _pendingColumnOrder = null; // applied
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to apply saved column order");
            }
        }

        private void RadioButton1_CheckedChanged(object sender, EventArgs e)
        {
            topNumericUpDown.Enabled = radioButtonTop.Checked;
        }

        List<SqlParameter> sqlParametersList  = new List<SqlParameter>();
        private async void Button1_Click(object sender, EventArgs e)
        {
            await ExecuteSqlAsync(sqlLabel.Text, sqlParametersList, CancellationToken.None);

        }
    }
}