using System.Text.Json.Serialization;

namespace ModernWinForms.Theming;

/// <summary>
/// Defines the spacing/sizing system for a theme.
/// Provides consistent spacing values based on a base unit (typically 8px).
/// </summary>
public sealed class SpacingSystem
{
    /// <summary>
    /// Base spacing unit in pixels.
    /// All other spacing values are typically multiples or fractions of this value.
    /// Common values: 4px, 8px (most common), 10px.
    /// </summary>
    [JsonPropertyName("baseUnit")] public int BaseUnit { get; set; } = 8;
    
    /// <summary>
    /// Small spacing in pixels (typically baseUnit * 0.5).
    /// Used for tight spacing between related elements.
    /// </summary>
    [JsonPropertyName("small")] public int Small { get; set; } = 4;
    
    /// <summary>
    /// Medium spacing in pixels (typically baseUnit * 1).
    /// Default spacing for most UI elements.
    /// </summary>
    [JsonPropertyName("medium")] public int Medium { get; set; } = 8;
    
    /// <summary>
    /// Large spacing in pixels (typically baseUnit * 2).
    /// Used for separating distinct sections or groups.
    /// </summary>
    [JsonPropertyName("large")] public int Large { get; set; } = 16;
    
    /// <summary>
    /// Extra large spacing in pixels (typically baseUnit * 3).
    /// Used for major visual separation or page-level spacing.
    /// </summary>
    [JsonPropertyName("xlarge")] public int XLarge { get; set; } = 24;
}
