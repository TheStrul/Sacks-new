using FluentAssertions;
using SacksMcp.Tests.Helpers;
using SacksMcp.Tools;
using SacksDataLayer.Data;
using SacksDataLayer.Entities;
using Xunit;

namespace SacksMcp.Tests.Unit.Tools;

/// <summary>
/// Unit tests for OfferTools class.
/// Tests all methods with various scenarios: happy path, edge cases, validation failures.
/// Uses EF Core in-memory database for fast, isolated unit tests.
/// </summary>
[Trait("Category", "Unit")]
public class OfferToolsTests : IDisposable
{
    private readonly SacksDbContext _context;
    private readonly OfferTools _sut;

    public OfferToolsTests()
    {
        // Create EF Core in-memory database
        _context = MockDbContextFactory.CreateInMemoryContext();

        // Seed test data
        var suppliers = new List<Supplier>
        {
            new TestSupplierBuilder()
                .WithId(1)
                .WithName("Premium Cosmetics Ltd")
                .WithDescription("High-end beauty products")
                .Build(),
            new TestSupplierBuilder()
                .WithId(2)
                .WithName("Discount Beauty Co")
                .WithDescription("Budget-friendly cosmetics")
                .Build(),
            new TestSupplierBuilder()
                .WithId(3)
                .WithName("Luxury Perfumes Inc")
                .Build()
        };

        var products = new List<Product>
        {
            new TestProductBuilder()
                .WithId(1)
                .WithName("Chanel No 5")
                .WithEan("1234567890123")
                .Build(),
            new TestProductBuilder()
                .WithId(2)
                .WithName("Dior Sauvage")
                .WithEan("9876543210987")
                .Build(),
            new TestProductBuilder()
                .WithId(3)
                .WithName("Calvin Klein Perfume")
                .WithEan("1111222233334")
                .Build()
        };

        var offers = new List<Offer>
        {
            new TestOfferBuilder()
                .WithId(1)
                .WithOfferName("Summer Sale 2025")
                .WithDescription("Best summer deals")
                .WithSupplierId(1)
                .WithCurrency("USD")
                .CreatedAt(DateTime.UtcNow.AddDays(-5))
                .Build(),
            new TestOfferBuilder()
                .WithId(2)
                .WithOfferName("Winter Collection")
                .WithDescription("New winter arrivals")
                .WithSupplierId(1)
                .WithCurrency("EUR")
                .CreatedAt(DateTime.UtcNow.AddDays(-10))
                .Build(),
            new TestOfferBuilder()
                .WithId(3)
                .WithOfferName("Budget Perfumes")
                .WithDescription("Affordable fragrances")
                .WithSupplierId(2)
                .WithCurrency("USD")
                .CreatedAt(DateTime.UtcNow.AddDays(-2))
                .Build(),
            new TestOfferBuilder()
                .WithId(4)
                .WithOfferName("Luxury Selection")
                .WithSupplierId(3)
                .WithCurrency("USD")
                .CreatedAt(DateTime.UtcNow.AddDays(-1))
                .Build()
        };

        var productOffers = new List<ProductOffer>
        {
            new TestProductOfferBuilder()
                .WithId(1)
                .WithProductId(1)
                .WithOfferId(1)
                .WithPrice(150.00m)
                .WithCurrency("USD")
                .Build(),
            new TestProductOfferBuilder()
                .WithId(2)
                .WithProductId(2)
                .WithOfferId(1)
                .WithPrice(120.00m)
                .WithCurrency("USD")
                .Build(),
            new TestProductOfferBuilder()
                .WithId(3)
                .WithProductId(1)
                .WithOfferId(2)
                .WithPrice(140.00m)
                .WithCurrency("EUR")
                .Build(),
            new TestProductOfferBuilder()
                .WithId(4)
                .WithProductId(3)
                .WithOfferId(3)
                .WithPrice(45.00m)
                .WithCurrency("USD")
                .Build(),
            new TestProductOfferBuilder()
                .WithId(5)
                .WithProductId(1)
                .WithOfferId(4)
                .WithPrice(200.00m)
                .WithCurrency("USD")
                .Build()
        };

        _context.Suppliers.AddRange(suppliers);
        _context.Products.AddRange(products);
        _context.SupplierOffers.AddRange(offers);
        _context.OfferProducts.AddRange(productOffers);
        _context.SaveChanges();

        var mockLogger = MockDbContextFactory.CreateMockLogger<OfferTools>();
        _sut = new OfferTools(_context, mockLogger.Object);
    }

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    #region GetRecentOffers Tests

    [Fact]
    public async Task GetRecentOffers_WithDefaultLimit_ReturnsRecentOffers()
    {
        // Act
        var result = await _sut.GetRecentOffers();

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.ShouldBeSuccessResponse();
        var doc = result.ToJsonDocument();
        var data = doc.RootElement.GetProperty("data");
        data.GetProperty("count").GetInt32().Should().Be(4);
        data.GetProperty("limit").GetInt32().Should().Be(20);
        result.Should().Contain("Luxury Selection"); // Most recent
        result.Should().Contain("Premium Cosmetics Ltd");
    }

    [Fact]
    public async Task GetRecentOffers_WithCustomLimit_RespectsLimit()
    {
        // Act
        var result = await _sut.GetRecentOffers(2);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.ShouldBeSuccessResponse();
        var doc = result.ToJsonDocument();
        doc.RootElement.GetProperty("data").GetProperty("count").GetInt32().Should().Be(2);
        result.Should().Contain("Luxury Selection"); // Most recent
        result.Should().Contain("Budget Perfumes");
    }

    [Fact]
    public async Task GetRecentOffers_OrdersByCreatedAtDescending()
    {
        // Act
        var result = await _sut.GetRecentOffers(10);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.ShouldBeSuccessResponse();
        var doc = result.ToJsonDocument();
        var offers = doc.RootElement.GetProperty("data").GetProperty("offers");
        var firstOffer = offers[0].GetProperty("OfferName").GetString();
        firstOffer.Should().Be("Luxury Selection"); // Most recent
    }

    [Fact]
    public async Task GetRecentOffers_IncludesSupplierNameAndProductCount()
    {
        // Act
        var result = await _sut.GetRecentOffers();

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.ShouldBeSuccessResponse();
        result.Should().Contain("\"SupplierName\":");
        result.Should().Contain("\"ProductCount\":");
        result.Should().Contain("Premium Cosmetics Ltd");
    }

    [Fact]
    public async Task GetRecentOffers_WithLimitBelowMinimum_ThrowsArgumentOutOfRangeException()
    {
        // Act & Assert
        await _sut.Invoking(x => x.GetRecentOffers(0))
            .Should().ThrowAsync<ArgumentOutOfRangeException>()
            .WithMessage("*limit*");
    }

    [Fact]
    public async Task GetRecentOffers_WithLimitAboveMaximum_ThrowsArgumentOutOfRangeException()
    {
        // Act & Assert
        await _sut.Invoking(x => x.GetRecentOffers(201))
            .Should().ThrowAsync<ArgumentOutOfRangeException>()
            .WithMessage("*limit*");
    }

    [Fact(Skip = "CancellationToken testing requires real database with delay. See Integration/CancellationTokenIntegrationTests.cs")]
    public async Task GetRecentOffers_WithCancelledToken_ThrowsOperationCanceledException()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await _sut.Invoking(x => x.GetRecentOffers(20, cts.Token))
            .Should().ThrowAsync<OperationCanceledException>();
    }

    #endregion

    #region GetOffersBySupplier Tests

    [Fact]
    public async Task GetOffersBySupplier_WithValidSupplierId_ReturnsSupplierOffers()
    {
        // Act
        var result = await _sut.GetOffersBySupplier(1);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.ShouldBeSuccessResponse();
        var doc = result.ToJsonDocument();
        var data = doc.RootElement.GetProperty("data");
        data.GetProperty("found").GetBoolean().Should().BeTrue();
        data.GetProperty("supplierId").GetInt32().Should().Be(1);
        data.GetProperty("offerCount").GetInt32().Should().Be(2);
        result.Should().Contain("Summer Sale 2025");
        result.Should().Contain("Winter Collection");
    }

    [Fact]
    public async Task GetOffersBySupplier_WithCustomLimit_RespectsLimit()
    {
        // Act
        var result = await _sut.GetOffersBySupplier(1, 1);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.ShouldBeSuccessResponse();
        var doc = result.ToJsonDocument();
        var data = doc.RootElement.GetProperty("data");
        data.GetProperty("offerCount").GetInt32().Should().Be(1);
        data.GetProperty("limit").GetInt32().Should().Be(1);
    }

    [Fact]
    public async Task GetOffersBySupplier_WithNonExistentSupplier_ReturnsNotFound()
    {
        // Act
        var result = await _sut.GetOffersBySupplier(999);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.ShouldBeSuccessResponse();
        var doc = result.ToJsonDocument();
        var data = doc.RootElement.GetProperty("data");
        data.GetProperty("found").GetBoolean().Should().BeFalse();
        data.GetProperty("supplierId").GetInt32().Should().Be(999);
    }

    [Fact]
    public async Task GetOffersBySupplier_OrdersByCreatedAtDescending()
    {
        // Act
        var result = await _sut.GetOffersBySupplier(1);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.ShouldBeSuccessResponse();
        var doc = result.ToJsonDocument();
        var offers = doc.RootElement.GetProperty("data").GetProperty("offers");
        var firstOffer = offers[0].GetProperty("OfferName").GetString();
        firstOffer.Should().Be("Summer Sale 2025"); // More recent than Winter Collection
    }

    [Fact]
    public async Task GetOffersBySupplier_WithLimitBelowMinimum_ThrowsArgumentOutOfRangeException()
    {
        // Act & Assert
        await _sut.Invoking(x => x.GetOffersBySupplier(1, 0))
            .Should().ThrowAsync<ArgumentOutOfRangeException>()
            .WithMessage("*limit*");
    }

    [Fact]
    public async Task GetOffersBySupplier_WithLimitAboveMaximum_ThrowsArgumentOutOfRangeException()
    {
        // Act & Assert
        await _sut.Invoking(x => x.GetOffersBySupplier(1, 201))
            .Should().ThrowAsync<ArgumentOutOfRangeException>()
            .WithMessage("*limit*");
    }

    [Fact(Skip = "CancellationToken testing requires real database with delay. See Integration/CancellationTokenIntegrationTests.cs")]
    public async Task GetOffersBySupplier_WithCancelledToken_ThrowsOperationCanceledException()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await _sut.Invoking(x => x.GetOffersBySupplier(1, 50, cts.Token))
            .Should().ThrowAsync<OperationCanceledException>();
    }

    #endregion

    #region SearchOffers Tests

    [Fact]
    public async Task SearchOffers_WithValidSearchTerm_ReturnsMatchingOffers()
    {
        // Act
        var result = await _sut.SearchOffers("Summer");

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.ShouldBeSuccessResponse();
        var doc = result.ToJsonDocument();
        var data = doc.RootElement.GetProperty("data");
        data.GetProperty("count").GetInt32().Should().Be(1);
        result.Should().Contain("Summer Sale 2025");
        result.Should().NotContain("Winter Collection");
    }

    [Fact]
    public async Task SearchOffers_SearchesInDescription_ReturnsMatches()
    {
        // Act
        var result = await _sut.SearchOffers("deals");

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.ShouldBeSuccessResponse();
        result.Should().Contain("Summer Sale 2025");
        result.Should().Contain("Best summer deals");
    }

    [Fact]
    public async Task SearchOffers_WithCustomLimit_RespectsLimit()
    {
        // Act
        var result = await _sut.SearchOffers("Collection", 1);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.ShouldBeSuccessResponse();
        var doc = result.ToJsonDocument();
        doc.RootElement.GetProperty("data").GetProperty("limit").GetInt32().Should().Be(1);
    }

    [Fact]
    public async Task SearchOffers_WithNoMatches_ReturnsEmptyResults()
    {
        // Act
        var result = await _sut.SearchOffers("NonExistentOffer123");

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.ShouldBeSuccessResponse();
        var doc = result.ToJsonDocument();
        doc.RootElement.GetProperty("data").GetProperty("count").GetInt32().Should().Be(0);
    }

    [Fact]
    public async Task SearchOffers_WithEmptySearchTerm_ThrowsArgumentException()
    {
        // Act & Assert
        await _sut.Invoking(x => x.SearchOffers(""))
            .Should().ThrowAsync<ArgumentException>()
            .WithMessage("*searchTerm*");
    }

    [Fact]
    public async Task SearchOffers_WithNullSearchTerm_ThrowsArgumentException()
    {
        // Act & Assert
        await _sut.Invoking(x => x.SearchOffers(null!))
            .Should().ThrowAsync<ArgumentException>()
            .WithMessage("*searchTerm*");
    }

    [Fact]
    public async Task SearchOffers_WithLimitBelowMinimum_ThrowsArgumentOutOfRangeException()
    {
        // Act & Assert
        await _sut.Invoking(x => x.SearchOffers("Summer", 0))
            .Should().ThrowAsync<ArgumentOutOfRangeException>()
            .WithMessage("*limit*");
    }

    [Fact]
    public async Task SearchOffers_WithLimitAboveMaximum_ThrowsArgumentOutOfRangeException()
    {
        // Act & Assert
        await _sut.Invoking(x => x.SearchOffers("Summer", 201))
            .Should().ThrowAsync<ArgumentOutOfRangeException>()
            .WithMessage("*limit*");
    }

    [Fact(Skip = "CancellationToken testing requires real database with delay. See Integration/CancellationTokenIntegrationTests.cs")]
    public async Task SearchOffers_WithCancelledToken_ThrowsOperationCanceledException()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await _sut.Invoking(x => x.SearchOffers("Summer", 50, cts.Token))
            .Should().ThrowAsync<OperationCanceledException>();
    }

    #endregion

    #region GetExpensiveProducts Tests

    [Fact]
    public async Task GetExpensiveProducts_WithMinPrice_ReturnsProductsAboveThreshold()
    {
        // Act
        var result = await _sut.GetExpensiveProducts(100.00m);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.ShouldBeSuccessResponse();
        var doc = result.ToJsonDocument();
        var data = doc.RootElement.GetProperty("data");
        data.GetProperty("count").GetInt32().Should().Be(4); // 150, 120, 140, 200 (not 45)
        result.Should().Contain("Chanel No 5");
        result.Should().Contain("Dior Sauvage");
        result.Should().NotContain("Calvin Klein");
    }

    [Fact]
    public async Task GetExpensiveProducts_WithCurrencyFilter_ReturnsOnlyMatchingCurrency()
    {
        // Act
        var result = await _sut.GetExpensiveProducts(100.00m, "EUR");

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.ShouldBeSuccessResponse();
        var doc = result.ToJsonDocument();
        var data = doc.RootElement.GetProperty("data");
        data.GetProperty("count").GetInt32().Should().Be(1);
        data.GetProperty("currency").GetString().Should().Be("EUR");
        result.Should().Contain("Chanel No 5");
    }

    [Fact]
    public async Task GetExpensiveProducts_OrdersByPriceDescending()
    {
        // Act
        var result = await _sut.GetExpensiveProducts(50.00m);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.ShouldBeSuccessResponse();
        var doc = result.ToJsonDocument();
        var products = doc.RootElement.GetProperty("data").GetProperty("products");
        var firstPrice = products[0].GetProperty("Price").GetDecimal();
        firstPrice.Should().Be(200.00m); // Highest price
    }

    [Fact]
    public async Task GetExpensiveProducts_WithCustomLimit_RespectsLimit()
    {
        // Act
        var result = await _sut.GetExpensiveProducts(50.00m, null, 2);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.ShouldBeSuccessResponse();
        var doc = result.ToJsonDocument();
        var data = doc.RootElement.GetProperty("data");
        data.GetProperty("count").GetInt32().Should().BeLessThanOrEqualTo(2);
        data.GetProperty("limit").GetInt32().Should().Be(2);
    }

    [Fact]
    public async Task GetExpensiveProducts_WithLimitBelowMinimum_ThrowsArgumentOutOfRangeException()
    {
        // Act & Assert
        await _sut.Invoking(x => x.GetExpensiveProducts(100.00m, null, 0))
            .Should().ThrowAsync<ArgumentOutOfRangeException>()
            .WithMessage("*limit*");
    }

    [Fact]
    public async Task GetExpensiveProducts_WithLimitAboveMaximum_ThrowsArgumentOutOfRangeException()
    {
        // Act & Assert
        await _sut.Invoking(x => x.GetExpensiveProducts(100.00m, null, 501))
            .Should().ThrowAsync<ArgumentOutOfRangeException>()
            .WithMessage("*limit*");
    }

    [Fact(Skip = "CancellationToken testing requires real database with delay. See Integration/CancellationTokenIntegrationTests.cs")]
    public async Task GetExpensiveProducts_WithCancelledToken_ThrowsOperationCanceledException()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await _sut.Invoking(x => x.GetExpensiveProducts(100.00m, null, 50, cts.Token))
            .Should().ThrowAsync<OperationCanceledException>();
    }

    #endregion

    #region CompareProductPrices Tests

    [Fact]
    public async Task CompareProductPrices_WithValidProductId_ReturnsAllOffersForProduct()
    {
        // Act
        var result = await _sut.CompareProductPrices(1); // Chanel No 5 in 3 offers

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.ShouldBeSuccessResponse();
        var doc = result.ToJsonDocument();
        var data = doc.RootElement.GetProperty("data");
        data.GetProperty("found").GetBoolean().Should().BeTrue();
        data.GetProperty("productId").GetInt32().Should().Be(1);
        data.GetProperty("productName").GetString().Should().Be("Chanel No 5");
        var offers = data.GetProperty("offers");
        offers.GetArrayLength().Should().Be(3);
    }

    [Fact]
    public async Task CompareProductPrices_CalculatesStatisticsCorrectly()
    {
        // Act
        var result = await _sut.CompareProductPrices(1); // Prices: 150, 140, 200

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.ShouldBeSuccessResponse();
        var doc = result.ToJsonDocument();
        var stats = doc.RootElement.GetProperty("data").GetProperty("statistics");
        stats.GetProperty("minPrice").GetDecimal().Should().Be(140.00m);
        stats.GetProperty("maxPrice").GetDecimal().Should().Be(200.00m);
        stats.GetProperty("offerCount").GetInt32().Should().Be(3);
    }

    [Fact]
    public async Task CompareProductPrices_OrdersByPriceAscending()
    {
        // Act
        var result = await _sut.CompareProductPrices(1);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.ShouldBeSuccessResponse();
        var doc = result.ToJsonDocument();
        var offers = doc.RootElement.GetProperty("data").GetProperty("offers");
        var firstPrice = offers[0].GetProperty("Price").GetDecimal();
        firstPrice.Should().Be(140.00m); // Lowest price first
    }

    [Fact]
    public async Task CompareProductPrices_WithNonExistentProduct_ReturnsNotFound()
    {
        // Act
        var result = await _sut.CompareProductPrices(999);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.ShouldBeSuccessResponse();
        var doc = result.ToJsonDocument();
        var data = doc.RootElement.GetProperty("data");
        data.GetProperty("found").GetBoolean().Should().BeFalse();
        data.GetProperty("productId").GetInt32().Should().Be(999);
    }

    [Fact]
    public async Task CompareProductPrices_IncludesSupplierInformation()
    {
        // Act
        var result = await _sut.CompareProductPrices(1);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.ShouldBeSuccessResponse();
        result.Should().Contain("\"SupplierName\":");
        result.Should().Contain("Premium Cosmetics Ltd");
    }

    [Fact(Skip = "CancellationToken testing requires real database with delay. See Integration/CancellationTokenIntegrationTests.cs")]
    public async Task CompareProductPrices_WithCancelledToken_ThrowsOperationCanceledException()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await _sut.Invoking(x => x.CompareProductPrices(1, cts.Token))
            .Should().ThrowAsync<OperationCanceledException>();
    }

    #endregion

    #region GetOfferStatistics Tests

    [Fact]
    public async Task GetOfferStatistics_ReturnsTotalOfferCount()
    {
        // Act
        var result = await _sut.GetOfferStatistics();

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.ShouldBeSuccessResponse();
        var doc = result.ToJsonDocument();
        var data = doc.RootElement.GetProperty("data");
        data.GetProperty("totalOffers").GetInt32().Should().Be(4);
    }

    [Fact]
    public async Task GetOfferStatistics_ReturnsRecentOffersCount()
    {
        // Act
        var result = await _sut.GetOfferStatistics();

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.ShouldBeSuccessResponse();
        var doc = result.ToJsonDocument();
        var data = doc.RootElement.GetProperty("data");
        data.GetProperty("recentOffers").GetInt32().Should().Be(4); // All within 30 days
    }

    [Fact]
    public async Task GetOfferStatistics_ReturnsTotalOfferProductsCount()
    {
        // Act
        var result = await _sut.GetOfferStatistics();

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.ShouldBeSuccessResponse();
        var doc = result.ToJsonDocument();
        var data = doc.RootElement.GetProperty("data");
        data.GetProperty("totalOfferProducts").GetInt32().Should().Be(5);
    }

    [Fact]
    public async Task GetOfferStatistics_CalculatesAverageProductsPerOffer()
    {
        // Act
        var result = await _sut.GetOfferStatistics();

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.ShouldBeSuccessResponse();
        var doc = result.ToJsonDocument();
        var data = doc.RootElement.GetProperty("data");
        var avgProducts = data.GetProperty("avgProductsPerOffer").GetDouble();
        avgProducts.Should().Be(1.25); // 5 products / 4 offers = 1.25
    }

    [Fact]
    public async Task GetOfferStatistics_WithNoOffers_HandlesGracefully()
    {
        // Arrange - Clear all data
        _context.OfferProducts.RemoveRange(_context.OfferProducts);
        _context.SupplierOffers.RemoveRange(_context.SupplierOffers);
        _context.SaveChanges();

        // Act
        var result = await _sut.GetOfferStatistics();

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.ShouldBeSuccessResponse();
        var doc = result.ToJsonDocument();
        var data = doc.RootElement.GetProperty("data");
        data.GetProperty("totalOffers").GetInt32().Should().Be(0);
        data.GetProperty("avgProductsPerOffer").GetDouble().Should().Be(0);
    }

    [Fact(Skip = "CancellationToken testing requires real database with delay. See Integration/CancellationTokenIntegrationTests.cs")]
    public async Task GetOfferStatistics_WithCancelledToken_ThrowsOperationCanceledException()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await _sut.Invoking(x => x.GetOfferStatistics(cts.Token))
            .Should().ThrowAsync<OperationCanceledException>();
    }

    #endregion
}
