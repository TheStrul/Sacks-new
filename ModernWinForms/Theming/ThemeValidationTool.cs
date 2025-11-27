using System.Text;
using System.Text.Json;

namespace ModernWinForms.Theming;

/// <summary>
/// ZERO TOLERANCE theme validation tool.
/// Validates all theme and skin JSON files before application startup.
/// NO FALLBACKS - fails fast with specific errors.
/// </summary>
public static class ThemeValidationTool
{
    /// <summary>
    /// Required states that MUST be defined for every control.
    /// ZERO TOLERANCE: Missing required states = validation failure.
    /// </summary>
    private static readonly string[] RequiredStates = { "normal" };

    /// <summary>
    /// All Modern controls that MUST have theme definitions.
    /// ZERO TOLERANCE: Missing control definitions = validation failure.
    /// </summary>
    private static readonly string[] RequiredControls = 
    {
        "ModernButton",
        "ModernCheckBox",
        "ModernComboBox",
        "ModernDataGridView",
        "ModernFlowLayoutPanel",
        "ModernGroupBox",
        "ModernLabel",
        "ModernMenuStrip",
        "ModernPanel",
        "ModernRadioButton",
        "ModernRichTextBox",
        "ModernSplitContainer",
        "ModernStatusStrip",
        "ModernTableLayoutPanel",
        "ModernTabControl",
        "ModernTextBox"
    };

    /// <summary>
    /// Validates all theme and skin files in the Skins directory.
    /// ZERO TOLERANCE: Returns validation result with ALL errors found.
    /// </summary>
    /// <param name="skinsDirectory">Path to the Skins directory.</param>
    /// <returns>Validation result with detailed error information.</returns>
    public static ThemeValidationResult ValidateAll(string skinsDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(skinsDirectory);

        var result = new ThemeValidationResult();

        if (!Directory.Exists(skinsDirectory))
        {
            result.AddError($"Skins directory not found: {skinsDirectory}");
            return result;
        }

        // Load base configuration
        var configPath = Path.Combine(skinsDirectory, "skins.json");
        if (!File.Exists(configPath))
        {
            result.AddError($"Base configuration file not found: {configPath}");
            return result;
        }

        ThemingConfiguration? config;
        try
        {
            var json = File.ReadAllText(configPath);
            config = JsonSerializer.Deserialize<ThemingConfiguration>(json);
            if (config == null)
            {
                result.AddError($"Failed to parse base configuration: {configPath}");
                return result;
            }
        }
        catch (JsonException ex)
        {
            result.AddError($"Invalid JSON in base configuration: {configPath} - {ex.Message}");
            return result;
        }

        // Validate that currentTheme and currentSkin are specified
        if (string.IsNullOrWhiteSpace(config.CurrentTheme))
        {
            result.AddError("Base configuration missing required property: currentTheme");
        }
        if (string.IsNullOrWhiteSpace(config.CurrentSkin))
        {
            result.AddError("Base configuration missing required property: currentSkin");
        }

        // Validate all theme files
        var themeFiles = Directory.GetFiles(skinsDirectory, "*.theme.json");
        var themeNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        
        foreach (var themeFile in themeFiles)
        {
            var themeName = Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(themeFile));
            themeNames.Add(themeName);
            ValidateThemeFile(themeFile, result);
        }

        // Validate all skin files
        var skinFiles = Directory.GetFiles(skinsDirectory, "*.skin.json");
        var skinNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        
        foreach (var skinFile in skinFiles)
        {
            var skinName = Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(skinFile));
            skinNames.Add(skinName);
            ValidateSkinFile(skinFile, result);
        }

        // Validate inheritance references for themes
        foreach (var themeFile in themeFiles)
        {
            try
            {
                var json = File.ReadAllText(themeFile);
                var theme = JsonSerializer.Deserialize<ThemeDefinition>(json);
                if (theme != null && !string.IsNullOrWhiteSpace(theme.InheritsFrom))
                {
                    if (!themeNames.Contains(theme.InheritsFrom))
                    {
                        var themeName = Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(themeFile));
                        result.AddError($"Theme '{themeName}' inherits from '{theme.InheritsFrom}' which does not exist");
                    }
                }
            }
            catch
            {
                // Already reported as parse error earlier
            }
        }

        // Validate inheritance references for skins
        foreach (var skinFile in skinFiles)
        {
            try
            {
                var json = File.ReadAllText(skinFile);
                var skin = JsonSerializer.Deserialize<SkinDefinition>(json);
                if (skin != null && !string.IsNullOrWhiteSpace(skin.InheritsFrom))
                {
                    if (!skinNames.Contains(skin.InheritsFrom))
                    {
                        var skinName = Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(skinFile));
                        result.AddError($"Skin '{skinName}' inherits from '{skin.InheritsFrom}' which does not exist");
                    }
                }
            }
            catch
            {
                // Already reported as parse error earlier
            }
        }

        // Validate that currentTheme exists
        if (!string.IsNullOrWhiteSpace(config.CurrentTheme))
        {
            var themeExists = themeFiles.Any(f => 
                Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(f))
                    .Equals(config.CurrentTheme, StringComparison.OrdinalIgnoreCase));
            
            if (!themeExists)
            {
                result.AddError($"Current theme '{config.CurrentTheme}' specified in skins.json does not exist");
            }
        }

        // Validate that currentSkin exists
        if (!string.IsNullOrWhiteSpace(config.CurrentSkin))
        {
            var skinExists = skinFiles.Any(f => 
                Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(f))
                    .Equals(config.CurrentSkin, StringComparison.OrdinalIgnoreCase));
            
            if (!skinExists)
            {
                result.AddError($"Current skin '{config.CurrentSkin}' specified in skins.json does not exist");
            }
        }

        return result;
    }

    /// <summary>
    /// Validates a single theme file.
    /// ZERO TOLERANCE: Checks structure, required controls, and required states.
    /// </summary>
    private static void ValidateThemeFile(string themeFile, ThemeValidationResult result
)
    {
        var themeName = Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(themeFile));
        result.IncrementFilesValidated($"Validating theme: {themeName} ({Path.GetFileName(themeFile)})");

        try
        {
            var json = File.ReadAllText(themeFile);
            var theme = JsonSerializer.Deserialize<ThemeDefinition>(json);
            
            if (theme == null)
            {
                result.AddError($"  Failed to parse theme file: {themeFile}");
                return;
            }

            // Validate typography
            if (theme.Typography != null)
            {
                if (string.IsNullOrWhiteSpace(theme.Typography.FontFamily))
                {
                    result.AddWarning($"  Theme '{themeName}' typography missing fontFamily (will use default)");
                }
                if (theme.Typography.FontSize <= 0)
                {
                    result.AddWarning($"  Theme '{themeName}' typography has invalid fontSize: {theme.Typography.FontSize} (will use default)");
                }
            }

            // Validate that all required controls are defined
            foreach (var controlName in RequiredControls)
            {
                if (!theme.Controls.ContainsKey(controlName))
                {
                    result.AddError($"  Theme '{themeName}' missing required control: {controlName}");
                    continue;
                }

                var controlStyle = theme.Controls[controlName];
                
                // Validate structural properties
                if (controlStyle.CornerRadius < 0)
                {
                    result.AddError($"  Theme '{themeName}' control '{controlName}' has invalid cornerRadius: {controlStyle.CornerRadius}");
                }
                if (controlStyle.BorderWidth < 0)
                {
                    result.AddError($"  Theme '{themeName}' control '{controlName}' has invalid borderWidth: {controlStyle.BorderWidth}");
                }

                // Validate that required states exist (even if empty in theme)
                foreach (var requiredState in RequiredStates)
                {
                    if (!controlStyle.States.ContainsKey(requiredState))
                    {
                        result.AddError($"  Theme '{themeName}' control '{controlName}' missing required state: {requiredState}");
                    }
                }
            }
        }
        catch (JsonException ex)
        {
            result.AddError($"  Invalid JSON in theme file: {themeFile} - {ex.Message}");
        }
        catch (Exception ex)
        {
            result.AddError($"  Failed to validate theme file: {themeFile} - {ex.Message}");
        }
    }

    /// <summary>
    /// Validates a single skin file.
    /// ZERO TOLERANCE: Checks colors, required states, and color format.
    /// Base skins MUST have all controls. Inherited skins can be partial.
    /// </summary>
    private static void ValidateSkinFile(string skinFile, ThemeValidationResult result)
    {
        var skinName = Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(skinFile));
        var isBaseSkin = skinName.StartsWith("Base", StringComparison.OrdinalIgnoreCase);
        
        result.IncrementFilesValidated($"Validating skin: {skinName} ({Path.GetFileName(skinFile)})");

        try
        {
            var json = File.ReadAllText(skinFile);
            var skin = JsonSerializer.Deserialize<SkinDefinition>(json);
            
            if (skin == null)
            {
                result.AddError($"  Failed to parse skin file: {skinFile}");
                return;
            }

            // Check if this skin inherits from another skin
            var inherits = !string.IsNullOrWhiteSpace(skin.InheritsFrom);
            
            if (isBaseSkin)
            {
                result.AddContext($"  Base skin '{skinName}' - must have all controls");
            }
            else if (inherits)
            {
                result.AddContext($"  Skin '{skinName}' inherits from '{skin.InheritsFrom}' (partial validation)");
            }

            // Validate palette colors (if defined) - COMPLETE validation
            if (skin.Palette != null)
            {
                ValidateColor(skin.Palette.Primary, "palette.primary", skinName, result);
                ValidateColor(skin.Palette.Secondary, "palette.secondary", skinName, result);
                ValidateColor(skin.Palette.Background, "palette.background", skinName, result);
                ValidateColor(skin.Palette.Surface, "palette.surface", skinName, result);
                ValidateColor(skin.Palette.Text, "palette.text", skinName, result);
                ValidateColor(skin.Palette.Border, "palette.border", skinName, result);
                ValidateColor(skin.Palette.Success, "palette.success", skinName, result);
                ValidateColor(skin.Palette.Danger, "palette.danger", skinName, result);
                ValidateColor(skin.Palette.Warning, "palette.warning", skinName, result);
                ValidateColor(skin.Palette.Info, "palette.info", skinName, result);
                ValidateColor(skin.Palette.Error, "palette.error", skinName, result);
            }

            // Base skins and standalone skins must have ALL controls
            // Inherited skins only validate controls they define
            if (isBaseSkin || !inherits)
            {
                // Base or standalone skin - must have ALL required controls
                foreach (var controlName in RequiredControls)
                {
                    if (!skin.Controls.ContainsKey(controlName))
                    {
                        result.AddError($"  Skin '{skinName}' missing required control: {controlName}");
                        continue;
                    }

                    ValidateControlColors(skin, controlName, skinName, result);
                }
            }
            else
            {
                // Inherited skin - only validate controls that ARE defined
                foreach (var controlEntry in skin.Controls)
                {
                    ValidateControlColors(skin, controlEntry.Key, skinName, result);
                }
            }
        }
        catch (JsonException ex)
        {
            result.AddError($"  Invalid JSON in skin file: {skinFile} - {ex.Message}");
        }
        catch (Exception ex)
        {
            result.AddError($"  Failed to validate skin file: {skinFile} - {ex.Message}");
        }
    }

    /// <summary>
    /// Validates color definitions for a specific control in a skin.
    /// </summary>
    private static void ValidateControlColors(SkinDefinition skin, string controlName, string skinName, ThemeValidationResult result)
    {
        if (!skin.Controls.TryGetValue(controlName, out var controlStyle))
        {
            return;
        }

        // Validate that 'normal' state exists and has required colors (if control is defined)
        if (!controlStyle.States.ContainsKey("normal"))
        {
            result.AddError($"  Skin '{skinName}' control '{controlName}' missing required state: normal");
            return;
        }

        var normalState = controlStyle.States["normal"];

        // ZERO TOLERANCE: normal state MUST have all required colors
        if (string.IsNullOrWhiteSpace(normalState.BackColor))
        {
            result.AddError($"  Skin '{skinName}' control '{controlName}' normal state missing required color: backColor");
        }
        else
        {
            ValidateColor(normalState.BackColor, $"{controlName}.normal.backColor", skinName, result);
        }

        if (string.IsNullOrWhiteSpace(normalState.ForeColor))
        {
            result.AddError($"  Skin '{skinName}' control '{controlName}' normal state missing required color: foreColor");
        }
        else
        {
            ValidateColor(normalState.ForeColor, $"{controlName}.normal.foreColor", skinName, result);
        }

        if (string.IsNullOrWhiteSpace(normalState.BorderColor))
        {
            result.AddError($"  Skin '{skinName}' control '{controlName}' normal state missing required color: borderColor");
        }
        else
        {
            ValidateColor(normalState.BorderColor, $"{controlName}.normal.borderColor", skinName, result);
        }

        // Validate other states' colors (if present)
        foreach (var (stateName, stateStyle) in controlStyle.States)
        {
            if (stateName == "normal") continue; // Already validated

            ValidateColor(stateStyle.BackColor, $"{controlName}.{stateName}.backColor", skinName, result);
            ValidateColor(stateStyle.ForeColor, $"{controlName}.{stateName}.foreColor", skinName, result);
            ValidateColor(stateStyle.BorderColor, $"{controlName}.{stateName}.borderColor", skinName, result);
        }
    }

    /// <summary>
    /// Validates a color string (hex format).
    /// ZERO TOLERANCE: Invalid color format = validation error.
    /// </summary>
    private static void ValidateColor(string? color, string propertyPath, string skinName, ThemeValidationResult result)
    {
        if (string.IsNullOrWhiteSpace(color))
        {
            return; // Color not defined (allowed for optional properties)
        }

        // Validate hex color format: #RGB, #RRGGBB, or #AARRGGBB
        if (!color.StartsWith('#'))
        {
            result.AddError($"  Skin '{skinName}' property '{propertyPath}' invalid color format: '{color}' (must start with #)");
            return;
        }

        var hexPart = color[1..];
        if (hexPart.Length != 3 && hexPart.Length != 6 && hexPart.Length != 8)
        {
            result.AddError($"  Skin '{skinName}' property '{propertyPath}' invalid color format: '{color}' (must be #RGB, #RRGGBB, or #AARRGGBB)");
            return;
        }

        if (!hexPart.All(c => Uri.IsHexDigit(c)))
        {
            result.AddError($"  Skin '{skinName}' property '{propertyPath}' invalid color format: '{color}' (contains non-hex characters)");
        }
    }

    /// <summary>
    /// Displays validation results in a formatted message box.
    /// ZERO TOLERANCE: If validation fails, show all errors and exit.
    /// </summary>
    public static void DisplayValidationResults(ThemeValidationResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (result.IsValid)
        {
            MessageBox.Show(
                $"[OK] Theme validation PASSED!\n\n" +
                $"Validated: {result.FilesValidated} files\n" +
                $"Warnings: {result.Warnings.Count}\n\n" +
                (result.Warnings.Count > 0 ? "See log for warnings." : "No issues found."),
                "Theme Validation - SUCCESS",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        else
        {
            var sb = new StringBuilder();
            sb.AppendLine("[FAIL] THEME VALIDATION FAILED!");
            sb.AppendLine();
            sb.AppendLine($"Errors found: {result.Errors.Count}");
            sb.AppendLine($"Warnings: {result.Warnings.Count}");
            sb.AppendLine();
            sb.AppendLine("ERRORS:");
            
            foreach (var error in result.Errors.Take(20)) // Show first 20 errors
            {
                sb.AppendLine($"  * {error}");
            }

            if (result.Errors.Count > 20)
            {
                sb.AppendLine($"  ... and {result.Errors.Count - 20} more errors");
            }

            MessageBox.Show(
                sb.ToString(),
                "Theme Validation - FAILED",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }
}

/// <summary>
/// Result of theme validation.
/// Contains all errors, warnings, and context information.
/// </summary>
public sealed class ThemeValidationResult
{
    /// <summary>
    /// Gets the list of validation errors.
    /// ZERO TOLERANCE: Any errors = validation failed.
    /// </summary>
    public List<string> Errors { get; } = new();

    /// <summary>
    /// Gets the list of validation warnings.
    /// Warnings don't fail validation but should be addressed.
    /// </summary>
    public List<string> Warnings { get; } = new();

    /// <summary>
    /// Gets the list of context messages (informational).
    /// </summary>
    public List<string> Context { get; } = new();

    /// <summary>
    /// Gets whether validation passed.
    /// ZERO TOLERANCE: Any error = validation failed.
    /// </summary>
    public bool IsValid => Errors.Count == 0;

    /// <summary>
    /// Gets the number of files validated.
    /// </summary>
    public int FilesValidated { get; private set; }

    /// <summary>
    /// Adds a validation error.
    /// </summary>
    public void AddError(string error)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(error);
        Errors.Add(error);
    }

    /// <summary>
    /// Adds a validation warning.
    /// </summary>
    public void AddWarning(string warning)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(warning);
        Warnings.Add(warning);
    }

    /// <summary>
    /// Adds a context message (informational only).
    /// </summary>
    public void AddContext(string context)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(context);
        Context.Add(context);
    }

    /// <summary>
    /// Increments the file counter and adds a context message for file validation.
    /// </summary>
    internal void IncrementFilesValidated(string context)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(context);
        Context.Add(context);
        FilesValidated++;
    }

    /// <summary>
    /// Gets a formatted report of all validation results.
    /// </summary>
    public string GetFullReport()
    {
        var sb = new StringBuilder();
        sb.AppendLine("=====================================");
        sb.AppendLine("  THEME VALIDATION REPORT");
        sb.AppendLine("=====================================");
        sb.AppendLine();
        sb.AppendLine($"Status: {(IsValid ? "[PASS]" : "[FAIL]")}");
        sb.AppendLine($"Files Validated: {FilesValidated}");
        sb.AppendLine($"Errors: {Errors.Count}");
        sb.AppendLine($"Warnings: {Warnings.Count}");
        sb.AppendLine();

        if (Context.Count > 0)
        {
            sb.AppendLine("CONTEXT:");
            foreach (var ctx in Context)
            {
                sb.AppendLine($"  {ctx}");
            }
            sb.AppendLine();
        }

        if (Errors.Count > 0)
        {
            sb.AppendLine("ERRORS:");
            foreach (var error in Errors)
            {
                sb.AppendLine($"  * {error}");
            }
            sb.AppendLine();
        }

        if (Warnings.Count > 0)
        {
            sb.AppendLine("WARNINGS:");
            foreach (var warning in Warnings)
            {
                sb.AppendLine($"  ! {warning}");
            }
            sb.AppendLine();
        }

        sb.AppendLine("=====================================");
        return sb.ToString();
    }
}
