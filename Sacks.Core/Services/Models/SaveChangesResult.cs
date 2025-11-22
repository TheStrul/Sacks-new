namespace Sacks.Core.Services.Models;

/// <summary>
/// Result of a SaveChanges operation
/// </summary>
public sealed class SaveChangesResult
{
    public int TotalChanges { get; init; }
    public int SuccessfulSaves { get; init; }
    public List<string> Errors { get; init; } = new();
}
