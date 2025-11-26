using System.Text.Json.Serialization;

namespace ModernWinForms.Theming;

/// <summary>
/// Defines a design system theme (e.g., Material, Fluent, GitHub).
/// A theme contains design rules like typography defaults, spacing system, and control shape guidelines.
/// </summary>
public sealed class ThemeDefinition
{
    /// <summary>
    /// Gets or sets the name of the parent theme to inherit from.
    /// </summary>
    [JsonPropertyName("inheritsFrom")] public string? InheritsFrom { get; set; }
    
    /// <summary>
    /// Gets or sets the description of this theme.
    /// </summary>
    [JsonPropertyName("description")] public string? Description { get; set; }
    
    /// <summary>
    /// Gets or sets the typography defaults for this design system.
    /// </summary>
    [JsonPropertyName("typography")] public Typography Typography { get; set; } = new();
    
    /// <summary>
    /// Gets or sets the spacing system for this theme.
    /// </summary>
    [JsonPropertyName("spacing")] public SpacingSystem? Spacing { get; set; }
    
    /// <summary>
    /// Gets or sets the default control styles for this design system.
    /// These define the shape language and structural properties (not colors).
    /// </summary>
    [JsonPropertyName("controls")] public Dictionary<string, ControlStyle> Controls { get; set; } = new();
    
    /// <summary>
    /// Gets or sets the default palette mappings for controls.
    /// Maps control states to palette color keys (e.g., "normal.backColor" -> "surface").
    /// When specified, controls automatically derive colors from the skin's palette.
    /// </summary>
    [JsonPropertyName("paletteMappings")] public Dictionary<string, PaletteMapping>? PaletteMappings { get; set; }
}
