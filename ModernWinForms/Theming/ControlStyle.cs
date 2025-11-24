using System.Text.Json.Serialization;

namespace ModernWinForms.Theming;

/// <summary>
/// Complete style configuration for a specific control type (e.g., ModernButton, ModernTextBox).
/// Extends ControlStateColors with structural properties like corner radius, border width, and padding.
/// Used in themes to define the complete design language for controls.
/// </summary>
public sealed class ControlStyle : ControlStateColors
{
    /// <summary>
    /// Corner radius in pixels.
    /// Defines how rounded the control's corners are.
    /// </summary>
    [JsonPropertyName("cornerRadius")] public int CornerRadius { get; set; }
    
    /// <summary>
    /// Border width in pixels.
    /// </summary>
    [JsonPropertyName("borderWidth")] public int BorderWidth { get; set; }
    
    /// <summary>
    /// Padding inside the control.
    /// </summary>
    [JsonPropertyName("padding")] public PaddingSpec? Padding { get; set; }
}
