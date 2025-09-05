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
        private readonly List<DataGridViewColumn> _hiddenColumns = new();

        public SqlQueryForm(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = _serviceProvider.GetRequiredService<ILogger<SqlQueryForm>>();
            _dbContext = _serviceProvider.GetRequiredService<SacksDbContext>();
            
            InitializeComponent();
            SetupControls();
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
            try
            {
                SetExecutionState(true);
                _logger.LogInformation("Executing SQL query: {Sql}", sql.Substring(0, Math.Min(sql.Length, 100)));
                
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                
                var connection = _dbContext.Database.GetDbConnection();
                using var command = connection.CreateCommand();
#pragma warning disable CA2100 // Review SQL queries for security vulnerabilities
                command.CommandText = sql; // This is intentional user input for SQL query tool
#pragma warning restore CA2100
                command.CommandTimeout = 30; // 30 second timeout
                
                if (connection.State != ConnectionState.Open)
                {
                    await connection.OpenAsync(cancellationToken);
                }

                using var reader = await command.ExecuteReaderAsync(cancellationToken);
                
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
                
                UpdateStatus($"? Query executed successfully - {rowCount:N0} rows, {columnCount} columns ({executionTime}ms)");
                
                _logger.LogInformation("SQL query executed successfully: {RowCount} rows, {ColumnCount} columns, {ExecutionTime}ms", 
                    rowCount, columnCount, executionTime);
            }
            catch (SqlException sqlEx)
            {
                _logger.LogError(sqlEx, "SQL error executing query");
                var errorMsg = $"SQL Error (Line {sqlEx.LineNumber}): {sqlEx.Message}";
                UpdateStatus($"? SQL Error: {sqlEx.Message}");
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
            _hiddenColumns.Add(column);
            column.Visible = false;
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
                    var item = new ToolStripMenuItem(column.HeaderText)
                    {
                        Checked = column.Visible,
                        CheckOnClick = true
                    };
                    item.Click += (s, e) => ToggleColumnVisibility(column);
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
            exportButton.Enabled = !executing && _currentDataTable != null;
            
            if (executing)
            {
                progressBar.Visible = true;
                progressBar.Style = ProgressBarStyle.Marquee;
                UpdateStatus("?? Executing query...");
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
            foreach (var column in _hiddenColumns.ToList())
            {
                column.Visible = true;
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
        
        private void ToggleColumnVisibility(DataGridViewColumn column)
        {
            column.Visible = !column.Visible;
            if (column.Visible)
            {
                _hiddenColumns.Remove(column);
            }
            else if (!_hiddenColumns.Contains(column))
            {
                _hiddenColumns.Add(column);
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