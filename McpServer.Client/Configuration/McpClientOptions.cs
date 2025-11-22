namespace McpServer.Client.Configuration;

/// <summary>
/// Configuration options for MCP client connections.
/// </summary>
public class McpClientOptions
{
    /// <summary>
    /// The URL of the MCP server (e.g., "http://localhost:5100").
    /// </summary>
    public string ServerUrl { get; set; } = "http://localhost:5100";

    /// <summary>
    /// Timeout in seconds for individual tool executions.
    /// </summary>
    public int ToolTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Maximum number of retry attempts for failed requests.
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Delay in milliseconds between retry attempts.
    /// </summary>
    public int RetryDelayMilliseconds { get; set; } = 1000;
}
