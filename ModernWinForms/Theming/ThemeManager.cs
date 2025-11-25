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
    private static readonly SemaphoreSlim _configLock = new(1, 1);

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
    /// Gets the effective typography by merging theme and skin typography settings.
    /// Priority: Skin typography > Theme typography > Default ("Segoe UI", 9.0f).
    /// </summary>
    /// <returns>The effective Typography settings.</returns>
    public static Typography GetEffectiveTypography()
    {
        var theme = CurrentThemeDefinition;
        var skin = CurrentSkinDefinition;

        // Start with default
        var result = new Typography { FontFamily = "Segoe UI", FontSize = 9.0f };

        // Apply theme typography if available
        if (theme.Typography != null)
        {
            if (!string.IsNullOrWhiteSpace(theme.Typography.FontFamily))
            {
                result.FontFamily = theme.Typography.FontFamily;
            }
            if (theme.Typography.FontSize > 0)
            {
                result.FontSize = theme.Typography.FontSize;
            }
        }

        // Override with skin typography if available (skin wins)
        if (skin.Typography != null)
        {
            if (!string.IsNullOrWhiteSpace(skin.Typography.FontFamily))
            {
                result.FontFamily = skin.Typography.FontFamily;
            }
            if (skin.Typography.FontSize > 0)
            {
                result.FontSize = skin.Typography.FontSize;
            }
        }

        return result;
    }

    /// <summary>
    /// Creates a Font object from the effective typography settings.
    /// </summary>
    /// <param name="style">Optional font style (Bold, Italic, etc.).</param>
    /// <returns>A Font object based on theme/skin typography.</returns>
    public static Font CreateFont(FontStyle style = FontStyle.Regular)
    {
        var typography = GetEffectiveTypography();
        try
        {
            return new Font(typography.FontFamily, typography.FontSize, style);
        }
        catch
        {
            // Fallback if font family is invalid
            return new Font("Segoe UI", typography.FontSize, style);
        }
    }

    /// <summary>
    /// Occurs when the current theme is changed.
    /// </summary>
    public static event EventHandler? ThemeChanged;

    /// <summary>
    /// Occurs when a validation error is encountered during theme or skin loading.
    /// Subscribe to this event to log or display validation errors.
    /// </summary>
    public static event EventHandler<ValidationEventArgs>? ValidationError;

    /// <summary>
    /// Gets or sets whether to enable diagnostic mode which logs all validation errors.
    /// Default is false. Set to true during development to see detailed validation messages.
    /// </summary>
    public static bool DiagnosticsEnabled { get; set; }

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
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>True if loaded successfully; otherwise, false.</returns>
    public static async Task<bool> LoadConfigurationFromAsync(string filePath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        
        try
        {
            if (File.Exists(filePath))
            {
                var json = await File.ReadAllTextAsync(filePath, cancellationToken).ConfigureAwait(false);
                var config = JsonSerializer.Deserialize<ThemingConfiguration>(json);
                if (config != null)
                {
                    await _configLock.WaitAsync(cancellationToken).ConfigureAwait(false);
                    try
                    {
                        _config = config;
                    }
                    finally
                    {
                        _configLock.Release();
                    }
                    ThemeChanged?.Invoke(null, EventArgs.Empty);
                    return true;
                }
            }
        }
        catch (IOException)
        {
            // File access error - return false
        }
        catch (JsonException)
        {
            // Invalid JSON - return false
        }
        catch (UnauthorizedAccessException)
        {
            // Permission denied - return false
        }
        catch (OperationCanceledException)
        {
            // Operation was cancelled - return false
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

    /// <summary>
    /// Loads the configuration and all theme/skin files from the Skins directory synchronously.
    /// Called from the static constructor.
    /// </summary>
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
                            
                            // Validate theme
                            var errors = ThemeValidator.ValidateTheme(themeName, themeDef);
                            if (errors.Count > 0)
                            {
                                RaiseValidationErrors($"Theme '{themeName}' ({themeFile})", errors);
                                if (!DiagnosticsEnabled)
                                {
                                    continue; // Skip invalid themes in production
                                }
                            }
                            
                            _config.Themes[themeName] = themeDef;
                        }
                    }
                    catch (JsonException ex)
                    {
                        RaiseValidationError($"Theme file '{themeFile}': Invalid JSON - {ex.Message}");
                        // Skip invalid theme files
                    }
                    catch (IOException)
                    {
                        // Skip inaccessible theme files
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
                            
                            // Validate skin
                            var errors = ThemeValidator.ValidateSkin(skinName, skinDef);
                            if (errors.Count > 0)
                            {
                                RaiseValidationErrors($"Skin '{skinName}' ({skinFile})", errors);
                                if (!DiagnosticsEnabled)
                                {
                                    continue; // Skip invalid skins in production
                                }
                            }
                            
                            _config.Skins[skinName] = skinDef;
                        }
                    }
                    catch (JsonException ex)
                    {
                        RaiseValidationError($"Skin file '{skinFile}': Invalid JSON - {ex.Message}");
                        // Skip invalid skin files
                    }
                    catch (IOException)
                    {
                        // Skip inaccessible skin files
                    }
                }

                // Resolve inheritance after all themes and skins are loaded
                ResolveThemeInheritance();
                ResolveSkinInheritance();
            }
        }
        catch (IOException)
        {
            // Use defaults on file system errors
            _config = new ThemingConfiguration();
        }
        catch (UnauthorizedAccessException)
        {
            // Use defaults on permission errors
            _config = new ThemingConfiguration();
        }
    }

    /// <summary>
    /// Reloads the configuration and all theme/skin files from the Skins directory asynchronously.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task that completes when the configuration is reloaded.</returns>
    public static async Task ReloadConfigurationAsync(CancellationToken cancellationToken = default)
    {
        await _configLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var newConfig = new ThemingConfiguration();
            
            // Load base configuration (version, currentTheme, and currentSkin)
            if (File.Exists(_configPath))
            {
                var json = await File.ReadAllTextAsync(_configPath, cancellationToken).ConfigureAwait(false);
                newConfig = JsonSerializer.Deserialize<ThemingConfiguration>(json) ?? new ThemingConfiguration();
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
                        var themeJson = await File.ReadAllTextAsync(themeFile, cancellationToken).ConfigureAwait(false);
                        var themeDef = JsonSerializer.Deserialize<ThemeDefinition>(themeJson);
                        if (themeDef != null)
                        {
                            var themeName = Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(themeFile));
                            newConfig.Themes[themeName] = themeDef;
                        }
                    }
                    catch (JsonException)
                    {
                        // Skip invalid theme files
                    }
                    catch (IOException)
                    {
                        // Skip inaccessible theme files
                    }
                }

                // Load individual skin files (*.skin.json)
                var skinFiles = Directory.GetFiles(skinsDir, "*.skin.json");
                foreach (var skinFile in skinFiles)
                {
                    try
                    {
                        var skinJson = await File.ReadAllTextAsync(skinFile, cancellationToken).ConfigureAwait(false);
                        var skinDef = JsonSerializer.Deserialize<SkinDefinition>(skinJson);
                        if (skinDef != null)
                        {
                            var skinName = Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(skinFile));
                            newConfig.Skins[skinName] = skinDef;
                        }
                    }
                    catch (JsonException)
                    {
                        // Skip invalid skin files
                    }
                    catch (IOException)
                    {
                        // Skip inaccessible skin files
                    }
                }

                // Resolve inheritance after all themes and skins are loaded
                ResolveThemeInheritance(newConfig);
                ResolveSkinInheritance(newConfig);
            }

            _config = newConfig;
            ThemeChanged?.Invoke(null, EventArgs.Empty);
        }
        catch (IOException)
        {
            // Keep current configuration on errors
        }
        catch (UnauthorizedAccessException)
        {
            // Keep current configuration on errors
        }
        finally
        {
            _configLock.Release();
        }
    }

    /// <summary>
    /// Resolves theme inheritance, merging derived themes with their base themes.
    /// </summary>
    /// <param name="config">The configuration to resolve. If null, uses the current configuration.</param>
    private static void ResolveThemeInheritance(ThemingConfiguration? config = null)
    {
        config ??= _config;
        var resolved = new Dictionary<string, ThemeDefinition>();
        var resolving = new HashSet<string>();

        ThemeDefinition ResolveTheme(string themeName)
        {
            if (resolved.TryGetValue(themeName, out var cachedTheme))
                return cachedTheme;

            if (!resolving.Add(themeName))
                throw new InvalidOperationException($"Circular inheritance detected for theme '{themeName}'");

            if (!config.Themes.TryGetValue(themeName, out var theme))
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

        var themeNames = config.Themes.Keys.ToList();
        foreach (var themeName in themeNames)
        {
            try
            {
                config.Themes[themeName] = ResolveTheme(themeName);
            }
            catch (InvalidOperationException)
            {
                // If inheritance fails, keep the original theme
            }
        }
    }

    /// <summary>
    /// Resolves skin inheritance, merging derived skins with their base skins.
    /// </summary>
    /// <param name="config">The configuration to resolve. If null, uses the current configuration.</param>
    private static void ResolveSkinInheritance(ThemingConfiguration? config = null)
    {
        config ??= _config;
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

            if (!config.Skins.TryGetValue(skinName, out var skin))
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
        var skinNames = config.Skins.Keys.ToList();
        foreach (var skinName in skinNames)
        {
            try
            {
                config.Skins[skinName] = ResolveSkin(skinName);
            }
            catch (InvalidOperationException)
            {
                // If inheritance fails, keep the original skin
            }
        }
    }

    /// <summary>
    /// Saves the current configuration to the skins.json file synchronously.
    /// </summary>
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
        catch (IOException)
        {
            // Ignore save errors - configuration changes remain in memory
        }
        catch (UnauthorizedAccessException)
        {
            // Ignore permission errors - configuration changes remain in memory
        }
    }

    /// <summary>
    /// Saves the current configuration to the skins.json file asynchronously.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task that completes when the configuration is saved.</returns>
    public static async Task SaveConfigurationAsync(CancellationToken cancellationToken = default)
    {
        await _configLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var dir = Path.GetDirectoryName(_configPath);
            if (dir != null && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var json = JsonSerializer.Serialize(_config, _jsonOptions);
            await File.WriteAllTextAsync(_configPath, json, cancellationToken).ConfigureAwait(false);
        }
        catch (IOException)
        {
            // Ignore save errors - configuration changes remain in memory
        }
        catch (UnauthorizedAccessException)
        {
            // Ignore permission errors - configuration changes remain in memory
        }
        catch (OperationCanceledException)
        {
            // Operation was cancelled
        }
        finally
        {
            _configLock.Release();
        }
    }

    private static void RaiseValidationError(string message)
    {
        ValidationError?.Invoke(null, new ValidationEventArgs(message));
    }

    private static void RaiseValidationErrors(string context, List<string> errors)
    {
        foreach (var error in errors)
        {
            RaiseValidationError($"{context}: {error}");
        }
    }
}

/// <summary>
/// Event arguments for validation errors.
/// </summary>
public class ValidationEventArgs : EventArgs
{
    /// <summary>
    /// Gets the validation error message.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Initializes a new instance of the ValidationEventArgs class.
    /// </summary>
    /// <param name="message">The validation error message.</param>
    public ValidationEventArgs(string message)
    {
        Message = message;
    }
}
