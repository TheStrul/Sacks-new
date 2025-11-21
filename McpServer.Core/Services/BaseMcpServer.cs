using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using McpServer.Core.Configuration;
using Microsoft.Extensions.Options;

namespace McpServer.Core.Services;

/// <summary>
/// Base MCP server implementation using Microsoft's ModelContextProtocol SDK.
/// This class provides core infrastructure for running an MCP server with the official SDK.
/// 
/// The ModelContextProtocol SDK uses attribute-based tool registration ([McpServerTool]) 
/// and automatic discovery via WithToolsFromAssembly(). Tools are registered as classes
/// with methods decorated with the [McpServerTool] attribute.
/// 
/// This base class provides:
/// - IHostedService integration for background service hosting
/// - Configuration support via McpServerExtendedOptions
/// - Structured logging
/// - Lifecycle management
/// 
/// To create a custom MCP server:
/// 1. Create tool classes with [McpServerTool] attribute on methods
/// 2. Configure services in Program.cs using AddMcpServer().WithToolsFromAssembly()
/// 3. Optionally inherit from this class to add custom initialization logic
/// </summary>
public abstract class BaseMcpServer : IHostedService
{
    protected readonly ILogger Logger;
    protected readonly McpServerExtendedOptions Options;
    protected readonly IHostApplicationLifetime AppLifetime;

    protected BaseMcpServer(
        ILogger logger,
        IOptions<McpServerExtendedOptions> options,
        IHostApplicationLifetime appLifetime)
    {
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        Options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        AppLifetime = appLifetime ?? throw new ArgumentNullException(nameof(appLifetime));
    }

    /// <summary>
    /// Called when the host starts. Override to add custom startup logic.
    /// </summary>
    public virtual Task StartAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("MCP Server starting: {ServerName} v{Version}", 
            Options.ServerName, 
            Options.Version);

        if (Options.EnableDetailedLogging)
        {
            Logger.LogDebug("Detailed logging enabled");
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Called when the host stops. Override to add custom shutdown logic.
    /// </summary>
    public virtual Task StopAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("MCP Server stopping: {ServerName}", Options.ServerName);
        return Task.CompletedTask;
    }
}
