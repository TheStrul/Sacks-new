using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.AI.Inference;
using Azure.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using McpServer.Client.Configuration;

namespace McpServer.Client.Llm;

/// <summary>
/// GitHub Models-based LLM service for intelligent query routing and tool selection.
/// Uses Azure.AI.Inference SDK to call GitHub's hosted AI models.
/// Completely generic - no workspace-specific dependencies.
/// </summary>
public class GitHubModelsLlmService : ILlmService
{
    private readonly ILogger<GitHubModelsLlmService> _logger;
    private readonly LlmOptions _options;
    private readonly ChatCompletionsClient _client;

    public GitHubModelsLlmService(
        ILogger<GitHubModelsLlmService> logger,
        IOptions<LlmOptions> options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));

        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new InvalidOperationException(
                "GitHub token (ApiKey) is required for GitHubModelsLlmService. " +
                "Get one at https://github.com/settings/tokens with 'model' scope.");
        }

        // Create Azure.AI.Inference client for GitHub Models
        _client = new ChatCompletionsClient(
            new Uri(_options.Endpoint),
            new AzureKeyCredential(_options.ApiKey));

        _logger.LogInformation("Initialized GitHub Models LLM service with model: {Model}", _options.ModelName);
    }

    public async Task<LlmToolSelection> SelectToolAsync(
        string query,
        List<McpToolInfo> availableTools,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return new LlmToolSelection
            {
                IsSuccessful = false,
                ErrorMessage = "Query cannot be empty"
            };
        }

        if (availableTools == null || availableTools.Count == 0)
        {
            return new LlmToolSelection
            {
                IsSuccessful = false,
                ErrorMessage = "No tools available"
            };
        }

        try
        {
            _logger.LogDebug("Routing query with LLM: {Query}", query);

            // Build system prompt describing available tools
            var systemPrompt = BuildSystemPrompt(availableTools);

            // Build user message
            var userMessage = $"User query: {query}\n\nSelect the most appropriate tool and extract parameters.";

            var requestOptions = new ChatCompletionsOptions()
            {
                Messages =
                {
                    new ChatRequestSystemMessage("You must respond with valid JSON only. No markdown, no code blocks, just raw JSON."),
                    new ChatRequestSystemMessage(systemPrompt),
                    new ChatRequestUserMessage(userMessage)
                },
                Model = _options.ModelName,
                Temperature = (float)_options.Temperature,
                MaxTokens = _options.MaxTokens
            };

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(_options.TimeoutSeconds));

            var response = await _client.CompleteAsync(requestOptions, cts.Token).ConfigureAwait(false);
            var result = response.Value;

            // Azure.AI.Inference 1.0.0-beta.2 returns single Content property
            var content = result.Content;

            if (string.IsNullOrWhiteSpace(content))
            {
                return new LlmToolSelection
                {
                    IsSuccessful = false,
                    ErrorMessage = "LLM returned empty response"
                };
            }

            _logger.LogDebug("LLM response: {Response}", content);

            // Parse JSON response
            var selection = ParseLlmResponse(content);
            
            // If LLM returned null toolName, it means no tool is appropriate
            if (selection.IsSuccessful && string.IsNullOrWhiteSpace(selection.SelectedToolName))
            {
                _logger.LogInformation("LLM determined no tool is appropriate for query: {Query}", query);
                return new LlmToolSelection
                {
                    IsSuccessful = false,
                    ErrorMessage = "No appropriate tool found for this query. I can only help with products, offers, and suppliers.",
                    Reasoning = selection.Reasoning
                };
            }
            
            // Validate selected tool exists
            if (selection.IsSuccessful && 
                !availableTools.Any(t => t.Name.Equals(selection.SelectedToolName, StringComparison.OrdinalIgnoreCase)))
            {
                _logger.LogWarning("LLM selected non-existent tool: {Tool}", selection.SelectedToolName);
                return new LlmToolSelection
                {
                    IsSuccessful = false,
                    ErrorMessage = $"LLM selected unknown tool: {selection.SelectedToolName}"
                };
            }

            return selection;
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Azure AI Inference request failed");
            return new LlmToolSelection
            {
                IsSuccessful = false,
                ErrorMessage = $"LLM request failed: {ex.Message}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in LLM tool selection");
            return new LlmToolSelection
            {
                IsSuccessful = false,
                ErrorMessage = $"Unexpected error: {ex.Message}"
            };
        }
    }

    private string BuildSystemPrompt(List<McpToolInfo> availableTools)
    {
        var sb = new StringBuilder();
        
        // Add domain knowledge context (user-maintained) if provided
        var domainContext = LoadFileContent(_options.DomainContext, "domain context");
        if (!string.IsNullOrWhiteSpace(domainContext))
        {
            sb.AppendLine("═══════════════════════════════════════════════════════");
            sb.AppendLine("📚 DOMAIN CONTEXT (User-Maintained)");
            sb.AppendLine("═══════════════════════════════════════════════════════");
            sb.AppendLine(domainContext);
            sb.AppendLine();
        }
        
        // Add AI learnings (AI-maintained) if available
        var aiLearnings = LoadFileContent(_options.AiLearningsPath, "AI learnings");
        if (!string.IsNullOrWhiteSpace(aiLearnings))
        {
            sb.AppendLine("═══════════════════════════════════════════════════════");
            sb.AppendLine("🧠 AI LEARNINGS (Self-Discovered Knowledge)");
            sb.AppendLine("═══════════════════════════════════════════════════════");
            sb.AppendLine(aiLearnings);
            sb.AppendLine();
        }
        
        // Add mode-specific instructions
        switch (_options.ResponseMode)
        {
            case "ToolOnly":
                sb.AppendLine("MODE: Tool-Only");
                sb.AppendLine("You MUST select one of the available tools for every query.");
                sb.AppendLine("If the query doesn't match any tool, explain why in the reasoning but still select the closest match.");
                break;
            
            case "Conversational":
                sb.AppendLine("MODE: Conversational");
                sb.AppendLine("You can either:");
                sb.AppendLine("1. Select a tool if the query relates to beauty products, offers, or suppliers");
                sb.AppendLine("2. Answer directly for general questions, greetings, or chitchat without calling any tool");
                break;
            
            default:
                sb.AppendLine("You are a tool selection assistant. Given a user query, select the most appropriate tool and extract parameters.");
                break;
        }
        
        sb.AppendLine();
        sb.AppendLine("Available tools:");
        sb.AppendLine();

        foreach (var tool in availableTools)
        {
            sb.AppendLine($"Tool: {tool.Name}");
            sb.AppendLine($"Description: {tool.Description}");
            
            if (tool.Parameters.Count > 0)
            {
                sb.AppendLine("Parameters:");
                foreach (var (paramName, paramInfo) in tool.Parameters)
                {
                    var required = paramInfo.Required ? " (required)" : " (optional)";
                    sb.AppendLine($"  - {paramName}: {paramInfo.Type}{required} - {paramInfo.Description}");
                }
            }
            
            sb.AppendLine();
        }

        sb.AppendLine("Respond with JSON in this exact format:");
        sb.AppendLine("{");
        
        if (_options.ResponseMode == "Conversational")
        {
            sb.AppendLine("  \"toolName\": \"selected_tool_name\" or null,");
            sb.AppendLine("  \"parameters\": { \"param1\": value1, \"param2\": value2 },");
            sb.AppendLine("  \"confidence\": 0.95,");
            sb.AppendLine("  \"reasoning\": \"why this tool was chosen\",");
            sb.AppendLine("  \"directResponse\": \"your answer here (only if toolName is null)\"");
        }
        else
        {
            sb.AppendLine("  \"toolName\": \"selected_tool_name\" or null,");
            sb.AppendLine("  \"parameters\": { \"param1\": value1, \"param2\": value2 },");
            sb.AppendLine("  \"confidence\": 0.95,");
            sb.AppendLine("  \"reasoning\": \"why this tool was chosen\"");
        }
        
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("Rules:");
        
        if (_options.ResponseMode == "Conversational")
        {
            sb.AppendLine("1. For business queries (products, offers, suppliers), set toolName and leave directResponse empty");
            sb.AppendLine("2. For general questions, set toolName to null and provide directResponse");
            sb.AppendLine("3. Extract all parameter values from the query");
            sb.AppendLine("4. Use appropriate types (numbers as numbers, strings as strings)");
            sb.AppendLine("5. Provide confidence between 0.0 and 1.0");
        }
        else
        {
            sb.AppendLine("1. If no tool is appropriate for the query, set toolName to null");
            sb.AppendLine("2. toolName must exactly match one of the available tool names OR be null");
            sb.AppendLine("3. Extract all parameter values from the query");
            sb.AppendLine("4. Use appropriate types (numbers as numbers, strings as strings)");
            sb.AppendLine("5. Provide confidence between 0.0 and 1.0");
            sb.AppendLine("6. Only select a tool if the query genuinely relates to its purpose");
        }

        return sb.ToString();
    }

    private LlmToolSelection ParseLlmResponse(string jsonResponse)
    {
        try
        {
            using var doc = JsonDocument.Parse(jsonResponse);
            var root = doc.RootElement;

            var toolName = root.TryGetProperty("toolName", out var tn) ? tn.GetString() : null;
            var reasoning = root.TryGetProperty("reasoning", out var r) ? r.GetString() : null;
            var directResponse = root.TryGetProperty("directResponse", out var dr) ? dr.GetString() : null;
            var confidence = root.TryGetProperty("confidence", out var c) && c.ValueKind == JsonValueKind.Number
                ? c.GetDouble()
                : 0.5;

            var parameters = new Dictionary<string, object>();
            if (root.TryGetProperty("parameters", out var paramsElement) && 
                paramsElement.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in paramsElement.EnumerateObject())
                {
                    parameters[prop.Name] = prop.Value.ValueKind switch
                    {
                        JsonValueKind.String => prop.Value.GetString()!,
                        JsonValueKind.Number => prop.Value.GetDecimal(),
                        JsonValueKind.True => true,
                        JsonValueKind.False => false,
                        JsonValueKind.Null => null!,
                        _ => prop.Value.GetRawText()
                    };
                }
            }

            // If LLM provided a direct response, treat it as success even without a tool
            var isSuccessful = !string.IsNullOrWhiteSpace(toolName) || !string.IsNullOrWhiteSpace(directResponse);

            return new LlmToolSelection
            {
                IsSuccessful = isSuccessful,
                SelectedToolName = toolName,
                Parameters = parameters,
                Confidence = confidence,
                Reasoning = reasoning,
                DirectResponse = directResponse
            };
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse LLM JSON response: {Response}", jsonResponse);
            return new LlmToolSelection
            {
                IsSuccessful = false,
                ErrorMessage = "Failed to parse LLM response"
            };
        }
    }

    /// <summary>
    /// Generic file content loader for domain context, AI learnings, or any @ file reference.
    /// </summary>
    private string? LoadFileContent(string? contentOrPath, string contentType)
    {
        if (string.IsNullOrWhiteSpace(contentOrPath))
        {
            return null;
        }

        var content = contentOrPath.Trim();

        // Check if it's a file path reference (starts with '@')
        if (content.StartsWith("@", StringComparison.Ordinal))
        {
            var filePath = content.Substring(1).Trim();

            try
            {
                // Support relative paths (relative to current directory)
                if (!Path.IsPathRooted(filePath))
                {
                    filePath = Path.Combine(Directory.GetCurrentDirectory(), filePath);
                }

                if (File.Exists(filePath))
                {
                    var fileContent = File.ReadAllText(filePath);
                    _logger.LogDebug("Loaded {ContentType} from file: {FilePath}", contentType, filePath);
                    return fileContent;
                }
                else
                {
                    _logger.LogWarning("{ContentType} file not found: {FilePath}", contentType, filePath);
                    return null; // Return null if file doesn't exist
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load {ContentType} from file: {FilePath}", contentType, filePath);
                return $"[Failed to load {contentType}: {ex.Message}]";
            }
        }

        // Return inline content as-is
        return content;
    }
}

