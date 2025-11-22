namespace Sacks.Core.Services.Models;

/// <summary>
/// Categories of columns for classification
/// </summary>
public sealed class ColumnCategories
{
    public required IReadOnlyCollection<string> ProductColumns { get; init; }
    public required IReadOnlyCollection<string> OfferColumns { get; init; }
    public required IReadOnlyCollection<string> EditableColumns { get; init; }
}
