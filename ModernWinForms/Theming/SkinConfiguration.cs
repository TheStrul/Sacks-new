using System.Text.Json.Serialization;

namespace ModernWinForms.Theming;

/// <summary>
/// Root configuration for the skin system.
/// </summary>
public sealed class SkinConfiguration
{
    /// <summary>
    /// Gets or sets the configuration version.
    /// </summary>
    [JsonPropertyName("version")] public string? Version { get; set; }
    
    /// <summary>
    /// Gets or sets the current active skin name.
    /// </summary>
    [JsonPropertyName("currentSkin")] public string CurrentSkin { get; set; } = "Light";
    
    /// <summary>
    /// Gets or sets the dictionary of available skins.
    /// </summary>
    [JsonPropertyName("skins")] public Dictionary<string, SkinDefinition> Skins { get; set; } = new();
}

/// <summary>
/// Definition of a single skin (Light, Dark, etc.).
/// </summary>
public sealed class SkinDefinition
{
    /// <summary>
    /// Gets or sets the description of this skin.
    /// </summary>
    [JsonPropertyName("description")] public string? Description { get; set; }
    
    /// <summary>
    /// Gets or sets the color palette for this skin.
    /// </summary>
    [JsonPropertyName("palette")] public SkinPalette Palette { get; set; } = new();
    
    /// <summary>
    /// Gets or sets the typography settings for this skin.
    /// </summary>
    [JsonPropertyName("typography")] public SkinTypography Typography { get; set; } = new();
    
    /// <summary>
    /// Gets or sets the control-specific styles for this skin.
    /// </summary>
    [JsonPropertyName("controls")] public Dictionary<string, ControlStyle> Controls { get; set; } = new();
}

/// <summary>
/// Color palette for a skin.
/// </summary>
public sealed class SkinPalette
{
    /// <summary>
    /// Primary color.
    /// </summary>
    [JsonPropertyName("primary")] public string? Primary { get; set; }
    
    /// <summary>
    /// Secondary color.
    /// </summary>
    [JsonPropertyName("secondary")] public string? Secondary { get; set; }
    
    /// <summary>
    /// Success color.
    /// </summary>
    [JsonPropertyName("success")] public string? Success { get; set; }
    
    /// <summary>
    /// Danger color.
    /// </summary>
    [JsonPropertyName("danger")] public string? Danger { get; set; }
    
    /// <summary>
    /// Warning color.
    /// </summary>
    [JsonPropertyName("warning")] public string? Warning { get; set; }
    
    /// <summary>
    /// Info color.
    /// </summary>
    [JsonPropertyName("info")] public string? Info { get; set; }
    
    /// <summary>
    /// Background color.
    /// </summary>
    [JsonPropertyName("background")] public string? Background { get; set; }
    
    /// <summary>
    /// Surface color.
    /// </summary>
    [JsonPropertyName("surface")] public string? Surface { get; set; }
    
    /// <summary>
    /// Text color.
    /// </summary>
    [JsonPropertyName("text")] public string? Text { get; set; }
    
    /// <summary>
    /// Border color.
    /// </summary>
    [JsonPropertyName("border")] public string? Border { get; set; }
}

/// <summary>
/// Typography settings for a skin.
/// </summary>
public sealed class SkinTypography
{
    /// <summary>
    /// Font family name.
    /// </summary>
    [JsonPropertyName("fontFamily")] public string FontFamily { get; set; } = "Segoe UI";
    
    /// <summary>
    /// Font size in points.
    /// </summary>
    [JsonPropertyName("fontSize")] public float FontSize { get; set; } = 9.0f;
}

/// <summary>
/// Style configuration for a control.
/// </summary>
public sealed class ControlStyle
{
    /// <summary>
    /// Corner radius in pixels.
    /// </summary>
    [JsonPropertyName("cornerRadius")] public int CornerRadius { get; set; }
    
    /// <summary>
    /// Border width in pixels.
    /// </summary>
    [JsonPropertyName("borderWidth")] public int BorderWidth { get; set; }
    
    /// <summary>
    /// Padding specification.
    /// </summary>
    [JsonPropertyName("padding")] public PaddingSpec? Padding { get; set; }
    
    /// <summary>
    /// State-specific styles (normal, hover, pressed, etc.).
    /// </summary>
    [JsonPropertyName("states")] public Dictionary<string, StateStyle> States { get; set; } = new();
}

/// <summary>
/// Style for a specific control state.
/// </summary>
public sealed class StateStyle
{
    /// <summary>
    /// Background color.
    /// </summary>
    [JsonPropertyName("backColor")] public string? BackColor { get; set; }
    
    /// <summary>
    /// Foreground color.
    /// </summary>
    [JsonPropertyName("foreColor")] public string? ForeColor { get; set; }
    
    /// <summary>
    /// Border color.
    /// </summary>
    [JsonPropertyName("borderColor")] public string? BorderColor { get; set; }
    
    /// <summary>
    /// Shadow configuration.
    /// </summary>
    [JsonPropertyName("shadow")] public ShadowStyle? Shadow { get; set; }
}

/// <summary>
/// Shadow effect configuration.
/// </summary>
public sealed class ShadowStyle
{
    /// <summary>
    /// Shadow color.
    /// </summary>
    [JsonPropertyName("color")] public string? Color { get; set; }
    
    /// <summary>
    /// Blur radius in pixels.
    /// </summary>
    [JsonPropertyName("blur")] public int Blur { get; set; }
    
    /// <summary>
    /// Horizontal offset in pixels.
    /// </summary>
    [JsonPropertyName("offsetX")] public int OffsetX { get; set; }
    
    /// <summary>
    /// Vertical offset in pixels.
    /// </summary>
    [JsonPropertyName("offsetY")] public int OffsetY { get; set; }
}

/// <summary>
/// Padding specification.
/// </summary>
public sealed class PaddingSpec
{
    /// <summary>
    /// Left padding.
    /// </summary>
    [JsonPropertyName("left")] public int Left { get; set; }
    
    /// <summary>
    /// Top padding.
    /// </summary>
    [JsonPropertyName("top")] public int Top { get; set; }
    
    /// <summary>
    /// Right padding.
    /// </summary>
    [JsonPropertyName("right")] public int Right { get; set; }
    
    /// <summary>
    /// Bottom padding.
    /// </summary>
    [JsonPropertyName("bottom")] public int Bottom { get; set; }

    /// <summary>
    /// Converts this specification to a Padding struct.
    /// </summary>
    /// <returns>A Padding struct with the specified values.</returns>
    public Padding ToPadding() => new(Left, Top, Right, Bottom);
}
