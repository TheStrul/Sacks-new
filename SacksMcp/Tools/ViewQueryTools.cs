using System.ComponentModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using McpServer.Database.Tools;
using Sacks.DataAccess.Data;
using SacksMcp.Services;
using System.Text.Json;

namespace SacksMcp.Tools;

/// <summary>
/// MCP tools for querying optimized database views.
/// Provides AI-accessible methods for complex product+offer queries using pre-built SQL views.
/// </summary>
[McpServerToolType]
public class ViewQueryTools : BaseDatabaseToolCollection<SacksDbContext>
{
    private readonly ConnectionTracker _connectionTracker;

    public ViewQueryTools(SacksDbContext dbContext, ILogger<ViewQueryTools> logger, ConnectionTracker connectionTracker) 
        : base(dbContext, logger)
    {
        _connectionTracker = connectionTracker;
    }

    /// <summary>
    /// Query the ProductOffersView - shows all offers for each product with pricing details.
    /// Use this when you need to see ALL offers/prices for products, including price comparisons.
    /// </summary>
    [McpServerTool]
    [Description("Query all product offers with details. Shows every offer for each product with prices, supplier info, and dynamic properties. Use for price comparisons across suppliers or when you need to see all available offers for products matching criteria.")]
    public async Task<string> QueryProductOffersView(
        [Description("Optional WHERE clause filters (e.g., 'Brand = ''L''Oreal''' or 'Price < 50 AND Gender = ''Women'''). Leave empty to get all records. Use single quotes for string values.")] string? whereClause = null,
        [Description("Optional ORDER BY clause (e.g., 'Price ASC' or 'Date DESC, Brand'). Default: Price ASC")] string? orderBy = "Price ASC",
        [Description("Maximum number of results (default: 100, max: 1000)")] int limit = 100,
        CancellationToken cancellationToken = default)
    {
        _connectionTracker.OnFirstRequest();
        Logger.LogInformation("[MCP] QueryProductOffersView called: whereClause='{Where}', orderBy='{OrderBy}', limit={Limit}", 
            whereClause ?? "(none)", orderBy, limit);
        
        ValidateRange(limit, 1, 1000, nameof(limit));

        return await ExecuteQueryAsync(async () =>
        {
            // Build dynamic SQL query
            var sql = $"SELECT TOP ({limit}) * FROM dbo.ProductOffersView";
            
            if (!string.IsNullOrWhiteSpace(whereClause))
            {
                sql += $" WHERE {whereClause}";
            }
            
            var effectiveOrderBy = orderBy ?? "Price ASC";
            sql += $" ORDER BY {effectiveOrderBy}";

            // Execute raw SQL query
            var connection = DbContext.Database.GetDbConnection();
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            
            using var command = connection.CreateCommand();
#pragma warning disable CA2100 // Review SQL queries for security vulnerabilities - intentional dynamic SQL for AI-powered queries
            command.CommandText = sql;
#pragma warning restore CA2100
            command.CommandTimeout = 30;
            
            var results = new List<Dictionary<string, object?>>();
            using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var row = new Dictionary<string, object?>();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                }
                results.Add(row);
            }

            return new
            {
                query = sql,
                whereClause = whereClause ?? "(none)",
                orderBy = effectiveOrderBy,
                count = results.Count,
                limit,
                results
            };
        }, "QueryProductOffersView", cancellationToken);
    }

    /// <summary>
    /// Query the ProductOffersViewCollapse - shows product details only on cheapest offer row.
    /// Use this for cleaner output when you want to see products with their cheapest price and all offers collapsed.
    /// </summary>
    [McpServerTool]
    [Description("Query collapsed product offers view. Shows product details (Brand, Type, Gender, etc.) only on the CHEAPEST offer row, with other offers showing just pricing/supplier info. Use when you want products grouped by cheapest price with minimal repetition. Products with only 1 offer are excluded.")]
    public async Task<string> QueryProductOffersViewCollapse(
        [Description("Optional WHERE clause filters (e.g., 'Brand = ''Chanel''' or 'Price < 100 AND OfferRank = 1'). Leave empty to get all records. Use single quotes for string values.")] string? whereClause = null,
        [Description("Optional ORDER BY clause (e.g., 'Price ASC' or 'EANKey, OfferRank'). Default: EANKey, OfferRank")] string? orderBy = "EANKey, OfferRank",
        [Description("Maximum number of results (default: 100, max: 1000)")] int limit = 100,
        CancellationToken cancellationToken = default)
    {
        _connectionTracker.OnFirstRequest();
        Logger.LogInformation("[MCP] QueryProductOffersViewCollapse called: whereClause='{Where}', orderBy='{OrderBy}', limit={Limit}", 
            whereClause ?? "(none)", orderBy, limit);
        
        ValidateRange(limit, 1, 1000, nameof(limit));

        return await ExecuteQueryAsync(async () =>
        {
            // Build dynamic SQL query
            var sql = $"SELECT TOP ({limit}) * FROM dbo.ProductOffersViewCollapse";
            
            if (!string.IsNullOrWhiteSpace(whereClause))
            {
                sql += $" WHERE {whereClause}";
            }
            
            var effectiveOrderBy = orderBy ?? "EANKey, OfferRank";
            sql += $" ORDER BY {effectiveOrderBy}";

            // Execute raw SQL query
            var connection = DbContext.Database.GetDbConnection();
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            
            using var command = connection.CreateCommand();
#pragma warning disable CA2100 // Review SQL queries for security vulnerabilities - intentional dynamic SQL for AI-powered queries
            command.CommandText = sql;
#pragma warning restore CA2100
            command.CommandTimeout = 30;
            
            var results = new List<Dictionary<string, object?>>();
            using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var row = new Dictionary<string, object?>();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                }
                results.Add(row);
            }

            return new
            {
                query = sql,
                whereClause = whereClause ?? "(none)",
                orderBy = effectiveOrderBy,
                count = results.Count,
                limit,
                note = "Product details (Brand, Name, etc.) appear only on OfferRank=1 (cheapest) rows. Other rows show pricing info only.",
                results
            };
        }, "QueryProductOffersViewCollapse", cancellationToken);
    }
}
