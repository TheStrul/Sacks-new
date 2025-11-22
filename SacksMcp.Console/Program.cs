using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using McpServer.Transport;
using Sacks.Configuration;
using SacksMcp.Configuration;

// Load centralized configuration singleton
var config = ConfigurationLoader.Instance;

// Create the host builder
var builder = Host.CreateApplicationBuilder(args);

// Add SacksMcp services
builder.Services.AddSacksMcpServices(config);

// Build the app
var app = builder.Build();

// Get logger and transport for lifecycle management
var logger = app.Services.GetRequiredService<ILogger<Program>>();
var transport = app.Services.GetRequiredService<ITransportLayer>();
var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();

// Start HTTP transport before running the app
logger.LogInformation("🚀 [MCP] Starting HTTP transport on port {Port}", config.McpServer.HttpPort);
try
{
    await transport.StartAsync(lifetime.ApplicationStopping);
    logger.LogInformation("✅ [MCP] HTTP transport started successfully");
}
catch (Exception ex)
{
    logger.LogError(ex, "❌ [MCP] Failed to start HTTP transport");
    throw;
}

// Stop transport when application is stopping
lifetime.ApplicationStopping.Register(() =>
{
    logger.LogInformation("🔌 [MCP] Shutting down HTTP transport");
    // Note: Async method call from sync Register - fire and forget
    _ = transport.StopAsync();
});

lifetime.ApplicationStopped.Register(() =>
{
    logger.LogInformation("🔌 [MCP] HTTP transport stopped");
});

// Run the app
await app.RunAsync().ConfigureAwait(false);
