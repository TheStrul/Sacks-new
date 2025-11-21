namespace SacksLogicLayer.Services.Interfaces;

/// <summary>
/// Service for routing natural language queries to appropriate MCP tools.
/// This is the foundation for LLM integration.
/// </summary>
public interface ILlmQueryRouterService
{
    /// <summary>
    /// Routes a natural language query to the most appropriate tool.
    /// </summary>
    /// <param name="query">Natural language query (e.g., "Show me products over $100")</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result from executing the most appropriate tool</returns>
    Task<LlmRoutingResult> RouteQueryAsync(string query, CancellationToken cancellationToken = default);
}

/// <summary>
/// Result from routing a natural language query to a tool.
/// </summary>
public class LlmRoutingResult
{
    /// <summary>
    /// The tool that was selected to handle this query.
    /// </summary>
    public string SelectedToolName { get; set; } = string.Empty;

    /// <summary>
    /// Description of why this tool was selected.
    /// </summary>
    public string RoutingReason { get; set; } = string.Empty;

    /// <summary>
    /// Parameters that were extracted from or generated for the query.
    /// </summary>
    public Dictionary<string, object> Parameters { get; set; } = new();

    /// <summary>
    /// The result from executing the selected tool.
    /// </summary>
    public string ToolResult { get; set; } = string.Empty;

    /// <summary>
    /// Confidence score (0-1) indicating how confident the routing is.
    /// </summary>
    public double RoutingConfidence { get; set; }

    /// <summary>
    /// Whether the query was successfully routed and executed.
    /// </summary>
    public bool IsSuccessful { get; set; }

    /// <summary>
    /// Error message if routing or execution failed.
    /// </summary>
    public string? ErrorMessage { get; set; }
}
