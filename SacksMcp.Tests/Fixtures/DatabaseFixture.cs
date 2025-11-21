using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Sacks.DataAccess.Data;
using Sacks.Core.Entities;
using Xunit;

namespace SacksMcp.Tests.Fixtures;

/// <summary>
/// Fixture for integration tests that provides a real SQL Server database.
/// Uses existing SQL Server LocalDB with a dedicated test database.
/// Implements IAsyncLifetime for proper setup and teardown.
/// </summary>
public class DatabaseFixture : IAsyncLifetime
{
    private const string TestDatabaseName = "SacksProductsDb_IntegrationTests";
    private const string MasterConnectionString = "Server=(localdb)\\mssqllocaldb;Database=master;Trusted_Connection=true";
    private const string ConnectionString = $"Server=(localdb)\\mssqllocaldb;Database={TestDatabaseName};Trusted_Connection=true";
    
    public SacksDbContext DbContext { get; private set; } = null!;
    public ILogger<T> GetLogger<T>() => new Mock<ILogger<T>>().Object;

    public async Task InitializeAsync()
    {
        // Forcefully drop the database if it exists (handles locked connections)
        await DropDatabaseIfExistsAsync().ConfigureAwait(false);

        // Wait a moment for database to be fully dropped
        await Task.Delay(500).ConfigureAwait(false);

        // Create DbContext with test database connection string
        var options = new DbContextOptionsBuilder<SacksDbContext>()
            .UseSqlServer(ConnectionString, sqlOptions =>
            {
                sqlOptions.CommandTimeout(60); // Increase timeout for LocalDB
            })
            .EnableSensitiveDataLogging()
            .Options;

        DbContext = new SacksDbContext(options);
        
        try
        {
            // Create fresh database with explicit creation
            var created = await DbContext.Database.EnsureCreatedAsync().ConfigureAwait(false);
            if (!created)
            {
                // Database already existed, recreate it
                await DbContext.Database.EnsureDeletedAsync().ConfigureAwait(false);
                await Task.Delay(500).ConfigureAwait(false);
                await DbContext.Database.EnsureCreatedAsync().ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to initialize test database. Ensure SQL Server LocalDB is running. Error: {ex.Message}", ex);
        }
    }

    public async Task DisposeAsync()
    {
        if (DbContext != null)
        {
            await DbContext.DisposeAsync().ConfigureAwait(false);
            
            // Drop test database after tests complete
            await DropDatabaseIfExistsAsync().ConfigureAwait(false);
        }
    }

    private static async Task DropDatabaseIfExistsAsync()
    {
        try
        {
            await using var connection = new SqlConnection(MasterConnectionString);
            await connection.OpenAsync().ConfigureAwait(false);

            // Drop database with IMMEDIATE rollback to force-close connections
            var dropCommand = connection.CreateCommand();
            dropCommand.CommandText = $@"
                IF EXISTS (SELECT name FROM sys.databases WHERE name = N'{TestDatabaseName}')
                BEGIN
                    ALTER DATABASE [{TestDatabaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                    DROP DATABASE [{TestDatabaseName}];
                END";
            
            await dropCommand.ExecuteNonQueryAsync().ConfigureAwait(false);
        }
        catch
        {
            // Ignore errors if database doesn't exist
        }
    }

    /// <summary>
    /// Seeds the database with test data for integration tests.
    /// </summary>
    public async Task SeedTestDataAsync(
        List<Product>? products = null,
        List<Supplier>? suppliers = null,
        List<Offer>? offers = null,
        List<ProductOffer>? offerProducts = null)
    {
        if (products != null)
        {
            await DbContext.Products.AddRangeAsync(products).ConfigureAwait(false);
        }

        if (suppliers != null)
        {
            await DbContext.Suppliers.AddRangeAsync(suppliers).ConfigureAwait(false);
        }

        if (offers != null)
        {
            await DbContext.SupplierOffers.AddRangeAsync(offers).ConfigureAwait(false);
        }

        if (offerProducts != null)
        {
            await DbContext.OfferProducts.AddRangeAsync(offerProducts).ConfigureAwait(false);
        }

        await DbContext.SaveChangesAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Clears all data from the database for test isolation.
    /// Uses raw SQL to truncate tables and reset identity columns.
    /// Also clears EF Core's change tracker to prevent tracking conflicts.
    /// </summary>
    public async Task ClearDatabaseAsync()
    {
        // Use TRUNCATE to clear data and reset IDENTITY columns
        await DbContext.Database.ExecuteSqlRawAsync(@"
            DELETE FROM OfferProducts;
            DELETE FROM SupplierOffers;
            DELETE FROM Products;
            DELETE FROM Suppliers;
            
            DBCC CHECKIDENT ('Products', RESEED, 0);
            DBCC CHECKIDENT ('Suppliers', RESEED, 0);
            DBCC CHECKIDENT ('SupplierOffers', RESEED, 0);
        ").ConfigureAwait(false);
        
        // Clear EF Core's change tracker to prevent entity tracking conflicts
        DbContext.ChangeTracker.Clear();
    }
}
