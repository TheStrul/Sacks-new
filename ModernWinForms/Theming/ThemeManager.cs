using System.Text.Json;
using ModernWinForms.Controls;

namespace ModernWinForms.Theming;

/// <summary>
/// Global manager for application skins and themes.
/// Provides centralized theme management and automatic control styling.
/// </summary>
public static class ThemeManager
{
    private static SkinConfiguration _config = new();
    private static readonly string _configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Skins", "skins.json");
    private static readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    static ThemeManager()
    {
        LoadConfiguration();
    }

    /// <summary>
    /// Gets or sets the current active theme name (e.g., "Light", "Dark").
    /// Setting this property triggers a ThemeChanged event.
    /// </summary>
    public static string CurrentTheme
    {
        get => _config.CurrentSkin;
        set
        {
            if (_config.CurrentSkin != value)
            {
                _config.CurrentSkin = value;
                SaveConfiguration();
                ThemeChanged?.Invoke(null, EventArgs.Empty);
            }
        }
    }

    /// <summary>
    /// Gets the current active skin definition.
    /// </summary>
    public static SkinDefinition CurrentSkin => 
        _config.Skins.TryGetValue(_config.CurrentSkin, out var skin) ? skin : new SkinDefinition();

    /// <summary>
    /// Gets all available theme names.
    /// </summary>
    public static IEnumerable<string> AvailableThemes => _config.Skins.Keys;

    /// <summary>
    /// Occurs when the current theme is changed.
    /// </summary>
    public static event EventHandler? ThemeChanged;

    /// <summary>
    /// Applies the current theme to the specified form and all its controls recursively.
    /// </summary>
    /// <param name="form">The form to apply the theme to.</param>
    public static void ApplyTheme(Form form)
    {
        ArgumentNullException.ThrowIfNull(form);

        var skin = CurrentSkin;
        
        // Apply global background if defined
        if (!string.IsNullOrEmpty(skin.Palette.Background))
        {
            try
            {
                form.BackColor = ColorTranslator.FromHtml(skin.Palette.Background);
            }
            catch { /* Ignore invalid colors */ }
        }

        ApplyThemeToControls(form.Controls, skin);
    }

    /// <summary>
    /// Applies the current theme to a specific control and its children.
    /// </summary>
    /// <param name="control">The control to apply the theme to.</param>
    public static void ApplyTheme(Control control)
    {
        ArgumentNullException.ThrowIfNull(control);
        var skin = CurrentSkin;
        ApplyThemeToControl(control, skin);
        
        if (control.HasChildren)
        {
            ApplyThemeToControls(control.Controls, skin);
        }
    }

    /// <summary>
    /// Loads a skin configuration from a JSON file.
    /// </summary>
    /// <param name="filePath">Path to the JSON configuration file.</param>
    /// <returns>True if loaded successfully; otherwise, false.</returns>
    public static bool LoadConfigurationFrom(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                var json = File.ReadAllText(filePath);
                var config = JsonSerializer.Deserialize<SkinConfiguration>(json);
                if (config != null)
                {
                    _config = config;
                    ThemeChanged?.Invoke(null, EventArgs.Empty);
                    return true;
                }
            }
        }
        catch
        {
            // Ignore errors
        }
        return false;
    }

    private static void ApplyThemeToControls(Control.ControlCollection controls, SkinDefinition skin)
    {
        foreach (Control control in controls)
        {
            ApplyThemeToControl(control, skin);

            if (control.HasChildren)
            {
                ApplyThemeToControls(control.Controls, skin);
            }
        }
    }

    private static void ApplyThemeToControl(Control control, SkinDefinition skin)
    {
        switch (control)
        {
            case ModernButton modernButton:
                modernButton.ApplySkin(skin);
                break;
            case ModernGroupBox modernGroupBox:
                modernGroupBox.ApplySkin(skin);
                break;
            case ModernTextBox modernTextBox:
                modernTextBox.ApplySkin(skin);
                break;
        }
    }

    private static void LoadConfiguration()
    {
        try
        {
            if (File.Exists(_configPath))
            {
                var json = File.ReadAllText(_configPath);
                _config = JsonSerializer.Deserialize<SkinConfiguration>(json) ?? new SkinConfiguration();
            }
        }
        catch
        {
            // Use defaults
            _config = new SkinConfiguration();
        }
    }

    private static void SaveConfiguration()
    {
        try
        {
            var dir = Path.GetDirectoryName(_configPath);
            if (dir != null && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var json = JsonSerializer.Serialize(_config, _jsonOptions);
            File.WriteAllText(_configPath, json);
        }
        catch
        {
            // Ignore save errors
        }
    }
}
