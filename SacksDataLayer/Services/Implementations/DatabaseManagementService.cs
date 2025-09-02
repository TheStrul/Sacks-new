using Microsoft.EntityFrameworkCore;
using SacksDataLayer.Data;
using SacksDataLayer.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace SacksDataLayer.Services.Implementations
{
    /// <summary>
    /// Service implementation for database management operations
    /// </summary>
    public class DatabaseManagementService : IDatabaseManagementService
    {
        private readonly SacksDbContext _context;

        public DatabaseManagementService(SacksDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <summary>
        /// Clears all data from all tables in the correct order (respecting foreign key constraints)
        /// <summary>
        /// Clears all data from all tables in the correct order (respecting foreign key constraints)
        /// </summary>
        public async Task<DatabaseOperationResult> ClearAllDataAsync()
        {
            var result = new DatabaseOperationResult();
            var stopwatch = Stopwatch.StartNew();

            try
            {
                // Drop and recreate database to ensure schema alignment
                await _context.Database.EnsureDeletedAsync();
                await _context.Database.EnsureCreatedAsync();

                // Clear the DbContext change tracker to remove any cached/tracked entities
                _context.ChangeTracker.Clear();

                result.Success = true;
                result.Message = "Database recreated successfully! All tables are now empty with correct schema.";

                stopwatch.Stop();
                result.ElapsedMilliseconds = stopwatch.ElapsedMilliseconds;

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                result.ElapsedMilliseconds = stopwatch.ElapsedMilliseconds;
                result.Success = false;
                result.Message = $"Error recreating database: {ex.Message}";
                result.Errors.Add(ex.Message);

                return result;
            }
        }

        /// <summary>
        /// Tests the database connection and retrieves current table counts
        /// </summary>
        public async Task<DatabaseConnectionResult> CheckConnectionAsync()
        {
            var result = new DatabaseConnectionResult();

            try
            {
                // Try to connect and get server version
                var canConnect = await _context.Database.CanConnectAsync();
                result.CanConnect = canConnect;

                if (canConnect)
                {
                    result.Message = "Successfully connected to MariaDB";

                    // Get server info if possible
                    try
                    {
                        var connectionString = _context.Database.GetConnectionString();
                        result.ServerInfo = $"Connected to: {connectionString}";
                    }
                    catch
                    {
                        result.ServerInfo = "Connected to MariaDB (connection details unavailable)";
                    }

                    // Get table counts
                    result.TableCounts = await GetTableCountsAsync();
                }
                else
                {
                    result.Message = "Cannot connect to MariaDB";
                }

                return result;
            }
            catch (Exception ex)
            {
                result.CanConnect = false;
                result.Message = $"Connection failed: {ex.Message}";
                result.Errors.Add(ex.Message);

                return result;
            }
        }

        /// <summary>
        /// Gets current record counts for all main tables
        /// </summary>
        public async Task<Dictionary<string, int>> GetTableCountsAsync()
        {
            try
            {
                var counts = new Dictionary<string, int>();

                counts["Products"] = await _context.Products.CountAsync();
                counts["Suppliers"] = await _context.Suppliers.CountAsync();
                counts["SupplierOffers"] = await _context.SupplierOffers.CountAsync();
                counts["OfferProducts"] = await _context.OfferProducts.CountAsync();

                return counts;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error retrieving table counts: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Resets auto-increment counters for all tables
        /// </summary>
        public async Task<DatabaseOperationResult> ResetAutoIncrementCountersAsync()
        {
            var result = new DatabaseOperationResult();
            var stopwatch = Stopwatch.StartNew();

            try
            {
                await ResetAutoIncrementCountersInternalAsync();

                stopwatch.Stop();
                result.ElapsedMilliseconds = stopwatch.ElapsedMilliseconds;
                result.Success = true;
                result.Message = "Auto-increment counters reset successfully";

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                result.ElapsedMilliseconds = stopwatch.ElapsedMilliseconds;
                result.Success = false;
                result.Message = $"Error resetting auto-increment counters: {ex.Message}";
                result.Errors.Add(ex.Message);

                return result;
            }
        }

        /// <summary>
        /// Internal method to reset auto-increment counters
        /// </summary>
        private async Task ResetAutoIncrementCountersInternalAsync()
        {
            await _context.Database.ExecuteSqlRawAsync("ALTER TABLE OfferProducts AUTO_INCREMENT = 1");
            await _context.Database.ExecuteSqlRawAsync("ALTER TABLE SupplierOffers AUTO_INCREMENT = 1");
            await _context.Database.ExecuteSqlRawAsync("ALTER TABLE Products AUTO_INCREMENT = 1");
            await _context.Database.ExecuteSqlRawAsync("ALTER TABLE Suppliers AUTO_INCREMENT = 1");
        }
    }
}
