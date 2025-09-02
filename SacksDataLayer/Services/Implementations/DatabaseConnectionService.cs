using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MySqlConnector;
using SacksDataLayer.Configuration;
using SacksDataLayer.Services.Interfaces;
using System;
using System.Threading.Tasks;

namespace SacksDataLayer.Services.Implementations
{
    /// <summary>
    /// Implementation of database connection service with proper error handling and logging
    /// </summary>
    public class DatabaseConnectionService : IDatabaseConnectionService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<DatabaseConnectionService> _logger;
        private readonly DatabaseSettings _databaseSettings;

        public DatabaseConnectionService(
            IConfiguration configuration,
            ILogger<DatabaseConnectionService> logger,
            IOptions<DatabaseSettings> databaseSettings)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _databaseSettings = databaseSettings?.Value ?? throw new ArgumentNullException(nameof(databaseSettings));
        }

        public string GetConnectionString()
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException("DefaultConnection connection string is not configured.");
            }

            return connectionString;
        }

        public DatabaseSettings GetDatabaseSettings()
        {
            return _databaseSettings;
        }

        public async Task<(bool IsAvailable, string Message, Exception? Exception)> TestConnectionAsync()
        {
            try
            {
                var connectionString = GetConnectionString();

                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                var serverInfo = await GetServerInfoAsync();
                var message = $"Successfully connected to {_databaseSettings.Provider}. {serverInfo}";
                
                return (true, message, null);
            }
            catch (MySqlException ex)
            {
                var message = ex.Number switch
                {
                    1045 => "Access denied - Invalid username or password",
                    2002 => "Cannot connect to server - Server may be down or unreachable",
                    1049 => "Unknown database - Database does not exist",
                    _ => $"MySQL Error ({ex.Number}): {ex.Message}"
                };

                _logger.LogError(ex, "Database connection failed: {ErrorMessage}", message);
                return (false, message, ex);
            }
            catch (Exception ex)
            {
                var message = $"Connection failed: {ex.Message}";
                _logger.LogError(ex, "Unexpected database connection error: {ErrorMessage}", message);
                return (false, message, ex);
            }
        }

        public async Task<string> GetServerInfoAsync()
        {
            try
            {
                var connectionString = GetConnectionString();
                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                using var command = connection.CreateCommand();
                command.CommandText = "SELECT VERSION(), DATABASE()";
                using var reader = await command.ExecuteReaderAsync();
                
                if (await reader.ReadAsync())
                {
                    var version = reader.GetString(0);
                    var database = reader.GetString(1);
                    return $"Server Version: {version}, Database: {database}";
                }

                return "Server information unavailable";
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not retrieve server information");
                return "Server information unavailable";
            }
        }
    }
}
