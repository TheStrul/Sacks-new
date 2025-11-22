using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SacksMcp.Tests.Helpers;
using SacksMcp.Tools;
using Sacks.DataAccess.Data;

namespace SacksMcp.Tests.Performance.Benchmarks;

/// <summary>
/// Performance benchmarks for ProductTools operations.
/// Run with: dotnet run -c Release --project SacksMcp.Tests
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net90)]
public class ProductToolsBenchmarks
{
    private SacksDbContext _context = null!;
    private ProductTools _tools = null!;

    [GlobalSetup]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<SacksDbContext>()
            .UseInMemoryDatabase(databaseName: "BenchmarkDb")
            .Options;

        _context = new SacksDbContext(options);

        // Seed with realistic data volume
        var products = TestProductBuilder.BuildMany(10000);
        _context.Products.AddRange(products);
        _context.SaveChanges();

        var mockLogger = new Mock<ILogger<ProductTools>>();
        var connectionTracker = Helpers.TestHelpers.CreateMockConnectionTracker();
        _tools = new ProductTools(_context, mockLogger.Object, connectionTracker);
    }

    [Benchmark]
    public async Task SearchProducts_10Results()
    {
        await _tools.SearchProducts("Product", 10);
    }

    [Benchmark]
    public async Task SearchProducts_50Results()
    {
        await _tools.SearchProducts("Product", 50);
    }

    [Benchmark]
    public async Task SearchProducts_500Results()
    {
        await _tools.SearchProducts("Product", 500);
    }

    [Benchmark]
    public async Task GetProductByEan()
    {
        await _tools.GetProductByEan("1234567890123");
    }

    [Benchmark]
    public async Task GetProductStatistics()
    {
        await _tools.GetProductStatistics();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _context.Dispose();
    }
}
