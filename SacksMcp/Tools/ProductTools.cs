using System.ComponentModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using McpServer.Database.Tools;
using SacksDataLayer.Data;
using SacksDataLayer.Entities;

namespace SacksMcp.Tools;

/// <summary>
/// MCP tools for product-related database operations in the Sacks system.
/// Provides AI-accessible methods for querying and analyzing product data.
/// </summary>
[McpServerToolType]
public class ProductTools : BaseDatabaseToolCollection<SacksDbContext>
{
    public ProductTools(SacksDbContext dbContext, ILogger<ProductTools> logger) 
        : base(dbContext, logger) { }

    /// <summary>
    /// Search products by name with optional filters.
    /// </summary>
    [McpServerTool]
    [Description("Search for products by name. Returns matching products with their details including EAN, brand, and creation date.")]
    public async Task<string> SearchProducts(
        [Description("Search term to match against product names (case-insensitive)")] string searchTerm,
        [Description("Maximum number of results to return (default: 50, max: 500)")] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        ValidateRequired(searchTerm, nameof(searchTerm));
        ValidateRange(limit, 1, 500, nameof(limit));

        return await ExecuteQueryAsync(async () =>
        {
            var products = await DbContext.Products
                .AsNoTracking()
                .Where(p => p.Name.Contains(searchTerm))
                .OrderBy(p => p.Name)
                .Take(limit)
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.EAN,
                    Brand = p.DynamicPropertiesJson != null ? 
                        System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(p.DynamicPropertiesJson) : null,
                    p.CreatedAt,
                    p.ModifiedAt
                })
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return new
            {
                searchTerm,
                count = products.Count,
                limit,
                products
            };
        }, "SearchProducts", cancellationToken);
    }

    /// <summary>
    /// Get products by EAN code.
    /// </summary>
    [McpServerTool]
    [Description("Find a product by its EAN (European Article Number) barcode. Returns product details if found.")]
    public async Task<string> GetProductByEan(
        [Description("EAN barcode to search for")] string ean,
        CancellationToken cancellationToken = default)
    {
        ValidateRequired(ean, nameof(ean));

        return await ExecuteQueryAsync<object>(async () =>
        {
            var product = await DbContext.Products
                .AsNoTracking()
                .Where(p => p.EAN == ean)
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.EAN,
                    DynamicProperties = p.DynamicPropertiesJson,
                    p.CreatedAt,
                    p.ModifiedAt,
                    OfferCount = p.OfferProducts.Count
                })
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            return product != null 
                ? (object)new { found = true, product }
                : (object)new { found = false, ean };
        }, "GetProductByEan", cancellationToken);
    }

    /// <summary>
    /// Get product statistics.
    /// </summary>
    [McpServerTool]
    [Description("Get overall statistics about products in the database including total count, products with EAN, recent products, and modification stats.")]
    public async Task<string> GetProductStatistics(
        CancellationToken cancellationToken = default)
    {
        return await ExecuteQueryAsync(async () =>
        {
            var totalCount = await DbContext.Products.CountAsync(cancellationToken).ConfigureAwait(false);
            
            var withEan = await DbContext.Products
                .Where(p => p.EAN != null && p.EAN != "")
                .CountAsync(cancellationToken)
                .ConfigureAwait(false);
            
            var recentProducts = await DbContext.Products
                .Where(p => p.CreatedAt >= DateTime.UtcNow.AddDays(-30))
                .CountAsync(cancellationToken)
                .ConfigureAwait(false);
            
            var modifiedRecently = await DbContext.Products
                .Where(p => p.ModifiedAt >= DateTime.UtcNow.AddDays(-7))
                .CountAsync(cancellationToken)
                .ConfigureAwait(false);

            return new
            {
                totalProducts = totalCount,
                productsWithEan = withEan,
                productsWithoutEan = totalCount - withEan,
                createdLast30Days = recentProducts,
                modifiedLast7Days = modifiedRecently
            };
        }, "GetProductStatistics", cancellationToken);
    }

    /// <summary>
    /// Get products with most offers.
    /// </summary>
    [McpServerTool]
    [Description("Get products that have the most supplier offers. Useful for identifying popular or well-distributed products.")]
    public async Task<string> GetProductsWithMostOffers(
        [Description("Number of top products to return (default: 10, max: 100)")] int limit = 10,
        CancellationToken cancellationToken = default)
    {
        ValidateRange(limit, 1, 100, nameof(limit));

        return await ExecuteQueryAsync(async () =>
        {
            var products = await DbContext.Products
                .AsNoTracking()
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.EAN,
                    OfferCount = p.OfferProducts.Count,
                    p.CreatedAt
                })
                .Where(p => p.OfferCount > 0)
                .OrderByDescending(p => p.OfferCount)
                .Take(limit)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return new
            {
                count = products.Count,
                limit,
                products
            };
        }, "GetProductsWithMostOffers", cancellationToken);
    }

    /// <summary>
    /// Get recent products.
    /// </summary>
    [McpServerTool]
    [Description("Get the most recently added products to the database.")]
    public async Task<string> GetRecentProducts(
        [Description("Number of recent products to return (default: 20, max: 200)")] int limit = 20,
        CancellationToken cancellationToken = default)
    {
        ValidateRange(limit, 1, 200, nameof(limit));

        return await ExecuteQueryAsync(async () =>
        {
            var products = await DbContext.Products
                .AsNoTracking()
                .OrderByDescending(p => p.CreatedAt)
                .Take(limit)
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.EAN,
                    p.CreatedAt,
                    OfferCount = p.OfferProducts.Count
                })
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return new
            {
                count = products.Count,
                limit,
                products
            };
        }, "GetRecentProducts", cancellationToken);
    }
}
