using System.Text.Json.Serialization;

namespace ModernWinForms.Theming;

/// <summary>
/// Color palette defining semantic colors for a skin.
/// All properties are optional - unspecified colors will be inherited from parent skins or theme defaults.
/// </summary>
public sealed class ColorPalette
{
    /// <summary>
    /// Primary brand color - used for primary actions and emphasis.
    /// </summary>
    [JsonPropertyName("primary")] public string? Primary { get; set; }
    
    /// <summary>
    /// Secondary brand color - used for secondary actions.
    /// </summary>
    [JsonPropertyName("secondary")] public string? Secondary { get; set; }
    
    /// <summary>
    /// Success color - used for positive feedback and success states.
    /// </summary>
    [JsonPropertyName("success")] public string? Success { get; set; }
    
    /// <summary>
    /// Danger/Error color - used for destructive actions and error states.
    /// </summary>
    [JsonPropertyName("danger")] public string? Danger { get; set; }
    
    /// <summary>
    /// Warning color - used for cautionary messages and warnings.
    /// </summary>
    [JsonPropertyName("warning")] public string? Warning { get; set; }
    
    /// <summary>
    /// Info color - used for informational messages.
    /// </summary>
    [JsonPropertyName("info")] public string? Info { get; set; }
    
    /// <summary>
    /// Background color - primary background of the application.
    /// </summary>
    [JsonPropertyName("background")] public string? Background { get; set; }
    
    /// <summary>
    /// Surface color - color of elevated surfaces (cards, panels, dialogs).
    /// </summary>
    [JsonPropertyName("surface")] public string? Surface { get; set; }
    
    /// <summary>
    /// Text color - primary text color on backgrounds and surfaces.
    /// </summary>
    [JsonPropertyName("text")] public string? Text { get; set; }
    
    /// <summary>
    /// Border color - default color for borders and dividers.
    /// </summary>
    [JsonPropertyName("border")] public string? Border { get; set; }
}
