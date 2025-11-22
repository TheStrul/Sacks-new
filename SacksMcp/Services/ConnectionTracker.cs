using Microsoft.Extensions.Logging;

namespace SacksMcp.Services;

/// <summary>
/// Tracks client connection state by monitoring first tool invocation.
/// Since MCP STDIO transport doesn't expose connection events, we infer connection
/// from the first JSON-RPC request received.
/// </summary>
public class ConnectionTracker
{
    private readonly ILogger<ConnectionTracker> _logger;
    private bool _connectionLogged;
    private readonly object _lock = new();

    public ConnectionTracker(ILogger<ConnectionTracker> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Call this on first tool invocation to log client connection.
    /// Thread-safe - only logs once.
    /// </summary>
    public void OnFirstRequest()
    {
        if (_connectionLogged)
        {
            return;
        }

        lock (_lock)
        {
            if (!_connectionLogged)
            {
                _logger.LogInformation("🔌 [MCP] Client connected - First request received");
                _connectionLogged = true;
            }
        }
    }
}
