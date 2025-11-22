using System.Threading;
using System.Threading.Tasks;

namespace Sacks.Core.Services.Interfaces;

/// <summary>
/// Interface for MCP (Model Context Protocol) client service.
/// Provides methods to communicate with the MCP server for AI-powered queries.
/// </summary>
public interface IMcpClientService
{
    /// <summary>
    /// Executes a tool on the MCP server and returns the result.
    /// </summary>
    /// <param name="toolName">The name of the tool to execute (e.g., "SearchProducts", "GetSupplierStats")</param>
    /// <param name="parameters">Dictionary of parameter names and values for the tool</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>JSON string result from the tool execution</returns>
    Task<string> ExecuteToolAsync(string toolName, Dictionary<string, object> parameters, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a natural language query to the AI which will determine which tools to call.
    /// </summary>
    /// <param name="query">Natural language query (e.g., "Show me expensive products over $100")</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>AI response with tool results</returns>
    Task<string> QueryAsync(string query, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all available tools from the MCP server.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of available tools with their descriptions and parameters</returns>
    Task<List<ToolInfo>> ListToolsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the MCP server is running and responsive.
    /// </summary>
    /// <returns>True if server is available, false otherwise</returns>
    Task<bool> IsServerAvailableAsync();

    /// <summary>
    /// Starts the MCP server process if not already running.
    /// </summary>
    Task StartServerAsync();

    /// <summary>
    /// Stops the MCP server process.
    /// </summary>
    Task StopServerAsync();
}

/// <summary>
/// Information about an available MCP tool.
/// </summary>
public class ToolInfo
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Dictionary<string, ParameterInfo> Parameters { get; set; } = new();
}

/// <summary>
/// Information about a tool parameter.
/// </summary>
public class ParameterInfo
{
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool Required { get; set; }
    public object? DefaultValue { get; set; }
}
