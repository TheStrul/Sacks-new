namespace Sacks.Core.Services.Models;

/// <summary>
/// Result of a database operation
/// </summary>
public sealed class DatabaseOperationResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public TimeSpan Duration { get; set; }
    public int AffectedRecords { get; set; }
    public long ElapsedMilliseconds { get; set; }
    public Dictionary<string, int> DeletedCounts { get; set; } = new();
    public List<string> Errors { get; set; } = new();
}
