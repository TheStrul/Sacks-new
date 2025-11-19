namespace SacksMcp.Configuration;

/// <summary>
/// Custom configuration options for the MCP server. These settings control how the server operates
/// and can be customized via appsettings.json for different environments.
/// Note: This is separate from ModelContextProtocol.Server.McpServerOptions
/// </summary>
public class CustomMcpServerOptions
{
    /// <summary>
    /// The section name in appsettings.json where these options are stored.
    /// </summary>
    public const string SectionName = "CustomMcpServer";

    /// <summary>
    /// The name of the MCP server. This appears in MCP client connections and logs.
    /// Default: "SacksMcp"
    /// </summary>
    public string ServerName { get; set; } = "SacksMcp";

    /// <summary>
    /// Version of the MCP server. Used for compatibility checking.
    /// Default: "1.0.0"
    /// </summary>
    public string Version { get; set; } = "1.0.0";

    /// <summary>
    /// Description of what this MCP server does. Displayed to clients.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Maximum number of concurrent tool executions allowed.
    /// Default: 10
    /// </summary>
    public int MaxConcurrentTools { get; set; } = 10;

    /// <summary>
    /// Timeout in seconds for tool execution. Tools taking longer will be cancelled.
    /// Default: 30 seconds
    /// </summary>
    public int ToolTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Whether to enable detailed logging for debugging.
    /// Default: false
    /// </summary>
    public bool EnableDetailedLogging { get; set; } = false;
}
