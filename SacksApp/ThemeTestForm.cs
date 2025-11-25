using ModernWinForms.Controls;
using ModernWinForms.Theming;

namespace SacksApp
{
    /// <summary>
    /// Test form to demonstrate and validate the theming system.
    /// Shows theme switching, skin switching, and control style inheritance.
    /// </summary>
    public partial class ThemeTestForm : Form
    {
        public ThemeTestForm()
        {
            InitializeComponent();
            InitializeThemeTest();
        }

        private void InitializeThemeTest()
        {
            // Load themes
            var themes = ThemeManager.AvailableThemes.ToArray();
            if (themes.Length == 0)
            {
                MessageBox.Show(
                    "No themes found!\n\n" +
                    "Please copy theme files from:\n" +
                    "ModernWinForms\\Skins\\*.theme.json\n\n" +
                    "To:\n" +
                    "SacksApp\\Skins\\*.theme.json\n\n" +
                    $"Looking in: {Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Skins")}",
                    "Theme Files Missing",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
            _themeComboBox.Items.AddRange(themes);
            _themeComboBox.SelectedItem = ThemeManager.CurrentTheme;

            // Load skins
            RefreshSkinList();
            
            // Update info
            UpdateInfoLabel();

            // Populate DataGridView with sample data
            PopulateDataGrid();

            // Apply initial theme
            ThemeManager.ApplyTheme(this);
        }

        private void RefreshSkinList()
        {
            _skinComboBox.Items.Clear();
            _skinComboBox.Items.AddRange(ThemeManager.AvailableSkinsForCurrentTheme.ToArray());
            _skinComboBox.SelectedItem = ThemeManager.CurrentSkin;
        }

        private void OnThemeChanged(object? sender, EventArgs e)
        {
            if (_themeComboBox.SelectedItem is string themeName)
            {
                ThemeManager.CurrentTheme = themeName;
                RefreshSkinList();
                UpdateInfoLabel();
                ThemeManager.ApplyTheme(this);
            }
        }

        private void OnSkinChanged(object? sender, EventArgs e)
        {
            if (_skinComboBox.SelectedItem is string skinName)
            {
                ThemeManager.CurrentSkin = skinName;
                UpdateInfoLabel();
                ThemeManager.ApplyTheme(this);
            }
        }

        private void UpdateInfoLabel()
        {
            var skin = ThemeManager.CurrentSkinDefinition;
            _infoLabel.Text = $"Active: {ThemeManager.CurrentTheme} theme + {ThemeManager.CurrentSkin} skin";

            if (!string.IsNullOrEmpty(skin.InheritsFrom))
            {
                _infoLabel.Text += $" (inherits from: {skin.InheritsFrom})";
            }
        }

        private void PopulateDataGrid()
        {
            // Create columns
            _dataGridView.Columns.Add("ControlName", "Control Name");
            _dataGridView.Columns.Add("Type", "Type");
            _dataGridView.Columns.Add("Features", "Key Features");
            _dataGridView.Columns.Add("ThemeSupport", "Theme Support");

            // Add data rows for all 15 controls
            _dataGridView.Rows.Add("ModernButton", "Action", "Hover animation, rounded corners", "✅ Full");
            _dataGridView.Rows.Add("ModernCheckBox", "Selection", "Check animation, custom rendering", "✅ Full");
            _dataGridView.Rows.Add("ModernComboBox", "Input", "Dropdown selection", "✅ Full");
            _dataGridView.Rows.Add("ModernTextBox", "Input", "Validation states, icons, focus animation", "✅ Full + IValidatable");
            _dataGridView.Rows.Add("ModernRadioButton", "Selection", "Radio animation, custom rendering", "✅ Full");
            _dataGridView.Rows.Add("ModernLabel", "Display", "Themed text display", "✅ Full");
            _dataGridView.Rows.Add("ModernPanel", "Container", "Background theming", "✅ Full");
            _dataGridView.Rows.Add("ModernGroupBox", "Container", "Rounded borders, title styling", "✅ Full");
            _dataGridView.Rows.Add("ModernDataGridView", "Data", "Modern grid styling, alternating rows", "✅ Full");
            _dataGridView.Rows.Add("ModernSplitContainer", "Layout", "Themed splitter", "✅ Full");
            _dataGridView.Rows.Add("ModernStatusStrip", "Status", "Bottom status bar", "✅ Full");
            _dataGridView.Rows.Add("ModernMenuStrip", "Navigation", "Custom renderer, modern colors", "✅ Full");
            _dataGridView.Rows.Add("ModernTabControl", "Navigation", "Custom-painted tabs", "✅ Full");
            _dataGridView.Rows.Add("ModernTableLayoutPanel", "Layout", "Grid-based layout", "✅ Full");
            _dataGridView.Rows.Add("ModernFlowLayoutPanel", "Layout", "Flow-based layout", "✅ Full");

            // Auto-size columns
            _dataGridView.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        }
    }
}
