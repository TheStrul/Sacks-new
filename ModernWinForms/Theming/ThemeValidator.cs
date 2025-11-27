using System.Text.RegularExpressions;

namespace ModernWinForms.Theming;

/// <summary>
/// Provides validation for theme and skin configurations.
/// </summary>
internal static partial class ThemeValidator
{
    [GeneratedRegex(@"^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3}|[A-Fa-f0-9]{8})$")]
    private static partial Regex HexColorRegex();

    /// <summary>
    /// Validates a theme definition and returns validation errors.
    /// </summary>
    /// <param name="themeName">The name of the theme being validated.</param>
    /// <param name="theme">The theme definition to validate.</param>
    /// <returns>A list of validation error messages, or empty if valid.</returns>
    public static List<string> ValidateTheme(string themeName, ThemeDefinition theme)
    {
        ArgumentNullException.ThrowIfNull(theme);
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(themeName))
        {
            errors.Add("Theme name cannot be empty.");
        }

        // Validate controls
        if (theme.Controls.Count == 0)
        {
            errors.Add($"Theme '{themeName}' has no controls defined.");
        }

        foreach (var (controlName, controlStyle) in theme.Controls)
        {
            if (string.IsNullOrWhiteSpace(controlName))
            {
                errors.Add($"Theme '{themeName}' contains a control with an empty name.");
                continue;
            }

            // Validate structural properties
            if (controlStyle.CornerRadius < 0 || controlStyle.CornerRadius > 100)
            {
                errors.Add($"Theme '{themeName}', control '{controlName}': CornerRadius must be between 0 and 100, got {controlStyle.CornerRadius}.");
            }

            if (controlStyle.BorderWidth < 0)
            {
                errors.Add($"Theme '{themeName}', control '{controlName}': BorderWidth cannot be negative.");
            }

            // Validate state colors if present
            foreach (var (stateName, stateStyle) in controlStyle.States)
            {
                ValidateStateStyle(errors, $"Theme '{themeName}', control '{controlName}', state '{stateName}'", stateStyle);
            }
        }

        return errors;
    }

    /// <summary>
    /// Validates a skin definition and returns validation errors.
    /// </summary>
    /// <param name="skinName">The name of the skin being validated.</param>
    /// <param name="skin">The skin definition to validate.</param>
    /// <returns>A list of validation error messages, or empty if valid.</returns>
    public static List<string> ValidateSkin(string skinName, SkinDefinition skin)
    {
        ArgumentNullException.ThrowIfNull(skin);
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(skinName))
        {
            errors.Add("Skin name cannot be empty.");
        }

        // Validate palette colors
        ValidateColor(errors, $"Skin '{skinName}' palette.primary", skin.Palette.Primary);
        ValidateColor(errors, $"Skin '{skinName}' palette.background", skin.Palette.Background);
        ValidateColor(errors, $"Skin '{skinName}' palette.surface", skin.Palette.Surface);
        ValidateColor(errors, $"Skin '{skinName}' palette.text", skin.Palette.Text);
        ValidateColor(errors, $"Skin '{skinName}' palette.border", skin.Palette.Border);

        // Validate controls
        if (skin.Controls.Count == 0)
        {
            errors.Add($"Skin '{skinName}' has no controls defined.");
        }

        foreach (var (controlName, controlColors) in skin.Controls)
        {
            if (string.IsNullOrWhiteSpace(controlName))
            {
                errors.Add($"Skin '{skinName}' contains a control with an empty name.");
                continue;
            }

            if (controlColors.States.Count == 0)
            {
                errors.Add($"Skin '{skinName}', control '{controlName}': No states defined.");
            }

            foreach (var (stateName, stateStyle) in controlColors.States)
            {
                if (string.IsNullOrWhiteSpace(stateName))
                {
                    errors.Add($"Skin '{skinName}', control '{controlName}': State name cannot be empty.");
                    continue;
                }

                ValidateStateStyle(errors, $"Skin '{skinName}', control '{controlName}', state '{stateName}'", stateStyle);
            }
        }

        return errors;
    }

    /// <summary>
    /// Validates a state style's color properties.
    /// </summary>
    private static void ValidateStateStyle(List<string> errors, string context, StateStyle stateStyle)
    {
        ValidateColor(errors, $"{context}.backColor", stateStyle.BackColor);
        ValidateColor(errors, $"{context}.foreColor", stateStyle.ForeColor);
        ValidateColor(errors, $"{context}.borderColor", stateStyle.BorderColor);
    }

    /// <summary>
    /// Validates a color string (hex format).
    /// </summary>
    private static void ValidateColor(List<string> errors, string context, string? color)
    {
        if (string.IsNullOrEmpty(color))
        {
            return; // Optional colors are allowed
        }

        if (!HexColorRegex().IsMatch(color))
        {
            errors.Add($"{context}: Invalid color format '{color}'. Expected hex format like #RGB, #RRGGBB, or #RRGGBBAA.");
        }
    }

    /// <summary>
    /// Validates that a color can be parsed by ColorTranslator.
    /// </summary>
    public static bool TryParseColor(string? color, out string? errorMessage)
    {
        errorMessage = null;

        if (string.IsNullOrEmpty(color))
        {
            return true; // Optional
        }

        try
        {
            ColorTranslator.FromHtml(color);
            return true;
        }
        catch (Exception ex)
        {
            errorMessage = $"Cannot parse color '{color}': {ex.Message}";
            return false;
        }
    }
}
