using System.Text.Json.Serialization;

namespace ModernWinForms.Theming;

/// <summary>
/// Root configuration for the theming system.
/// Manages both themes (design systems) and skins (color variants).
/// </summary>
public sealed class ThemingConfiguration
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ThemingConfiguration"/> class
    /// with case-insensitive dictionaries for themes and skins.
    /// </summary>
    [JsonConstructor]
    public ThemingConfiguration()
    {
        Themes = new Dictionary<string, ThemeDefinition>(StringComparer.OrdinalIgnoreCase);
        Skins = new Dictionary<string, SkinDefinition>(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets or sets the configuration version.
    /// </summary>
    [JsonPropertyName("version")] public string? Version { get; set; }
    
    /// <summary>
    /// Gets or sets the current active theme name (design system).
    /// </summary>
    [JsonPropertyName("currentTheme")] public required string CurrentTheme { get; set; }
    
    /// <summary>
    /// Gets or sets the current active skin name (color variant).
    /// </summary>
    [JsonPropertyName("currentSkin")] public required string CurrentSkin { get; set; }
    
    /// <summary>
    /// Gets or sets the dictionary of available themes (design systems).
    /// </summary>
    [JsonPropertyName("themes")] public Dictionary<string, ThemeDefinition> Themes { get; init; }
    
    /// <summary>
    /// Gets or sets the dictionary of available skins (color variants).
    /// </summary>
    [JsonPropertyName("skins")] public Dictionary<string, SkinDefinition> Skins { get; init; }
}
