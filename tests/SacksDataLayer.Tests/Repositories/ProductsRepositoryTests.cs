using SacksDataLayer.Entities;
using SacksDataLayer.Repositories.Implementations;
using SacksDataLayer.Tests.Infrastructure;
using Xunit;

namespace SacksDataLayer.Tests.Repositories;

public class ProductsRepositoryTests : DatabaseTestBase
{
    private readonly ProductsRepository _repository;

    public ProductsRepositoryTests()
    {
        _repository = new ProductsRepository(Context);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllProducts()
    {
        // Arrange
        await CreateTestProductAsync("Product 1");
        await CreateTestProductAsync("Product 2");

        // Act
        var products = await _repository.GetAllAsync(CancellationToken.None);

        // Assert
        Assert.Equal(2, products.Count());
        Assert.Contains(products, p => p.Name == "Product 1");
        Assert.Contains(products, p => p.Name == "Product 2");
    }

    [Fact]
    public async Task GetByIdAsync_WithValidId_ReturnsProduct()
    {
        // Arrange
        var product = await CreateTestProductAsync("Test Product");

        // Act
        var result = await _repository.GetByIdAsync(product.Id, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(product.Id, result.Id);
        Assert.Equal("Test Product", result.Name);
    }

    [Fact]
    public async Task GetByIdAsync_WithInvalidId_ReturnsNull()
    {
        // Act
        var result = await _repository.GetByIdAsync(999, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task CreateAsync_ValidProduct_SavesAndReturnsProduct()
    {
        // Arrange
        var product = new ProductEntity
        {
            Name = "New Product",
            EAN = "EAN123456",
            Description = "New Product Description",
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow
        };

        // Act
        var result = await _repository.CreateAsync(product, null, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Id > 0);
        Assert.Equal("New Product", result.Name);
        
        // Verify it was saved to database
        var saved = await Context.Products.FindAsync(result.Id);
        Assert.NotNull(saved);
        Assert.Equal("New Product", saved.Name);
    }
}
