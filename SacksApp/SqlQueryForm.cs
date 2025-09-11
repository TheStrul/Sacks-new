using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SacksDataLayer.Data;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Text;
using System.ComponentModel;
using Microsoft.EntityFrameworkCore;

namespace SacksApp
{
    public partial class SqlQueryForm : Form
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<SqlQueryForm> _logger;
        private readonly SacksDbContext _dbContext;
    private DataTable? _currentDataTable;
    // Store hidden columns by a stable key (Name if set, otherwise HeaderText)
    private readonly List<string> _hiddenColumns = new();

        public SqlQueryForm(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = _serviceProvider.GetRequiredService<ILogger<SqlQueryForm>>();
            _dbContext = _serviceProvider.GetRequiredService<SacksDbContext>();
            
            InitializeComponent();
            SetupControls();
            WireQueryBuilder();
        }

        private void WireQueryBuilder()
        {
            try
            {
                // Populate tables list from DbContext model
                var tables = _dbContext.Model.GetEntityTypes()
                    .Select(et => et.GetTableName())
                    .Where(n => !string.IsNullOrEmpty(n))
                    .Distinct()
                    .OrderBy(n => n)
                    .Select(n => n!) // tell compiler non-null
                    .ToArray();

                tableComboBox.Items.AddRange((object[])tables);
                if (tableComboBox.Items.Count > 0) tableComboBox.SelectedIndex = 0;

                // Populate simple operators and defaults
                filterOperatorComboBox.Items.AddRange(new object[] { "=", "<>", ">", "<", ">=", "<=", "LIKE" });
                filterOperatorComboBox.SelectedIndex = 0;
                orderByDirectionComboBox.SelectedIndex = 0; // default ASC

                // Wire events
                tableComboBox.SelectedIndexChanged += (s, e) => LoadColumnsForSelectedTable();
                if (tableComboBox.Items.Count > 0) LoadColumnsForSelectedTable();
                addFilterButton.Click += (s, e) => AddFilter();
                removeFilterButton.Click += (s, e) => RemoveSelectedFilter();
                buildButton.Click += async (s, e) =>
                {
                    var built = BuildSelectQuery();
                    if (!string.IsNullOrEmpty(built))
                    {
                        sqlTextBox.Text = built;
                        await ExecuteQueryAsync(built, CancellationToken.None);
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not initialize query builder UI");
            }
        }

        private void LoadColumnsForSelectedTable()
        {
            columnsCheckedListBox.Items.Clear();
            filterColumnComboBox.Items.Clear();
            orderByComboBox.Items.Clear();

            var table = tableComboBox.SelectedItem as string;
            if (string.IsNullOrEmpty(table)) return;

            // Get column names for the entity/table from EF model when possible
            var columns = _dbContext.Model.GetEntityTypes()
                .Where(et => et.GetTableName() == table)
                .SelectMany(et => et.GetProperties().Select(p => p.GetColumnName()))
                .Where(c => !string.IsNullOrEmpty(c))
                .Distinct()
                .OrderBy(c => c)
                .ToList();

            if (columns.Count == 0)
            {
                // fallback: run a lightweight query to fetch zero rows and use reader schema
                try
                {
                    var conn = _dbContext.Database.GetDbConnection();
                    using var cmd = conn.CreateCommand();
                    // Use safe identifier escape and minimal query to get schema
                    if (!IsSafeIdentifier(table)) throw new InvalidOperationException("Invalid table name");
                    // CommandText is built from validated identifier only (safe). Suppress CA2100 analyzer here.
#pragma warning disable CA2100 // Review SQL queries for security vulnerabilities
                    cmd.CommandText = $"SELECT TOP (0) * FROM {EscapeIdentifier(table)}";
#pragma warning restore CA2100
                    if (conn.State != ConnectionState.Open) conn.Open();
                    using var reader = cmd.ExecuteReader(CommandBehavior.SchemaOnly);
                    var schema = reader.GetSchemaTable();
                    if (schema != null)
                    {
                        foreach (DataRow row in schema.Rows)
                        {
                            var colName = row["ColumnName"]?.ToString();
                            if (!string.IsNullOrEmpty(colName)) columns.Add(colName);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to fetch columns by schema query for table {Table}", table);
                }
            }

            columnsCheckedListBox.Items.AddRange(columns.ToArray());
            filterColumnComboBox.Items.AddRange(columns.ToArray());
            orderByComboBox.Items.AddRange(columns.ToArray());
        }

        private void AddFilter()
        {
            var col = filterColumnComboBox.SelectedItem as string;
            var op = filterOperatorComboBox.SelectedItem as string ?? "=";
            var val = filterValueTextBox.Text ?? string.Empty;
            if (string.IsNullOrEmpty(col) || string.IsNullOrEmpty(val)) return;

            // show simple representation
            filtersListBox.Items.Add($"{col} {op} {val}");
            filterValueTextBox.Clear();
        }

        private void RemoveSelectedFilter()
        {
            var idx = filtersListBox.SelectedIndex;
            if (idx >= 0) filtersListBox.Items.RemoveAt(idx);
        }

        private string BuildSelectQuery()
        {
            var table = tableComboBox.SelectedItem as string;
            if (string.IsNullOrEmpty(table)) return string.Empty;

            // Validate identifier characters (simple allowlist)
            if (!IsSafeIdentifier(table)) return string.Empty;

            var selectedColumns = columnsCheckedListBox.CheckedItems.Cast<string>().ToList();
            var columnList = selectedColumns.Count > 0 ? string.Join(", ", selectedColumns.Select(EscapeIdentifier)) : "*";

            var sb = new StringBuilder();
            var top = (int)topNumericUpDown.Value;
            if (top > 0) sb.Append($"SELECT TOP ({top}) ");
            else sb.Append("SELECT ");

            sb.Append(columnList);
            sb.Append(" FROM ");
            sb.Append(EscapeIdentifier(table));

            // WHERE
            if (filtersListBox.Items.Count > 0)
            {
                var where = new List<string>();
                foreach (var item in filtersListBox.Items.Cast<string>())
                {
                    // item format: "col op value"
                    var parts = item.Split(new[] { ' ' }, 3);
                    if (parts.Length < 3) continue;
                    var col = parts[0];
                    var op = parts[1];
                    var val = parts[2];

                    if (!IsSafeIdentifier(col)) continue;
                    // For simplicity, quote the value as literal (not parameterized) but escape single quotes
                    var safeVal = val.Replace("'", "''");
                    if (op.Equals("LIKE", StringComparison.OrdinalIgnoreCase))
                        where.Add($"{EscapeIdentifier(col)} LIKE '{safeVal}'");
                    else
                        where.Add($"{EscapeIdentifier(col)} {op} '{safeVal}'");
                }

                if (where.Count > 0)
                {
                    sb.Append(" WHERE ");
                    sb.Append(string.Join(" AND ", where));
                }
            }

            // ORDER BY
            if (orderByComboBox.SelectedItem is string ob && !string.IsNullOrEmpty(ob) && IsSafeIdentifier(ob))
            {
                var dir = orderByDirectionComboBox.SelectedItem as string ?? "ASC";
                sb.Append($" ORDER BY {EscapeIdentifier(ob)} {dir}");
            }

            return sb.ToString();
        }

        private static string EscapeIdentifier(string ident)
        {
            // Enclose in brackets and escape closing bracket
            return $"[{ident.Replace("]", "]]")}]";
        }

        private static bool IsSafeIdentifier(string ident)
        {
            // Simple check: allow letters, numbers, underscore and dot (schema.table)
            if (string.IsNullOrEmpty(ident)) return false;
            foreach (var ch in ident)
            {
                if (!(char.IsLetterOrDigit(ch) || ch == '_' || ch == '.' || ch == ']')) return false;
            }
            return true;
        }

        private void SetupControls()
        {
            // Setup SQL TextBox with syntax highlighting-like appearance
            sqlTextBox.Font = new Font("Consolas", 10);
            sqlTextBox.AcceptsTab = true;
            sqlTextBox.WordWrap = false;
            sqlTextBox.ScrollBars = ScrollBars.Both;
            
            // Setup DataGridView
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
            
            // Enable sorting
            resultsGrid.ColumnHeaderMouseClick += ResultsGrid_ColumnHeaderMouseClick;
            
            // Add sample queries
            LoadSampleQueries();
            
            // Set initial status
            UpdateStatus("Ready - Enter SQL query and click Execute");
        }

        private void LoadSampleQueries()
        {
            var samples = new[]
            {
                "-- Sample Queries",
                "SELECT TOP 100 * FROM Products",
                "SELECT TOP 100 * FROM Suppliers", 
                "SELECT TOP 100 * FROM SupplierOffers",
                "SELECT TOP 100 * FROM OfferProducts",
                "",
                "-- Products with Offers View",
                "SELECT * FROM vw_ProductsWithOffers",
                "",
                "-- Count by Supplier",
                "SELECT s.Name, COUNT(*) as ProductCount",
                "FROM Suppliers s",
                "INNER JOIN SupplierOffers so ON s.Id = so.SupplierId", 
                "INNER JOIN OfferProducts op ON so.Id = op.OfferId",
                "GROUP BY s.Name",
                "ORDER BY ProductCount DESC"
            };
            
            sqlTextBox.Text = string.Join(Environment.NewLine, samples);
        }

        private async void ExecuteButton_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(sqlTextBox.Text))
            {
                MessageBox.Show("Please enter a SQL query.", "No Query", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            await ExecuteQueryAsync(sqlTextBox.Text, CancellationToken.None);
        }

        private async Task ExecuteQueryAsync(string sql, CancellationToken cancellationToken)
        {
            System.Data.Common.DbConnection? connection = null;
            try
            {
                SetExecutionState(true);
                _logger.LogDebug("Executing SQL query: {Sql}", sql.Substring(0, Math.Min(sql.Length, 100)));
                
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                
#pragma warning disable CA2000 // Review SQL queries for security vulnerabilities
                connection = _dbContext.Database.GetDbConnection();
                using var command = connection.CreateCommand();
#pragma warning restore CA2000
#pragma warning disable CA2100 // Review SQL queries for security vulnerabilities
                command.CommandText = sql; // This is intentional user input for SQL query tool
#pragma warning restore CA2100
                command.CommandTimeout = 30; // 30 second timeout
                
                if (connection.State != ConnectionState.Open)
                {
                    await connection.OpenAsync(cancellationToken);
                }

                // Use IAsyncDisposable reader when available and load into DataTable
                await using var reader = await command.ExecuteReaderAsync(cancellationToken);
                var dataTable = new DataTable();
                dataTable.Load(reader);
                _currentDataTable = dataTable;
                
                stopwatch.Stop();
                
                // Update UI on main thread
                resultsGrid.DataSource = dataTable;
                
                // Setup column features
                SetupColumnFeatures();
                
                var rowCount = dataTable.Rows.Count;
                var columnCount = dataTable.Columns.Count;
                var executionTime = stopwatch.ElapsedMilliseconds;
                
                UpdateStatus($"Query executed successfully - {rowCount:N0} rows, {columnCount} columns ({executionTime}ms)");
                
                _logger.LogDebug("SQL query executed successfully: {RowCount} rows, {ColumnCount} columns, {ExecutionTime}ms", 
                    rowCount, columnCount, executionTime);
            }
            catch (SqlException sqlEx)
            {
                _logger.LogError(sqlEx, "SQL error executing query");
                var errorMsg = $"SQL Error (Line {sqlEx.LineNumber}): {sqlEx.Message}";
                UpdateStatus($"SQL Error: {sqlEx.Message}");
                MessageBox.Show(errorMsg, "SQL Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing SQL query");
                UpdateStatus($"? Error: {ex.Message}");
                MessageBox.Show($"Error executing query: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                // Ensure connection is closed if we opened it
                try
                {
                    if (connection?.State == ConnectionState.Open)
                    {
                        await connection.CloseAsync();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error closing DB connection after query");
                }

                SetExecutionState(false);
            }
        }

        private void SetupColumnFeatures()
        {
            if (_currentDataTable == null) return;

            foreach (DataGridViewColumn column in resultsGrid.Columns)
            {
                // Enable sorting for all columns
                column.SortMode = DataGridViewColumnSortMode.Automatic;
                
                // Add context menu for column operations
                column.HeaderCell.ContextMenuStrip = CreateColumnContextMenu(column);
            }
        }

        private ContextMenuStrip CreateColumnContextMenu(DataGridViewColumn column)
        {
#pragma warning disable CA2000 // Dispose objects before losing scope
            var menu = new ContextMenuStrip(); // Menu is disposed automatically when used
#pragma warning restore CA2000
            
            var hideItem = new ToolStripMenuItem($"Hide '{column.HeaderText}'");
            hideItem.Click += (s, e) => HideColumn(column);
            menu.Items.Add(hideItem);
            
            var sortAscItem = new ToolStripMenuItem("Sort Ascending");
            sortAscItem.Click += (s, e) => SortColumn(column, ListSortDirection.Ascending);
            menu.Items.Add(sortAscItem);
            
            var sortDescItem = new ToolStripMenuItem("Sort Descending");
            sortDescItem.Click += (s, e) => SortColumn(column, ListSortDirection.Descending);
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
        {
            return !string.IsNullOrEmpty(column.Name) ? column.Name : column.HeaderText ?? string.Empty;
        }

        private void SortColumn(DataGridViewColumn column, ListSortDirection direction)
        {
            resultsGrid.Sort(column, direction);
        }

        private void ColumnsButton_Click(object sender, EventArgs e)
        {
            // Create and show context menu for column visibility
            if (_currentDataTable != null)
            {
#pragma warning disable CA2000 // Dispose objects before losing scope
                var menu = new ContextMenuStrip(); // Menu is disposed automatically when shown
#pragma warning restore CA2000
                
                foreach (DataGridViewColumn column in resultsGrid.Columns)
                {
                    var col = column; // local copy for closure
                    var colKey = GetColumnKey(col);
                    var item = new ToolStripMenuItem(col.HeaderText)
                    {
                        Checked = col.Visible,
                        CheckOnClick = true
                    };
                    item.Click += (s, e) => ToggleColumnVisibility(colKey);
                    menu.Items.Add(item);
                }
                
                if (_hiddenColumns.Count > 0)
                {
                    menu.Items.Add(new ToolStripSeparator());
                    var showAllItem = new ToolStripMenuItem("Show All Columns");
                    showAllItem.Click += (s, e) => ShowAllColumns();
                    menu.Items.Add(showAllItem);
                }
                
                // Show menu below the button
                var buttonLocation = columnsButton.PointToScreen(Point.Empty);
                menu.Show(buttonLocation.X, buttonLocation.Y + columnsButton.Height);
            }
        }

        private void SetExecutionState(bool executing)
        {
            executeButton.Enabled = !executing;
            clearButton.Enabled = !executing;
            exportButton.Enabled = !executing && _currentDataTable != null && _currentDataTable.Rows.Count > 0;
            
            if (executing)
            {
                progressBar.Visible = true;
                progressBar.Style = ProgressBarStyle.Marquee;
                UpdateStatus("Executing query...");
            }
            else
            {
                progressBar.Visible = false;
            }
        }

        private void UpdateStatus(string message)
        {
            statusLabel.Text = message;
            // ToolStripStatusLabel doesn't have Refresh method, form will handle updates
        }

        private void ShowAllColumns()
        {
            foreach (var key in _hiddenColumns.ToList())
            {
                var col = resultsGrid.Columns.Cast<DataGridViewColumn>().FirstOrDefault(c => GetColumnKey(c) == key);
                if (col != null) col.Visible = true;
            }
            _hiddenColumns.Clear();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                components?.Dispose();
                _currentDataTable?.Dispose();
            }
            base.Dispose(disposing);
        }
        
        private void ToggleColumnVisibility(string columnKey)
        {
            var column = resultsGrid.Columns.Cast<DataGridViewColumn>().FirstOrDefault(c => GetColumnKey(c) == columnKey);
            if (column == null) return;

            column.Visible = !column.Visible;
            if (column.Visible)
            {
                _hiddenColumns.Remove(columnKey);
            }
            else if (!_hiddenColumns.Contains(columnKey))
            {
                _hiddenColumns.Add(columnKey);
            }
        }

        private void ResultsGrid_ColumnHeaderMouseClick(object? sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                var column = resultsGrid.Columns[e.ColumnIndex];
                using var menu = CreateColumnContextMenu(column); // Properly dispose the menu
                var location = resultsGrid.PointToScreen(new Point(e.X, e.Y));
                menu.Show(location);
            }
        }

        private void ClearButton_Click(object sender, EventArgs e)
        {
            sqlTextBox.Clear();
            resultsGrid.DataSource = null;
            _currentDataTable = null;
            _hiddenColumns.Clear();
            UpdateStatus("Query cleared");
        }

        private void ExportButton_Click(object sender, EventArgs e)
        {
            if (_currentDataTable == null || _currentDataTable.Rows.Count == 0)
            {
                MessageBox.Show("No data to export.", "No Data", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var saveDialog = new SaveFileDialog
            {
                Filter = "CSV files (*.csv)|*.csv|Excel files (*.xlsx)|*.xlsx|All files (*.*)|*.*",
                DefaultExt = "csv",
                AddExtension = true
            };

            if (saveDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    ExportToCsv(saveDialog.FileName);
                    MessageBox.Show($"Data exported successfully to:\n{saveDialog.FileName}", 
                        "Export Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error exporting data: {ex.Message}", "Export Error", 
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void ExportToCsv(string fileName)
        {
            if (_currentDataTable == null) return;

            var csv = new StringBuilder();
            
            // Add headers for visible columns only
            var visibleColumns = resultsGrid.Columns.Cast<DataGridViewColumn>()
                .Where(c => c.Visible)
                .OrderBy(c => c.DisplayIndex)
                .ToList();
            
            csv.AppendLine(string.Join(",", visibleColumns.Select(c => $"\"{c.HeaderText}\"")));
            
            // Add data rows
            foreach (DataRow row in _currentDataTable.Rows)
            {
                var values = visibleColumns.Select(c => 
                {
                    var value = row[c.DataPropertyName]?.ToString() ?? "";
                    return $"\"{value.Replace("\"", "\"\"")}\""; // Escape quotes
                });
                csv.AppendLine(string.Join(",", values));
            }
            
            File.WriteAllText(fileName, csv.ToString(), Encoding.UTF8);
        }
    }
}