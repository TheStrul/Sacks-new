using System.Text.Json;
using ModernWinForms.Controls;

namespace ModernWinForms.Theming;

/// <summary>
/// Global manager for application skins and themes.
/// Provides centralized theme management and automatic control styling.
/// </summary>
public static class ThemeManager
{
    private static ThemingConfiguration _config = null!;
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
        get
        {
            _configLock.Wait();
            try
            {
                return _config.CurrentTheme;
            }
            finally
            {
                _configLock.Release();
            }
        }
        set
        {
            _configLock.Wait();
            try
            {
                if (_config.CurrentTheme != value)
                {
                    _config.CurrentTheme = value;
                    SaveConfiguration();
                }
            }
            finally
            {
                _configLock.Release();
            }
            // Invoke event outside lock to prevent deadlocks
            ThemeChanged?.Invoke(null, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Gets or sets the current active skin name (color variant, e.g., "Light", "Dark").
    /// Setting this property triggers a ThemeChanged event.
    /// </summary>
    public static string CurrentSkin
    {
        get
        {
            _configLock.Wait();
            try
            {
                return _config.CurrentSkin;
            }
            finally
            {
                _configLock.Release();
            }
        }
        set
        {
            _configLock.Wait();
            try
            {
                if (_config.CurrentSkin != value)
                {
                    _config.CurrentSkin = value;
                    SaveConfiguration();
                }
            }
            finally
            {
                _configLock.Release();
            }
            // Invoke event outside lock to prevent deadlocks
            ThemeChanged?.Invoke(null, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Gets the current active theme definition (design system).
    /// </summary>
    public static ThemeDefinition CurrentThemeDefinition
    {
        get
        {
            try
            {
                return _config.Themes.TryGetValue(_config.CurrentTheme, out var theme) ? theme : new ThemeDefinition();
            }
            catch
            {
                return new ThemeDefinition();
            }
        }
    }

    /// <summary>
    /// Gets the current active skin definition (color variant).
    /// </summary>
    public static SkinDefinition CurrentSkinDefinition
    {
        get
        {
            try
            {
                return _config.Skins.TryGetValue(_config.CurrentSkin, out var skin) ? skin : new SkinDefinition();
            }
            catch
            {
                return new SkinDefinition();
            }
        }
    }

    /// <summary>
    /// Gets all available theme names (design systems).
    /// </summary>
    public static IEnumerable<string> AvailableThemes => _config.Themes.Keys;

    /// <summary>
    /// Gets all available skin names (color variants).
    /// </summary>
    public static IEnumerable<string> AvailableSkins => _config.Skins
            .Where(kvp => !IsBaseSkinName(kvp.Key) && !IsTemplateSkin(kvp.Value))
            .Select(kvp => kvp.Key);

    /// <summary>
    /// Gets the skins available for the current theme.
    /// </summary>
    public static IEnumerable<string> AvailableSkinsForCurrentTheme =>
        _config.Skins
            .Where(kvp => !IsBaseSkinName(kvp.Key) && !IsTemplateSkin(kvp.Value) && (string.IsNullOrEmpty(kvp.Value.Theme) || string.Equals(kvp.Value.Theme, _config.CurrentTheme, StringComparison.OrdinalIgnoreCase)))
            .Select(kvp => kvp.Key);

    private static bool HasPaletteColors(ColorPalette? palette)
    {
        if (palette == null) return false;
        return !string.IsNullOrWhiteSpace(palette.Background)
            || !string.IsNullOrWhiteSpace(palette.Surface)
            || !string.IsNullOrWhiteSpace(palette.Text)
            || !string.IsNullOrWhiteSpace(palette.Primary)
            || !string.IsNullOrWhiteSpace(palette.Secondary)
            || !string.IsNullOrWhiteSpace(palette.Border)
            || !string.IsNullOrWhiteSpace(palette.Info)
            || !string.IsNullOrWhiteSpace(palette.Warning)
            || !string.IsNullOrWhiteSpace(palette.Danger)
            || !string.IsNullOrWhiteSpace(palette.Success);
    }

    private static bool IsTemplateSkin(SkinDefinition? skin)
    {
        if (skin == null) return true;
        var hasPalette = HasPaletteColors(skin.Palette);
        var hasControls = skin.Controls != null && skin.Controls.Count > 0;
        return !hasPalette && !hasControls;
    }

    private static bool IsBaseSkinName(string? skinName)
    {
        if (string.IsNullOrWhiteSpace(skinName)) return false;
        return skinName.StartsWith("Base", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets a complete control style by merging theme structure with skin colors.
    /// Applies automatic palette mappings if defined in theme.
    /// </summary>
    /// <param name="controlName">The name of the control (e.g., "ModernButton").</param>
    /// <returns>A complete ControlStyle with both structure and colors, or null if not found.</returns>
    public static ControlStyle? GetControlStyle(string controlName)
    {
        var theme = CurrentThemeDefinition;
        var skin = CurrentSkinDefinition;

        // Start with theme's structural definition
        ControlStyle? result = null;
        if (theme != null && theme.Controls != null && theme.Controls.TryGetValue(controlName, out var themeStyle))
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

        // Apply palette mappings from theme (if defined)
        if (theme?.PaletteMappings != null && theme.PaletteMappings.TryGetValue(controlName, out var paletteMapping) && skin?.Palette != null)
        {
            foreach (var (stateName, stateMapping) in paletteMapping.States)
            {
                if (!result.States.ContainsKey(stateName))
                {
                    result.States[stateName] = new StateStyle();
                }

                var state = result.States[stateName];
                
                // Apply palette colors if not already set
                if (string.IsNullOrEmpty(state.BackColor) && !string.IsNullOrEmpty(stateMapping.BackColor))
                {
                    state.BackColor = GetPaletteColor(skin.Palette, stateMapping.BackColor);
                }
                if (string.IsNullOrEmpty(state.ForeColor) && !string.IsNullOrEmpty(stateMapping.ForeColor))
                {
                    state.ForeColor = GetPaletteColor(skin.Palette, stateMapping.ForeColor);
                }
                if (string.IsNullOrEmpty(state.BorderColor) && !string.IsNullOrEmpty(stateMapping.BorderColor))
                {
                    state.BorderColor = GetPaletteColor(skin.Palette, stateMapping.BorderColor);
                }
            }
        }

        // Override with skin's explicit color definitions (these take precedence)
        if (skin != null && skin.Controls != null && skin.Controls.TryGetValue(controlName, out var skinColors))
        {
            foreach (var (stateName, stateStyle) in skinColors.States)
            {
                result.States[stateName] = stateStyle;
            }
        }

        return result;
    }

    /// <summary>
    /// Gets a color from the palette by key name.
    /// </summary>
    private static string? GetPaletteColor(ColorPalette palette, string key)
    {
        return key.ToLowerInvariant() switch
        {
            "primary" => palette.Primary,
            "secondary" => palette.Secondary,
            "background" => palette.Background,
            "surface" => palette.Surface,
            "text" => palette.Text,
            "border" => palette.Border,
            "success" => palette.Success,
            "warning" => palette.Warning,
            "danger" => palette.Danger,
            "error" => palette.Error,
            "info" => palette.Info,
            _ => null
        };
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
        if (theme?.Typography != null)
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
        if (skin?.Typography != null)
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
        if (skin?.Palette != null && !string.IsNullOrEmpty(skin.Palette.Background))
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
    /// Sets the current theme and skin using type-safe enums.
    /// This is the recommended API for switching themes.
    /// </summary>
    /// <param name="theme">The theme to activate.</param>
    /// <param name="skin">The skin/color variant to activate.</param>
    /// <example>
    /// <code>
    /// ThemeManager.SetTheme(Theme.GitHub, Skin.Dracula);
    /// ThemeManager.SetTheme(Theme.Material, Skin.Nord);
    /// </code>
    /// </example>
    public static void SetTheme(Theme theme, Skin skin)
    {
        var themeName = theme.ToString();
        var skinName = skin switch
        {
            Skin.BaseLight => "BaseLight",
            Skin.BaseDark => "BaseDark",
            Skin.SolarizedLight => "Solarized Light",
            Skin.SolarizedDark => "Solarized Dark",
            _ => skin.ToString()
        };

        _configLock.Wait();
        try
        {
            bool changed = false;
            
            if (_config.CurrentTheme != themeName)
            {
                _config.CurrentTheme = themeName;
                changed = true;
            }
            
            if (_config.CurrentSkin != skinName)
            {
                _config.CurrentSkin = skinName;
                changed = true;
            }
            
            if (changed)
            {
                SaveConfiguration();
            }
        }
        finally
        {
            _configLock.Release();
        }
        
        // Invoke event outside lock to prevent deadlocks
        ThemeChanged?.Invoke(null, EventArgs.Empty);
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

    private static void ApplyThemeToControls(Control.ControlCollection controls, SkinDefinition? skin)
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

    private static void ApplyThemeToControl(Control control, SkinDefinition? skin)
    {
        switch (control)
        {
            case ModernButton modernButton:
                modernButton.ApplySkin(skin ?? new SkinDefinition());
                break;
            case ModernGroupBox modernGroupBox:
                modernGroupBox.ApplySkin(skin ?? new SkinDefinition());
                break;
            case ModernTextBox modernTextBox:
                modernTextBox.ApplySkin(skin ?? new SkinDefinition());
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
            ThemingConfiguration? loadedConfig = null;

            // Load base configuration (version, currentTheme, and currentSkin)
            if (!File.Exists(_configPath))
            {
                throw new InvalidOperationException($"Theme configuration file not found: {_configPath}. Cannot initialize theme system.");
            }

            var json = File.ReadAllText(_configPath);
            loadedConfig = JsonSerializer.Deserialize<ThemingConfiguration>(json);
            
            if (loadedConfig == null)
            {
                throw new InvalidOperationException($"Failed to parse theme configuration from: {_configPath}");
            }

            var newConfig = loadedConfig;

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
                            
                            newConfig.Themes[themeName] = themeDef;
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
                            
                            newConfig.Skins[skinName] = skinDef;
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
                ResolveThemeInheritance(newConfig);
                ResolveSkinInheritance(newConfig);
            }
            
            _config = newConfig;
        }
        catch (IOException ex)
        {
            throw new InvalidOperationException($"Failed to load theme configuration due to I/O error: {_configPath}", ex);
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new InvalidOperationException($"Access denied when loading theme configuration: {_configPath}", ex);
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
            // Load base configuration (version, currentTheme, and currentSkin)
            if (!File.Exists(_configPath))
            {
                throw new InvalidOperationException($"Theme configuration file not found: {_configPath}. Cannot reload theme system.");
            }

            var json = await File.ReadAllTextAsync(_configPath, cancellationToken).ConfigureAwait(false);
            var loadedConfig = JsonSerializer.Deserialize<ThemingConfiguration>(json);
            
            if (loadedConfig == null)
            {
                throw new InvalidOperationException($"Failed to parse theme configuration from: {_configPath}");
            }

            var newConfig = loadedConfig;

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
        var resolved = new Dictionary<string, ThemeDefinition>(StringComparer.OrdinalIgnoreCase);
        var resolving = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

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
        var resolved = new Dictionary<string, SkinDefinition>(StringComparer.OrdinalIgnoreCase);
        var resolving = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        SkinDefinition ResolveSkin(string skinName)
        {
            if (resolved.TryGetValue(skinName, out var cachedSkin))
                return cachedSkin;

            // Detect circular dependency
            if (!resolving.Add(skinName))
            {
                RaiseValidationError($"Circular inheritance detected for skin '{skinName}'");
                throw new InvalidOperationException($"Circular inheritance detected for skin '{skinName}'");
            }

            if (!config.Skins.TryGetValue(skinName, out var skin))
            {
                RaiseValidationError($"Skin '{skinName}' not found during inheritance resolution.");
                throw new InvalidOperationException($"Skin '{skinName}' not found");
            }

            try
            {
                // If no inheritance, use as-is
                if (string.IsNullOrEmpty(skin.InheritsFrom))
                {
                    resolved[skinName] = skin;
                    return skin;
                }

                // Resolve parent first - handle case where parent doesn't exist
                if (!config.Skins.TryGetValue(skin.InheritsFrom, out _))
                {
                    RaiseValidationError($"Base skin '{skin.InheritsFrom}' not found for derived skin '{skinName}'.");
                    resolved[skinName] = skin; // Use the skin as-is without inheritance
                    return skin;
                }

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

        // Resolve all skins - wrap in try-catch to prevent startup failures
        var skinNames = config.Skins.Keys.ToList();
        foreach (var skinName in skinNames)
        {
            try
            {
                config.Skins[skinName] = ResolveSkin(skinName);
            }
            catch (Exception ex)
            {
                // If inheritance resolution fails completely, keep the original skin
                RaiseValidationError($"Failed to resolve inheritance for skin '{skinName}': {ex.Message}");
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
        catch (OperationCanceledException ex)
        {
            throw new InvalidOperationException("Theme configuration reload was cancelled.", ex);
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

    /// <summary>
    /// Cleans up static resources used by ThemeManager.
    /// Should be called during application shutdown to ensure proper resource disposal.
    /// </summary>
    public static void Cleanup()
    {
        _configLock?.Dispose();
        GraphicsPathPool.Clear();
        ColorCache.Clear();
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
