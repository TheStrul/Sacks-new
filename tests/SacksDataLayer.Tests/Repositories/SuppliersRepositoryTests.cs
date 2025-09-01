using SacksDataLayer.Entities;
using SacksDataLayer.Repositories.Implementations;
using SacksDataLayer.Tests.Infrastructure;
using Xunit;

namespace SacksDataLayer.Tests.Repositories;

public class SuppliersRepositoryTests : DatabaseTestBase
{
    private readonly SuppliersRepository _repository;

    public SuppliersRepositoryTests()
    {
        _repository = new SuppliersRepository(Context);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllSuppliers()
    {
        // Arrange
        await CreateTestSupplierAsync("Supplier 1");
        await CreateTestSupplierAsync("Supplier 2");

        // Act
        var suppliers = await _repository.GetAllAsync(CancellationToken.None);

        // Assert
        Assert.Equal(2, suppliers.Count());
        Assert.Contains(suppliers, s => s.Name == "Supplier 1");
        Assert.Contains(suppliers, s => s.Name == "Supplier 2");
    }

    [Fact]
    public async Task GetByNameAsync_WithValidName_ReturnsSupplier()
    {
        // Arrange
        await CreateTestSupplierAsync("ACME Corp");

        // Act
        var result = await _repository.GetByNameAsync("ACME Corp", CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("ACME Corp", result.Name);
    }

    [Fact]
    public async Task GetByNameAsync_WithInvalidName_ReturnsNull()
    {
        // Act
        var result = await _repository.GetByNameAsync("NonExistent", CancellationToken.None);

        // Assert
        Assert.Null(result);
    }
}
