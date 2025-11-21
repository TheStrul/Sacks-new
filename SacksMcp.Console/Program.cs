using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sacks.Configuration;
using SacksMcp.Configuration;

// Create the host builder
var builder = Host.CreateApplicationBuilder(args);

// Load centralized configuration from solution root and merge it
var centralizedConfig = ConfigurationHelper.BuildConfiguration();
builder.Configuration.AddConfiguration(centralizedConfig);

// Add SacksMcp services with stderr logging for MCP protocol compatibility
builder.Services.AddSacksMcpServices(
    builder.Configuration,
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
