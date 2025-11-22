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
/// MCP tools for supplier-related database operations in the Sacks system.
/// Provides AI-accessible methods for querying and analyzing supplier data.
/// </summary>
[McpServerToolType]
public class SupplierTools : BaseDatabaseToolCollection<SacksDbContext>
{
    private readonly ConnectionTracker _connectionTracker;

    public SupplierTools(SacksDbContext dbContext, ILogger<SupplierTools> logger, ConnectionTracker connectionTracker) 
        : base(dbContext, logger)
    {
        _connectionTracker = connectionTracker;
    }

    /// <summary>
    /// Search suppliers by name.
    /// </summary>
    [McpServerTool]
    [Description("Search for suppliers by name. Returns matching suppliers with their basic information and offer count.")]
    public async Task<string> SearchSuppliers(
        [Description("Search term to match against supplier names (case-insensitive)")] string searchTerm,
        [Description("Maximum number of results to return (default: 50, max: 200)")] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        _connectionTracker.OnFirstRequest();
        Logger.LogInformation("[MCP] SearchSuppliers called: searchTerm='{SearchTerm}', limit={Limit}", searchTerm, limit);
        ValidateRequired(searchTerm, nameof(searchTerm));
        ValidateRange(limit, 1, 200, nameof(limit));

        return await ExecuteQueryAsync(async () =>
        {
            var suppliers = await DbContext.Suppliers
                .AsNoTracking()
                .Where(s => s.Name.Contains(searchTerm))
                .OrderBy(s => s.Name)
                .Take(limit)
                .Select(s => new
                {
                    s.Id,
                    s.Name,
                    s.Description,
                    OfferCount = s.Offers.Count
                })
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return new
            {
                searchTerm,
                count = suppliers.Count,
                limit,
                suppliers
            };
        }, "SearchSuppliers", cancellationToken);
    }

    /// <summary>
    /// Get supplier statistics.
    /// </summary>
    [McpServerTool]
    [Description("Get detailed statistics for a specific supplier including offer count, product count, and recent activity.")]
    public async Task<string> GetSupplierStats(
        [Description("Supplier ID to get statistics for")] int supplierId,
        CancellationToken cancellationToken = default)
    {
        Logger.LogInformation("[MCP] GetSupplierStats called: supplierId={SupplierId}", supplierId);
        return await ExecuteQueryAsync<object>(async () =>
        {
            var supplier = await DbContext.Suppliers
                .AsNoTracking()
                .Where(s => s.Id == supplierId)
                .Select(s => new
                {
                    s.Id,
                    s.Name,
                    s.Description,
                    OfferCount = s.Offers.Count,
                    ProductCount = s.Offers
                        .SelectMany(o => o.OfferProducts)
                        .Select(op => op.ProductId)
                        .Distinct()
                        .Count(),
                    RecentOffers = s.Offers
                        .Where(o => o.CreatedAt >= DateTime.UtcNow.AddDays(-30))
                        .Count()
                })
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            return supplier != null
                ? (object)new { found = true, supplier }
                : (object)new { found = false, supplierId };
        }, "GetSupplierStats", cancellationToken);
    }

    /// <summary>
    /// Get all suppliers.
    /// </summary>
    [McpServerTool]
    [Description("Get a list of all suppliers in the system with their basic information and offer counts.")]
    public async Task<string> GetAllSuppliers(
        [Description("Maximum number of results to return (default: 100, max: 500)")] int limit = 100,
        CancellationToken cancellationToken = default)
    {
        Logger.LogInformation("[MCP] GetAllSuppliers called: limit={Limit}", limit);
        ValidateRange(limit, 1, 500, nameof(limit));

        return await ExecuteQueryAsync(async () =>
        {
            var suppliers = await DbContext.Suppliers
                .AsNoTracking()
                .OrderBy(s => s.Name)
                .Take(limit)
                .Select(s => new
                {
                    s.Id,
                    s.Name,
                    s.Description,
                    OfferCount = s.Offers.Count
                })
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return new
            {
                count = suppliers.Count,
                limit,
                suppliers
            };
        }, "GetAllSuppliers", cancellationToken);
    }

    /// <summary>
    /// Get suppliers with most offers.
    /// </summary>
    [McpServerTool]
    [Description("Get suppliers ranked by the number of offers they have. Useful for identifying major suppliers.")]
    public async Task<string> GetSuppliersWithMostOffers(
        [Description("Number of top suppliers to return (default: 10, max: 100)")] int limit = 10,
        CancellationToken cancellationToken = default)
    {
        Logger.LogInformation("[MCP] GetSuppliersWithMostOffers called: limit={Limit}", limit);
        ValidateRange(limit, 1, 100, nameof(limit));

        return await ExecuteQueryAsync(async () =>
        {
            var suppliers = await DbContext.Suppliers
                .AsNoTracking()
                .Select(s => new
                {
                    s.Id,
                    s.Name,
                    s.Description,
                    OfferCount = s.Offers.Count,
                    ProductCount = s.Offers
                        .SelectMany(o => o.OfferProducts)
                        .Select(op => op.ProductId)
                        .Distinct()
                        .Count()
                })
                .Where(s => s.OfferCount > 0)
                .OrderByDescending(s => s.OfferCount)
                .Take(limit)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return new
            {
                count = suppliers.Count,
                limit,
                suppliers
            };
        }, "GetSuppliersWithMostOffers", cancellationToken);
    }

    /// <summary>
    /// Get products from a specific supplier.
    /// </summary>
    [McpServerTool]
    [Description("Get all unique products offered by a specific supplier across all their offers.")]
    public async Task<string> GetSupplierProducts(
        [Description("Supplier ID to get products for")] int supplierId,
        [Description("Maximum number of products to return (default: 100, max: 500)")] int limit = 100,
        CancellationToken cancellationToken = default)
    {
        Logger.LogInformation("[MCP] GetSupplierProducts called: supplierId={SupplierId}, limit={Limit}", supplierId, limit);
        ValidateRange(limit, 1, 500, nameof(limit));

        return await ExecuteQueryAsync<object>(async () =>
        {
            var supplierExists = await DbContext.Suppliers
                .AnyAsync(s => s.Id == supplierId, cancellationToken)
                .ConfigureAwait(false);

            if (!supplierExists)
            {
                return (object)new { found = false, supplierId };
            }

            var products = await DbContext.OfferProducts
                .AsNoTracking()
                .Where(op => op.Offer.SupplierId == supplierId)
                .Select(op => new
                {
                    op.Product.Id,
                    op.Product.Name,
                    op.Product.EAN,
                    Price = op.Price,
                    Currency = op.Currency,
                    OfferName = op.Offer.OfferName
                })
                .Distinct()
                .Take(limit)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return (object)new
            {
                found = true,
                supplierId,
                productCount = products.Count,
                limit,
                products
            };
        }, "GetSupplierProducts", cancellationToken);
    }
}
