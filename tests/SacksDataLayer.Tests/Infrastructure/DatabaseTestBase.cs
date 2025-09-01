using Microsoft.EntityFrameworkCore;
using SacksDataLayer.Data;
using SacksDataLayer.Entities;

namespace SacksDataLayer.Tests.Infrastructure;

/// <summary>
/// Base class for tests that need a database context
/// </summary>
public abstract class DatabaseTestBase : IDisposable
{
    protected SacksDbContext Context { get; private set; }

    protected DatabaseTestBase()
    {
        var options = new DbContextOptionsBuilder<SacksDbContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;

        Context = new SacksDbContext(options);
        Context.Database.OpenConnection();
        Context.Database.EnsureCreated();
    }

    protected async Task<ProductEntity> CreateTestProductAsync(string name = "Test Product", string? ean = null)
    {
        var product = new ProductEntity
        {
            Name = name,
            EAN = ean ?? $"EAN{Random.Shared.Next(1000, 9999)}",
            Description = "Test Description",
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow
        };

        Context.Products.Add(product);
        await Context.SaveChangesAsync();
        return product;
    }

    protected async Task<SupplierEntity> CreateTestSupplierAsync(string name = "Test Supplier")
    {
        var supplier = new SupplierEntity
        {
            Name = name,
            Description = "Test Supplier Description",
            Industry = "Technology",
            Region = "Global",
            ContactName = "John Doe",
            ContactEmail = "john@testsupplier.com",
            Company = "Test Company",
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow
        };

        Context.Suppliers.Add(supplier);
        await Context.SaveChangesAsync();
        return supplier;
    }

    public void Dispose()
    {
        Context?.Dispose();
    }
}
