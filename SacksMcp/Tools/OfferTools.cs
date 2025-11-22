#nullable disable warnings

using System.ComponentModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using McpServer.Database.Tools;
using Sacks.DataAccess.Data;
using Sacks.Core.Entities;
using SacksMcp.Services;

namespace SacksMcp.Tools;

/// <summary>
/// MCP tools for offer-related database operations in the Sacks system.
/// Provides AI-accessible methods for querying and analyzing offer and product-offer data.
/// </summary>
[McpServerToolType]
public class OfferTools : BaseDatabaseToolCollection<SacksDbContext>
{
    private readonly ConnectionTracker _connectionTracker;

    public OfferTools(SacksDbContext dbContext, ILogger<OfferTools> logger, ConnectionTracker connectionTracker) 
        : base(dbContext, logger)
    {
        _connectionTracker = connectionTracker;
    }

    /// <summary>
    /// Get recent offers.
    /// </summary>
    [McpServerTool]
    [Description("Get the most recently created supplier offers with basic information including supplier name and product count.")]
    public async Task<string> GetRecentOffers(
        [Description("Number of recent offers to return (default: 20, max: 200)")] int limit = 20,
        CancellationToken cancellationToken = default)
    {
        _connectionTracker.OnFirstRequest();
        Logger.LogInformation("[MCP] GetRecentOffers called: limit={Limit}", limit);
        ValidateRange(limit, 1, 200, nameof(limit));

        return await ExecuteQueryAsync(async () =>
        {
            var offers = await DbContext.SupplierOffers
                .AsNoTracking()
                .OrderByDescending(o => o.CreatedAt)
                .Take(limit)
                .Select(o => new
                {
                    o.Id,
                    o.OfferName,
                    o.Description,
                    SupplierName = o.Supplier.Name ?? "",
                    o.SupplierId,
                    o.Currency,
                    ProductCount = o.OfferProducts.Count,
                    o.CreatedAt
                })
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return new
            {
                count = offers.Count,
                limit,
                offers
            };
        }, "GetRecentOffers", cancellationToken);
    }

    /// <summary>
    /// Get offers by supplier.
    /// </summary>
    [McpServerTool]
    [Description("Get all offers from a specific supplier with details about products and pricing.")]
    public async Task<string> GetOffersBySupplier(
        [Description("Supplier ID to get offers for")] int supplierId,
        [Description("Maximum number of offers to return (default: 50, max: 200)")] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        Logger.LogInformation("[MCP] GetOffersBySupplier called: supplierId={SupplierId}, limit={Limit}", supplierId, limit);
        ValidateRange(limit, 1, 200, nameof(limit));

        return await ExecuteQueryAsync<object>(async () =>
        {
            var supplierExists = await DbContext.Suppliers
                .AnyAsync(s => s.Id == supplierId, cancellationToken)
                .ConfigureAwait(false);

            if (!supplierExists)
            {
                return (object)new { found = false, supplierId };
            }

            var offers = await DbContext.SupplierOffers
                .AsNoTracking()
                .Where(o => o.SupplierId == supplierId)
                .OrderByDescending(o => o.CreatedAt)
                .Take(limit)
                .Select(o => new
                {
                    o.Id,
                    OfferName = o.OfferName ?? "",
                    o.Description,
                    o.Currency,
                    ProductCount = o.OfferProducts.Count,
                    o.CreatedAt,
                    o.ModifiedAt
                })
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return (object)new
            {
                found = true,
                supplierId,
                offerCount = offers.Count,
                limit,
                offers
            };
        }, "GetOffersBySupplier", cancellationToken);
    }

    /// <summary>
    /// Search offers by name.
    /// </summary>
    [McpServerTool]
    [Description("Search for offers by name or description. Returns matching offers with supplier information.")]
    public async Task<string> SearchOffers(
        [Description("Search term to match against offer names or descriptions (case-insensitive)")] string searchTerm,
        [Description("Maximum number of results to return (default: 50, max: 200)")] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        Logger.LogInformation("[MCP] SearchOffers called: searchTerm='{SearchTerm}', limit={Limit}", searchTerm, limit);
        ValidateRequired(searchTerm, nameof(searchTerm));
        ValidateRange(limit, 1, 200, nameof(limit));

        return await ExecuteQueryAsync(async () =>
        {
            var offers = await DbContext.SupplierOffers
                .AsNoTracking()
                .Where(o => (o.OfferName != null && o.OfferName.Contains(searchTerm)) || 
                           (o.Description != null && o.Description.Contains(searchTerm)))
                .OrderByDescending(o => o.CreatedAt)
                .Take(limit)
                .Select(o => new
                {
                    o.Id,
                    o.OfferName,
                    o.Description,
                    SupplierName = o.Supplier.Name ?? "",
                    o.SupplierId,
                    o.Currency,
                    ProductCount = o.OfferProducts.Count,
                    o.CreatedAt
                })
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return new
            {
                searchTerm,
                count = offers.Count,
                limit,
                offers
            };
        }, "SearchOffers", cancellationToken);
    }

    /// <summary>
    /// Get expensive products from offers.
    /// </summary>
    [McpServerTool]
    [Description("Find products with prices above a threshold across all offers. Returns products sorted by price descending.")]
    public async Task<string> GetExpensiveProducts(
        [Description("Minimum price threshold")] decimal minPrice,
        [Description("Currency code to filter by (optional, e.g., 'USD', 'EUR')")] string? currency = null,
        [Description("Maximum number of results to return (default: 50, max: 500)")] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        Logger.LogInformation("[MCP] GetExpensiveProducts called: minPrice={MinPrice}, currency='{Currency}', limit={Limit}", minPrice, currency ?? "all", limit);
        ValidateRange(limit, 1, 500, nameof(limit));

        return await ExecuteQueryAsync(async () =>
        {
            var query = DbContext.OfferProducts
                .AsNoTracking()
                .Where(op => op.Price >= minPrice);

            if (!string.IsNullOrWhiteSpace(currency))
            {
                query = query.Where(op => op.Currency == currency);
            }

            var products = await query
                .OrderByDescending(op => op.Price)
                .Take(limit)
                .Select(op => new
                {
                    ProductId = op.Product.Id,
                    ProductName = op.Product.Name,
                    op.Product.EAN,
                    op.Price,
                    op.Currency,
                    OfferName = op.Offer.OfferName ?? "",
                    SupplierName = op.Offer.Supplier.Name ?? "",
                    op.Description
                })
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return new
            {
                minPrice,
                currency = currency ?? "all",
                count = products.Count,
                limit,
                products
            };
        }, "GetExpensiveProducts", cancellationToken);
    }

    /// <summary>
    /// Compare prices for a product.
    /// </summary>
    [McpServerTool]
    [Description("Compare prices for a specific product across all offers and suppliers. Useful for price analysis.")]
    public async Task<string> CompareProductPrices(
        [Description("Product ID to compare prices for")] int productId,
        CancellationToken cancellationToken = default)
    {
        Logger.LogInformation("[MCP] CompareProductPrices called: productId={ProductId}", productId);
        return await ExecuteQueryAsync<object>(async () =>
        {
            var productExists = await DbContext.Products
                .AnyAsync(p => p.Id == productId, cancellationToken)
                .ConfigureAwait(false);

            if (!productExists)
            {
                return (object)new { found = false, productId };
            }

            var productName = await DbContext.Products
                .Where(p => p.Id == productId)
                .Select(p => p.Name)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            var offers = await DbContext.OfferProducts
                .AsNoTracking()
                .Where(op => op.ProductId == productId)
                .Select(op => new
                {
                    OfferId = op.Offer.Id,
                    OfferName = op.Offer.OfferName ?? "",
                    SupplierName = op.Offer.Supplier.Name ?? "",
                    SupplierId = op.Offer.SupplierId,
                    op.Price,
                    op.Currency,
                    op.Description,
                    CreatedAt = op.Offer.CreatedAt
                })
                .OrderBy(op => op.Price)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            var priceStats = offers.Any() ? new
            {
                minPrice = offers.Min(o => o.Price),
                maxPrice = offers.Max(o => o.Price),
                avgPrice = offers.Average(o => o.Price),
                offerCount = offers.Count
            } : null;

            return (object)new
            {
                found = true,
                productId,
                productName,
                statistics = priceStats,
                offers
            };
        }, "CompareProductPrices", cancellationToken);
    }

    /// <summary>
    /// Get offer statistics.
    /// </summary>
    [McpServerTool]
    [Description("Get overall statistics about offers in the database including total count, recent offers, and product distribution.")]
    public async Task<string> GetOfferStatistics(
        CancellationToken cancellationToken = default)
    {
        Logger.LogInformation("[MCP] GetOfferStatistics called");
        return await ExecuteQueryAsync(async () =>
        {
            var totalOffers = await DbContext.SupplierOffers
                .CountAsync(cancellationToken)
                .ConfigureAwait(false);

            var recentOffers = await DbContext.SupplierOffers
                .Where(o => o.CreatedAt >= DateTime.UtcNow.AddDays(-30))
                .CountAsync(cancellationToken)
                .ConfigureAwait(false);

            var totalOfferProducts = await DbContext.OfferProducts
                .CountAsync(cancellationToken)
                .ConfigureAwait(false);

            var avgProductsPerOffer = totalOffers > 0
                ? (double)totalOfferProducts / totalOffers
                : 0;

            return new
            {
                totalOffers,
                recentOffers,
                totalOfferProducts,
                avgProductsPerOffer = Math.Round(avgProductsPerOffer, 2)
            };
        }, "GetOfferStatistics", cancellationToken);
    }
}
