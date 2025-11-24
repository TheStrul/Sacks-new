using System.Text.Json.Serialization;

namespace ModernWinForms.Theming;

/// <summary>
/// Padding specification for controls.
/// Defines the internal spacing between the control's border and its content.
/// </summary>
public sealed class PaddingSpec
{
    /// <summary>
    /// Left padding in pixels.
    /// </summary>
    [JsonPropertyName("left")] public int Left { get; set; }
    
    /// <summary>
    /// Top padding in pixels.
    /// </summary>
    [JsonPropertyName("top")] public int Top { get; set; }
    
    /// <summary>
    /// Right padding in pixels.
    /// </summary>
    [JsonPropertyName("right")] public int Right { get; set; }
    
    /// <summary>
    /// Bottom padding in pixels.
    /// </summary>
    [JsonPropertyName("bottom")] public int Bottom { get; set; }

    /// <summary>
    /// Converts this specification to a WinForms Padding struct.
    /// </summary>
    /// <returns>A Padding struct with the specified values.</returns>
    public Padding ToPadding() => new(Left, Top, Right, Bottom);
}
