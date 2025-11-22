namespace Sacks.Core.Services.Models;

/// <summary>
/// Result of a database connection test
/// </summary>
public sealed class DatabaseConnectionResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public long ElapsedMilliseconds { get; set; }
    public bool CanConnect { get; set; }
    public string? ServerInfo { get; set; }
    public Dictionary<string, int> TableCounts { get; set; } = new();
    public List<string> Errors { get; set; } = new();
}
