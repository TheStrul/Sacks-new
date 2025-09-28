using System.Data;
using System.Globalization;
using System.Runtime.Intrinsics.Arm;
using System.Text;

using Microsoft.Data.SqlClient; // Added for parameterized SQL execution
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using SacksDataLayer.Data;
using SacksDataLayer.Entities;
using SacksDataLayer.FileProcessing.Configuration;

using SacksLogicLayer.Services;
using SacksLogicLayer.Services.Interfaces;

namespace SacksApp
{
    public partial class SqlQueryForm : Form
    {
        private readonly IServiceProvider? _serviceProvider;
        private readonly ILogger<SqlQueryForm> _logger;
        private readonly SacksDbContext _dbContext;
        private readonly List<string> _hiddenColumns = new();
        private readonly List<FilterCondition> _filters = new();
        private List<string> _availableColumns = new();
        private ISupplierConfigurationService? svc;
        private ISuppliersConfiguration? _suppliersConfiguration;

        private List<SqlParameter> sqlParametersList = new List<SqlParameter>();

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
            _logger = _serviceProvider!.GetRequiredService<ILogger<SqlQueryForm>>();
            _dbContext = _serviceProvider!.GetRequiredService<SacksDbContext>();

            // Try to eagerly load supplier configurations so lookup lists are available in the UI
            try
            {
                svc = _serviceProvider?.GetService<SacksLogicLayer.Services.Interfaces.ISupplierConfigurationService>();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load supplier configurations during form initialization");
                throw;
            }

           

            InitializeComponent();
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
                RefreshOperatorList();

                // No persisted UI state load — session state persistence has been removed
                // Load persisted state after controls are populated
                // LoadUserState();

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
        private static string EscapeIdentifier(string name) => "[" + name.Replace("]", "]]") + "]";

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
        {
            return string.Join(", ", columns.Select(EscapeIdentifier));
        }

        // Simple query from base view
        private string BuildSimpleQuery(List<string> selectedColumns, string where)
        {
            var sb = new StringBuilder();
            var cols = BuildSelectList(selectedColumns);
            sb.Append($"SELECT {cols} FROM [ProductOffersView]");
            sb.Append(where);
            var orderCols = new List<string>();
            if (selectedColumns.Contains("EAN", StringComparer.OrdinalIgnoreCase)) orderCols.Add("[EAN]");
            if (selectedColumns.Contains("OfferRank", StringComparer.OrdinalIgnoreCase)) orderCols.Add("[OfferRank]");
            else if (selectedColumns.Contains("Price", StringComparer.OrdinalIgnoreCase)) orderCols.Add("[Price]");
            if (orderCols.Count > 0) sb.Append(" ORDER BY " + string.Join(", ", orderCols));
            return sb.ToString();
        }

        // Collapsed query using dedicated collapsed view
        private string BuildCollapsedUsingView(List<string> selectedColumns, string where)
        {
            var sb = new StringBuilder();
            var cols = BuildSelectList(selectedColumns);
            sb.Append($"SELECT {cols} FROM [ProductOffersViewCollapse] AS c");
            sb.Append(where);
            sb.Append(" ORDER BY [EANKey], [OfferRank]");
            return sb.ToString();
        }

        // Dynamic collapsed query: filter base view then recompute collapse using ROW_NUMBER
        private string BuildDynamicCollapsed(List<string> selectedColumns, string innerWhere)
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
            sb.Append("SELECT ");
            sb.Append(string.Join(", ", projected));
            sb.Append(" FROM (");
            sb.Append(inner);
            sb.Append(") AS x WHERE x.[cnt] > 1 ORDER BY x.[EAN], x.[OfferRank]");
            return sb.ToString();
        }
        #endregion



        private void ResultsGrid_CellMouseUp(object sender, DataGridViewCellMouseEventArgs e)
        {

        }

        #region Existing UI Logic (Moved to partial)

        // Track current editing TextBox to observe selection changes
        private TextBox? _currentEditingTextBox;
        private KeyEventHandler? _editingKeyUpHandler;
        private MouseEventHandler? _editingMouseUpHandler;
        // Shared empty context menu used to suppress default TextBox context menu
        private static readonly ContextMenuStrip s_emptyContextMenu = new ContextMenuStrip();

        private void ResultsGrid_EditingControlShowing(object? sender, DataGridViewEditingControlShowingEventArgs e)
        {
            try
            {
                // Detach from previous
                if (_currentEditingTextBox != null)
                {
                    if (_editingKeyUpHandler != null) _currentEditingTextBox.KeyUp -= _editingKeyUpHandler;
                    if (_editingMouseUpHandler != null) _currentEditingTextBox.MouseUp -= _editingMouseUpHandler;
                    _editingKeyUpHandler = null;
                    _editingMouseUpHandler = null;
                    _currentEditingTextBox = null;
                }

                if (e.Control is TextBox tb)
                {
                    _currentEditingTextBox = tb;
                    // Suppress default TextBox context menu by assigning an empty ContextMenuStrip
                    try { tb.ContextMenuStrip = s_emptyContextMenu; } catch { }
                    // create handlers and attach so we can detach later
                    _editingKeyUpHandler = new KeyEventHandler((s, ev) => OnEditingSelectionChanged(s));
                    _editingMouseUpHandler = new MouseEventHandler((s, ev) => OnEditingSelectionChangedMousup(s, ev));
                    _currentEditingTextBox.KeyUp += _editingKeyUpHandler;
                    _currentEditingTextBox.MouseUp += _editingMouseUpHandler;
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed during EditingControlShowing");
            }
        }

        private void ResultsGrid_CellEndEdit(object? sender, DataGridViewCellEventArgs e)
        {
            try
            {
                if (_currentEditingTextBox != null)
                {
                    if (_editingKeyUpHandler != null) _currentEditingTextBox.KeyUp -= _editingKeyUpHandler;
                    if (_editingMouseUpHandler != null) _currentEditingTextBox.MouseUp -= _editingMouseUpHandler;
                    _editingKeyUpHandler = null;
                    _editingMouseUpHandler = null;
                    _currentEditingTextBox = null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed during CellEndEdit");
            }
        }

        private void OnEditingSelectionChangedMousup(object? sender, MouseEventArgs e)
        {
            if ((e.Button == MouseButtons.Right) &&
                (resultsGrid.CurrentCell != null) &&
                (e.Clicks == 1))
            {
                int r = resultsGrid.CurrentCell.RowIndex;
                int c = resultsGrid.CurrentCell.ColumnIndex;
                if (r >= 0 && c >= 0)
                {
                    var rect = resultsGrid.GetCellDisplayRectangle(c, r, true); // true to include header offset
                    Point buttomLeft = new Point(rect.X, rect.Y + rect.Height);
                    Point cellButtomLeft = resultsGrid.PointToScreen(buttomLeft);
                    contextMenuStrip1.Show(cellButtomLeft);
                }
            }
        }
        private void OnEditingSelectionChanged(object? sender)
        {
            try
            {
                var tb = sender as TextBox ?? _currentEditingTextBox;
                var selected = tb?.SelectedText ?? string.Empty;
                UpdateStatus($"Selection length: {selected.Length}");
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to handle selection change");
            }
        }

        private void UpdateGridSortability()
        {
            try
            {
                bool collapseActive = collapseProductsCheckBox?.Checked == true;

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
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to update grid sortability state");
            }
        }

        private void UpdateStatus(string message) => statusLabel.Text = message;



        #endregion

        // Designer event handlers required by Designer.cs
        private async void RunQueryButton_Click(object? sender, EventArgs e)
        {
            var selectedColumns = columnsCheckedListBox.CheckedItems.Cast<string>().ToList();
            if (selectedColumns.Count == 0) selectedColumns = _availableColumns.ToList();

            var parameters = new List<SqlParameter>();
            string sql;
            bool collapse = collapseProductsCheckBox?.Checked == true;

            if (!collapse)
            {
                var where = BuildBaseWhere(parameters);
                sql = BuildSimpleQuery(selectedColumns, where);
            }
            else
            {

                // collapse mode
                bool hasOfferFilters = _filters.Any(f => OfferColumns.Contains(f.PropertyName));
                if (!hasOfferFilters)
                {
                    var where = BuildCollapsedViewWhere(parameters);
                    sql = BuildCollapsedUsingView(selectedColumns, where);
                }
                else
                {
                    var innerWhere = BuildDynamicInnerWhere(parameters);
                    sql = BuildDynamicCollapsed(selectedColumns, innerWhere);
                }
            }
            sqlParametersList = parameters;
            _logger.LogDebug("Executing generated query: {SqlPreview}", sql.Length > 200 ? sql[..200] + "..." : sql);
            sqlLabel.Text = sql;
            await ExecuteSqlAsync(sql, parameters, CancellationToken.None);
        }

        private void AddFilterButton_Click(object? sender, EventArgs e) => AddFilter();

        private void RemoveFilterButton_Click(object? sender, EventArgs e) => RemoveSelectedFilter();

        private void FilterColumnComboBox_SelectedIndexChanged(object? sender, EventArgs e) => RefreshOperatorList();

        private void CollapseProductsCheckBox_CheckedChanged(object? sender, EventArgs e) => UpdateGridSortability();

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

                    // Ensure columns have Name set to stable identifier before applying state
                    foreach (DataGridViewColumn c in resultsGrid.Columns)
                    {
                        if (string.IsNullOrEmpty(c.Name)) c.Name = c.HeaderText ?? string.Empty;
                    }

                    // Apply visibility based on checked list
                    TryApplyColumnVisibility();
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
            runQueryButton.Enabled = !executing;
            addFilterButton.Enabled = !executing;
            removeFilterButton.Enabled = !executing;
            columnsCheckedListBox.Enabled = !executing;
        }
        private void TryApplyColumnVisibility()
        {
            try
            {
                if (resultsGrid.Columns.Count == 0) return;
                var checkedSet = new HashSet<string>(columnsCheckedListBox.CheckedItems.Cast<string>(), StringComparer.OrdinalIgnoreCase);
                foreach (DataGridViewColumn col in resultsGrid.Columns)
                {
                    // Match by HeaderText
                    var name = col.HeaderText ?? string.Empty;
                    col.Visible = checkedSet.Contains(name);
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to apply column visibility from checked list");
            }
        }

        private async void AddToLookupToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                if (svc != null)
                {
                    // Synchronously block here - configuration load is expected to be small and fast
                    _suppliersConfiguration = await svc.GetAllConfigurationsAsync();
                }

                // Prefer selection from the active editing TextBox (only selected substring will be added)
                string? selectedText = null;

                if (_currentEditingTextBox != null)
                {
                    selectedText = _currentEditingTextBox.SelectedText;
                }
                else if (resultsGrid.IsCurrentCellInEditMode && resultsGrid.EditingControl is TextBox editTb)
                {
                    selectedText = editTb.SelectedText;
                }

                selectedText = selectedText?.Trim();

                if (string.IsNullOrEmpty(selectedText))
                {
                    MessageBox.Show("Please select the text inside the cell (start editing the cell and select a substring) before using 'Add to lookup'.", "Add to lookup", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                const string lookupName = "Brand";

                // Check existing in-memory
                bool exists = false;
                if (_suppliersConfiguration != null && _suppliersConfiguration.Lookups != null && _suppliersConfiguration.Lookups.TryGetValue(lookupName, out var inner))
                {
                    if (inner != null && inner.ContainsKey(selectedText)) exists = true;
                }

                if (exists)
                {
                    MessageBox.Show($"Value '{selectedText}' already exists in lookup '{lookupName}'.", "Add to lookup", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }


                string key = selectedText.ToLowerInvariant();
                string value = ChangeToCapital(selectedText);
                await AddValueToLookupAsync(lookupName, key, value);
                MessageBox.Show($"Added '{selectedText}' to lookup '{lookupName}'.", "Lookup Updated", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding selected value to Brand lookup");
                MessageBox.Show($"Failed to add value to lookup: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Persist a single value into the specified lookup table inside supplier-formats.json
        private async Task AddValueToLookupAsync(string lookupName, string key,string value)
        {

            if (!_suppliersConfiguration!.Lookups.TryGetValue(lookupName, out var inner))
            {
                inner = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                _suppliersConfiguration.Lookups[lookupName] = inner;
            }

            if (inner.ContainsKey(key)) return; // already present

            inner[key] = value; // map to itself

            await _suppliersConfiguration.Save();
        }

        // Convert a raw lookup value to a canonical capitalized form.
        // Examples: "apple juice" -> "Apple Juice"; trims and collapses whitespace.
        private static string ChangeToCapital(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;
            // Normalize whitespace
            var parts = input.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var normalized = string.Join(' ', parts).Trim();
            var ti = CultureInfo.InvariantCulture.TextInfo;
            // ToTitleCase expects lower-case for proper behavior on invariant culture
            return ti.ToTitleCase(normalized.ToLowerInvariant());
        }
    }
}