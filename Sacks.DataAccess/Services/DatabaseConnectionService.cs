using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sacks.Configuration;
using Sacks.Core.Services.Interfaces;
using Sacks.Core.Services.Models;
using Sacks.DataAccess.Data;

namespace Sacks.DataAccess.Services
{
    /// <summary>
    /// Implementation of database connection service with proper error handling and logging.
    /// Uses centralized configuration and EF Core context.
    /// </summary>
    public class DatabaseConnectionService : IDatabaseConnectionService
    {
        private readonly ILogger<DatabaseConnectionService> _logger;
        private readonly DatabaseOptions _databaseOptions;
        private readonly SacksDbContext _context;

        public DatabaseConnectionService(
            ILogger<DatabaseConnectionService> logger,
            DatabaseOptions databaseOptions,
            SacksDbContext context)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _databaseOptions = databaseOptions ?? throw new ArgumentNullException(nameof(databaseOptions));
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public string GetConnectionString()
        {
            // Get connection string from EF Core context (already configured)
            var connectionString = _context.Database.GetConnectionString();
            
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                // Fallback to centralized config
                connectionString = _databaseOptions.ConnectionString;
            }

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException("Database connection string is not configured.");
            }

            return connectionString;
        }

        public DatabaseOptions GetDatabaseSettings()
        {
            return _databaseOptions;
        }

        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                var connectionString = GetConnectionString();

                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                }

                var serverInfo = await GetServerInfoAsync();
                _logger.LogInformation("Successfully connected to {Provider}. {ServerInfo}", _databaseOptions.Provider, serverInfo);
                
                return true;
            }
            catch (SqlException ex)
            {
                var message = ex.Number switch
                {
                    18456 => "Access denied - Invalid username or password",
                    2 => "Cannot connect to server - Server may be down or unreachable", 
                    4060 => "Cannot open database - Database does not exist",
                    _ => $"SQL Server Error ({ex.Number}): {ex.Message}"
                };

                _logger.LogError(ex, "Database connection failed: {ErrorMessage}", message);
                return false;
            }
            catch (Exception ex)
            {
                var message = $"Connection failed: {ex.Message}";
                _logger.LogError(ex, "Unexpected database connection error: {ErrorMessage}", message);
                return false;
            }
        }

        public async Task<string> GetServerInfoAsync()
        {
            try
            {
                var connectionString = GetConnectionString();
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = "SELECT @@VERSION, DB_NAME()";
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                var version = reader.GetString(0);
                                var database = reader.GetString(1);
                                return $"Server Version: {version}, Database: {database}";
                            }
                        }
                    }
                }

                return "Server information unavailable";
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not retrieve server information");
                return "Server information unavailable";
            }
        }

        /// <summary>
        /// Ensures the database exists and creates it if it doesn't.
        /// Does NOT apply migrations to existing databases.
        /// </summary>
        public async Task<(bool Success, string Message, Exception? Exception)> EnsureDatabaseExistsAsync()
        {
            try
            {
                // First, try to connect to see if database exists
                var isAvailable = await TestConnectionAsync();
                
                if (isAvailable)
                {
                    return (true, "Database exists and is accessible", null);
                }
                
                // If connection failed, try to create the database
                try
                {
                    // Try to create the database using EF Core
                    var created = await _context.Database.EnsureCreatedAsync();
                    
                    if (created)
                    {
                        return (true, "Database created successfully", null);
                    }
                    else
                    {
                        return (true, "Database already existed", null);
                    }
                }
                catch (Exception createEx)
                {
                    _logger.LogError(createEx, "Failed to create database");
                    return (false, $"Failed to create database: {createEx.Message}", createEx);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while ensuring database exists");
                return (false, $"Unexpected error: {ex.Message}", ex);
            }
        }
    }
}
