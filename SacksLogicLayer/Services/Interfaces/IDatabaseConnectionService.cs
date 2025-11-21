namespace SacksLogicLayer.Services.Interfaces
{
    using Sacks.Configuration;

    /// <summary>
    /// Service for managing database connections and health checks
    /// </summary>
    public interface IDatabaseConnectionService
    {
        /// <summary>
        /// Tests if the database connection is available
        /// </summary>
        Task<(bool IsAvailable, string Message, Exception? Exception)> TestConnectionAsync();

        /// <summary>
        /// Gets the current connection string
        /// </summary>
        string GetConnectionString();

        /// <summary>
        /// Gets database configuration settings
        /// </summary>
        DatabaseOptions GetDatabaseSettings();

        /// <summary>
        /// Gets server information
        /// </summary>
        Task<string> GetServerInfoAsync();

        /// <summary>
        /// Ensures the database exists and creates it if it doesn't.
        /// Does NOT apply migrations to existing databases.
        /// </summary>
        Task<(bool Success, string Message, Exception? Exception)> EnsureDatabaseExistsAsync();
    }
}
