using FluentAssertions;
using SacksMcp.Tests.Helpers;
using SacksMcp.Tools;
using Sacks.DataAccess.Data;
using Sacks.Core.Entities;
using Xunit;

namespace SacksMcp.Tests.Unit.Tools;

/// <summary>
/// Unit tests for SupplierTools class.
/// Tests all methods with various scenarios: happy path, edge cases, validation failures.
/// Uses EF Core in-memory database for fast, isolated unit tests.
/// </summary>
[Trait("Category", "Unit")]
public class SupplierToolsTests : IDisposable
{
    private readonly SacksDbContext _context;
    private readonly SupplierTools _sut;

    public SupplierToolsTests()
    {
        // Create EF Core in-memory database
        _context = MockDbContextFactory.CreateInMemoryContext();

        // Seed test data
        var suppliers = new List<Supplier>
        {
            new TestSupplierBuilder()
                .WithId(1)
                .WithName("Premium Cosmetics Ltd")
                .WithDescription("High-end beauty products supplier")
                .Build(),
            new TestSupplierBuilder()
                .WithId(2)
                .WithName("Discount Beauty Co")
                .WithDescription("Budget-friendly cosmetics distributor")
                .Build(),
            new TestSupplierBuilder()
                .WithId(3)
                .WithName("Luxury Perfumes Inc")
                .WithDescription("Exclusive fragrances from around the world")
                .Build(),
            new TestSupplierBuilder()
                .WithId(4)
                .WithName("Beauty Supply Warehouse")
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
                .Build(),
            new TestProductBuilder()
                .WithId(4)
                .WithName("Gucci Bloom")
                .WithEan("5555666677778")
                .Build()
        };

        var offers = new List<Offer>
        {
            new TestOfferBuilder()
                .WithId(1)
                .WithOfferName("Premium Summer 2025")
                .WithSupplierId(1)
                .WithCurrency("USD")
                .CreatedAt(DateTime.UtcNow.AddDays(-5))
                .Build(),
            new TestOfferBuilder()
                .WithId(2)
                .WithOfferName("Premium Winter 2025")
                .WithSupplierId(1)
                .WithCurrency("EUR")
                .CreatedAt(DateTime.UtcNow.AddDays(-10))
                .Build(),
            new TestOfferBuilder()
                .WithId(3)
                .WithOfferName("Premium Spring 2025")
                .WithSupplierId(1)
                .WithCurrency("USD")
                .CreatedAt(DateTime.UtcNow.AddDays(-20))
                .Build(),
            new TestOfferBuilder()
                .WithId(4)
                .WithOfferName("Discount Budget Perfumes")
                .WithSupplierId(2)
                .WithCurrency("USD")
                .CreatedAt(DateTime.UtcNow.AddDays(-2))
                .Build(),
            new TestOfferBuilder()
                .WithId(5)
                .WithOfferName("Luxury Selection")
                .WithSupplierId(3)
                .WithCurrency("USD")
                .CreatedAt(DateTime.UtcNow.AddDays(-1))
                .Build()
        };

        var productOffers = new List<ProductOffer>
        {
            // Supplier 1 (Premium) - 4 products across 3 offers
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
                .WithProductId(4)
                .WithOfferId(3)
                .WithPrice(180.00m)
                .WithCurrency("USD")
                .Build(),
            // Supplier 2 (Discount) - 1 product
            new TestProductOfferBuilder()
                .WithId(5)
                .WithProductId(3)
                .WithOfferId(4)
                .WithPrice(45.00m)
                .WithCurrency("USD")
                .Build(),
            // Supplier 3 (Luxury) - 2 products
            new TestProductOfferBuilder()
                .WithId(6)
                .WithProductId(1)
                .WithOfferId(5)
                .WithPrice(200.00m)
                .WithCurrency("USD")
                .Build(),
            new TestProductOfferBuilder()
                .WithId(7)
                .WithProductId(4)
                .WithOfferId(5)
                .WithPrice(220.00m)
                .WithCurrency("USD")
                .Build()
        };

        _context.Suppliers.AddRange(suppliers);
        _context.Products.AddRange(products);
        _context.SupplierOffers.AddRange(offers);
        _context.OfferProducts.AddRange(productOffers);
        _context.SaveChanges();

        var mockLogger = MockDbContextFactory.CreateMockLogger<SupplierTools>();
        var connectionTracker = Helpers.TestHelpers.CreateMockConnectionTracker();
        _sut = new SupplierTools(_context, mockLogger.Object, connectionTracker);
    }

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    #region SearchSuppliers Tests

    [Fact]
    public async Task SearchSuppliers_WithValidSearchTerm_ReturnsMatchingSuppliers()
    {
        // Act
        var result = await _sut.SearchSuppliers("Premium");

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.ShouldBeSuccessResponse();
        var doc = result.ToJsonDocument();
        var data = doc.RootElement.GetProperty("data");
        data.GetProperty("count").GetInt32().Should().Be(1);
        result.Should().Contain("Premium Cosmetics Ltd");
        result.Should().NotContain("Discount Beauty Co");
    }

    [Fact]
    public async Task SearchSuppliers_WithPartialMatch_ReturnsAllMatches()
    {
        // Act
        var result = await _sut.SearchSuppliers("Beauty");

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.ShouldBeSuccessResponse();
        var doc = result.ToJsonDocument();
        var data = doc.RootElement.GetProperty("data");
        data.GetProperty("count").GetInt32().Should().Be(2);
        result.Should().Contain("Discount Beauty Co");
        result.Should().Contain("Beauty Supply Warehouse");
    }

    [Fact]
    public async Task SearchSuppliers_WithCustomLimit_RespectsLimit()
    {
        // Act
        var result = await _sut.SearchSuppliers("Beauty", 1);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.ShouldBeSuccessResponse();
        var doc = result.ToJsonDocument();
        var data = doc.RootElement.GetProperty("data");
        data.GetProperty("count").GetInt32().Should().Be(1);
        data.GetProperty("limit").GetInt32().Should().Be(1);
    }

    [Fact]
    public async Task SearchSuppliers_WithNoMatches_ReturnsEmptyResults()
    {
        // Act
        var result = await _sut.SearchSuppliers("NonExistentSupplier123");

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.ShouldBeSuccessResponse();
        var doc = result.ToJsonDocument();
        doc.RootElement.GetProperty("data").GetProperty("count").GetInt32().Should().Be(0);
    }

    [Fact]
    public async Task SearchSuppliers_WithEmptySearchTerm_ThrowsArgumentException()
    {
        // Act & Assert
        await _sut.Invoking(x => x.SearchSuppliers(""))
            .Should().ThrowAsync<ArgumentException>()
            .WithMessage("*searchTerm*");
    }

    [Fact]
    public async Task SearchSuppliers_WithNullSearchTerm_ThrowsArgumentException()
    {
        // Act & Assert
        await _sut.Invoking(x => x.SearchSuppliers(null!))
            .Should().ThrowAsync<ArgumentException>()
            .WithMessage("*searchTerm*");
    }

    [Fact]
    public async Task SearchSuppliers_WithLimitBelowMinimum_ThrowsArgumentOutOfRangeException()
    {
        // Act & Assert
        await _sut.Invoking(x => x.SearchSuppliers("Beauty", 0))
            .Should().ThrowAsync<ArgumentOutOfRangeException>()
            .WithMessage("*limit*");
    }

    [Fact]
    public async Task SearchSuppliers_WithLimitAboveMaximum_ThrowsArgumentOutOfRangeException()
    {
        // Act & Assert
        await _sut.Invoking(x => x.SearchSuppliers("Beauty", 201))
            .Should().ThrowAsync<ArgumentOutOfRangeException>()
            .WithMessage("*limit*");
    }

    [Fact(Skip = "CancellationToken testing requires real database with delay. See Integration/CancellationTokenIntegrationTests.cs")]
    public async Task SearchSuppliers_WithCancelledToken_ThrowsOperationCanceledException()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await _sut.Invoking(x => x.SearchSuppliers("Beauty", 50, cts.Token))
            .Should().ThrowAsync<OperationCanceledException>();
    }

    #endregion

    #region GetSupplierStats Tests

    [Fact]
    public async Task GetSupplierStats_WithValidSupplierId_ReturnsStats()
    {
        // Act
        var result = await _sut.GetSupplierStats(1);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.ShouldBeSuccessResponse();
        var doc = result.ToJsonDocument();
        var data = doc.RootElement.GetProperty("data");
        data.GetProperty("found").GetBoolean().Should().BeTrue();
        var supplier = data.GetProperty("supplier");
        supplier.GetProperty("Id").GetInt32().Should().Be(1);
        supplier.GetProperty("Name").GetString().Should().Be("Premium Cosmetics Ltd");
        supplier.GetProperty("OfferCount").GetInt32().Should().Be(3);
    }

    [Fact]
    public async Task GetSupplierStats_CalculatesProductCountCorrectly()
    {
        // Act - Supplier 1 has products: 1, 2, 1 (duplicate), 4 = 3 unique products
        var result = await _sut.GetSupplierStats(1);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.ShouldBeSuccessResponse();
        var doc = result.ToJsonDocument();
        var supplier = doc.RootElement.GetProperty("data").GetProperty("supplier");
        supplier.GetProperty("ProductCount").GetInt32().Should().Be(3); // Unique products
    }

    [Fact]
    public async Task GetSupplierStats_CalculatesRecentOffersCorrectly()
    {
        // Act - All offers are within 30 days
        var result = await _sut.GetSupplierStats(1);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.ShouldBeSuccessResponse();
        var doc = result.ToJsonDocument();
        var supplier = doc.RootElement.GetProperty("data").GetProperty("supplier");
        supplier.GetProperty("RecentOffers").GetInt32().Should().Be(3);
    }

    [Fact]
    public async Task GetSupplierStats_WithNonExistentSupplier_ReturnsNotFound()
    {
        // Act
        var result = await _sut.GetSupplierStats(999);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.ShouldBeSuccessResponse();
        var doc = result.ToJsonDocument();
        var data = doc.RootElement.GetProperty("data");
        data.GetProperty("found").GetBoolean().Should().BeFalse();
        data.GetProperty("supplierId").GetInt32().Should().Be(999);
    }

    [Fact]
    public async Task GetSupplierStats_IncludesDescription()
    {
        // Act
        var result = await _sut.GetSupplierStats(1);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.ShouldBeSuccessResponse();
        result.Should().Contain("High-end beauty products supplier");
    }

    [Fact(Skip = "CancellationToken testing requires real database with delay. See Integration/CancellationTokenIntegrationTests.cs")]
    public async Task GetSupplierStats_WithCancelledToken_ThrowsOperationCanceledException()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await _sut.Invoking(x => x.GetSupplierStats(1, cts.Token))
            .Should().ThrowAsync<OperationCanceledException>();
    }

    #endregion

    #region GetAllSuppliers Tests

    [Fact]
    public async Task GetAllSuppliers_WithDefaultLimit_ReturnsAllSuppliers()
    {
        // Act
        var result = await _sut.GetAllSuppliers();

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.ShouldBeSuccessResponse();
        var doc = result.ToJsonDocument();
        var data = doc.RootElement.GetProperty("data");
        data.GetProperty("count").GetInt32().Should().Be(4);
        data.GetProperty("limit").GetInt32().Should().Be(100);
    }

    [Fact]
    public async Task GetAllSuppliers_WithCustomLimit_RespectsLimit()
    {
        // Act
        var result = await _sut.GetAllSuppliers(2);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.ShouldBeSuccessResponse();
        var doc = result.ToJsonDocument();
        var data = doc.RootElement.GetProperty("data");
        data.GetProperty("count").GetInt32().Should().Be(2);
        data.GetProperty("limit").GetInt32().Should().Be(2);
    }

    [Fact]
    public async Task GetAllSuppliers_OrdersByNameAlphabetically()
    {
        // Act
        var result = await _sut.GetAllSuppliers();

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.ShouldBeSuccessResponse();
        var doc = result.ToJsonDocument();
        var suppliers = doc.RootElement.GetProperty("data").GetProperty("suppliers");
        var firstName = suppliers[0].GetProperty("Name").GetString();
        firstName.Should().Be("Beauty Supply Warehouse"); // Alphabetically first
    }

    [Fact]
    public async Task GetAllSuppliers_IncludesOfferCounts()
    {
        // Act
        var result = await _sut.GetAllSuppliers();

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.ShouldBeSuccessResponse();
        result.Should().Contain("\"OfferCount\":");
    }

    [Fact]
    public async Task GetAllSuppliers_WithLimitBelowMinimum_ThrowsArgumentOutOfRangeException()
    {
        // Act & Assert
        await _sut.Invoking(x => x.GetAllSuppliers(0))
            .Should().ThrowAsync<ArgumentOutOfRangeException>()
            .WithMessage("*limit*");
    }

    [Fact]
    public async Task GetAllSuppliers_WithLimitAboveMaximum_ThrowsArgumentOutOfRangeException()
    {
        // Act & Assert
        await _sut.Invoking(x => x.GetAllSuppliers(501))
            .Should().ThrowAsync<ArgumentOutOfRangeException>()
            .WithMessage("*limit*");
    }

    [Fact(Skip = "CancellationToken testing requires real database with delay. See Integration/CancellationTokenIntegrationTests.cs")]
    public async Task GetAllSuppliers_WithCancelledToken_ThrowsOperationCanceledException()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await _sut.Invoking(x => x.GetAllSuppliers(100, cts.Token))
            .Should().ThrowAsync<OperationCanceledException>();
    }

    #endregion

    #region GetSuppliersWithMostOffers Tests

    [Fact]
    public async Task GetSuppliersWithMostOffers_WithDefaultLimit_ReturnsTopSuppliers()
    {
        // Act
        var result = await _sut.GetSuppliersWithMostOffers();

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.ShouldBeSuccessResponse();
        var doc = result.ToJsonDocument();
        var data = doc.RootElement.GetProperty("data");
        data.GetProperty("count").GetInt32().Should().Be(3); // Only 3 suppliers have offers
    }

    [Fact]
    public async Task GetSuppliersWithMostOffers_OrdersByOfferCountDescending()
    {
        // Act
        var result = await _sut.GetSuppliersWithMostOffers();

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.ShouldBeSuccessResponse();
        var doc = result.ToJsonDocument();
        var suppliers = doc.RootElement.GetProperty("data").GetProperty("suppliers");
        var topSupplier = suppliers[0];
        topSupplier.GetProperty("Name").GetString().Should().Be("Premium Cosmetics Ltd");
        topSupplier.GetProperty("OfferCount").GetInt32().Should().Be(3);
    }

    [Fact]
    public async Task GetSuppliersWithMostOffers_ExcludesSuppliersWithNoOffers()
    {
        // Act
        var result = await _sut.GetSuppliersWithMostOffers();

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.ShouldBeSuccessResponse();
        result.Should().NotContain("Beauty Supply Warehouse"); // Has 0 offers
    }

    [Fact]
    public async Task GetSuppliersWithMostOffers_IncludesProductCount()
    {
        // Act
        var result = await _sut.GetSuppliersWithMostOffers();

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.ShouldBeSuccessResponse();
        result.Should().Contain("\"ProductCount\":");
    }

    [Fact]
    public async Task GetSuppliersWithMostOffers_WithCustomLimit_RespectsLimit()
    {
        // Act
        var result = await _sut.GetSuppliersWithMostOffers(1);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.ShouldBeSuccessResponse();
        var doc = result.ToJsonDocument();
        var data = doc.RootElement.GetProperty("data");
        data.GetProperty("count").GetInt32().Should().Be(1);
        data.GetProperty("limit").GetInt32().Should().Be(1);
    }

    [Fact]
    public async Task GetSuppliersWithMostOffers_WithLimitBelowMinimum_ThrowsArgumentOutOfRangeException()
    {
        // Act & Assert
        await _sut.Invoking(x => x.GetSuppliersWithMostOffers(0))
            .Should().ThrowAsync<ArgumentOutOfRangeException>()
            .WithMessage("*limit*");
    }

    [Fact]
    public async Task GetSuppliersWithMostOffers_WithLimitAboveMaximum_ThrowsArgumentOutOfRangeException()
    {
        // Act & Assert
        await _sut.Invoking(x => x.GetSuppliersWithMostOffers(101))
            .Should().ThrowAsync<ArgumentOutOfRangeException>()
            .WithMessage("*limit*");
    }

    [Fact(Skip = "CancellationToken testing requires real database with delay. See Integration/CancellationTokenIntegrationTests.cs")]
    public async Task GetSuppliersWithMostOffers_WithCancelledToken_ThrowsOperationCanceledException()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await _sut.Invoking(x => x.GetSuppliersWithMostOffers(10, cts.Token))
            .Should().ThrowAsync<OperationCanceledException>();
    }

    #endregion

    #region GetSupplierProducts Tests

    [Fact]
    public async Task GetSupplierProducts_WithValidSupplierId_ReturnsProducts()
    {
        // Act
        var result = await _sut.GetSupplierProducts(1);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.ShouldBeSuccessResponse();
        var doc = result.ToJsonDocument();
        var data = doc.RootElement.GetProperty("data");
        data.GetProperty("found").GetBoolean().Should().BeTrue();
        data.GetProperty("supplierId").GetInt32().Should().Be(1);
        result.Should().Contain("Chanel No 5");
        result.Should().Contain("Dior Sauvage");
        result.Should().Contain("Gucci Bloom");
    }

    [Fact]
    public async Task GetSupplierProducts_WithCustomLimit_RespectsLimit()
    {
        // Act
        var result = await _sut.GetSupplierProducts(1, 2);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.ShouldBeSuccessResponse();
        var doc = result.ToJsonDocument();
        var data = doc.RootElement.GetProperty("data");
        data.GetProperty("limit").GetInt32().Should().Be(2);
    }

    [Fact]
    public async Task GetSupplierProducts_WithNonExistentSupplier_ReturnsNotFound()
    {
        // Act
        var result = await _sut.GetSupplierProducts(999);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.ShouldBeSuccessResponse();
        var doc = result.ToJsonDocument();
        var data = doc.RootElement.GetProperty("data");
        data.GetProperty("found").GetBoolean().Should().BeFalse();
        data.GetProperty("supplierId").GetInt32().Should().Be(999);
    }

    [Fact]
    public async Task GetSupplierProducts_IncludesPriceAndOfferInfo()
    {
        // Act
        var result = await _sut.GetSupplierProducts(1);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.ShouldBeSuccessResponse();
        result.Should().Contain("\"Price\":");
        result.Should().Contain("\"Currency\":");
        result.Should().Contain("\"OfferName\":");
    }

    [Fact]
    public async Task GetSupplierProducts_ReturnsProductCount()
    {
        // Act
        var result = await _sut.GetSupplierProducts(1);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.ShouldBeSuccessResponse();
        var doc = result.ToJsonDocument();
        var data = doc.RootElement.GetProperty("data");
        data.GetProperty("productCount").GetInt32().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetSupplierProducts_WithLimitBelowMinimum_ThrowsArgumentOutOfRangeException()
    {
        // Act & Assert
        await _sut.Invoking(x => x.GetSupplierProducts(1, 0))
            .Should().ThrowAsync<ArgumentOutOfRangeException>()
            .WithMessage("*limit*");
    }

    [Fact]
    public async Task GetSupplierProducts_WithLimitAboveMaximum_ThrowsArgumentOutOfRangeException()
    {
        // Act & Assert
        await _sut.Invoking(x => x.GetSupplierProducts(1, 501))
            .Should().ThrowAsync<ArgumentOutOfRangeException>()
            .WithMessage("*limit*");
    }

    [Fact(Skip = "CancellationToken testing requires real database with delay. See Integration/CancellationTokenIntegrationTests.cs")]
    public async Task GetSupplierProducts_WithCancelledToken_ThrowsOperationCanceledException()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await _sut.Invoking(x => x.GetSupplierProducts(1, 100, cts.Token))
            .Should().ThrowAsync<OperationCanceledException>();
    }

    #endregion
}
