using System.Data;
using System.Text.Json;
using System.Globalization;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SacksLogicLayer.Services.Interfaces;
using FilterCondition = SacksLogicLayer.Services.Interfaces.FilterCondition;
using FilterOperator = SacksLogicLayer.Services.Interfaces.FilterOperator;
using ClosedXML.Excel; // Excel export

namespace SacksApp
{
    public partial class SqlQueryForm : Form
    {
        private readonly IServiceProvider? _serviceProvider;
        private readonly ILogger<SqlQueryForm> _logger;
        private readonly IProductOffersQueryService _queryService;
        private readonly IOfferProductDataService _dataService;
        private readonly IGridStateManagementService _gridStateService;
        private readonly ISupplierConfigurationService? _supplierConfigService;

        private readonly List<FilterCondition> _filters = new();
        private List<string> _availableColumns = new();
        private List<string> _selectedColumns = new();

        // Track single instance of modeless child form
        private LookupEditorForm? _lookupEditor;

        private static readonly Type ViewType = typeof(Sacks.Core.Entities.ProductOffersView);
        private static readonly string[] EntityPropertyNames = ViewType
            .GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
            .Select(p => p.Name)
            .OrderBy(n => n)
            .ToArray();

        private static readonly HashSet<string> ProductColumns = new(StringComparer.OrdinalIgnoreCase)
        {
            // Core product + product dynamic properties (columns we blank on non-lowest offer)
            "EAN","Name","Category","Brand","Gender","Concentration","Size","Type 1","Type 2","Decoded","COO","Units","Ref"
        };
        private static readonly HashSet<string> OfferColumns = new(StringComparer.OrdinalIgnoreCase)
        {
            // Offer-level & per-offer metrics (always shown)
            "Price","Currency","Quantity","Description", // op level
            "Supplier Name","Offer Name","Date Offer",     // supplier/offer metadata (aliases with spaces)
            "OfferRank","TotalOffers"                       // ranking + count (needed for ordering / context)
        };

        private const string RowNumberHeader = "Row #"; // Always show as first column

        // Editable columns for inline persistence (support a few synonyms found across versions)
        private static readonly HashSet<string> EditableColumns = new(StringComparer.OrdinalIgnoreCase)
        {
            "Price", "Currency", "Quantity", "Description", "Details"
        };

        private bool _editMode;
        private bool _hasUnsavedChanges;

        public SqlQueryForm(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = _serviceProvider!.GetRequiredService<ILogger<SqlQueryForm>>();
            _queryService = _serviceProvider!.GetRequiredService<IProductOffersQueryService>();
            _dataService = _serviceProvider!.GetRequiredService<IOfferProductDataService>();
            _gridStateService = _serviceProvider!.GetRequiredService<IGridStateManagementService>();

            // Try to eagerly load supplier configurations so lookup lists are available in the UI
            try
            {
                _supplierConfigService = _serviceProvider?.GetService<ISupplierConfigurationService>();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load supplier configurations during form initialization");
                throw;
            }

            InitializeComponent();

            // Apply modern theme for controls
            try
            {
                UITheme.ApplyBadgeStyle(addFilterButton, Color.FromArgb(33, 150, 243), ""); // Add
                UITheme.ApplyBadgeStyle(removeFilterButton, Color.FromArgb(244, 67, 54), ""); // Delete
                UITheme.ApplyBadgeStyle(runQueryButton, Color.FromArgb(76, 175, 80), ""); // Play (E768)
                UITheme.ApplyBadgeStyle(buttonShowFilter, Color.FromArgb(156, 39, 176), ""); // Settings
                UITheme.ApplyBadgeStyle(buttonHideFilters, Color.FromArgb(156, 39, 176), ""); // Settings
            }
            catch { }

            // hook form closing to save state and prompt unsaved
            this.FormClosing += SqlQueryForm_FormClosing;

            // Add Export to Excel to context menu (programmatically to avoid designer changes)
            try
            {
                if (contextMenuStrip1 != null)
                {
                    contextMenuStrip1.Items.Add(new ToolStripSeparator());
                    var exportItem = new ToolStripMenuItem("Export to Excel...");
                    exportItem.Click += ExportToExcelToolStripMenuItem_Click;
                    contextMenuStrip1.Items.Add(exportItem);
                }
            }
            catch { }

            InitializeQueryDesignerAsync();

            // initialize edit UI state
            UpdateEditUi();
        }

        #region Setup / Initialization
        private async void InitializeQueryDesignerAsync()
        {
            try
            {
                await LoadAvailableColumnsAsync().ConfigureAwait(true);

                // Try to load previously selected columns; fallback to all available
                var savedColumns = await _gridStateService.LoadSelectedColumnsAsync(CancellationToken.None).ConfigureAwait(true);
                if (savedColumns.Count > 0)
                {
                    _selectedColumns = savedColumns.ToList();
                }
                else
                {
                    // Default selected columns = all available
                    _selectedColumns = _availableColumns.ToList();
                }

                filterColumnComboBox.Items.Clear();
                filterColumnComboBox.Items.AddRange(_availableColumns.ToArray());
                if (filterColumnComboBox.Items.Count > 0) filterColumnComboBox.SelectedIndex = 0;

                filterOperatorComboBox.Items.Clear();
                RefreshOperatorList();

                // Load persisted filters if any
                await LoadFiltersStateAsync().ConfigureAwait(true);

                UpdateStatus("Ready - Design your query and click Run");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not initialize ProductOffersView query designer");
            }
        }

        private async Task LoadAvailableColumnsAsync()
        {
            try
            {
                var columns = await _queryService.GetAvailableColumnsAsync().ConfigureAwait(false);
                _availableColumns = columns.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load available columns from query service");
            }
        }

        private async Task LoadFiltersStateAsync()
        {
            try
            {
                var loadedFilters = await _gridStateService.LoadFiltersStateAsync().ConfigureAwait(false);
                _filters.Clear();
                _filters.AddRange(loadedFilters);

                if (filtersListBox is CheckedListBox clb)
                {
                    clb.Items.Clear();
                    foreach (var filter in _filters)
                    {
                        clb.Items.Add(filter.ToString(), filter.Enabled);
                    }
                }
                else
                {
                    filtersListBox.Items.Clear();
                    foreach (var filter in _filters)
                    {
                        filtersListBox.Items.Add(filter.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load filters state");
            }
        }
        #endregion

        #region Filter Infrastructure
        private void RefreshOperatorList()
        {
            filterOperatorComboBox.Items.Clear();
            if (filterColumnComboBox.SelectedItem is not string col) return;

            var operators = _queryService.GetFilterOperators(col);
            foreach (var op in operators)
            {
                filterOperatorComboBox.Items.Add(op);
            }
            if (filterOperatorComboBox.Items.Count > 0)
                filterOperatorComboBox.SelectedIndex = 0;
        }

        // Helper to convert raw string to typed value
        private static object? ConvertToType(string raw, Type target)
        {
            if (target == typeof(string)) return raw;
            if (target == typeof(int)) return int.Parse(raw, CultureInfo.InvariantCulture);
            if (target == typeof(decimal)) return decimal.Parse(raw, CultureInfo.InvariantCulture);
            if (target == typeof(double)) return double.Parse(raw, CultureInfo.InvariantCulture);
            if (target == typeof(DateTime)) return DateTime.Parse(raw, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal);
            return Convert.ChangeType(raw, target, CultureInfo.InvariantCulture);
        }

        private void AddFilter()
        {
            if (filterColumnComboBox.SelectedItem is not string col) return;
            if (filterOperatorComboBox.SelectedItem is not FilterOperator op) return;

            var type = _queryService.GetColumnType(col);

            string? raw = null;
            // avoid incompatible 'as' pattern between unrelated control types by using object
            var ctl = (object?)filterValueTextBox;
            var tv = ctl as TextBox;
            var clb = ctl as CheckedListBox;
            if (tv != null)
            {
                raw = tv.Text.Trim();
            }
            else if (clb != null)
            {
                raw = clb.SelectedItem?.ToString()?.Trim();
            }

            if (op is FilterOperator.IsEmpty or FilterOperator.IsNotEmpty)
            {
                raw = null; // value not needed
            }
            else if (string.IsNullOrWhiteSpace(raw))
            {
                return; // require value
            }

            // Validate using the service
            if (!_queryService.ValidateFilterValue(col, raw, op))
            {
                MessageBox.Show($"Value '{raw}' is not valid for {col}", "Invalid Value", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var condition = new FilterCondition
            {
                PropertyName = col,
                Operator = op,
                Value = raw,
                PropertyType = type,
                Enabled = true
            };
            _filters.Add(condition);

            // Add to checked list; mark checked by default
            if (filtersListBox is CheckedListBox clbBox)
            {
                clbBox.Items.Add(condition.ToString(), true);
            }
            else
            {
                filtersListBox.Items.Add(condition.ToString());
            }

            if (tv != null) tv.Clear();
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

        // Called when user checks/unchecks an item in the filters checked-list
        private void FiltersListBox_ItemCheck(object? sender, ItemCheckEventArgs e)
        {
            try
            {
                // ItemCheck occurs before the check state changes; NewValue contains desired state
                var idx = e.Index;
                if (idx >= 0 && idx < _filters.Count)
                {
                    _filters[idx].Enabled = e.NewValue == CheckState.Checked;
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed handling filter item check");
            }
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

            // Mark unsaved change (defer persistence to Save Changes)
            try
            {
                if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
                if (!_editMode) return;
                if (groupByProductToolStripMenuItem.Checked) return; // disable editing in grouped mode
                if (resultsGrid.DataSource is not DataTable dt) return;

                var columnHeader = resultsGrid.Columns[e.ColumnIndex].HeaderText ?? string.Empty;
                if (string.Equals(columnHeader, "EAN", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(columnHeader, RowNumberHeader, StringComparison.OrdinalIgnoreCase))
                {
                    return; // not considered editable
                }

                _hasUnsavedChanges = true;
                UpdateEditUi();
                UpdateStatus("Edited cell; pending save");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed after editing cell");
                MessageBox.Show($"Failed to process edit: {ex.Message}", "Edit Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                bool collapseActive = groupByProductToolStripMenuItem.Checked == true;

                // Reduce flicker during mass updates
                try
                {
                    typeof(DataGridView).InvokeMember("DoubleBuffered", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.SetProperty, null, resultsGrid, new object[] { true });
                }
                catch { }

                if (collapseActive)
                {
                    // Keep sorting enabled, but reset any existing sort on the data view
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
                    // Always allow automatic sorting. The SQL query will handle the grouping.
                    col.SortMode = DataGridViewColumnSortMode.Automatic;
                }

                // Disable editing in grouped mode to avoid ambiguous updates
                resultsGrid.ReadOnly = collapseActive || !_editMode;

                // Apply colorful header and selection when grid is available
                resultsGrid.EnableHeadersVisualStyles = false;
                resultsGrid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(55, 71, 79);
                resultsGrid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
                resultsGrid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(245, 248, 255);
                resultsGrid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(3, 169, 244);
                resultsGrid.DefaultCellStyle.SelectionForeColor = Color.White;

                // Detach/Attach sort event handler
                resultsGrid.Sorted -= ResultsGrid_Sorted;
                if (collapseActive)
                {
                    resultsGrid.Sorted += ResultsGrid_Sorted;
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to update grid sortability state");
            }
        }

        private void ResultsGrid_Sorted(object? sender, EventArgs e)
        {
            if (resultsGrid.DataSource is not DataTable dt) return;
            if (!groupByProductToolStripMenuItem.Checked) return;

            var sort = dt.DefaultView.Sort;
            if (string.IsNullOrEmpty(sort)) return;

            // When grouping, the primary sort should be by the original row number to keep groups intact.
            // The user-selected sort should be secondary.
            if (!sort.StartsWith($"[{RowNumberHeader}]", StringComparison.OrdinalIgnoreCase))
            {
                dt.DefaultView.Sort = $"[{RowNumberHeader}] ASC, {sort}";
            }
        }

        private void UpdateStatus(string message) => statusLabel.Text = message;

        #endregion

        // Designer event handlers required by Designer.cs
        private async void RunQueryButton_Click(object? sender, EventArgs e)
        {
            try
            {
                SetExecuting(true);

                var selectedColumns = _selectedColumns?.ToList() ?? new List<string>();
                if (selectedColumns.Count == 0)
                    selectedColumns = _availableColumns.ToList();

                bool collapse = groupByProductToolStripMenuItem.Checked == true;

                var sw = System.Diagnostics.Stopwatch.StartNew();
                var dataTable = await _queryService.ExecuteQueryAsync(selectedColumns, _filters, collapse, CancellationToken.None)
                    .ConfigureAwait(true);
                sw.Stop();

                // Apply editability rules to the DataTable
                ApplyDataTableEditability(dataTable);

                resultsGrid.DataSource = dataTable;
                UpdateGridSortability();

                // Ensure columns have proper configuration
                foreach (DataGridViewColumn c in resultsGrid.Columns)
                {
                    if (string.IsNullOrEmpty(c.Name)) c.Name = c.HeaderText ?? string.Empty;
                    c.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                    c.Resizable = DataGridViewTriState.True;
                }
                resultsGrid.AllowUserToResizeColumns = true;

                // Apply visibility based on selected columns
                TryApplyColumnVisibility();

                // Keep Row # first and visible
                EnsureRowNumberGridColumnFirst();

                // Apply per-column editability
                UpdateGridEditability();

                // Load and apply saved grid state (column widths, sort, display order)
                await ApplyGridStateAsync();

                // Auto-size columns after binding (only if no saved state)
                var hasAppliedState = resultsGrid.Tag as bool? == true;
                if (!hasAppliedState)
                {
                    resultsGrid.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
                }

                var visibleCols = resultsGrid.Columns.GetColumnCount(DataGridViewElementStates.Visible);
                UpdateStatus($"Rows: {dataTable.Rows.Count:N0} | Columns: {resultsGrid.Columns.Count:N0} (Visible: {visibleCols:N0}) | {sw.ElapsedMilliseconds} ms");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing query");
                MessageBox.Show($"Error executing query: {ex.Message}", "Query Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                UpdateStatus("Error");
            }
            finally
            {
                SetExecuting(false);
            }
        }

        private void AddFilterButton_Click(object? sender, EventArgs e) => AddFilter();

        private void RemoveFilterButton_Click(object? sender, EventArgs e) => RemoveSelectedFilter();

        private void FilterColumnComboBox_SelectedIndexChanged(object? sender, EventArgs e) => RefreshOperatorList();

        private void EnsureRowNumberGridColumnFirst()
        {
            try
            {
                var col = resultsGrid.Columns.Cast<DataGridViewColumn>().FirstOrDefault(c => string.Equals(c.HeaderText, RowNumberHeader, StringComparison.OrdinalIgnoreCase));
                if (col != null)
                {
                    col.DisplayIndex = 0;
                    col.ReadOnly = true;
                    col.Frozen = true;
                    if (col.Width == 100) // default width; shrink a bit
                        col.Width = 60;
                }
            }
            catch { }
        }

        // Ensure underlying DataTable columns allow editing when edit mode is active (except EAN and Row #)
        private void ApplyDataTableEditability(DataTable dt)
        {
            try
            {
                bool grouped = groupByProductToolStripMenuItem?.Checked == true;
                foreach (DataColumn dc in dt.Columns)
                {
                    var name = dc.ColumnName ?? string.Empty;
                    if (grouped || !_editMode || string.Equals(name, RowNumberHeader, StringComparison.OrdinalIgnoreCase) || string.Equals(name, "EAN", StringComparison.OrdinalIgnoreCase))
                    {
                        dc.ReadOnly = true;
                    }
                    else
                    {
                        dc.ReadOnly = false;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to apply DataTable editability");
            }
        }

        // Excel export (visible grid as shown, in current order)
        private async void ExportToExcelToolStripMenuItem_Click(object? sender, EventArgs e)
        {
            try
            {
                await ExportResultsToExcelAsync(CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Export to Excel failed");
                MessageBox.Show(this, ex.Message, "Export failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task ExportResultsToExcelAsync(CancellationToken ct)
        {
            if (resultsGrid == null || resultsGrid.Columns.Count == 0 || resultsGrid.Rows.Count == 0)
            {
                MessageBox.Show(this, "There is no data to export.", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var visibleCols = resultsGrid.Columns.Cast<DataGridViewColumn>()
                .Where(c => c.Visible)
                .OrderBy(c => c.DisplayIndex)
                .ToList();

            DataTable? source = null;
            if (resultsGrid.DataSource is DataTable dtSrc) source = dtSrc;
            else if (resultsGrid.DataSource is BindingSource bs && bs.DataSource is DataTable dtBs) source = dtBs;

            using var export = new DataTable("Results");

            foreach (var gc in visibleCols)
            {
                var header = gc.HeaderText ?? gc.Name ?? string.Empty;
                if (string.IsNullOrWhiteSpace(header)) header = $"Col{gc.Index}";

                var type = typeof(string);
                if (source != null && source.Columns.Contains(header))
                {
                    var col = source.Columns[header];
                    if (col != null) type = col.DataType;
                }

                export.Columns.Add(header, type);
            }

            foreach (DataGridViewRow gr in resultsGrid.Rows)
            {
                if (gr.IsNewRow) continue;
                ct.ThrowIfCancellationRequested();
                var newRow = export.NewRow();

                var boundRow = (gr.DataBoundItem as DataRowView)?.Row;
                foreach (var gc in visibleCols)
                {
                    var header = gc.HeaderText ?? gc.Name ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(header)) header = $"Col{gc.Index}";

                    object? value = null;
                    if (boundRow != null && boundRow.Table != null && boundRow.Table.Columns.Contains(header))
                    {
                        value = boundRow[header];
                    }

                    value ??= gr.Cells[gc.Index].Value;
                    newRow[header] = value ?? DBNull.Value;
                }

                export.Rows.Add(newRow);
            }

            string? filePath = null;
            using var sfd = new SaveFileDialog
            {
                Title = "Export to Excel",
                Filter = "Excel Workbook (*.xlsx)|*.xlsx",
                FileName = $"Sacks_Results_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xlsx",
                OverwritePrompt = true
            };

            if (sfd.ShowDialog(this) != DialogResult.OK) return;
            filePath = sfd.FileName;

            await Task.Run(() =>
            {
                using var wb = new XLWorkbook();
                var ws = wb.Worksheets.Add("Results");
                ws.Cell(1, 1).InsertTable(export, true);
                ws.Columns().AdjustToContents();
                wb.SaveAs(filePath!);
            }, ct);

            statusLabel.Text = $"Exported {export.Rows.Count:N0} rows to '{Path.GetFileName(filePath ?? string.Empty)}'.";
        }

        // Ensure SetExecuting exists (used to toggle UI state)
        private void SetExecuting(bool executing)
        {
            try
            {
                progressBar.Visible = executing;
                runQueryButton.Enabled = !executing;
                addFilterButton.Enabled = !executing;
                removeFilterButton.Enabled = !executing;
                // keep other controls responsive
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed in SetExecuting");
            }
        }

        // TryApplyColumnVisibility if referenced from other locations
        private void TryApplyColumnVisibility()
        {
            try
            {
                if (resultsGrid.Columns.Count == 0) return;
                var checkedSet = new HashSet<string>(_selectedColumns ?? Enumerable.Empty<string>(), StringComparer.OrdinalIgnoreCase);
                foreach (DataGridViewColumn col in resultsGrid.Columns)
                {
                    var name = col.HeaderText ?? string.Empty;
                    if (string.Equals(name, RowNumberHeader, StringComparison.OrdinalIgnoreCase))
                    {
                        col.Visible = true;
                        col.DisplayIndex = 0;
                        continue;
                    }
                    col.Visible = checkedSet.Contains(name);
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to apply column visibility");
            }
        }

        // Designer wired context menu handlers
        private void OpenLookupsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                // Check if form already exists and is not disposed
                if (_lookupEditor != null && !_lookupEditor.IsDisposed)
                {
                    // Form already open - bring to front and activate
                    _lookupEditor.BringToFront();
                    _lookupEditor.Activate();
                    _logger.LogDebug("Activated existing Lookup Editor instance");
                    return;
                }

                // Create new instance
                _lookupEditor = new LookupEditorForm(_serviceProvider!, string.Empty);

                // Auto-cleanup when form closes
                _lookupEditor.FormClosed += (s, ev) =>
                {
                    _logger.LogDebug("Lookup Editor closed - cleaning up reference");
                    _lookupEditor?.Dispose();
                    _lookupEditor = null;
                };

                // Show as modeless (non-blocking)
                _lookupEditor.Show(this);
                _logger.LogDebug("Opened new Lookup Editor instance");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error opening Lookup Editor");
                MessageBox.Show($"Error opening Lookup Editor: {ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }

        private void ShowHideCoulmnsToolStripMenuItem_Click(object? sender, EventArgs e)
        {
            try
            {
                // Prefer to reflect the current runtime visibility from the grid if present
                IEnumerable<string>? checkedCols = null;
                try
                {
                    if (resultsGrid != null && resultsGrid.Columns != null && resultsGrid.Columns.Count > 0)
                    {
                        checkedCols = resultsGrid.Columns.Cast<DataGridViewColumn>()
                            .Where(c => c.Visible)
                            .Select(c => c.HeaderText ?? string.Empty)
                            .ToList();
                    }
                }
                catch
                {
                    // ignore and fall back
                }

                checkedCols ??= _selectedColumns;

                using var dlg = new ColumnSelectorForm(_availableColumns, checkedCols);
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    _selectedColumns = dlg.SelectedColumns.ToList();

                    // Apply to existing grid if present
                    var set = new HashSet<string>(_selectedColumns, StringComparer.OrdinalIgnoreCase);
                    if (resultsGrid != null && resultsGrid.Columns != null)
                    {
                        foreach (DataGridViewColumn col in resultsGrid.Columns)
                        {
                            var name = col.HeaderText ?? string.Empty;
                            if (string.Equals(name, RowNumberHeader, StringComparison.OrdinalIgnoreCase))
                            {
                                col.Visible = true;
                                col.DisplayIndex = 0;
                                continue;
                            }
                            col.Visible = set.Contains(name);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed showing column selector");
                MessageBox.Show($"Failed to show column selector: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void SqlQueryForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            try
            {
                if (_hasUnsavedChanges)
                {
                    var res = MessageBox.Show(this, "You have unsaved changes. Save before closing?",
                        "Unsaved changes", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning,
                        MessageBoxDefaultButton.Button1);
                    if (res == DialogResult.Cancel)
                    {
                        e.Cancel = true;
                        return;
                    }
                    else if (res == DialogResult.Yes)
                    {
                        // Cancel close, run save, and let user close manually after
                        e.Cancel = true;
                        await SaveChangesAsync(CancellationToken.None);
                        return;
                    }
                }

                // Close modeless child forms before parent closes
                if (_lookupEditor != null && !_lookupEditor.IsDisposed)
                {
                    _logger.LogDebug("Closing Lookup Editor before parent form closes");
                    _lookupEditor.Close();
                    _lookupEditor.Dispose();
                    _lookupEditor = null;
                }

                // Save grid state (column widths, visibility, display order, sort)
                var gridState = new GridState
                {
                    Columns = resultsGrid.Columns.Cast<DataGridViewColumn>()
                        .Select(c => new ColumnState
                        {
                            Header = c.HeaderText ?? string.Empty,
                            Visible = c.Visible,
                            Width = c.Width,
                            DisplayIndex = c.DisplayIndex
                        }).ToList(),
                    SortedColumnHeader = resultsGrid.SortedColumn?.HeaderText ?? string.Empty,
                    SortDirection = resultsGrid.SortOrder.ToString()
                };
                await _gridStateService.SaveGridStateAsync(gridState);

                // Save filters
                await _gridStateService.SaveFiltersStateAsync(_filters);

                // Save selected columns
                await _gridStateService.SaveSelectedColumnsAsync(_selectedColumns);

                _logger.LogDebug("Grid state, filters, and selected columns saved successfully on form closing");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to save state on form closing");
            }
        }

        // add cached JsonSerializerOptions
        private static readonly JsonSerializerOptions s_jsonOptions = new JsonSerializerOptions { WriteIndented = true };

        // Adjust SplitContainer.SplitterDistance to fit Panel1 contents when its size changes
        private void UpdateSplitterToFitPanel1()
        {
            try
            {
                // Ensure splitter layout calculations are based on current layout
                mainSplitContainer.SuspendLayout();

                if (tableLayoutFilters.Visible)
                {
                    // Ask the filters panel for its preferred size constrained to the SplitContainer width
                    var availableWidth = Math.Max(0, mainSplitContainer.Width - mainSplitContainer.SplitterWidth);
                    var preferred = tableLayoutFilters.GetPreferredSize(new Size(availableWidth, 0));

                    // Add a small margin and honor min/max sizes
                    var desired = preferred.Height + 48;
                    var min = mainSplitContainer.Panel1MinSize > 0 ? mainSplitContainer.Panel1MinSize : 50;
                    var max = Math.Max(min, mainSplitContainer.Height - (mainSplitContainer.Panel2MinSize > 0 ? mainSplitContainer.Panel2MinSize : 50) - 10);
                    desired = Math.Clamp(desired, min, max);

                    mainSplitContainer.SplitterDistance = desired;
                    tableLayoutFilters.BringToFront();
                }
                else
                {
                    // Collapse to minimum when content is hidden
                    var min = mainSplitContainer.Panel1MinSize > 0 ? mainSplitContainer.Panel1MinSize : 0;
                    mainSplitContainer.SplitterDistance = min;
                }

                mainSplitContainer.ResumeLayout();
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to update splitter distance");
            }
        }

        private void ButtonShowFilter_Click(object sender, EventArgs e)
        {
            this.buttonHideFilters.Visible = true;
            this.buttonShowFilter.Visible = false;
            this.tableLayoutFilters.Visible = true;
            UpdateSplitterToFitPanel1();
        }

        private void ButtonHideFilters_Click(object sender, EventArgs e)
        {
            this.buttonHideFilters.Visible = false;
            this.buttonShowFilter.Visible = true;
            this.tableLayoutFilters.Visible = false;
            UpdateSplitterToFitPanel1();
        }

        // Edit mode UI + commands
        private void EditModeCheckBox_CheckedChanged(object? sender, EventArgs e)
        {
            _editMode = editModeCheckBox.Checked;
            UpdateEditUi();

            // If a table is already bound, reflect editability immediately
            if (resultsGrid.DataSource is DataTable dt)
            {
                ApplyDataTableEditability(dt);
                UpdateGridEditability();
            }
        }

        private void UpdateEditUi()
        {
            // In grouped mode force read-only regardless of edit mode
            var grouped = groupByProductToolStripMenuItem?.Checked == true;
            resultsGrid.ReadOnly = !_editMode || grouped;
            resultsGrid.DefaultCellStyle.BackColor = (!_editMode || grouped) ? Color.Gainsboro : Color.White;
            saveChangesButton.Enabled = _editMode && _hasUnsavedChanges;
            cancelAllButton.Enabled = _editMode && _hasUnsavedChanges;

            UpdateGridEditability();
        }

        private void UpdateGridEditability()
        {
            try
            {
                if (resultsGrid?.Columns == null || resultsGrid.Columns.Count == 0) return;
                bool grouped = groupByProductToolStripMenuItem?.Checked == true;
                foreach (DataGridViewColumn col in resultsGrid.Columns)
                {
                    var header = col.HeaderText ?? string.Empty;
                    if (string.Equals(header, RowNumberHeader, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(header, "EAN", StringComparison.OrdinalIgnoreCase))
                    {
                        col.ReadOnly = true;
                    }
                    else
                    {
                        // Editable only when edit mode ON and not grouped
                        col.ReadOnly = !_editMode || grouped;
                    }
                }
            }
            catch { }
        }

        private async void SaveChangesButton_Click(object? sender, EventArgs e)
        {
            await SaveChangesAsync(CancellationToken.None);
        }

        private async Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            try
            {
                SetExecuting(true);
                if (resultsGrid.DataSource is not DataTable dt)
                {
                    statusLabel.Text = "No data to save.";
                    return;
                }

                var result = await _dataService.SaveAllChangesAsync(dt, cancellationToken);

                if (result.TotalChanges > 0)
                {
                    if (result.IsFullySuccessful)
                    {
                        dt.AcceptChanges();
                        _hasUnsavedChanges = false;
                        statusLabel.Text = $"Saved {result.SuccessfulSaves} change(s).";
                    }
                    else
                    {
                        _hasUnsavedChanges = true;
                        statusLabel.Text = $"Partially saved: {result.SuccessfulSaves}/{result.TotalChanges} change(s). Some changes failed.";
                    }
                }
                else
                {
                    _hasUnsavedChanges = false;
                    statusLabel.Text = "No changes to save.";
                }

                UpdateEditUi();
            }
            catch (OperationCanceledException)
            {
                statusLabel.Text = "Save canceled";
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Save failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                SetExecuting(false);
            }
        }

        private void CancelAllButton_Click(object? sender, EventArgs e)
        {
            if (!_hasUnsavedChanges) return;
            var result = MessageBox.Show(this, "Discard all unsaved changes?", "Cancel All", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result != DialogResult.Yes) return;

            if (resultsGrid.DataSource is DataTable dt)
            {
                try { dt.RejectChanges(); } catch { /* ignore */ }
            }
            else if (resultsGrid.DataSource is BindingSource bs)
            {
                bs.ResetBindings(false);
            }
            else
            {
                RunQueryButton_Click(this, EventArgs.Empty);
            }

            _hasUnsavedChanges = false;
            UpdateEditUi();
            statusLabel.Text = "Changes discarded";
        }

        /// <summary>
        /// Loads and applies previously saved grid state (column widths, visibility, sort order)
        /// </summary>
        private async Task ApplyGridStateAsync()
        {
            try
            {
                var gridState = await _gridStateService.LoadGridStateAsync(CancellationToken.None).ConfigureAwait(true);
                if (gridState == null || gridState.Columns.Count == 0)
                {
                    resultsGrid.Tag = null; // No saved state applied
                    return;
                }

                // Apply column widths and display order
                foreach (var savedCol in gridState.Columns)
                {
                    var gridCol = resultsGrid.Columns.Cast<DataGridViewColumn>()
                        .FirstOrDefault(c => string.Equals(c.HeaderText, savedCol.Header, StringComparison.OrdinalIgnoreCase));

                    if (gridCol != null)
                    {
                        // Apply width if reasonable
                        if (savedCol.Width > 0 && savedCol.Width < 2000)
                        {
                            gridCol.Width = savedCol.Width;
                        }

                        // Apply display index (be careful with out-of-range values)
                        if (savedCol.DisplayIndex >= 0 && savedCol.DisplayIndex < resultsGrid.Columns.Count)
                        {
                            try
                            {
                                gridCol.DisplayIndex = savedCol.DisplayIndex;
                            }
                            catch
                            {
                                // Ignore display index conflicts
                            }
                        }

                        // Visibility is already applied by TryApplyColumnVisibility, but restore if needed
                        // (skip Row # which should always be visible)
                        if (!string.Equals(savedCol.Header, RowNumberHeader, StringComparison.OrdinalIgnoreCase))
                        {
                            gridCol.Visible = savedCol.Visible;
                        }
                    }
                }

                // Apply sort order if present
                if (!string.IsNullOrWhiteSpace(gridState.SortedColumnHeader))
                {
                    var sortCol = resultsGrid.Columns.Cast<DataGridViewColumn>()
                        .FirstOrDefault(c => string.Equals(c.HeaderText, gridState.SortedColumnHeader, StringComparison.OrdinalIgnoreCase));

                    if (sortCol != null && Enum.TryParse<System.Windows.Forms.SortOrder>(gridState.SortDirection, out var sortOrder))
                    {
                        try
                        {
                            var direction = sortOrder == System.Windows.Forms.SortOrder.Ascending
                                ? System.ComponentModel.ListSortDirection.Ascending
                                : System.ComponentModel.ListSortDirection.Descending;
                            resultsGrid.Sort(sortCol, direction);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogDebug(ex, "Failed to apply saved sort order");
                        }
                    }
                }

                resultsGrid.Tag = true; // Mark that saved state was applied
                _logger.LogDebug("Grid state applied successfully");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to apply saved grid state");
                resultsGrid.Tag = null;
            }
        }
    }
}
