using System.Text.Json.Serialization;

namespace ModernWinForms.Theming;

/// <summary>
/// Definition of a skin - a color variation within a theme/design system.
/// Skins contain only color information, not structural properties.
/// </summary>
public sealed class SkinDefinition
{
    /// <summary>
    /// Gets or sets the theme (design system) this skin belongs to.
    /// If not specified, the current active theme will be used.
    /// </summary>
    [JsonPropertyName("theme")] public string? Theme { get; set; }
    
    /// <summary>
    /// Gets or sets the name of the parent skin to inherit from.
    /// Allows building skin hierarchies (e.g., BaseDark → Dark → Dracula).
    /// </summary>
    [JsonPropertyName("inheritsFrom")] public string? InheritsFrom { get; set; }
    
    /// <summary>
    /// Gets or sets the description of this skin.
    /// </summary>
    [JsonPropertyName("description")] public string? Description { get; set; }
    
    /// <summary>
    /// Gets or sets the color palette for this skin.
    /// Contains all semantic colors (primary, background, text, etc.).
    /// </summary>
    [JsonPropertyName("palette")] public ColorPalette Palette { get; set; } = new();
    
    /// <summary>
    /// Gets or sets the control-specific color overrides for this skin.
    /// Contains only state colors (normal, hover, pressed, disabled).
    /// Structural properties (cornerRadius, borderWidth, padding) come from themes.
    /// Uses ControlStateColors to enforce color-only overrides.
    /// </summary>
    [JsonPropertyName("controls")] public Dictionary<string, ControlStateColors> Controls { get; set; } = new();
}
