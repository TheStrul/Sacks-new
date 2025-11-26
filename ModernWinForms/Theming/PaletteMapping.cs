using System.Text.Json.Serialization;

namespace ModernWinForms.Theming;

/// <summary>
/// Defines automatic mapping from palette colors to control states.
/// Allows themes to specify default color mappings so skins only need to define palette.
/// </summary>
public sealed class PaletteMapping
{
    /// <summary>
    /// Gets or sets the state mappings (e.g., "normal", "hover", "pressed", "disabled").
    /// </summary>
    [JsonPropertyName("states")]
    public Dictionary<string, PaletteStateMapping> States { get; set; } = new();
}

/// <summary>
/// Defines palette references for a specific control state.
/// </summary>
public sealed class PaletteStateMapping
{
    /// <summary>
    /// Palette key for background color (e.g., "surface", "background", "primary").
    /// </summary>
    [JsonPropertyName("backColor")]
    public string? BackColor { get; set; }

    /// <summary>
    /// Palette key for foreground/text color.
    /// </summary>
    [JsonPropertyName("foreColor")]
    public string? ForeColor { get; set; }

    /// <summary>
    /// Palette key for border color.
    /// </summary>
    [JsonPropertyName("borderColor")]
    public string? BorderColor { get; set; }
}
