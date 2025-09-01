using Microsoft.EntityFrameworkCore;
using SacksDataLayer.Entities;
using SacksDataLayer.Tests.Infrastructure;
using Xunit;

namespace SacksDataLayer.Tests.Integration;

public class TransactionalTests : DatabaseTestBase
{
    [Fact]
    public async Task CreateSupplierWithOfferAndProducts_TransactionallySucceeds()
    {
        // Arrange
        using var transaction = await Context.Database.BeginTransactionAsync();

        try
        {
            // Create supplier
            var supplier = new SupplierEntity
            {
                Name = "Transactional Supplier",
                Description = "Test supplier for transactional test",
                Industry = "Technology",
                Region = "Global",
                ContactName = "Jane Doe",
                ContactEmail = "jane@supplier.com",
                Company = "Supplier Corp",
                CreatedAt = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow
            };
            Context.Suppliers.Add(supplier);
            await Context.SaveChangesAsync();

            // Create offer
            var offer = new SupplierOfferEntity
            {
                SupplierId = supplier.Id,
                OfferName = "Test Offer",
                Description = "Test offer description",
                Currency = "USD",
                OfferType = "Standard",
                Version = "1.0",
                ValidFrom = DateTime.UtcNow,
                ValidTo = DateTime.UtcNow.AddDays(30),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow
            };
            Context.SupplierOffers.Add(offer);
            await Context.SaveChangesAsync();

            // Create products
            var product1 = new ProductEntity
            {
                Name = "Product 1",
                EAN = "EAN001",
                Description = "First product",
                CreatedAt = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow
            };

            var product2 = new ProductEntity
            {
                Name = "Product 2", 
                EAN = "EAN002",
                Description = "Second product",
                CreatedAt = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow
            };

            Context.Products.AddRange(product1, product2);
            await Context.SaveChangesAsync();

            // Create offer-product relationships
            var offerProduct1 = new OfferProductEntity
            {
                OfferId = offer.Id,
                ProductId = product1.Id,
                Price = 10.99m,
                UnitOfMeasure = "piece",
                CreatedAt = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow
            };

            var offerProduct2 = new OfferProductEntity
            {
                OfferId = offer.Id,
                ProductId = product2.Id,
                Price = 15.99m,
                UnitOfMeasure = "piece",
                CreatedAt = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow
            };

            Context.OfferProducts.AddRange(offerProduct1, offerProduct2);
            await Context.SaveChangesAsync();

            // Commit transaction
            await transaction.CommitAsync();

            // Assert - Verify all entities were created successfully
            var savedSupplier = await Context.Suppliers
                .Include(s => s.Offers)
                .ThenInclude(o => o.OfferProducts)
                .ThenInclude(op => op.Product)
                .FirstOrDefaultAsync(s => s.Id == supplier.Id);

            Assert.NotNull(savedSupplier);
            Assert.Equal("Transactional Supplier", savedSupplier.Name);
            Assert.Single(savedSupplier.Offers);
            
            var savedOffer = savedSupplier.Offers.First();
            Assert.Equal("Test Offer", savedOffer.OfferName);
            Assert.Equal(2, savedOffer.OfferProducts.Count);

            var offerProducts = savedOffer.OfferProducts.ToList();
            Assert.Contains(offerProducts, op => op.Product.Name == "Product 1" && op.Price == 10.99m);
            Assert.Contains(offerProducts, op => op.Product.Name == "Product 2" && op.Price == 15.99m);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    [Fact]
    public async Task CreateSupplierWithInvalidOffer_RollsBackTransaction()
    {
        // Arrange
        using var transaction = await Context.Database.BeginTransactionAsync();

        var initialSupplierCount = await Context.Suppliers.CountAsync();

        try
        {
            // Create supplier
            var supplier = new SupplierEntity
            {
                Name = "Rollback Test Supplier",
                Description = "This should be rolled back",
                Industry = "Technology",
                Region = "Global", 
                ContactName = "Test User",
                ContactEmail = "test@rollback.com",
                Company = "Rollback Corp",
                CreatedAt = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow
            };
            Context.Suppliers.Add(supplier);
            await Context.SaveChangesAsync();

            // Try to create duplicate supplier name (should cause unique constraint violation)
            var duplicateSupplier = new SupplierEntity
            {
                Name = "Rollback Test Supplier", // Same name - should violate unique constraint
                Description = "Duplicate supplier",
                Industry = "Technology",
                Region = "Global",
                ContactName = "Test User 2",
                ContactEmail = "test2@rollback.com",
                Company = "Rollback Corp 2",
                CreatedAt = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow
            };
            Context.Suppliers.Add(duplicateSupplier);

            // This should fail due to unique constraint on Name
            await Assert.ThrowsAsync<DbUpdateException>(async () => await Context.SaveChangesAsync());

            await transaction.RollbackAsync();

            // Assert - Verify rollback worked
            var finalSupplierCount = await Context.Suppliers.CountAsync();
            Assert.Equal(initialSupplierCount, finalSupplierCount);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
