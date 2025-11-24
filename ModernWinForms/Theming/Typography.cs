using System.Text.Json.Serialization;

namespace ModernWinForms.Theming;

/// <summary>
/// Typography settings for themes.
/// Defines font family and size defaults for the design system.
/// </summary>
public sealed class Typography
{
    /// <summary>
    /// Font family name (e.g., "Segoe UI", "Roboto", "Arial").
    /// </summary>
    [JsonPropertyName("fontFamily")] public string FontFamily { get; set; } = "Segoe UI";
    
    /// <summary>
    /// Base font size in points.
    /// </summary>
    [JsonPropertyName("fontSize")] public float FontSize { get; set; } = 9.0f;
}
