# McpServer.Database

**Generic Database Tool Foundation** - This class library extends McpServer.Core to provide database-specific MCP server capabilities for any workspace.

## Purpose

McpServer.Database is ONE implementation option for MCP servers that need database access. It's **100% generic** and **workspace-agnostic**, providing:

- Base tool collection for database operations with EF Core integration
- Database configuration options (connection string, provider, timeouts, retries)
- Common database helper methods with error handling
- Structured logging for all database operations

## Architecture Position

```
McpServer.Core (generic base)
    ↓
McpServer.Database (this project - ONE option for databases)
    ↓
YourSpecificMcpServer (implements your specific database tools)
```

**Note**: Database is just ONE of many possible MCP server capabilities:
- McpServer.FileSystem (file operations)
- McpServer.WebApi (HTTP/REST calls)
- McpServer.Email (email operations)
- McpServer.Messaging (message queues)
- ...endless possibilities

## Key Features

### BaseDatabaseToolCollection<TDbContext>
Abstract base class extending `BaseMcpToolCollection` with database-specific features:

```csharp
public abstract class BaseDatabaseToolCollection<TDbContext> : BaseMcpToolCollection
    where TDbContext : DbContext
{
    protected TDbContext DbContext { get; }
    
    // Comprehensive error handling with DbUpdateException, InvalidOperationException, etc.
    protected async Task<string> ExecuteQueryAsync<T>(
        Func<Task<T>> query, 
        string operationName,
        CancellationToken cancellationToken = default)
    
    // Convenient wrappers with null validation
    protected Task<bool> AnyAsync<TEntity>(...)
    protected Task<TEntity?> FirstOrDefaultAsync<TEntity>(...)
    protected Task<List<TEntity>> ToListAsync<TEntity>(...)
}
```

### DatabaseOptions
Configuration class for database connections:
- `ConnectionString` - Database connection string
- `Provider` - Database provider (default: "SqlServer")
- `CommandTimeout` - Command timeout in seconds (default: 30)
- `EnableSensitiveDataLogging` - Log parameter values (default: false)
- `EnableDetailedErrors` - Detailed error messages (default: false)
- `MaxRetryAttempts` - Retry count for transient failures (default: 3)

Configuration section: `"Database"`

## Usage Pattern

### 1. Reference both Core and Database projects
```xml
<ItemGroup>
  <ProjectReference Include="..\McpServer.Core\McpServer.Core.csproj" />
  <ProjectReference Include="..\McpServer.Database\McpServer.Database.csproj" />
</ItemGroup>
```

### 2. Install EF Core packages in your project
```powershell
dotnet add package Microsoft.EntityFrameworkCore
dotnet add package Microsoft.EntityFrameworkCore.SqlServer  # or your provider
```

### 3. Create your DbContext
```csharp
using Microsoft.EntityFrameworkCore;

public class MyDbContext : DbContext
{
    public MyDbContext(DbContextOptions<MyDbContext> options) : base(options) { }
    
    public DbSet<Product> Products { get; set; }
    public DbSet<Order> Orders { get; set; }
    
    // Your entity configurations
}
```

### 4. Create your database tool collection
```csharp
using McpServer.Database.Tools;
using ModelContextProtocol.Server;

[McpServerToolType]
public class MyDatabaseTools : BaseDatabaseToolCollection<MyDbContext>
{
    public MyDatabaseTools(MyDbContext dbContext, ILogger<MyDatabaseTools> logger) 
        : base(dbContext, logger) { }
    
    [McpServerTool]
    [Description("Get expensive products over a price threshold")]
    public async Task<string> GetExpensiveProducts(
        [Description("Minimum price threshold")] decimal minPrice,
        CancellationToken cancellationToken = default)
    {
        return await ExecuteQueryAsync(async () =>
        {
            var products = await DbContext.Products
                .Where(p => p.Price >= minPrice)
                .OrderByDescending(p => p.Price)
                .Select(p => new { p.Name, p.Price, p.Category })
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
                
            return new { count = products.Count, products };
        }, 
        "GetExpensiveProducts", 
        cancellationToken);
    }
    
    [McpServerTool]
    [Description("Search products by name")]
    public async Task<string> SearchProducts(
        [Description("Search term")] string searchTerm,
        CancellationToken cancellationToken = default)
    {
        ValidateRequired(searchTerm, nameof(searchTerm));
        
        var products = await ToListAsync(
            DbContext.Products.Where(p => p.Name.Contains(searchTerm)),
            "SearchProducts",
            cancellationToken);
            
        return FormatSuccess(products);
    }
}
```

### 5. Configure in Program.cs
```csharp
using McpServer.Core.Configuration;
using McpServer.Database.Configuration;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);

// Configure MCP server
builder.Services.AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

// Configure DbContext
builder.Services.AddDbContext<MyDbContext>(options =>
{
    var dbOptions = builder.Configuration
        .GetSection(DatabaseOptions.SectionName)
        .Get<DatabaseOptions>() ?? new DatabaseOptions();
    
    options.UseSqlServer(dbOptions.ConnectionString, sqlOptions =>
    {
        sqlOptions.CommandTimeout(dbOptions.CommandTimeout);
        sqlOptions.EnableRetryOnFailure(dbOptions.MaxRetryAttempts);
    });
    
    if (dbOptions.EnableSensitiveDataLogging)
        options.EnableSensitiveDataLogging();
        
    if (dbOptions.EnableDetailedErrors)
        options.EnableDetailedErrors();
});

// Configure options
builder.Services.Configure<McpServerExtendedOptions>(
    builder.Configuration.GetSection(McpServerExtendedOptions.SectionName));
    
builder.Services.Configure<DatabaseOptions>(
    builder.Configuration.GetSection(DatabaseOptions.SectionName));

await builder.Build().RunAsync();
```

### 6. Configure in appsettings.json
```json
{
  "McpServerExtended": {
    "ServerName": "MyDatabaseMcpServer",
    "Version": "1.0.0"
  },
  "Database": {
    "ConnectionString": "Server=localhost;Database=MyDb;Integrated Security=true;TrustServerCertificate=true;",
    "Provider": "SqlServer",
    "CommandTimeout": 30,
    "EnableSensitiveDataLogging": false,
    "EnableDetailedErrors": false,
    "MaxRetryAttempts": 3
  }
}
```

## Error Handling

`ExecuteQueryAsync` provides comprehensive error handling:

- **DbUpdateException**: Database update/constraint violations
- **InvalidOperationException**: Invalid query operations (e.g., multiple results when expecting one)
- **General Exceptions**: All other errors with structured logging

All errors are formatted consistently using `FormatError()` from the base class.

## Best Practices

### 1. Always use ExecuteQueryAsync for complex queries
```csharp
return await ExecuteQueryAsync(async () =>
{
    // Your complex query logic with multiple operations
    var result = await DbContext.ComplexQuery()
        .ConfigureAwait(false);
    return result;
}, "OperationName", cancellationToken);
```

### 2. Use helper methods for simple queries
```csharp
var exists = await AnyAsync(
    DbContext.Products.Where(p => p.Id == id),
    "CheckProductExists",
    cancellationToken);
```

### 3. Always pass CancellationToken
```csharp
[McpServerTool]
public async Task<string> MyTool(string param, CancellationToken cancellationToken = default)
{
    // Pass token to all async operations
    var result = await DbContext.Set<Entity>()
        .ToListAsync(cancellationToken)
        .ConfigureAwait(false);
    // ...
}
```

### 4. Use AsNoTracking() for read-only queries
```csharp
var products = await DbContext.Products
    .AsNoTracking()
    .Where(p => p.IsActive)
    .ToListAsync(cancellationToken);
```

### 5. Project only required columns
```csharp
var summary = await DbContext.Products
    .Select(p => new { p.Id, p.Name, p.Price })
    .ToListAsync(cancellationToken);
```

### 6. Prevent N+1 queries with eager loading
```csharp
var orders = await DbContext.Orders
    .Include(o => o.Customer)
    .Include(o => o.OrderItems)
    .ToListAsync(cancellationToken);
```

## Dependencies

- **McpServer.Core** - Base MCP server infrastructure (project reference)
- **Microsoft.EntityFrameworkCore 10.0.0** - EF Core framework
- **Microsoft.EntityFrameworkCore.SqlServer 10.0.0** - SQL Server provider (example)

## Target Framework

- **.NET 10** (net10.0)

## Cross-Workspace Reusability

This project is intentionally generic:
- ✅ No workspace-specific naming
- ✅ No hardcoded database schema
- ✅ Generic `TDbContext` parameter - works with ANY DbContext
- ✅ No business logic - just infrastructure
- ✅ Provider-agnostic configuration

You can use this in ANY workspace that needs database MCP tools.

## Example Implementation

See the **SacksMcp** project in this solution for a complete example using:
- SacksDbContext (specific DbContext)
- Product and Supplier entities
- Search, filter, and statistics tools

## Alternatives

Remember: Database is just ONE option. For other capabilities, create similar projects:
- **McpServer.FileSystem** - File operations with System.IO
- **McpServer.WebApi** - HTTP calls with HttpClient
- **McpServer.Email** - Email with MailKit or similar
- **McpServer.Redis** - Cache operations
- **McpServer.Blob** - Azure Blob Storage operations

Each follows the same pattern: extend McpServer.Core, provide specialized base classes, stay 100% generic.
