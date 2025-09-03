using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SacksDataLayer.Configuration;
using SacksDataLayer.Data;
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
        private readonly SacksDbContext _context;

        public DatabaseConnectionService(
            IConfiguration configuration,
            ILogger<DatabaseConnectionService> logger,
            IOptions<DatabaseSettings> databaseSettings,
            SacksDbContext context)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _databaseSettings = databaseSettings?.Value ?? throw new ArgumentNullException(nameof(databaseSettings));
            _context = context ?? throw new ArgumentNullException(nameof(context));
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

                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var serverInfo = await GetServerInfoAsync();
                var message = $"Successfully connected to {_databaseSettings.Provider}. {serverInfo}";
                
                return (true, message, null);
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
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                using var command = connection.CreateCommand();
                command.CommandText = "SELECT @@VERSION, DB_NAME()";
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

        /// <summary>
        /// Ensures the database exists and creates it if it doesn't.
        /// Does NOT apply migrations to existing databases.
        /// </summary>
        public async Task<(bool Success, string Message, Exception? Exception)> EnsureDatabaseExistsAsync()
        {
            try
            {
                _logger.LogInformation("Checking if database exists...");
                
                // First, try to connect to see if database exists
                var (isAvailable, message, exception) = await TestConnectionAsync();
                
                if (isAvailable)
                {
                    _logger.LogInformation("Database already exists and is accessible");
                    return (true, "Database exists and is accessible", null);
                }
                
                // If we get a "database does not exist" error, try to create it
                if (exception is SqlException sqlEx && sqlEx.Number == 4060)
                {
                    _logger.LogInformation("Database does not exist. Attempting to create it...");
                    
                    try
                    {
                        // Try to create the database using EF Core
                        var created = await _context.Database.EnsureCreatedAsync();
                        
                        if (created)
                        {
                            _logger.LogInformation("Database created successfully");
                            return (true, "Database created successfully", null);
                        }
                        else
                        {
                            _logger.LogInformation("Database already existed (race condition)");
                            return (true, "Database already existed", null);
                        }
                    }
                    catch (Exception createEx)
                    {
                        _logger.LogError(createEx, "Failed to create database");
                        return (false, $"Failed to create database: {createEx.Message}", createEx);
                    }
                }
                
                // For other connection errors, return the original error
                return (false, message, exception);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while ensuring database exists");
                return (false, $"Unexpected error: {ex.Message}", ex);
            }
        }
    }
}
