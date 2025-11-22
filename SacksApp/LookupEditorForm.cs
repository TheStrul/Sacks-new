using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sacks.Core.Services.Interfaces;
using Sacks.Core.FileProcessing.Configuration;

namespace SacksApp
{
    public sealed partial class LookupEditorForm : Form
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<LookupEditorForm> _logger;
        private string _lookupName;
        private ISupplierConfigurationService? _svc;
        private ISuppliersConfiguration? _suppliersConfig;

        private readonly BindingList<LookupEntryViewModel> _entries = new();
        private CancellationTokenSource? _cts;
        private bool _suppressLookupComboEvents; // prevent re-entrant SelectedIndexChanged loops

        // Sorting state
        private string? _currentSortColumn;
        private ListSortDirection _currentSortDirection = ListSortDirection.Ascending;

        /// <summary>
        /// View model for editing lookup entries in Canonical + Aliases format
        /// </summary>
        private sealed class LookupEntryViewModel
        {
            public string Canonical { get; set; } = string.Empty;
            public string Aliases { get; set; } = string.Empty; // Comma-separated
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
            ConfigureGridForProgrammaticSort();

            // Wire events
            _grid.KeyDown += Grid_KeyDown;
            _grid.ColumnHeaderMouseClick += Grid_ColumnHeaderMouseClick;
            _addButton.Click += (_, __) => AddRow();
            _removeButton.Click += (_, __) => RemoveSelectedRows();
            _reloadButton.Click += async (_, __) => await LoadAsync(CancellationToken.None);
            _saveButton.Click += async (_, __) => await SaveAsync(CancellationToken.None);
            _closeButton.Click += (_, __) => Close();
            _lookupCombo.SelectedIndexChanged += LookupCombo_SelectedIndexChanged;

            Shown += async (_, __) => await LoadAsync(CancellationToken.None);
            FormClosing += LookupEditorForm_FormClosing;
        }

        private void ConfigureGridForProgrammaticSort()
        {
            try
            {
                foreach (DataGridViewColumn col in _grid.Columns)
                {
                    col.SortMode = DataGridViewColumnSortMode.Programmatic;
                }
            }
            catch { }
        }

        private void Grid_ColumnHeaderMouseClick(object? sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            if (e.ColumnIndex < 0 || e.ColumnIndex >= _grid.Columns.Count) return;

            // Finish editing before sorting
            if (_grid.IsCurrentCellInEditMode) _grid.EndEdit();

            var col = _grid.Columns[e.ColumnIndex];
            var prop = string.IsNullOrWhiteSpace(col.DataPropertyName) ? col.Name : col.DataPropertyName;
            if (string.IsNullOrWhiteSpace(prop)) return;

            // Toggle direction if clicking same column
            if (string.Equals(_currentSortColumn, prop, StringComparison.OrdinalIgnoreCase))
            {
                _currentSortDirection = _currentSortDirection == ListSortDirection.Ascending
                    ? ListSortDirection.Descending
                    : ListSortDirection.Ascending;
            }
            else
            {
                _currentSortColumn = prop;
                _currentSortDirection = ListSortDirection.Ascending;
            }

            SortEntries(_currentSortColumn!, _currentSortDirection);
            ApplySortGlyphs(_currentSortColumn!, _currentSortDirection);
        }

        private void ApplySortGlyphs(string columnProperty, ListSortDirection dir)
        {
            try
            {
                foreach (DataGridViewColumn c in _grid.Columns)
                {
                    var prop = string.IsNullOrWhiteSpace(c.DataPropertyName) ? c.Name : c.DataPropertyName;
                    c.HeaderCell.SortGlyphDirection = string.Equals(prop, columnProperty, StringComparison.OrdinalIgnoreCase)
                        ? (dir == ListSortDirection.Ascending ? SortOrder.Ascending : SortOrder.Descending)
                        : SortOrder.None;
                }
            }
            catch { }
        }

        private void SortEntries(string property, ListSortDirection direction)
        {
            IEnumerable<LookupEntryViewModel> ordered = _entries;
            var cmp = StringComparer.OrdinalIgnoreCase;

            if (string.Equals(property, nameof(LookupEntryViewModel.Canonical), StringComparison.OrdinalIgnoreCase))
            {
                ordered = direction == ListSortDirection.Ascending
                    ? _entries.OrderBy(x => x.Canonical ?? string.Empty, cmp)
                    : _entries.OrderByDescending(x => x.Canonical ?? string.Empty, cmp);
            }
            else if (string.Equals(property, nameof(LookupEntryViewModel.Aliases), StringComparison.OrdinalIgnoreCase))
            {
                ordered = direction == ListSortDirection.Ascending
                    ? _entries.OrderBy(x => x.Aliases ?? string.Empty, cmp)
                    : _entries.OrderByDescending(x => x.Aliases ?? string.Empty, cmp);
            }
            else
            {
                return;
            }

            var list = ordered.ToList();
            _entries.RaiseListChangedEvents = false;
            _entries.Clear();
            foreach (var it in list) _entries.Add(it);
            _entries.RaiseListChangedEvents = true;
            _entries.ResetBindings();
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
            _entries.Add(new LookupEntryViewModel { Canonical = string.Empty, Aliases = string.Empty });
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
                .Where(r => r.DataBoundItem is LookupEntryViewModel)
                .Select(r => (LookupEntryViewModel)r.DataBoundItem!)
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

                // Load entries for current lookup - group by canonical value
                _entries.RaiseListChangedEvents = false;
                _entries.Clear();

                if (_suppliersConfig != null && _suppliersConfig.Lookups.TryGetValue(_lookupName, out var dict) && dict != null)
                {
                    // Group aliases by canonical value
                    var grouped = dict
                        .GroupBy(kv => kv.Value, StringComparer.OrdinalIgnoreCase)
                        .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase);

                    foreach (var group in grouped)
                    {
                        var canonical = group.Key;
                        var aliases = string.Join(", ", group.Select(kv => kv.Key).OrderBy(k => k, StringComparer.OrdinalIgnoreCase));
                        _entries.Add(new LookupEntryViewModel { Canonical = canonical, Aliases = aliases });
                    }
                }

                _entries.RaiseListChangedEvents = true;
                _entries.ResetBindings();
                ConfigureGridForProgrammaticSort();
                ApplySortGlyphs(_currentSortColumn ?? nameof(LookupEntryViewModel.Canonical), _currentSortDirection);

                UpdateStatus($"Loaded {_entries.Count} canonical value{(_entries.Count == 1 ? "" : "s")} for '{_lookupName}'");
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
                    if (_lookupCombo.Items[0] is string s and not null && !string.Equals(s, CreateNewItemText, StringComparison.Ordinal))
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

                // No empty canonical values
                if (_entries.Any(e => string.IsNullOrWhiteSpace(e.Canonical)))
                {
                    errors.Add("Canonical values cannot be empty.");
                }

                // Unique canonical values
                var dupCanonical = _entries
                    .GroupBy(e => e.Canonical ?? string.Empty, comparer)
                    .Where(g => g.Count() > 1)
                    .ToList();
                if (dupCanonical.Count > 0)
                {
                    errors.Add("Duplicate canonical values found: " + string.Join(", ", dupCanonical.Select(g => g.Key)));
                }

                // Validate aliases
                var warnings = new List<string>();
                foreach (var entry in _entries)
                {
                    if (string.IsNullOrWhiteSpace(entry.Aliases))
                    {
                        warnings.Add($"Canonical '{entry.Canonical}' has no aliases - will be self-referencing only.");
                    }
                    else
                    {
                        var aliases = entry.Aliases.Split(',').Select(a => a.Trim()).Where(a => !string.IsNullOrWhiteSpace(a)).ToList();
                        if (!aliases.Any())
                        {
                            warnings.Add($"Canonical '{entry.Canonical}' has no valid aliases.");
                        }
                        else if (!aliases.Contains(entry.Canonical, comparer))
                        {
                            warnings.Add($"Canonical '{entry.Canonical}' should be included in its own aliases.");
                        }
                    }
                }

                if (errors.Count > 0)
                {
                    MessageBox.Show(this, string.Join(Environment.NewLine, errors), "Validation Errors", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (warnings.Count > 0)
                {
                    var result = MessageBox.Show(
                        this,
                        string.Join(Environment.NewLine, warnings) + Environment.NewLine + Environment.NewLine + "Continue saving?",
                        "Validation Warnings",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning);
                    if (result != DialogResult.Yes)
                    {
                        return;
                    }
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

                // Flatten canonical + aliases back to key-value dictionary
                int totalAliases = 0;
                foreach (var entry in _entries)
                {
                    var canonical = entry.Canonical.Trim();
                    var aliases = entry.Aliases
                        .Split(',')
                        .Select(a => a.Trim())
                        .Where(a => !string.IsNullOrWhiteSpace(a))
                        .Distinct(comparer)
                        .ToList();

                    // If no aliases, add canonical as self-referencing
                    if (aliases.Count == 0)
                    {
                        aliases.Add(canonical);
                    }

                    // Map all aliases to canonical value
                    foreach (var alias in aliases)
                    {
                        dict[alias] = canonical;
                        totalAliases++;
                    }
                }

                await _suppliersConfig.Save();
                UpdateStatus($"Saved {_entries.Count} canonical value{(_entries.Count == 1 ? "" : "s")} ({totalAliases} total alias{(totalAliases == 1 ? "" : "es")}) for '{_lookupName}'");

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
