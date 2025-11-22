namespace McpServer.Client.Configuration;

/// <summary>
/// LLM (Large Language Model) configuration for natural language query routing.
/// Generic configuration that works with any LLM provider.
/// </summary>
public class LlmOptions
{
    /// <summary>
    /// LLM provider: "GitHub", "Azure", "OpenAI", "Anthropic", or "None" to disable.
    /// </summary>
    public string Provider { get; set; } = "GitHub";

    /// <summary>
    /// API endpoint URL (provider-specific).
    /// GitHub: https://models.inference.ai.azure.com
    /// Azure: https://YOUR-RESOURCE.openai.azure.com
    /// OpenAI: https://api.openai.com/v1
    /// Anthropic: https://api.anthropic.com
    /// </summary>
    public string Endpoint { get; set; } = "https://models.inference.ai.azure.com";

    /// <summary>
    /// API key or token for authentication.
    /// GitHub: Personal Access Token with model permissions
    /// Azure: Azure OpenAI API key
    /// OpenAI: OpenAI API key
    /// Anthropic: Anthropic API key
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Model name to use for query routing.
    /// GitHub: gpt-4o, gpt-4o-mini, etc.
    /// Azure: your-deployment-name
    /// OpenAI: gpt-4, gpt-4-turbo, gpt-3.5-turbo
    /// Anthropic: claude-3-5-sonnet-20241022
    /// </summary>
    public string ModelName { get; set; } = "gpt-4o-mini";

    /// <summary>
    /// Maximum tokens for LLM response.
    /// </summary>
    public int MaxTokens { get; set; } = 1000;

    /// <summary>
    /// Temperature for LLM responses (0.0 = deterministic, 1.0 = creative).
    /// Lower is better for tool routing (use 0.0-0.3).
    /// </summary>
    public double Temperature { get; set; } = 0.1;

    /// <summary>
    /// Request timeout in seconds.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Response mode: how the LLM should handle queries.
    /// - "ToolOnly": Must select a tool (error if no match)
    /// - "ToolWithEcho": Use tool if relevant, otherwise Echo for unrelated queries
    /// - "Conversational": Can answer directly without calling tools
    /// </summary>
    public string ResponseMode { get; set; } = "ToolWithEcho";

    /// <summary>
    /// Optional domain context to help LLM understand the business domain.
    /// Can be inline text or a file path (relative or absolute).
    /// If starts with '@', treated as file path: "@domain-context.md"
    /// File is reloaded on each query to capture runtime updates.
    /// This is typically maintained by users (entities, relationships, business rules).
    /// </summary>
    public string? DomainContext { get; set; }

    /// <summary>
    /// Optional AI learnings file path for self-learning capabilities.
    /// If starts with '@', treated as file path: "@ai-learnings.md"
    /// This is typically written by the AI using RecordDomainLearning tool.
    /// File is reloaded on each query to capture new learnings.
    /// </summary>
    public string? AiLearningsPath { get; set; }
}
