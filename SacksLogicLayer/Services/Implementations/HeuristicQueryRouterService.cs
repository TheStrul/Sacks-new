using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SacksLogicLayer.Services.Interfaces;

namespace SacksLogicLayer.Services.Implementations;

/// <summary>
/// Heuristic-based query router that uses keyword matching to route natural language queries to tools.
/// This is a temporary implementation until full LLM integration is available.
/// Future: Replace with calls to OpenAI/Azure OpenAI for intelligent routing.
/// </summary>
public class HeuristicQueryRouterService : ILlmQueryRouterService
{
    private readonly ILogger<HeuristicQueryRouterService> _logger;
    private readonly IMcpClientService _mcpClient;

    // Tool keywords for heuristic matching
    private static readonly Dictionary<string, ToolPattern> ToolPatterns = new(StringComparer.OrdinalIgnoreCase)
    {
        {
            "SearchProducts", new ToolPattern
            {
                Keywords = new[] { "product", "search", "find", "look", "show", "list", "query", "get", "retrieve", "item", "items", "thing" },
                RegexPatterns = new[] 
                { 
                    @"(?:find|search|show|list|get)\s+(?:me\s+)?(?:products?|items?|things?)",
                    @"products?\s+(?:with|by|containing|matching)",
                    @"(?:how\s+)?(?:many|count)\s+(?:products|items)"
                },
                MinConfidence = 0.5,  // Lowered threshold for more flexibility
                Description = "Search and retrieve products"
            }
        },
        {
            "GetSupplierStats", new ToolPattern
            {
                Keywords = new[] { "supplier", "stats", "statistics", "summary", "report", "overview", "analyze", "vendor", "seller" },
                RegexPatterns = new[] 
                { 
                    @"(?:supplier|vendor|seller)\s+(?:stats|statistics|summary)",
                    @"show\s+me\s+(?:supplier|vendor)\s+(?:stats|overview)",
                    @"how\s+(?:many|count)\s+suppliers"
                },
                MinConfidence = 0.6,
                Description = "Get supplier statistics and analysis"
            }
        },
        {
            "GetOfferDetails", new ToolPattern
            {
                Keywords = new[] { "offer", "deals", "prices", "pricing", "cost", "price", "bid", "deal", "offer" },
                RegexPatterns = new[] 
                { 
                    @"(?:show|get|list|find)\s+(?:me\s+)?(?:offers?|deals?|prices?|bids?)",
                    @"offer\s+(?:details?|information|stats|summary)",
                    @"(?:what|which)\s+(?:offers?|deals?|prices?)"
                },
                MinConfidence = 0.55,
                Description = "Get offer details and pricing"
            }
        }
    };

    public HeuristicQueryRouterService(ILogger<HeuristicQueryRouterService> logger, IMcpClientService mcpClient)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _mcpClient = mcpClient ?? throw new ArgumentNullException(nameof(mcpClient));
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
            _logger.LogDebug("Routing natural language query: {Query}", query);

            // Find best matching tool
            var (bestTool, confidence, reason) = FindBestMatchingTool(query);

            if (bestTool == null)
            {
                _logger.LogWarning("No suitable tool found for query: {Query}", query);
                return new LlmRoutingResult
                {
                    IsSuccessful = false,
                    ErrorMessage = $"Could not determine appropriate tool for query: '{query}'",
                    RoutingConfidence = confidence,
                    RoutingReason = reason
                };
            }

            _logger.LogInformation("Routing query to tool: {ToolName} (Confidence: {Confidence})", bestTool, confidence);

            // Extract parameters from query
            var parameters = ExtractParameters(query);
            parameters["query"] = query; // Always include original query

            // Execute the tool
            var toolResult = await _mcpClient.ExecuteToolAsync(bestTool, parameters, cancellationToken).ConfigureAwait(false);

            return new LlmRoutingResult
            {
                IsSuccessful = true,
                SelectedToolName = bestTool,
                RoutingReason = reason,
                RoutingConfidence = confidence,
                Parameters = parameters,
                ToolResult = toolResult
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error routing query");
            return new LlmRoutingResult
            {
                IsSuccessful = false,
                ErrorMessage = $"Error routing query: {ex.Message}",
                RoutingConfidence = 0.0
            };
        }
    }

    /// <summary>
    /// Finds the best matching tool for the given query using heuristics.
    /// </summary>
    /// <returns>Tuple of (toolName, confidence, reason)</returns>
    private (string? toolName, double confidence, string reason) FindBestMatchingTool(string query)
    {
        var queryLower = query.ToLowerInvariant();
        var scores = new Dictionary<string, (double score, string reason)>();

        // Check for greeting/help queries first
        if (IsGreetingOrHelpQuery(queryLower, out var greetingReason))
        {
            // Default to SearchProducts for general queries
            return ("SearchProducts", 0.4, greetingReason);
        }

        foreach (var (toolName, pattern) in ToolPatterns)
        {
            double score = 0.0;
            var reasons = new List<string>();

            // Check keyword matches
            var keywordMatches = pattern.Keywords.Count(k => queryLower.Contains(k));
            if (keywordMatches > 0)
            {
                score += keywordMatches * 0.15; // Each keyword: +0.15
                reasons.Add($"{keywordMatches} keyword match(es)");
            }

            // Check regex patterns
            foreach (var regexPattern in pattern.RegexPatterns)
            {
                if (Regex.IsMatch(query, regexPattern, RegexOptions.IgnoreCase))
                {
                    score += 0.3; // Regex match: +0.3
                    reasons.Add($"Pattern match");
                    break; // Count each pattern once
                }
            }

            // Normalize score to 0-1 range
            score = Math.Min(score, 1.0);

            if (score >= pattern.MinConfidence)
            {
                scores[toolName] = (score, string.Join("; ", reasons));
            }
        }

        // Return best match
        if (scores.Count == 0)
        {
            // No tool matched - return default with helpful message
            return ("SearchProducts", 0.3, "No specific match found; defaulting to product search. Try queries like 'show me products', 'list suppliers', or 'show offers'");
        }

        var bestMatch = scores.OrderByDescending(x => x.Value.score).First();
        return (bestMatch.Key, bestMatch.Value.score, bestMatch.Value.reason);
    }

    /// <summary>
    /// Checks if the query is a greeting or help request
    /// </summary>
    private bool IsGreetingOrHelpQuery(string queryLower, out string reason)
    {
        var greetings = new[] { "hi", "hello", "hey", "help", "what can you do", "what do you do", "how", "huh", "hm", "hmm", "?" };
        
        if (greetings.Any(g => queryLower.Equals(g, StringComparison.OrdinalIgnoreCase) || queryLower.StartsWith(g + " ")))
        {
            reason = "Greeting or help query detected; showing product search as default";
            return true;
        }

        reason = string.Empty;
        return false;
    }

    /// <summary>
    /// Extracts parameters from the natural language query.
    /// This is basic extraction; LLM would do this more intelligently.
    /// </summary>
    private Dictionary<string, object> ExtractParameters(string query)
    {
        var parameters = new Dictionary<string, object>();

        // Extract numeric values (prices, quantities, etc.)
        var numberMatches = Regex.Matches(query, @"\$?\d+(?:\.\d{2})?");
        if (numberMatches.Count > 0)
        {
            parameters["minPrice"] = numberMatches[0].Value.TrimStart('$');
        }

        // Extract ranges (e.g., "between 100 and 200")
        var rangeMatch = Regex.Match(query, @"between\s+(\d+)\s+and\s+(\d+)", RegexOptions.IgnoreCase);
        if (rangeMatch.Success)
        {
            parameters["minValue"] = rangeMatch.Groups[1].Value;
            parameters["maxValue"] = rangeMatch.Groups[2].Value;
        }

        // Extract "over/above/more than" values
        var overMatch = Regex.Match(query, @"(?:over|above|more\s+than|>\s*)\s*(?:\$)?(\d+(?:\.\d{2})?)", RegexOptions.IgnoreCase);
        if (overMatch.Success)
        {
            parameters["minValue"] = overMatch.Groups[1].Value;
        }

        // Extract "under/below/less than" values
        var underMatch = Regex.Match(query, @"(?:under|below|less\s+than|<\s*)\s*(?:\$)?(\d+(?:\.\d{2})?)", RegexOptions.IgnoreCase);
        if (underMatch.Success)
        {
            parameters["maxValue"] = underMatch.Groups[1].Value;
        }

        // Extract sorting preferences
        if (Regex.IsMatch(query, @"\b(?:sort|order)\s+(?:by|).*\b(?:price|cost)\b", RegexOptions.IgnoreCase))
        {
            parameters["sortBy"] = "price";
        }
        if (Regex.IsMatch(query, @"\b(?:sort|order)\s+(?:by|).*\b(?:name|title)\b", RegexOptions.IgnoreCase))
        {
            parameters["sortBy"] = "name";
        }

        // Extract ascending/descending
        if (Regex.IsMatch(query, @"\b(?:ascending|lowest|cheapest|smallest|asc)\b", RegexOptions.IgnoreCase))
        {
            parameters["sortOrder"] = "asc";
        }
        if (Regex.IsMatch(query, @"\b(?:descending|highest|most|expensive|largest|desc)\b", RegexOptions.IgnoreCase))
        {
            parameters["sortOrder"] = "desc";
        }

        // Extract limit/count preferences
        var limitMatch = Regex.Match(query, @"(?:first|top|show|limit|take)\s+(\d+)", RegexOptions.IgnoreCase);
        if (limitMatch.Success)
        {
            parameters["limit"] = limitMatch.Groups[1].Value;
        }

        return parameters;
    }

    /// <summary>
    /// Pattern information for matching queries to tools.
    /// </summary>
    private sealed class ToolPattern
    {
        public string[] Keywords { get; set; } = Array.Empty<string>();
        public string[] RegexPatterns { get; set; } = Array.Empty<string>();
        public double MinConfidence { get; set; } = 0.5;
        public string Description { get; set; } = string.Empty;
    }
}
