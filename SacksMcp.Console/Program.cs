using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sacks.Configuration;
using SacksMcp.Configuration;

// Load centralized configuration singleton
var config = ConfigurationLoader.Instance;

// Create the host builder
var builder = Host.CreateApplicationBuilder(args);

// Add SacksMcp services with stderr logging for MCP protocol compatibility
builder.Services.AddSacksMcpServices(
    config,
    configureLogging: logging =>
    {
        logging.AddConsole(options =>
        {
            // MCP protocol uses stdout, so log to stderr
            options.LogToStandardErrorThreshold = LogLevel.Trace;
        });
    });

// Build and run
var app = builder.Build();
await app.RunAsync().ConfigureAwait(false);
