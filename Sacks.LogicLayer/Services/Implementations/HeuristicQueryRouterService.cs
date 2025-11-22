using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using McpServer.Client.Llm;
using Sacks.Core.Services.Interfaces;
using Sacks.LogicLayer.Services.Interfaces;

namespace Sacks.LogicLayer.Services.Implementations;

/// <summary>
/// LLM-based query router that uses AI to intelligently route natural language queries to MCP tools.
/// Uses generic ILlmService for provider-agnostic LLM integration (GitHub, Azure, OpenAI, Anthropic).
/// </summary>
public class LlmQueryRouterService : ILlmQueryRouterService
{
    private readonly ILogger<LlmQueryRouterService> _logger;
    private readonly IMcpClientService _mcpClient;
    private readonly ILlmService _llmService;

    public LlmQueryRouterService(
        ILogger<LlmQueryRouterService> logger,
        IMcpClientService mcpClient,
        ILlmService llmService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _mcpClient = mcpClient ?? throw new ArgumentNullException(nameof(mcpClient));
        _llmService = llmService ?? throw new ArgumentNullException(nameof(llmService));
    }

    /// <inheritdoc/>
    public async Task<LlmRoutingResult> RouteQueryAsync(string query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return new LlmRoutingResult
            {
                IsSuccessful = false,
                ErrorMessage = "Query cannot be empty",
                RoutingConfidence = 0.0
            };
        }

        try
        {
            _logger.LogDebug("Routing natural language query with LLM: {Query}", query);

            // Get available tools from MCP server (as ToolInfo for Sacks)
            var sacksTools = await _mcpClient.ListToolsAsync(cancellationToken).ConfigureAwait(false);
            if (sacksTools.Count == 0)
            {
                return new LlmRoutingResult
                {
                    IsSuccessful = false,
                    ErrorMessage = "No MCP tools are available",
                    RoutingConfidence = 0.0
                };
            }

            // Convert Sacks ToolInfo to generic McpToolInfo for LLM service
            var mcpTools = sacksTools.Select(t => new McpServer.Client.McpToolInfo
            {
                Name = t.Name,
                Description = t.Description,
                Parameters = t.Parameters.ToDictionary(
                    p => p.Key,
                    p => new McpServer.Client.McpParameterInfo
                    {
                        Type = p.Value.Type,
                        Description = p.Value.Description,
                        Required = p.Value.Required
                    })
            }).ToList();

            // Use LLM to select tool and extract parameters
            var selection = await _llmService.SelectToolAsync(query, mcpTools, cancellationToken).ConfigureAwait(false);

            if (!selection.IsSuccessful)
            {
                _logger.LogWarning("LLM failed to select tool for query: {Query} - {Error}", query, selection.ErrorMessage);
                return new LlmRoutingResult
                {
                    IsSuccessful = false,
                    ErrorMessage = selection.ErrorMessage ?? "LLM could not determine appropriate tool",
                    RoutingConfidence = selection.Confidence,
                    RoutingReason = selection.Reasoning ?? "LLM selection failed"
                };
            }

            // Check if LLM provided a direct response (Conversational mode)
            if (!string.IsNullOrWhiteSpace(selection.DirectResponse))
            {
                _logger.LogInformation("LLM provided direct response (no tool call): {Response}", selection.DirectResponse);
                
                // Return the direct response wrapped in a simple JSON structure
                var directResponseJson = System.Text.Json.JsonSerializer.Serialize(new
                {
                    success = true,
                    data = new { response = selection.DirectResponse }
                });

                return new LlmRoutingResult
                {
                    IsSuccessful = true,
                    SelectedToolName = "(direct response)",
                    RoutingReason = selection.Reasoning ?? "LLM answered directly",
                    RoutingConfidence = selection.Confidence,
                    Parameters = new Dictionary<string, object>(),
                    ToolResult = directResponseJson
                };
            }

            _logger.LogInformation("LLM routed query to tool: {ToolName} (Confidence: {Confidence})", 
                selection.SelectedToolName, selection.Confidence);

            // Execute the tool with LLM-extracted parameters
            var toolResult = await _mcpClient.ExecuteToolAsync(
                selection.SelectedToolName!, 
                selection.Parameters, 
                cancellationToken).ConfigureAwait(false);

            return new LlmRoutingResult
            {
                IsSuccessful = true,
                SelectedToolName = selection.SelectedToolName ?? string.Empty,
                RoutingReason = selection.Reasoning ?? "LLM selected tool",
                RoutingConfidence = selection.Confidence,
                Parameters = selection.Parameters,
                ToolResult = toolResult
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error routing query with LLM");
            return new LlmRoutingResult
            {
                IsSuccessful = false,
                ErrorMessage = $"Error routing query: {ex.Message}",
                RoutingConfidence = 0.0
            };
        }
    }
}
