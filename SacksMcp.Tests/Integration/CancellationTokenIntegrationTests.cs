using FluentAssertions;
using McpServer.Database.Tools;
using Microsoft.Extensions.Logging;
using Moq;
using SacksMcp.Tests.Fixtures;
using SacksMcp.Tests.Helpers;
using SacksMcp.Tools;
using Xunit;

namespace SacksMcp.Tests.Integration;

/// <summary>
/// Integration tests for CancellationToken support with real SQL Server LocalDB.
/// Uses TestingModeDelaySeconds to simulate slow queries that can be cancelled.
/// </summary>
[Trait("Category", "Integration")]
public class CancellationTokenIntegrationTests : IClassFixture<DatabaseFixture>, IDisposable
{
    private readonly DatabaseFixture _fixture;
    private readonly ProductTools _productTools;
    private readonly OfferTools _offerTools;
    private readonly SupplierTools _supplierTools;
    private readonly int _originalDelay;

    public CancellationTokenIntegrationTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
        
        // Save original delay and set testing delay to 2 seconds
        _originalDelay = BaseDatabaseToolCollection<SacksDataLayer.Data.SacksDbContext>.TestingModeDelaySeconds;
        BaseDatabaseToolCollection<SacksDataLayer.Data.SacksDbContext>.TestingModeDelaySeconds = 2;
        
        var productLogger = new Mock<ILogger<ProductTools>>().Object;
        var offerLogger = new Mock<ILogger<OfferTools>>().Object;
        var supplierLogger = new Mock<ILogger<SupplierTools>>().Object;
        
        _productTools = new ProductTools(_fixture.DbContext, productLogger);
        _offerTools = new OfferTools(_fixture.DbContext, offerLogger);
        _supplierTools = new SupplierTools(_fixture.DbContext, supplierLogger);
    }

    public void Dispose()
    {
        // Restore original delay
        BaseDatabaseToolCollection<SacksDataLayer.Data.SacksDbContext>.TestingModeDelaySeconds = _originalDelay;
    }

    [Fact]
    public async Task ProductTools_SearchProducts_WithCancelledToken_ThrowsOperationCanceledException()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();
        var products = new[]
        {
            new TestProductBuilder().WithName("Test Product").WithEan("1111111111111").Build()
        };
        await _fixture.SeedTestDataAsync(products: products.ToList());

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(500)); // Cancel after 500ms (before 2 second delay completes)

        // Act & Assert
        await _productTools.Invoking(x => x.SearchProducts("Test", 50, cts.Token))
            .Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task ProductTools_GetProductStatistics_WithCancelledToken_ThrowsOperationCanceledException()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();
        var products = new[]
        {
            new TestProductBuilder().WithName("Product 1").WithEan("1111111111111").Build(),
            new TestProductBuilder().WithName("Product 2").WithEan("2222222222222").Build()
        };
        await _fixture.SeedTestDataAsync(products: products.ToList());

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(500));

        // Act & Assert
        await _productTools.Invoking(x => x.GetProductStatistics(cts.Token))
            .Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task OfferTools_SearchOffers_WithCancelledToken_ThrowsOperationCanceledException()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();
        var supplier = new TestSupplierBuilder().WithName("Test Supplier").Build();
        var product = new TestProductBuilder().WithName("Test Product").WithEan("1111111111111").Build();
        await _fixture.SeedTestDataAsync(
            products: new List<SacksDataLayer.Entities.Product> { product },
            suppliers: new List<SacksDataLayer.Entities.Supplier> { supplier }
        );
        
        // Add offer after seeding to get proper IDs
        var offer = new TestOfferBuilder()
            .WithSupplierId(supplier.Id)
            .WithDescription("Test Offer")
            .Build();
        await _fixture.SeedTestDataAsync(offers: new List<SacksDataLayer.Entities.Offer> { offer });

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(500));

        // Act & Assert
        await _offerTools.Invoking(x => x.SearchOffers("Test", 50, cts.Token))
            .Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task OfferTools_GetOfferStatistics_WithCancelledToken_ThrowsOperationCanceledException()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();
        var supplier = new TestSupplierBuilder().WithName("Test Supplier").Build();
        await _fixture.SeedTestDataAsync(suppliers: new List<SacksDataLayer.Entities.Supplier> { supplier });
        
        var offer = new TestOfferBuilder()
            .WithSupplierId(supplier.Id)
            .WithDescription("Test Offer")
            .Build();
        await _fixture.SeedTestDataAsync(offers: new List<SacksDataLayer.Entities.Offer> { offer });

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(500));

        // Act & Assert
        await _offerTools.Invoking(x => x.GetOfferStatistics(cts.Token))
            .Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task SupplierTools_SearchSuppliers_WithCancelledToken_ThrowsOperationCanceledException()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();
        var suppliers = new[]
        {
            new TestSupplierBuilder().WithName("Test Supplier 1").Build(),
            new TestSupplierBuilder().WithName("Test Supplier 2").Build()
        };
        await _fixture.SeedTestDataAsync(suppliers: suppliers.ToList());

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(500));

        // Act & Assert
        await _supplierTools.Invoking(x => x.SearchSuppliers("Test", 50, cts.Token))
            .Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task SupplierTools_GetAllSuppliers_WithCancelledToken_ThrowsOperationCanceledException()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();
        var suppliers = new[]
        {
            new TestSupplierBuilder().WithName("Supplier A").Build(),
            new TestSupplierBuilder().WithName("Supplier B").Build()
        };
        await _fixture.SeedTestDataAsync(suppliers: suppliers.ToList());

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(500));

        // Act & Assert
        await _supplierTools.Invoking(x => x.GetAllSuppliers(100, cts.Token))
            .Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task SupplierTools_GetSupplierStats_WithCancelledToken_ThrowsOperationCanceledException()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();
        var supplier = new TestSupplierBuilder().WithName("Test Supplier").Build();
        await _fixture.SeedTestDataAsync(suppliers: new List<SacksDataLayer.Entities.Supplier> { supplier });

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(500));

        // Act & Assert
        await _supplierTools.Invoking(x => x.GetSupplierStats(supplier.Id, cts.Token))
            .Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task WithoutCancellation_QueryCompletesSuccessfully()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();
        var products = new[]
        {
            new TestProductBuilder().WithName("Normal Product").WithEan("1111111111111").Build()
        };
        await _fixture.SeedTestDataAsync(products: products.ToList());

        // Act - No cancellation token, query should complete despite 2 second delay
        var result = await _productTools.SearchProducts("Normal");

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.ShouldBeSuccessResponse();
        result.Should().Contain("Normal Product");
    }
}
