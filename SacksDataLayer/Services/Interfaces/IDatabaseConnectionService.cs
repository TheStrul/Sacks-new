using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using MySqlConnector;
using SacksDataLayer.Configuration;

namespace SacksDataLayer.Services.Interfaces
{
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
        DatabaseSettings GetDatabaseSettings();

        /// <summary>
        /// Gets server information
        /// </summary>
        Task<string> GetServerInfoAsync();
    }
}
