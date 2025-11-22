using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace McpServer.Client.Llm;

/// <summary>
/// Interface for LLM-based query routing and tool selection.
/// Implementations provide different LLM providers (GitHub Models, Azure OpenAI, OpenAI, Anthropic).
/// </summary>
public interface ILlmService
{
    /// <summary>
    /// Routes a natural language query to the most appropriate MCP tool using LLM intelligence.
    /// </summary>
    /// <param name="query">Natural language query from the user.</param>
    /// <param name="availableTools">List of available MCP tools with their descriptions and parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Selected tool name and extracted parameters.</returns>
    Task<LlmToolSelection> SelectToolAsync(
        string query,
        List<McpToolInfo> availableTools,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result from LLM tool selection containing the chosen tool and parameters.
/// </summary>
public class LlmToolSelection
{
    /// <summary>
    /// Whether the LLM successfully selected a tool.
    /// </summary>
    public bool IsSuccessful { get; set; }

    /// <summary>
    /// Name of the selected tool to execute.
    /// </summary>
    public string? SelectedToolName { get; set; }

    /// <summary>
    /// Parameters extracted by the LLM for the tool.
    /// </summary>
    public Dictionary<string, object> Parameters { get; set; } = new();

    /// <summary>
    /// LLM's confidence score (0.0 to 1.0).
    /// </summary>
    public double Confidence { get; set; }

    /// <summary>
    /// Reasoning or explanation from the LLM for why it chose this tool.
    /// </summary>
    public string? Reasoning { get; set; }

    /// <summary>
    /// Error message if selection failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Direct response from the LLM when no tool is needed.
    /// If this is set, the LLM has decided to answer directly instead of calling a tool.
    /// </summary>
    public string? DirectResponse { get; set; }
}
