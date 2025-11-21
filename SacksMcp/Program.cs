using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using McpServer.Core.Configuration;
using McpServer.Database.Configuration;
using SacksDataLayer.Data;

// Create the host builder
var builder = Host.CreateApplicationBuilder(args);

// Configure MCP server
builder.Services.AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

// Configure SacksDbContext
builder.Services.AddDbContext<SacksDbContext>(options =>
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
    {
        options.EnableSensitiveDataLogging();
    }
    
    if (dbOptions.EnableDetailedErrors)
    {
        options.EnableDetailedErrors();
    }
});

// Configure custom options
builder.Services.Configure<McpServerExtendedOptions>(options =>
{
    options.ServerName = "SacksMcp";
    options.Version = "1.0.0";
});

builder.Services.Configure<DatabaseOptions>(
    builder.Configuration.GetSection(DatabaseOptions.SectionName));

// Configure logging to stderr (MCP protocol uses stdout)
builder.Logging.AddConsole(options =>
{
    options.LogToStandardErrorThreshold = LogLevel.Trace;
});

// Build and run
var app = builder.Build();
await app.RunAsync().ConfigureAwait(false);
