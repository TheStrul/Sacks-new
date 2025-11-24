using System.Text.Json;

namespace SacksApp.Theming;

/// <summary>
/// Global manager for application skins
/// </summary>
public static class AppThemeManager
{
    private static SkinConfiguration _config = new();
    private static readonly string _configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Skins", "skins.json");
    private static readonly object _lock = new();
    private static readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    static AppThemeManager()
    {
        LoadConfiguration();
    }

    /// <summary>
    /// Gets or sets the current skin name (e.g., "Light", "Dark")
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
    /// Gets the current active skin definition
    /// </summary>
    public static SkinDefinition CurrentSkin => 
        _config.Skins.TryGetValue(_config.CurrentSkin, out var skin) ? skin : new SkinDefinition();

    public static event EventHandler? ThemeChanged;

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

    private static void ApplyThemeToControls(Control.ControlCollection controls, SkinDefinition skin)
    {
        foreach (Control control in controls)
        {
            if (control is ModernButton modernButton)
            {
                modernButton.ApplySkin(skin);
            }
            else if (control is ModernGroupBox modernGroupBox)
            {
                modernGroupBox.ApplySkin(skin);
            }
            else if (control is ModernTextBox modernTextBox)
            {
                modernTextBox.ApplySkin(skin);
            }

            if (control.HasChildren)
            {
                ApplyThemeToControls(control.Controls, skin);
            }
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
            // Fallback
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
        catch { }
    }
}
