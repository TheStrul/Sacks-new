using System.Text.Json;
using ModernWinForms.Controls;

namespace ModernWinForms.Theming;

/// <summary>
/// Global manager for application skins and themes.
/// Provides centralized theme management and automatic control styling.
/// </summary>
public static class ThemeManager
{
    private static ThemingConfiguration _config = new();
    private static readonly string _configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Skins", "skins.json");
    private static readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    static ThemeManager()
    {
        LoadConfiguration();
    }

    /// <summary>
    /// Gets or sets the current active theme name (design system, e.g., "GitHub", "Material", "Fluent").
    /// Setting this property triggers a ThemeChanged event.
    /// </summary>
    public static string CurrentTheme
    {
        get => _config.CurrentTheme;
        set
        {
            if (_config.CurrentTheme != value)
            {
                _config.CurrentTheme = value;
                SaveConfiguration();
                ThemeChanged?.Invoke(null, EventArgs.Empty);
            }
        }
    }

    /// <summary>
    /// Gets or sets the current active skin name (color variant, e.g., "Light", "Dark").
    /// Setting this property triggers a ThemeChanged event.
    /// </summary>
    public static string CurrentSkin
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
    /// Gets the current active theme definition (design system).
    /// </summary>
    public static ThemeDefinition CurrentThemeDefinition => 
        _config.Themes.TryGetValue(_config.CurrentTheme, out var theme) ? theme : new ThemeDefinition();

    /// <summary>
    /// Gets the current active skin definition (color variant).
    /// </summary>
    public static SkinDefinition CurrentSkinDefinition => 
        _config.Skins.TryGetValue(_config.CurrentSkin, out var skin) ? skin : new SkinDefinition();

    /// <summary>
    /// Gets all available theme names (design systems).
    /// </summary>
    public static IEnumerable<string> AvailableThemes => _config.Themes.Keys;

    /// <summary>
    /// Gets all available skin names (color variants).
    /// </summary>
    public static IEnumerable<string> AvailableSkins => _config.Skins.Keys;

    /// <summary>
    /// Gets skins available for the current theme.
    /// </summary>
    public static IEnumerable<string> AvailableSkinsForCurrentTheme => 
        _config.Skins.Where(kvp => kvp.Value.Theme == _config.CurrentTheme || string.IsNullOrEmpty(kvp.Value.Theme))
            .Select(kvp => kvp.Key);

    /// <summary>
    /// Gets a complete control style by merging theme structure with skin colors.
    /// </summary>
    /// <param name="controlName">The name of the control (e.g., "ModernButton").</param>
    /// <returns>A complete ControlStyle with both structure and colors, or null if not found.</returns>
    public static ControlStyle? GetControlStyle(string controlName)
    {
        var theme = CurrentThemeDefinition;
        var skin = CurrentSkinDefinition;

        // Start with theme's structural definition
        ControlStyle? result = null;
        if (theme.Controls.TryGetValue(controlName, out var themeStyle))
        {
            result = new ControlStyle
            {
                CornerRadius = themeStyle.CornerRadius,
                BorderWidth = themeStyle.BorderWidth,
                Padding = themeStyle.Padding,
                States = new Dictionary<string, StateStyle>(themeStyle.States)
            };
        }
        else
        {
            // No theme definition, create minimal structure
            result = new ControlStyle();
        }

        // Override with skin's color definitions
        if (skin.Controls.TryGetValue(controlName, out var skinColors))
        {
            foreach (var (stateName, stateStyle) in skinColors.States)
            {
                result.States[stateName] = stateStyle;
            }
        }

        return result;
    }

    /// <summary>
    /// Occurs when the current theme is changed.
    /// </summary>
    public static event EventHandler? ThemeChanged;

    /// <summary>
    /// Applies the current theme and skin to the specified form and all its controls recursively.
    /// </summary>
    /// <param name="form">The form to apply the theme to.</param>
    public static void ApplyTheme(Form form)
    {
        ArgumentNullException.ThrowIfNull(form);

        var skin = CurrentSkinDefinition;
        
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
    /// Applies the current theme and skin to a specific control and its children.
    /// </summary>
    /// <param name="control">The control to apply the theme to.</param>
    public static void ApplyTheme(Control control)
    {
        ArgumentNullException.ThrowIfNull(control);
        var skin = CurrentSkinDefinition;
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
                var config = JsonSerializer.Deserialize<ThemingConfiguration>(json);
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
            // Load base configuration (version, currentTheme, and currentSkin)
            if (File.Exists(_configPath))
            {
                var json = File.ReadAllText(_configPath);
                _config = JsonSerializer.Deserialize<ThemingConfiguration>(json) ?? new ThemingConfiguration();
            }
            else
            {
                _config = new ThemingConfiguration();
            }

            var skinsDir = Path.GetDirectoryName(_configPath);
            if (skinsDir != null && Directory.Exists(skinsDir))
            {
                // Load theme files (*.theme.json)
                var themeFiles = Directory.GetFiles(skinsDir, "*.theme.json");
                foreach (var themeFile in themeFiles)
                {
                    try
                    {
                        var themeJson = File.ReadAllText(themeFile);
                        var themeDef = JsonSerializer.Deserialize<ThemeDefinition>(themeJson);
                        if (themeDef != null)
                        {
                            // Use filename without extension as theme name (e.g., "Material.theme.json" -> "Material")
                            var themeName = Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(themeFile));
                            _config.Themes[themeName] = themeDef;
                        }
                    }
                    catch
                    {
                        // Skip invalid theme files
                    }
                }

                // Load individual skin files (*.skin.json)
                var skinFiles = Directory.GetFiles(skinsDir, "*.skin.json");
                foreach (var skinFile in skinFiles)
                {
                    try
                    {
                        var skinJson = File.ReadAllText(skinFile);
                        var skinDef = JsonSerializer.Deserialize<SkinDefinition>(skinJson);
                        if (skinDef != null)
                        {
                            // Use filename without extension as skin name (e.g., "Light.skin.json" -> "Light")
                            var skinName = Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(skinFile));
                            _config.Skins[skinName] = skinDef;
                        }
                    }
                    catch
                    {
                        // Skip invalid skin files
                    }
                }

                // Resolve inheritance after all themes and skins are loaded
                ResolveThemeInheritance();
                ResolveSkinInheritance();
            }
        }
        catch
        {
            // Use defaults
            _config = new ThemingConfiguration();
        }
    }

    /// <summary>
    /// Resolves theme inheritance, merging derived themes with their base themes.
    /// </summary>
    private static void ResolveThemeInheritance()
    {
        var resolved = new Dictionary<string, ThemeDefinition>();
        var resolving = new HashSet<string>();

        ThemeDefinition ResolveTheme(string themeName)
        {
            if (resolved.TryGetValue(themeName, out var cachedTheme))
                return cachedTheme;

            if (!resolving.Add(themeName))
                throw new InvalidOperationException($"Circular inheritance detected for theme '{themeName}'");

            if (!_config.Themes.TryGetValue(themeName, out var theme))
                throw new InvalidOperationException($"Theme '{themeName}' not found");

            try
            {
                if (string.IsNullOrEmpty(theme.InheritsFrom))
                {
                    resolved[themeName] = theme;
                    return theme;
                }

                var baseTheme = ResolveTheme(theme.InheritsFrom);
                var merged = theme.MergeWith(baseTheme);
                resolved[themeName] = merged;
                return merged;
            }
            finally
            {
                resolving.Remove(themeName);
            }
        }

        var themeNames = _config.Themes.Keys.ToList();
        foreach (var themeName in themeNames)
        {
            try
            {
                _config.Themes[themeName] = ResolveTheme(themeName);
            }
            catch
            {
                // If inheritance fails, keep the original theme
            }
        }
    }

    /// <summary>
    /// Resolves skin inheritance, merging derived skins with their base skins.
    /// </summary>
    private static void ResolveSkinInheritance()
    {
        var resolved = new Dictionary<string, SkinDefinition>();
        var resolving = new HashSet<string>();

        SkinDefinition ResolveSkin(string skinName)
        {
            // Already resolved
            if (resolved.TryGetValue(skinName, out var cachedSkin))
                return cachedSkin;

            // Detect circular dependency
            if (!resolving.Add(skinName))
                throw new InvalidOperationException($"Circular inheritance detected for skin '{skinName}'");

            if (!_config.Skins.TryGetValue(skinName, out var skin))
                throw new InvalidOperationException($"Skin '{skinName}' not found");

            try
            {
                // If no inheritance, use as-is
                if (string.IsNullOrEmpty(skin.InheritsFrom))
                {
                    resolved[skinName] = skin;
                    return skin;
                }

                // Resolve parent first
                var baseSkin = ResolveSkin(skin.InheritsFrom);

                // Merge with parent
                var merged = skin.MergeWith(baseSkin);
                resolved[skinName] = merged;
                return merged;
            }
            finally
            {
                resolving.Remove(skinName);
            }
        }

        // Resolve all skins
        var skinNames = _config.Skins.Keys.ToList();
        foreach (var skinName in skinNames)
        {
            try
            {
                _config.Skins[skinName] = ResolveSkin(skinName);
            }
            catch
            {
                // If inheritance fails, keep the original skin
            }
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
