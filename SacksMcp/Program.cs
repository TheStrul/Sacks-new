using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using SacksMcp.Configuration;

// Create the host builder
var builder = Host.CreateApplicationBuilder(args);

// Configure MCP server
builder.Services.AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

// Configure custom options
builder.Services.Configure<CustomMcpServerOptions>(options =>
{
    options.ServerName = "SacksMcp";
    options.Version = "1.0.0";
    options.Description = "Reusable MCP Server template for database operations";
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
