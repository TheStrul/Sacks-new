using System.Text.Json.Serialization;

namespace ModernWinForms.Theming;

/// <summary>
/// Style for a specific control state (normal, hover, pressed, disabled, focused, etc.).
/// Contains only color properties - structural properties come from the control's base style.
/// </summary>
public sealed class StateStyle
{
    /// <summary>
    /// Background color for this state.
    /// </summary>
    [JsonPropertyName("backColor")] public string? BackColor { get; set; }
    
    /// <summary>
    /// Foreground/text color for this state.
    /// </summary>
    [JsonPropertyName("foreColor")] public string? ForeColor { get; set; }
    
    /// <summary>
    /// Border color for this state.
    /// </summary>
    [JsonPropertyName("borderColor")] public string? BorderColor { get; set; }
    

}
