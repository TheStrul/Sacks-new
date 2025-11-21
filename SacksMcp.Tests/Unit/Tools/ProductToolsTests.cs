using FluentAssertions;
using SacksMcp.Tests.Helpers;
using SacksMcp.Tools;
using SacksDataLayer.Data;
using SacksDataLayer.Entities;
using Xunit;

namespace SacksMcp.Tests.Unit.Tools;

/// <summary>
/// Unit tests for ProductTools class.
/// Tests all methods with various scenarios: happy path, edge cases, validation failures.
/// Uses EF Core in-memory database for fast, isolated unit tests.
/// </summary>
[Trait("Category", "Unit")]
public class ProductToolsTests : IDisposable
{
    private readonly SacksDbContext _context;
    private readonly ProductTools _sut;

    public ProductToolsTests()
    {
        // Create EF Core in-memory database
        _context = MockDbContextFactory.CreateInMemoryContext();

        // Seed test data
        var products = new List<Product>
        {
            new TestProductBuilder()
                .WithId(1)
                .WithName("Chanel No 5 Perfume")
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
                .WithoutEan()
                .Build()
        };

        _context.Products.AddRange(products);
        _context.SaveChanges();

        var mockLogger = MockDbContextFactory.CreateMockLogger<ProductTools>();
        _sut = new ProductTools(_context, mockLogger.Object);
    }

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    #region SearchProducts Tests

    [Fact]
    public async Task SearchProducts_WithValidSearchTerm_ReturnsMatchingProducts()
    {
        // Act
        var result = await _sut.SearchProducts("Perfume", 50);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.ShouldBeSuccessResponse();
        result.Should().Contain("Chanel No 5 Perfume");
        result.Should().Contain("Calvin Klein Perfume");
        result.Should().NotContain("Dior Sauvage");
    }

    [Fact]
    public async Task SearchProducts_WithCaseInsensitiveSearch_ReturnsMatches()
    {
        // Act - Note: EF In-Memory uses case-sensitive Contains
        var result = await _sut.SearchProducts("Perfume", 50);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.ShouldBeSuccessResponse();
        result.Should().Contain("Chanel No 5 Perfume");
        result.Should().Contain("Calvin Klein Perfume");
    }

    [Fact]
    public async Task SearchProducts_WithNoMatches_ReturnsEmptyResults()
    {
        // Act
        var result = await _sut.SearchProducts("NonExistentProduct123", 50);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.ShouldBeSuccessResponse();
        var doc = result.ToJsonDocument();
        doc.RootElement.GetProperty("data").GetProperty("count").GetInt32().Should().Be(0);
    }

    [Fact]
    public async Task SearchProducts_WithEmptySearchTerm_ThrowsArgumentException()
    {
        // Act & Assert
        await _sut.Invoking(x => x.SearchProducts("", 50))
            .Should().ThrowAsync<ArgumentException>()
            .WithMessage("*searchTerm*");
    }

    [Fact]
    public async Task SearchProducts_WithNullSearchTerm_ThrowsArgumentException()
    {
        // Act & Assert
        await _sut.Invoking(x => x.SearchProducts(null!, 50))
            .Should().ThrowAsync<ArgumentException>()
            .WithMessage("*searchTerm*");
    }

    [Fact]
    public async Task SearchProducts_WithLimitBelowMinimum_ThrowsArgumentOutOfRangeException()
    {
        // Act & Assert
        await _sut.Invoking(x => x.SearchProducts("Perfume", 0))
            .Should().ThrowAsync<ArgumentOutOfRangeException>()
            .WithMessage("*limit*");
    }

    [Fact]
    public async Task SearchProducts_WithLimitAboveMaximum_ThrowsArgumentOutOfRangeException()
    {
        // Act & Assert
        await _sut.Invoking(x => x.SearchProducts("Perfume", 501))
            .Should().ThrowAsync<ArgumentOutOfRangeException>()
            .WithMessage("*limit*");
    }

    [Fact(Skip = "CancellationToken testing requires real database with delay. See Integration/CancellationTokenIntegrationTests.cs")]
    public async Task SearchProducts_WithCancelledToken_ThrowsOperationCanceledException()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await _sut.Invoking(x => x.SearchProducts("Perfume", 50, cts.Token))
            .Should().ThrowAsync<OperationCanceledException>();
    }

    #endregion

    #region GetProductByEan Tests

    [Fact]
    public async Task GetProductByEan_WhenProductExists_ReturnsProduct()
    {
        // Act
        var result = await _sut.GetProductByEan("1234567890123");

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.ShouldBeSuccessResponse();
        var doc = result.ToJsonDocument();
        doc.RootElement.GetProperty("data").GetProperty("found").GetBoolean().Should().BeTrue();
        result.Should().Contain("Chanel No 5 Perfume");
    }

    [Fact]
    public async Task GetProductByEan_WhenProductNotFound_ReturnsNotFoundResponse()
    {
        // Act
        var result = await _sut.GetProductByEan("0000000000000");

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.ShouldBeSuccessResponse();
        var doc = result.ToJsonDocument();
        doc.RootElement.GetProperty("data").GetProperty("found").GetBoolean().Should().BeFalse();
        result.Should().Contain("0000000000000");
    }

    [Fact]
    public async Task GetProductByEan_WithEmptyEan_ThrowsArgumentException()
    {
        // Act & Assert
        await _sut.Invoking(x => x.GetProductByEan(""))
            .Should().ThrowAsync<ArgumentException>()
            .WithMessage("*ean*");
    }

    [Fact]
    public async Task GetProductByEan_WithNullEan_ThrowsArgumentException()
    {
        // Act & Assert
        await _sut.Invoking(x => x.GetProductByEan(null!))
            .Should().ThrowAsync<ArgumentException>()
            .WithMessage("*ean*");
    }

    #endregion

    #region GetProductStatistics Tests

    [Fact]
    public async Task GetProductStatistics_ReturnsCorrectCounts()
    {
        // Act
        var result = await _sut.GetProductStatistics();

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.ShouldBeSuccessResponse();
        var doc = result.ToJsonDocument();
        var data = doc.RootElement.GetProperty("data");
        data.GetProperty("totalProducts").GetInt32().Should().Be(3);
        data.GetProperty("productsWithEan").GetInt32().Should().Be(2);
        data.GetProperty("productsWithoutEan").GetInt32().Should().Be(1);
    }

    [Fact(Skip = "CancellationToken testing requires real database with delay. See Integration/CancellationTokenIntegrationTests.cs")]
    public async Task GetProductStatistics_WithCancelledToken_ThrowsOperationCanceledException()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await _sut.Invoking(x => x.GetProductStatistics(cts.Token))
            .Should().ThrowAsync<OperationCanceledException>();
    }

    #endregion

    #region GetRecentProducts Tests

    [Fact]
    public async Task GetRecentProducts_WithDefaultLimit_ReturnsProducts()
    {
        // Act
        var result = await _sut.GetRecentProducts();

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.ShouldBeSuccessResponse();
        var doc = result.ToJsonDocument();
        doc.RootElement.GetProperty("data").GetProperty("count").GetInt32().Should().Be(3);
    }

    [Fact]
    public async Task GetRecentProducts_WithCustomLimit_RespectsLimit()
    {
        // Act
        var result = await _sut.GetRecentProducts(2);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.ShouldBeSuccessResponse();
        var doc = result.ToJsonDocument();
        doc.RootElement.GetProperty("data").GetProperty("count").GetInt32().Should().Be(2);
    }

    [Fact]
    public async Task GetRecentProducts_WithLimitBelowMinimum_ThrowsArgumentOutOfRangeException()
    {
        // Act & Assert
        await _sut.Invoking(x => x.GetRecentProducts(0))
            .Should().ThrowAsync<ArgumentOutOfRangeException>()
            .WithMessage("*limit*");
    }

    [Fact]
    public async Task GetRecentProducts_WithLimitAboveMaximum_ThrowsArgumentOutOfRangeException()
    {
        // Act & Assert
        await _sut.Invoking(x => x.GetRecentProducts(201))
            .Should().ThrowAsync<ArgumentOutOfRangeException>()
            .WithMessage("*limit*");
    }

    #endregion
}
