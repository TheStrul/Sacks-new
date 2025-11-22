using Sacks.Core.Services.Models;

namespace Sacks.Core.Services.Interfaces;

/// <summary>
/// Service for database management operations
/// </summary>
public interface IDatabaseManagementService
{
    /// <summary>
    /// Clears all data from all tables in the correct order (respecting foreign key constraints)
    /// </summary>
    Task<DatabaseOperationResult> ClearAllDataAsync();

    /// <summary>
    /// Gets counts for all main entities
    /// </summary>
    Task<EntityCounts> GetEntityCountsAsync();

    /// <summary>
    /// Checks database connection
    /// </summary>
    Task<DatabaseConnectionResult> CheckConnectionAsync();
}
