namespace SacksDataLayer.FileProcessing.Configuration;

/// <summary>
/// Represents a lookup table entry with a canonical value and multiple search aliases.
/// Used for cleaner JSON representation: one canonical value with explicit aliases.
/// </summary>
public sealed class LookupEntry
{
    /// <summary>
    /// The canonical/normalized value returned when any alias matches.
    /// Example: "United States" is canonical for aliases ["USA", "US", "United States"].
    /// </summary>
    public required string Canonical { get; set; }

    /// <summary>
    /// List of search patterns that map to the canonical value.
    /// Should include the Canonical itself for consistency.
    /// </summary>
    public List<string> Aliases { get; set; } = new();
}
