using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SacksDataLayer.Data;
using System.Data;
using System.Text;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel; // Needed for ListSortDirection
using Microsoft.Data.SqlClient; // Added for parameterized SQL execution
using System.Text.Json; // For state persistence
using SacksDataLayer.FileProcessing.Configuration; // for SuppliersConfiguration
using Microsoft.Extensions.Configuration;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging.Abstractions;

namespace SacksApp
{
    public partial class SqlQueryForm : Form
    {
        // JSON options for reading/writing supplier formats
        private static readonly JsonSerializerOptions s_supplierJsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
            WriteIndented = true
        };

        #region Existing UI Logic (Moved to partial)
        private void SetupControls()
        {
            resultsGrid.AllowUserToAddRows = false;
            resultsGrid.AllowUserToDeleteRows = false;
            resultsGrid.ReadOnly = true;
            resultsGrid.AllowUserToOrderColumns = true;
            resultsGrid.AllowUserToResizeColumns = true;
            resultsGrid.AllowUserToResizeRows = true;
            resultsGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            // Allow selection at cell level so we can operate on cell text
            resultsGrid.SelectionMode = DataGridViewSelectionMode.CellSelect;
            resultsGrid.MultiSelect = true;
            resultsGrid.ClipboardCopyMode = DataGridViewClipboardCopyMode.EnableWithAutoHeaderText;
            resultsGrid.ColumnHeaderMouseClick += ResultsGrid_ColumnHeaderMouseClick;
            resultsGrid.CellMouseDown += ResultsGrid_CellMouseDown; // handle cell right-clicks
            resultsGrid.ColumnDisplayIndexChanged += ResultsGrid_ColumnDisplayIndexChanged;
            resultsGrid.ColumnWidthChanged += ResultsGrid_ColumnWidthChanged;
            resultsGrid.Sorted += ResultsGrid_Sorted;
        }

        private ContextMenuStrip CreateCellContextMenu(DataGridViewCell? cell, string selectedText)
        {
#pragma warning disable CA2000
            var menu = new ContextMenuStrip();
#pragma warning restore CA2000
            var copyItem = new ToolStripMenuItem("Copy");
            copyItem.Click += (s, e) =>
            {
                try
                {
                    if (!string.IsNullOrEmpty(selectedText)) Clipboard.SetText(selectedText);
                    else if (cell != null) Clipboard.SetText(cell.FormattedValue?.ToString() ?? string.Empty);
                }
                catch { }
            };
            menu.Items.Add(copyItem);

            var selectAllItem = new ToolStripMenuItem("Select All");
            selectAllItem.Click += (s, e) =>
            {
                try
                {
                    if (resultsGrid.EditingControl is TextBox tb) tb.SelectAll();
                    else if (cell != null) resultsGrid.CurrentCell = cell;
                }
                catch { }
            };
            menu.Items.Add(selectAllItem);

            menu.Items.Add(new ToolStripSeparator());

            // Add a single "Add to lookup..." item that opens a modal dialog to choose lookup and confirm value
            var addToLookup = new ToolStripMenuItem("Add to lookup...");
            addToLookup.Click += async (s, e) =>
            {
                try
                {
                    var svc = _serviceProvider?.GetService<SacksLogicLayer.Services.Interfaces.ISupplierConfigurationService>() ?? _serviceProvider?.GetService<SacksLogicLayer.Services.Implementations.SupplierConfigurationService>();
                    SuppliersConfiguration? configs = null;
                    if (svc != null)
                    {
                        configs = await svc.GetAllConfigurationsAsync();
                    }

                    var lookupNames = configs?.Lookups?.Keys?.OrderBy(k => k).ToList() ?? new List<string>();
                    if (lookupNames.Count == 0)
                    {
                        MessageBox.Show("No lookup tables are defined in supplier configuration.", "Add to lookup", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }

                    // Build dialog
                    using var dlg = new Form()
                    {
                        Text = "Add to lookup",
                        FormBorderStyle = FormBorderStyle.FixedDialog,
                        StartPosition = FormStartPosition.CenterParent,
                        MinimizeBox = false,
                        MaximizeBox = false,
                        Size = new Size(420, 180)
                    };

                    var lblTable = new Label() { Text = "Lookup table:", Left = 10, Top = 15, AutoSize = true };
                    var cmb = new ComboBox() { Left = 110, Top = 10, Width = 280, DropDownStyle = ComboBoxStyle.DropDownList };
                    cmb.Items.AddRange(lookupNames.ToArray());
                    cmb.SelectedIndex = 0;

                    var lblValue = new Label() { Text = "Value:", Left = 10, Top = 55, AutoSize = true };
                    var txt = new TextBox() { Left = 110, Top = 50, Width = 280 };
                    txt.Text = string.IsNullOrEmpty(selectedText) ? (cell?.FormattedValue?.ToString() ?? string.Empty) : selectedText;

                    var btnOk = new Button() { Text = "Add", Left = 220, Width = 80, Top = 95, DialogResult = DialogResult.OK };
                    var btnCancel = new Button() { Text = "Cancel", Left = 310, Width = 80, Top = 95, DialogResult = DialogResult.Cancel };

                    dlg.Controls.AddRange(new Control[] { lblTable, cmb, lblValue, txt, btnOk, btnCancel });
                    dlg.AcceptButton = btnOk;
                    dlg.CancelButton = btnCancel;

                    if (dlg.ShowDialog(this) != DialogResult.OK) return;

                    var chosen = cmb.SelectedItem as string;
                    var valueToAdd = txt.Text?.Trim() ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(chosen) || string.IsNullOrWhiteSpace(valueToAdd))
                    {
                        MessageBox.Show("Please select a lookup table and enter a non-empty value.", "Add to lookup", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    await AddValueToLookupAsync(chosen, valueToAdd);

                    MessageBox.Show($"Added '{valueToAdd}' to lookup '{chosen}'.\n\nNote: configuration file updated on disk; other services may require restart to pick up changes.",
                        "Lookup Updated", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to add value to lookup");
                    MessageBox.Show($"Failed to add to lookup: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
            menu.Items.Add(addToLookup);

            return menu;
        }

        private async Task AddValueToLookupAsync(string lookupName, string key)
        {
            if (string.IsNullOrWhiteSpace(lookupName)) throw new ArgumentNullException(nameof(lookupName));
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentNullException(nameof(key));

            try
            {
                var config = _serviceProvider!.GetRequiredService<IConfiguration>();
                var supplierPath = config["ConfigurationFiles:SupplierFormats"]; // may be relative
                if (string.IsNullOrWhiteSpace(supplierPath)) throw new InvalidOperationException("SupplierFormats configuration path is not set");

                string ResolvePath(string path)
                {
                    if (string.IsNullOrWhiteSpace(path)) return path ?? string.Empty;
                    return Path.IsPathRooted(path) ? path : Path.Combine(AppContext.BaseDirectory, path);
                }

                var resolved = ResolvePath(supplierPath!);
                if (!File.Exists(resolved)) throw new FileNotFoundException("Supplier formats file not found", resolved);

                var json = await File.ReadAllTextAsync(resolved);
                var suppliersConfig = JsonSerializer.Deserialize<SuppliersConfiguration>(json, s_supplierJsonOptions) ?? new SuppliersConfiguration();

                // ensure top-level case-insensitive dictionary
                if (suppliersConfig.Lookups == null)
                {
                    suppliersConfig.Lookups = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
                }
                else if (!(suppliersConfig.Lookups.Comparer is StringComparer scTop && scTop.Equals(StringComparer.OrdinalIgnoreCase)))
                {
                    suppliersConfig.Lookups = new Dictionary<string, Dictionary<string, string>>(suppliersConfig.Lookups, StringComparer.OrdinalIgnoreCase);
                }

                if (!suppliersConfig.Lookups.TryGetValue(lookupName, out var inner))
                {
                    inner = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    suppliersConfig.Lookups[lookupName] = inner;
                }
                else if (!(inner.Comparer is StringComparer scInner && scInner.Equals(StringComparer.OrdinalIgnoreCase)))
                {
                    inner = new Dictionary<string, string>(inner, StringComparer.OrdinalIgnoreCase);
                    suppliersConfig.Lookups[lookupName] = inner;
                }

                // add or update mapping (use display value same as key)
                inner[key] = key;

                // write back atomically
                var outJson = JsonSerializer.Serialize(suppliersConfig, s_supplierJsonOptions);
                var tempPath = resolved + ".tmp";
                await File.WriteAllTextAsync(tempPath, outJson, Encoding.UTF8);
                File.Move(tempPath, resolved, true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating supplier lookups");
                throw;
            }
        }

        private void ResultsGrid_ColumnWidthChanged(object? sender, DataGridViewColumnEventArgs e)
        {
            try
            {
                // Session persistence removed - no-op
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
                // Session persistence removed - nothing to persist
                if (resultsGrid.SortedColumn == null) return;
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
                // Session persistence removed - no-op
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
            try
            {
                var idx = columnsCheckedListBox.Items.IndexOf(column.HeaderText);
                if (idx >= 0)
                {
                    columnsCheckedListBox.SetItemChecked(idx, false);
                }
            }
            catch { }

            column.Visible = false;
        }

        private static string GetColumnKey(DataGridViewColumn column)
            => !string.IsNullOrEmpty(column.Name) ? column.Name : column.HeaderText ?? string.Empty;

        private void SortColumn(DataGridViewColumn column, ListSortDirection direction)
        {
            if (collapseProductsCheckBox?.Checked == true) return;
            resultsGrid.Sort(column, direction);
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

                if (!collapseActive)
                {
                    // No saved sort to apply - session persistence removed
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to update grid sortability state");
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

        private void ResultsGrid_CellMouseDown(object? sender, DataGridViewCellMouseEventArgs e)
        {
            try
            {
                if (e.Button != MouseButtons.Right) return;
                if (e.RowIndex < 0 || e.ColumnIndex < 0) return;

                // set current cell
                resultsGrid.CurrentCell = resultsGrid.Rows[e.RowIndex].Cells[e.ColumnIndex];

                // Enter edit mode so the editing control is available for text selection.
                // The grid may be ReadOnly for normal operations; temporarily allow editing for selection
                var gridWasReadOnly = resultsGrid.ReadOnly;
                if (gridWasReadOnly) resultsGrid.ReadOnly = false;
                try
                {
                    if (!resultsGrid.IsCurrentCellInEditMode)
                    {
                        resultsGrid.BeginEdit(true);
                    }
                }
                catch { }

                // Extract cell text - prefer selected text from editing control when available
                string cellText = resultsGrid.CurrentCell?.FormattedValue?.ToString() ?? string.Empty;
                string selectedText = cellText;
                if (resultsGrid.IsCurrentCellInEditMode && resultsGrid.EditingControl is TextBox tb)
                {
                    try
                    {
                        // If user has a selection in the editing control use it; otherwise select all for convenience
                        if (!string.IsNullOrEmpty(tb.SelectedText)) selectedText = tb.SelectedText;
                        else tb.SelectAll();
                    }
                    catch { }
                }

                #pragma warning disable CA2000 // Owned by Closed handler and disposed there
                var menu = CreateCellContextMenu(resultsGrid.CurrentCell, selectedText);
                #pragma warning restore CA2000
                // restore grid readonly when menu closes and end edit
                menu.Closed += (s, ev) =>
                {
                    try
                    {
                        if (resultsGrid.IsCurrentCellInEditMode)
                        {
                            // leave cell edit mode
                            resultsGrid.EndEdit();
                        }
                    }
                    catch { }
                    try { if (gridWasReadOnly) resultsGrid.ReadOnly = true; } catch { }
                    // Dispose menu after close
                    try { menu.Dispose(); } catch { }
                };
                var location = resultsGrid.PointToScreen(new Point(e.X, e.Y));
                menu.Show(location);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to show cell context menu");
            }
        }
        #endregion

        // Designer event handlers required by Designer.cs
        private async void RunQueryButton_Click(object? sender, EventArgs e)
        {
            var (sql, parameters) = BuildSqlQuery();
            sqlParametersList = parameters;
            _logger.LogDebug("Executing generated query: {SqlPreview}", sql.Length > 200 ? sql[..200] + "..." : sql);
            sqlLabel.Text = sql;
            await ExecuteSqlAsync(sql, parameters, CancellationToken.None);
        }

        private void AddFilterButton_Click(object? sender, EventArgs e) => AddFilter();

        private void RemoveFilterButton_Click(object? sender, EventArgs e) => RemoveSelectedFilter();

        private void FilterColumnComboBox_SelectedIndexChanged(object? sender, EventArgs e) => RefreshOperatorList();

        private void CollapseProductsCheckBox_CheckedChanged(object? sender, EventArgs e) => UpdateGridSortability();
    }
}