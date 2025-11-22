namespace McpServer.Transport;

/// <summary>
/// Defines a transport layer for MCP server communication.
/// </summary>
public interface ITransportLayer
{
    /// <summary>
    /// Starts the transport layer and begins listening for incoming connections.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task that completes when the transport layer has started.</returns>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops the transport layer and closes all active connections.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task that completes when the transport layer has stopped.</returns>
    Task StopAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a value indicating whether the transport layer is currently running.
    /// </summary>
    bool IsRunning { get; }
}
