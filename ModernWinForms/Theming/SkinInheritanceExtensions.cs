using System.Text.Json;

namespace ModernWinForms.Theming;

/// <summary>
/// Extension methods for merging skin and theme definitions with inheritance.
/// </summary>
internal static class SkinInheritanceExtensions
{
    /// <summary>
    /// Merges this theme definition with its parent, creating a new fully-resolved theme.
    /// </summary>
    public static ThemeDefinition MergeWith(this ThemeDefinition derived, ThemeDefinition baseTheme)
    {
        var merged = new ThemeDefinition
        {
            Description = derived.Description ?? baseTheme.Description,
            InheritsFrom = null
        };

        // Merge typography
        merged.Typography = MergeTypography(derived.Typography, baseTheme.Typography);

        // Merge spacing
        merged.Spacing = derived.Spacing ?? baseTheme.Spacing;

        // Merge controls
        merged.Controls = new Dictionary<string, ControlStyle>(baseTheme.Controls);
        foreach (var (controlName, controlStyle) in derived.Controls)
        {
            if (merged.Controls.TryGetValue(controlName, out var baseControl))
            {
                merged.Controls[controlName] = MergeControlStyle(controlStyle, baseControl);
            }
            else
            {
                merged.Controls[controlName] = DeepCloneControlStyle(controlStyle);
            }
        }

        return merged;
    }
    /// <summary>
    /// Merges this skin definition with its parent, creating a new fully-resolved skin.
    /// </summary>
    /// <param name="derived">The derived skin definition.</param>
    /// <param name="baseSkin">The base skin to inherit from.</param>
    /// <returns>A new SkinDefinition with merged properties.</returns>
    public static SkinDefinition MergeWith(this SkinDefinition derived, SkinDefinition baseSkin)
    {
        var merged = new SkinDefinition
        {
            Description = derived.Description ?? baseSkin.Description,
            InheritsFrom = null // Clear inheritance in the resolved skin
        };

        // Merge palette
        merged.Palette = MergePalette(derived.Palette, baseSkin.Palette);

        // Merge controls - start with base controls
        merged.Controls = new Dictionary<string, ControlStateColors>(baseSkin.Controls);
        
        // Override/add controls from derived
        foreach (var (controlName, controlStateColors) in derived.Controls)
        {
            if (merged.Controls.TryGetValue(controlName, out var baseControl))
            {
                merged.Controls[controlName] = MergeControlStateColors(controlStateColors, baseControl);
            }
            else
            {
                merged.Controls[controlName] = DeepCloneControlStateColors(controlStateColors);
            }
        }

        return merged;
    }

    private static ColorPalette MergePalette(ColorPalette derived, ColorPalette basePalette)
    {
        return new ColorPalette
        {
            Primary = derived.Primary ?? basePalette.Primary,
            Secondary = derived.Secondary ?? basePalette.Secondary,
            Success = derived.Success ?? basePalette.Success,
            Danger = derived.Danger ?? basePalette.Danger,
            Warning = derived.Warning ?? basePalette.Warning,
            Info = derived.Info ?? basePalette.Info,
            Background = derived.Background ?? basePalette.Background,
            Surface = derived.Surface ?? basePalette.Surface,
            Text = derived.Text ?? basePalette.Text,
            Border = derived.Border ?? basePalette.Border
        };
    }

    private static Typography MergeTypography(Typography derived, Typography baseTypography)
    {
        return new Typography
        {
            FontFamily = !string.IsNullOrEmpty(derived.FontFamily) && derived.FontFamily != "Segoe UI" 
                ? derived.FontFamily 
                : baseTypography.FontFamily,
            FontSize = derived.FontSize != 9.0f ? derived.FontSize : baseTypography.FontSize
        };
    }

    private static ControlStyle MergeControlStyle(ControlStyle derived, ControlStyle baseStyle)
    {
        var merged = new ControlStyle
        {
            CornerRadius = derived.CornerRadius != 0 ? derived.CornerRadius : baseStyle.CornerRadius,
            BorderWidth = derived.BorderWidth != 0 ? derived.BorderWidth : baseStyle.BorderWidth,
            Padding = derived.Padding ?? baseStyle.Padding
        };

        // Merge states
        merged.States = new Dictionary<string, StateStyle>(baseStyle.States);
        foreach (var (stateName, stateStyle) in derived.States)
        {
            if (merged.States.TryGetValue(stateName, out var baseState))
            {
                merged.States[stateName] = MergeStateStyle(stateStyle, baseState);
            }
            else
            {
                merged.States[stateName] = DeepCloneStateStyle(stateStyle);
            }
        }

        return merged;
    }

    private static StateStyle MergeStateStyle(StateStyle derived, StateStyle baseState)
    {
        return new StateStyle
        {
            BackColor = derived.BackColor ?? baseState.BackColor,
            ForeColor = derived.ForeColor ?? baseState.ForeColor,
            BorderColor = derived.BorderColor ?? baseState.BorderColor,
            Shadow = derived.Shadow ?? baseState.Shadow
        };
    }

    private static ControlStyle DeepCloneControlStyle(ControlStyle source)
    {
        // Use JSON serialization for deep cloning
        var json = JsonSerializer.Serialize(source);
        return JsonSerializer.Deserialize<ControlStyle>(json) ?? new ControlStyle();
    }

    private static ControlStateColors MergeControlStateColors(ControlStateColors derived, ControlStateColors baseColors)
    {
        var merged = new ControlStateColors();

        // Merge states - only colors, no structural properties
        merged.States = new Dictionary<string, StateStyle>(baseColors.States);
        foreach (var (stateName, stateStyle) in derived.States)
        {
            if (merged.States.TryGetValue(stateName, out var baseState))
            {
                merged.States[stateName] = MergeStateStyle(stateStyle, baseState);
            }
            else
            {
                merged.States[stateName] = DeepCloneStateStyle(stateStyle);
            }
        }

        return merged;
    }

    private static ControlStateColors DeepCloneControlStateColors(ControlStateColors source)
    {
        var json = JsonSerializer.Serialize(source);
        return JsonSerializer.Deserialize<ControlStateColors>(json) ?? new ControlStateColors();
    }

    private static StateStyle DeepCloneStateStyle(StateStyle source)
    {
        var json = JsonSerializer.Serialize(source);
        return JsonSerializer.Deserialize<StateStyle>(json) ?? new StateStyle();
    }
}
