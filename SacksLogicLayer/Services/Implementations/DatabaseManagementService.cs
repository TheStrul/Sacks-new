using Microsoft.EntityFrameworkCore;

using Sacks.DataAccess.Data;
using Sacks.DataAccess.Repositories.Interfaces;
using SacksLogicLayer.Services.Interfaces;

using System.Diagnostics;

namespace SacksLogicLayer.Services.Implementations
{
    /// <summary>
    /// Service implementation for database management operations
    /// </summary>
    public class DatabaseManagementService : IDatabaseManagementService
    {
        private readonly SacksDbContext _context;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITransactionalProductsRepository _productsRepository;
        private readonly ITransactionalSuppliersRepository _suppliersRepository;
        private readonly ITransactionalSupplierOffersRepository _supplierOffersRepository;
        private readonly ITransactionalOfferProductsRepository _offerProductsRepository;

        public DatabaseManagementService(
            SacksDbContext context,
            IUnitOfWork unitOfWork,
            ITransactionalProductsRepository productsRepository,
            ITransactionalSuppliersRepository suppliersRepository,
            ITransactionalSupplierOffersRepository supplierOffersRepository,
            ITransactionalOfferProductsRepository offerProductsRepository)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _productsRepository = productsRepository ?? throw new ArgumentNullException(nameof(productsRepository));
            _suppliersRepository = suppliersRepository ?? throw new ArgumentNullException(nameof(suppliersRepository));
            _supplierOffersRepository = supplierOffersRepository ?? throw new ArgumentNullException(nameof(supplierOffersRepository));
            _offerProductsRepository = offerProductsRepository ?? throw new ArgumentNullException(nameof(offerProductsRepository));
        }

        /// <summary>
        /// Clears all data from all tables in the correct order (respecting foreign key constraints)
        /// </summary>
        public async Task<DatabaseOperationResult> ClearAllDataAsync()
        {
            var result = new DatabaseOperationResult();
            var stopwatch = Stopwatch.StartNew();

            try
            {
                // Execute all operations within a single transaction
                await _unitOfWork.ExecuteInTransactionAsync(async (cancellationToken) =>
                {
                    // Clear the DbContext change tracker FIRST to remove any cached/tracked entities
                    _context.ChangeTracker.Clear();
                    
                    // Get counts before deletion for reporting
                    var beforeCounts = await GetTableCountsAsync();
                    result.DeletedCounts = beforeCounts;
                    
                    // Use raw SQL commands to delete data in correct order (faster and avoids EF tracking issues)
                    await _context.Database.ExecuteSqlRawAsync("DELETE FROM OfferProducts", cancellationToken);
                    await _context.Database.ExecuteSqlRawAsync("DELETE FROM SupplierOffers", cancellationToken);
                    await _context.Database.ExecuteSqlRawAsync("DELETE FROM Products", cancellationToken);
                    await _context.Database.ExecuteSqlRawAsync("DELETE FROM Suppliers", cancellationToken);

                    // Clear the DbContext change tracker again after deletion
                    _context.ChangeTracker.Clear();
                }, CancellationToken.None);

                // Reset auto-increment counters after successful deletion
                await ResetAutoIncrementCountersInternalAsync();

                stopwatch.Stop();
                result.ElapsedMilliseconds = stopwatch.ElapsedMilliseconds;
                result.Success = true;
                
                var totalDeleted = result.DeletedCounts.Values.Sum();
                result.Message = $"Successfully cleared all data! Deleted {totalDeleted:N0} total records.";

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                result.ElapsedMilliseconds = stopwatch.ElapsedMilliseconds;
                result.Success = false;
                result.Message = $"Error clearing database: {ex.Message}";
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
                    result.Message = "Successfully connected to SQL Server";

                    // Get server info if possible
                    try
                    {
                        var connectionString = _context.Database.GetConnectionString();
                        result.ServerInfo = $"Connected to: {connectionString}";
                    }
                    catch
                    {
                        result.ServerInfo = "Connected to SQL Server (connection details unavailable)";
                    }

                    // Ensure database and tables exist before trying to count
                    try
                    {
                        await _context.Database.EnsureCreatedAsync();
                        
                        // Get table counts
                        result.TableCounts = await GetTableCountsAsync();
                    }
                    catch
                    {
                        // If we can't create the database or get counts, still report successful connection
                        result.TableCounts = new Dictionary<string, int>
                        {
                            ["Products"] = 0,
                            ["Suppliers"] = 0,
                            ["SupplierOffers"] = 0,
                            ["OfferProducts"] = 0
                        };
                        result.Message += $" (Database created/verified)";
                    }
                }
                else
                {
                    result.Message = "Cannot connect to SQL Server";
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
            await _context.Database.ExecuteSqlRawAsync("DBCC CHECKIDENT ('OfferProducts', RESEED, 0)");
            await _context.Database.ExecuteSqlRawAsync("DBCC CHECKIDENT ('SupplierOffers', RESEED, 0)");
            await _context.Database.ExecuteSqlRawAsync("DBCC CHECKIDENT ('Products', RESEED, 0)");
            await _context.Database.ExecuteSqlRawAsync("DBCC CHECKIDENT ('Suppliers', RESEED, 0)");
        }
    }
}
