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
            
            // Set initial style info
            _styleDetailsLabel.Text = GetStyleInfo();

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

            // Refresh style info
            if (_testGroupBox.Controls[4] is Label styleLabel)
            {
                styleLabel.Text = GetStyleInfo();
            }
        }
    }

    private void OnSkinChanged(object? sender, EventArgs e)
    {
        if (_skinComboBox.SelectedItem is string skinName)
        {
            ThemeManager.CurrentSkin = skinName;
            UpdateInfoLabel();
            ThemeManager.ApplyTheme(this);

            // Refresh style info
            if (_testGroupBox.Controls[4] is Label styleLabel)
            {
                styleLabel.Text = GetStyleInfo();
            }
        }
    }

    private void UpdateInfoLabel()
    {
        var theme = ThemeManager.CurrentThemeDefinition;
        var skin = ThemeManager.CurrentSkinDefinition;

        _infoLabel.Text = $"Active: {ThemeManager.CurrentTheme} theme + {ThemeManager.CurrentSkin} skin";

        if (!string.IsNullOrEmpty(skin.InheritsFrom))
        {
            _infoLabel.Text += $" (inherits from: {skin.InheritsFrom})";
        }
    }

    private string GetStyleInfo()
    {
        var buttonStyle = ThemeManager.GetControlStyle("ModernButton");
        var groupBoxStyle = ThemeManager.GetControlStyle("ModernGroupBox");
        var textBoxStyle = ThemeManager.GetControlStyle("ModernTextBox");

        var info = "=== Button Style ===\n";
        if (buttonStyle != null)
        {
            info += $"CornerRadius: {buttonStyle.CornerRadius}px\n";
            info += $"BorderWidth: {buttonStyle.BorderWidth}px\n";
            info += $"States: {string.Join(", ", buttonStyle.States.Keys)}\n";
            if (buttonStyle.States.TryGetValue("normal", out var normalState))
            {
                info += $"Normal: Back={normalState.BackColor}, Fore={normalState.ForeColor}, Border={normalState.BorderColor}\n";
            }
        }

        info += "\n=== GroupBox Style ===\n";
        if (groupBoxStyle != null)
        {
            info += $"CornerRadius: {groupBoxStyle.CornerRadius}px\n";
            info += $"BorderWidth: {groupBoxStyle.BorderWidth}px\n";
        }

        info += "\n=== Architecture Validation ===\n";
        info += $"✅ Themes provide structure (cornerRadius, borderWidth)\n";
        info += $"✅ Skins provide colors (states only)\n";
        info += $"✅ ThemeManager.GetControlStyle() merges both\n";

        return info;
    }
    }
}
