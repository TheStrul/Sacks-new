using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SacksLogicLayer.Services.Interfaces;
using SacksDataLayer.FileProcessing.Configuration;
using System.Drawing;

namespace SacksApp
{
    public sealed partial class LookupEditorForm : Form
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<LookupEditorForm> _logger;
        private string _lookupName;
        private ISupplierConfigurationService? _svc;
        private ISuppliersConfiguration? _suppliersConfig;

        private readonly BindingList<LookupEntry> _entries = new();
        private CancellationTokenSource? _cts;
        private bool _suppressLookupComboEvents; // prevent re-entrant SelectedIndexChanged loops

        private sealed class LookupEntry
        {
            public string Key { get; set; } = string.Empty;
            public string Value { get; set; } = string.Empty;
        }

        private const string CreateNewItemText = "<Create new...>";

        public LookupEditorForm(IServiceProvider serviceProvider, string lookupName)
        {
            if (serviceProvider is null) throw new ArgumentNullException(nameof(serviceProvider));
            if (lookupName is null) throw new ArgumentNullException(nameof(lookupName));

            _serviceProvider = serviceProvider;
            _logger = _serviceProvider.GetRequiredService<ILogger<LookupEditorForm>>();
            _lookupName = lookupName;
            _svc = _serviceProvider.GetService<ISupplierConfigurationService>();

            InitializeComponent();

            // Titles
            Text = $"Lookup Editor - {_lookupName}";
            _titleLabel.Text = "Lookup:";

            // Theming with circular badges + rounded corners
            try
            {
                UITheme.ApplyBadgeStyle(_addButton, Color.FromArgb(33, 150, 243), "?"); // Add (E710)
                UITheme.ApplyBadgeStyle(_removeButton, Color.FromArgb(244, 67, 54), "?"); // Delete (E74D)
                UITheme.ApplyBadgeStyle(_reloadButton, Color.FromArgb(156, 39, 176), "?"); // Sync/Refresh (E72C)
                UITheme.ApplyBadgeStyle(_saveButton, Color.FromArgb(76, 175, 80), "?"); // Save (E74E)
                UITheme.ApplyBadgeStyle(_closeButton, Color.FromArgb(96, 125, 139), "?"); // Cancel (E710/E711)
            }
            catch { }

            // Grid styling
            try
            {
                _grid.BackgroundColor = Color.White;
                _grid.BorderStyle = BorderStyle.None;
                _grid.EnableHeadersVisualStyles = false;
                _grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(55, 71, 79);
                _grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
                _grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(245, 248, 255);
                _grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(3, 169, 244);
                _grid.DefaultCellStyle.SelectionForeColor = Color.White;
            }
            catch { }

            // Bind grid
            _grid.DataSource = _entries;

            // Wire events
            _grid.KeyDown += Grid_KeyDown;
            _addButton.Click += (_, __) => AddRow();
            _removeButton.Click += (_, __) => RemoveSelectedRows();
            _reloadButton.Click += async (_, __) => await LoadAsync(CancellationToken.None);
            _saveButton.Click += async (_, __) => await SaveAsync(CancellationToken.None);
            _closeButton.Click += (_, __) => Close();
            _lookupCombo.SelectedIndexChanged += LookupCombo_SelectedIndexChanged;

            Shown += async (_, __) => await LoadAsync(CancellationToken.None);
            FormClosing += LookupEditorForm_FormClosing;
        }

        private void LookupCombo_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (_suppressLookupComboEvents) return;

            var selected = _lookupCombo.SelectedItem as string;
            if (string.IsNullOrWhiteSpace(selected)) return;

            if (string.Equals(selected, CreateNewItemText, StringComparison.Ordinal))
            {
                var name = PromptForText(this, "New Lookup", "Enter new lookup name:", string.Empty);
                if (string.IsNullOrWhiteSpace(name))
                {
                    ResetSelectionToCurrentLookup();
                    return;
                }

                // Validate name
                if (_suppliersConfig != null && _suppliersConfig.Lookups.ContainsKey(name))
                {
                    MessageBox.Show(this, $"Lookup '{name}' already exists.", "Exists", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    ResetSelectionToCurrentLookup();
                    return;
                }

                // Create empty lookup in-memory
                if (_suppliersConfig != null)
                {
                    _suppliersConfig.Lookups[name] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    _lookupName = name;
                    PopulateLookupCombo();

                    _suppressLookupComboEvents = true;
                    try { _lookupCombo.SelectedItem = name; }
                    finally { _suppressLookupComboEvents = false; }

                    UpdateTitles();
                    // Clear entries for new lookup
                    _entries.Clear();
                    _entries.ResetBindings();
                    UpdateStatus("Created new lookup - not saved yet");
                }
                else
                {
                    _lookupName = name;
                    PopulateLookupCombo();
                    _suppressLookupComboEvents = true;
                    try { _lookupCombo.SelectedItem = name; }
                    finally { _suppressLookupComboEvents = false; }
                    UpdateTitles();
                }
                return;
            }

            // Switch to existing lookup
            _lookupName = selected;
            UpdateTitles();
            _ = LoadAsync(CancellationToken.None);
        }

        private void ResetSelectionToCurrentLookup()
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(_lookupName))
                {
                    _suppressLookupComboEvents = true;
                    try { _lookupCombo.SelectedItem = _lookupName; }
                    finally { _suppressLookupComboEvents = false; }
                }
            }
            catch { }
        }

        private void UpdateTitles()
        {
            Text = $"Lookup Editor - {_lookupName}";
        }

        private static string? PromptForText(IWin32Window owner, string title, string prompt, string defaultValue)
        {
            using var f = new Form
            {
                Text = title,
                StartPosition = FormStartPosition.CenterParent,
                MinimizeBox = false,
                MaximizeBox = false,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                ClientSize = new System.Drawing.Size(420, 140)
            };
            var lbl = new Label { Left = 12, Top = 12, Width = 390, Text = prompt };
            var tb = new TextBox { Left = 12, Top = 40, Width = 390, Text = defaultValue };
            var ok = new Button { Text = "OK", DialogResult = DialogResult.OK, Left = 240, Top = 80, Width = 75 };
            var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Left = 327, Top = 80, Width = 75 };
            f.AcceptButton = ok;
            f.CancelButton = cancel;
            f.Controls.AddRange(new Control[] { lbl, tb, ok, cancel });
            return f.ShowDialog(owner) == DialogResult.OK ? tb.Text.Trim() : null;
        }

        private void Grid_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete && !_grid.ReadOnly)
            {
                RemoveSelectedRows();
                e.Handled = true;
            }
        }

        private void LookupEditorForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            if (_entries.Any() && _grid.IsCurrentCellInEditMode)
            {
                _grid.EndEdit();
            }
        }

        private void AddRow()
        {
            _entries.Add(new LookupEntry { Key = string.Empty, Value = string.Empty });
            if (_entries.Count > 0)
            {
                var idx = _entries.Count - 1;
                _grid.CurrentCell = _grid.Rows[idx].Cells[0];
                _grid.BeginEdit(true);
            }
        }

        private void RemoveSelectedRows()
        {
            var toRemove = _grid.SelectedRows
                .Cast<DataGridViewRow>()
                .Where(r => r.DataBoundItem is LookupEntry)
                .Select(r => (LookupEntry)r.DataBoundItem!)
                .ToList();

            foreach (var item in toRemove)
            {
                _entries.Remove(item);
            }
            UpdateStatus($"Removed {toRemove.Count} entr{(toRemove.Count == 1 ? "y" : "ies")}");
        }

        private void UpdateStatus(string msg) => _statusLabel.Text = msg;

        private async Task LoadAsync(CancellationToken ct)
        {
            try
            {
                SetBusy(true);
                _cts?.Cancel();
                _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                ct = _cts.Token;

                _svc ??= _serviceProvider.GetService<ISupplierConfigurationService>();
                if (_svc == null)
                {
                    MessageBox.Show(this, "SupplierConfigurationService not available.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                _suppliersConfig ??= await _svc.GetAllConfigurationsAsync();

                // Populate combo from config
                PopulateLookupCombo();

                // Ensure selected lookup exists
                if (_suppliersConfig != null && !_suppliersConfig.Lookups.ContainsKey(_lookupName) && _suppliersConfig.Lookups.Count > 0)
                {
                    _lookupName = _suppliersConfig.Lookups.Keys.OrderBy(k => k, StringComparer.OrdinalIgnoreCase).First();
                    _suppressLookupComboEvents = true;
                    try { _lookupCombo.SelectedItem = _lookupName; }
                    finally { _suppressLookupComboEvents = false; }
                    UpdateTitles();
                }

                // Load entries for current lookup
                _entries.RaiseListChangedEvents = false;
                _entries.Clear();

                if (_suppliersConfig != null && _suppliersConfig.Lookups.TryGetValue(_lookupName, out var dict) && dict != null)
                {
                    foreach (var kv in dict.OrderBy(k => k.Key, StringComparer.OrdinalIgnoreCase))
                    {
                        _entries.Add(new LookupEntry { Key = kv.Key, Value = kv.Value });
                    }
                }

                _entries.RaiseListChangedEvents = true;
                _entries.ResetBindings();

                UpdateStatus($"Loaded {_entries.Count} entr{(_entries.Count == 1 ? "y" : "ies")} for '{_lookupName}'");
            }
            catch (OperationCanceledException)
            {
                UpdateStatus("Load canceled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load lookup {Lookup}", _lookupName);
                MessageBox.Show(this, ex.Message, "Load failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                SetBusy(false);
            }
        }

        private void PopulateLookupCombo()
        {
            try
            {
                _suppressLookupComboEvents = true;
                _lookupCombo.BeginUpdate();
                _lookupCombo.Items.Clear();
                if (_suppliersConfig != null)
                {
                    var names = _suppliersConfig.Lookups.Keys.OrderBy(k => k, StringComparer.OrdinalIgnoreCase).ToArray();
                    _lookupCombo.Items.AddRange(names);
                }
                _lookupCombo.Items.Add(CreateNewItemText);

                if (!string.IsNullOrWhiteSpace(_lookupName) && _lookupCombo.Items.Contains(_lookupName))
                {
                    _lookupCombo.SelectedItem = _lookupName;
                }
                else if (_lookupCombo.Items.Count > 0)
                {
                    if (_lookupCombo.Items[0] is string s && !string.Equals(s, CreateNewItemText, StringComparison.Ordinal))
                    {
                        _lookupCombo.SelectedIndex = 0;
                    }
                }
            }
            finally
            {
                _lookupCombo.EndUpdate();
                _suppressLookupComboEvents = false;
            }
        }

        private async Task SaveAsync(CancellationToken ct)
        {
            try
            {
                SetBusy(true);
                if (_grid.IsCurrentCellInEditMode) _grid.EndEdit();

                // Validate
                var errors = new List<string>();
                var comparer = StringComparer.OrdinalIgnoreCase;

                // Lookup name validation
                if (string.IsNullOrWhiteSpace(_lookupName))
                {
                    errors.Add("Lookup name is required.");
                }

                // No empty keys and unique keys
                if (_entries.Any(e => string.IsNullOrWhiteSpace(e.Key)))
                {
                    errors.Add("Keys cannot be empty.");
                }
                var dupGroups = _entries.GroupBy(e => e.Key ?? string.Empty, comparer)
                                        .Where(g => g.Count() > 1)
                                        .ToList();
                if (dupGroups.Count > 0)
                {
                    errors.Add("Duplicate keys found: " + string.Join(", ", dupGroups.Select(g => g.Key)));
                }

                if (errors.Count > 0)
                {
                    MessageBox.Show(this, string.Join(Environment.NewLine, errors), "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                _svc ??= _serviceProvider.GetService<ISupplierConfigurationService>();
                if (_svc == null)
                {
                    MessageBox.Show(this, "SupplierConfigurationService not available.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                _suppliersConfig ??= await _svc.GetAllConfigurationsAsync();
                if (_suppliersConfig == null)
                {
                    MessageBox.Show(this, "Supplier configuration not loaded.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (!_suppliersConfig.Lookups.TryGetValue(_lookupName, out var dict) || dict == null)
                {
                    dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    _suppliersConfig.Lookups[_lookupName] = dict;
                }
                else
                {
                    dict.Clear();
                }

                foreach (var e in _entries)
                {
                    dict[e.Key.Trim()] = e.Value?.Trim() ?? string.Empty;
                }

                await _suppliersConfig.Save();
                UpdateStatus($"Saved {dict.Count} entr{(dict.Count == 1 ? "y" : "ies")} for '{_lookupName}'");

                // Refresh combo to include possibly new lookup
                PopulateLookupCombo();
                _suppressLookupComboEvents = true;
                try { _lookupCombo.SelectedItem = _lookupName; }
                finally { _suppressLookupComboEvents = false; }
            }
            catch (OperationCanceledException)
            {
                UpdateStatus("Save canceled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save lookup {Lookup}", _lookupName);
                MessageBox.Show(this, ex.Message, "Save failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                SetBusy(false);
            }
        }

        private void SetBusy(bool busy)
        {
            Cursor = busy ? Cursors.AppStarting : Cursors.Default;
            _saveButton.Enabled = !busy;
            _reloadButton.Enabled = !busy;
            _addButton.Enabled = !busy;
            _removeButton.Enabled = !busy;
            _lookupCombo.Enabled = !busy;
        }
    }
}
