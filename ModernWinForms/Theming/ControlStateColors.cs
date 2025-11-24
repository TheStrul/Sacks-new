using System.Text.Json.Serialization;

namespace ModernWinForms.Theming;

/// <summary>
/// Base class containing only state-specific color styles for controls.
/// Used by both skins (color-only overrides) and themes (complete control styles).
/// </summary>
public class ControlStateColors
{
    /// <summary>
    /// State-specific styles for different control states.
    /// Keys: "normal", "hover", "pressed", "disabled", "focused", etc.
    /// </summary>
    [JsonPropertyName("states")] public Dictionary<string, StateStyle> States { get; set; } = new();
}
