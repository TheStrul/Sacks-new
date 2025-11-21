using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using McpServer.Core.Tools;

namespace McpServer.Database.Tools;

/// <summary>
/// Base class for database-oriented MCP tool collections. 
/// Extends BaseMcpToolCollection with database-specific utilities.
/// 
/// Example:
/// <code>
/// [McpServerToolType]
/// public class ProductTools : BaseDatabaseToolCollection&lt;MyDbContext&gt;
/// {
///     public ProductTools(MyDbContext db, ILogger&lt;ProductTools&gt; logger) 
///         : base(db, logger)
///     {
///     }
///     
///     [McpServerTool]
///     [Description("Search products by name")]
///     public async Task&lt;string&gt; SearchProducts(string searchTerm)
///     {
///         var products = await DbContext.Products
///             .Where(p => p.Name.Contains(searchTerm))
///             .ToListAsync();
///         return FormatSuccess(products);
///     }
/// }
/// </code>
/// </summary>
/// <typeparam name="TDbContext">The Entity Framework DbContext type</typeparam>
public abstract class BaseDatabaseToolCollection<TDbContext> : BaseMcpToolCollection 
    where TDbContext : DbContext
{
    protected readonly TDbContext DbContext;

    /// <summary>
    /// Testing mode delay in seconds. When set to a value > 0, queries will be delayed 
    /// to simulate slow operations for testing cancellation tokens.
    /// Default is 0 (no delay).
    /// </summary>
    public static int TestingModeDelaySeconds { get; set; } = 0;

    protected BaseDatabaseToolCollection(TDbContext dbContext, ILogger logger) 
        : base(logger)
    {
        DbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    /// <summary>
    /// Helper method to execute a query with error handling and formatting.
    /// Supports testing mode delay for cancellation token testing.
    /// </summary>
    protected async Task<string> ExecuteQueryAsync<T>(
        Func<Task<T>> query, 
        string operationName,
        CancellationToken cancellationToken = default)
    {
        if (query is null)
        {
            throw new ArgumentNullException(nameof(query));
        }
        
        try
        {
            Logger.LogInformation("Executing database query: {OperationName}", operationName);
            
            // Apply testing delay if configured (for cancellation token testing)
            if (TestingModeDelaySeconds > 0)
            {
                Logger.LogDebug("Testing mode: Delaying query by {Seconds} seconds", TestingModeDelaySeconds);
                await Task.Delay(TimeSpan.FromSeconds(TestingModeDelaySeconds), cancellationToken).ConfigureAwait(false);
            }
            
            var result = await query().ConfigureAwait(false);
            
            Logger.LogInformation("Query completed successfully: {OperationName}", operationName);
            return FormatSuccess(result!);
        }
        catch (OperationCanceledException ex)
        {
            Logger.LogWarning(ex, "Query cancelled: {OperationName}", operationName);
            throw; // Re-throw to allow tests to verify cancellation
        }
        catch (DbUpdateException ex)
        {
            Logger.LogError(ex, "Database update error in {OperationName}", operationName);
            return FormatError($"Database update failed: {ex.Message}", ex.InnerException?.Message);
        }
        catch (InvalidOperationException ex)
        {
            Logger.LogError(ex, "Invalid operation in {OperationName}", operationName);
            return FormatError($"Invalid operation: {ex.Message}");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Unexpected error in {OperationName}", operationName);
            return FormatError($"Query failed: {ex.Message}", ex.GetType().Name);
        }
    }

    /// <summary>
    /// Helper method to check if any records exist matching a condition.
    /// </summary>
    protected async Task<bool> AnyAsync<TEntity>(
        IQueryable<TEntity> query,
        CancellationToken cancellationToken = default) where TEntity : class
    {
        return await query.AnyAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Helper method to get a single record or null.
    /// </summary>
    protected async Task<TEntity?> FirstOrDefaultAsync<TEntity>(
        IQueryable<TEntity> query,
        CancellationToken cancellationToken = default) where TEntity : class
    {
        return await query.FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Helper method to get a list of records.
    /// </summary>
    protected async Task<List<TEntity>> ToListAsync<TEntity>(
        IQueryable<TEntity> query,
        CancellationToken cancellationToken = default) where TEntity : class
    {
        return await query.ToListAsync(cancellationToken).ConfigureAwait(false);
    }
}
