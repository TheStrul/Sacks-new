using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace SacksDataLayer.Services.Interfaces
{
    /// <summary>
    /// Service interface for database management operations
    /// </summary>
    public interface IDatabaseManagementService
    {
        /// <summary>
        /// Clears all data from all tables in the correct order (respecting foreign key constraints)
        /// </summary>
        /// <returns>Result containing operation success status and statistics</returns>
        Task<DatabaseOperationResult> ClearAllDataAsync();

        /// <summary>
        /// Tests the database connection and retrieves current table counts
        /// </summary>
        /// <returns>Result containing connection status and table statistics</returns>
        Task<DatabaseConnectionResult> CheckConnectionAsync();

        /// <summary>
        /// Gets current record counts for all main tables
        /// </summary>
        /// <returns>Dictionary with table names and their record counts</returns>
        Task<Dictionary<string, int>> GetTableCountsAsync();

        /// <summary>
        /// Resets auto-increment counters for all tables
        /// </summary>
        /// <returns>Result containing operation success status</returns>
        Task<DatabaseOperationResult> ResetAutoIncrementCountersAsync();
    }

    /// <summary>
    /// Result of a database operation
    /// </summary>
    public class DatabaseOperationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public Dictionary<string, int> DeletedCounts { get; set; } = new();
        public long ElapsedMilliseconds { get; set; }
        public List<string> Errors { get; set; } = new();
    }

    /// <summary>
    /// Result of a database connection check
    /// </summary>
    public class DatabaseConnectionResult
    {
        public bool CanConnect { get; set; }
        public string Message { get; set; } = string.Empty;
        public Dictionary<string, int> TableCounts { get; set; } = new();
        public string ServerInfo { get; set; } = string.Empty;
        public List<string> Errors { get; set; } = new();
    }
}
