namespace Sacks.Core.Services.Interfaces;

/// <summary>
/// Service for testing and managing database connections
/// </summary>
public interface IDatabaseConnectionService
{
    /// <summary>
    /// Tests the database connection
    /// </summary>
    /// <returns>True if connection successful, false otherwise</returns>
    Task<bool> TestConnectionAsync();
}
