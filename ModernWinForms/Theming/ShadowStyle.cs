using System.Text.Json.Serialization;

namespace ModernWinForms.Theming;

/// <summary>
/// Shadow effect configuration for controls.
/// </summary>
public sealed class ShadowStyle
{
    /// <summary>
    /// Shadow color (typically with alpha/transparency).
    /// </summary>
    [JsonPropertyName("color")] public string? Color { get; set; }
    
    /// <summary>
    /// Blur radius in pixels - how soft/diffused the shadow is.
    /// </summary>
    [JsonPropertyName("blur")] public int Blur { get; set; }
    
    /// <summary>
    /// Horizontal offset in pixels - positive moves shadow right, negative moves left.
    /// </summary>
    [JsonPropertyName("offsetX")] public int OffsetX { get; set; }
    
    /// <summary>
    /// Vertical offset in pixels - positive moves shadow down, negative moves up.
    /// </summary>
    [JsonPropertyName("offsetY")] public int OffsetY { get; set; }
}
