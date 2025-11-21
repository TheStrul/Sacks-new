# Natural Language Query Support - User Guide

## Quick Start

### Typing "Hi" - How It Works Now

When you type "Hi" in the `aiQueryTextBox` and click **Execute AI Query**:

1. ? **Leave the tool dropdown on "-- Select a tool --"** (default)
2. ? **Type your natural language query**: "Hi" or "Show me products over $100"
3. ? **Click "Execute AI Query"**
4. ?? **The system will automatically:**
   - Analyze your query with heuristic pattern matching
   - Select the best matching tool
   - Extract parameters (prices, sorting, etc.)
   - Execute the tool
   - Display results with routing explanation

### Example Queries

#### Product Search
```
"Show me all products"
"Find products over $100"
"List items between $50 and $200"
"What products do we have?"
"Search for expensive products"
```

#### Supplier Statistics
```
"How many suppliers do we have?"
"Show me supplier stats"
"Get supplier summary"
"Analyze supplier information"
```

#### Offer Details
```
"Show me offers"
"List deals"
"What prices do we have?"
"Get offer information"
```

## How Query Routing Works

### Architecture

```
User Types: "Show me products over $100"
           ?
       DashBoard.ExecuteAiQueryButton_Click
           ?
       ProcessNaturalLanguageQueryAsync
           ?
       ILlmQueryRouterService.RouteQueryAsync
           ?
    HeuristicQueryRouterService (Pattern Matching)
           ?
    Match Tools: SearchProducts (90% confidence)
           ?
    Extract Parameters: { minPrice: "100", query: "..." }
           ?
    IMcpClientService.ExecuteToolAsync("SearchProducts", params)
           ?
    Display Results with Routing Explanation
```

### Routing Algorithm

The system uses **heuristic pattern matching** to select the best tool:

#### 1. Keyword Matching (0.15 points per keyword)
```csharp
// For "SearchProducts" tool
Keywords: ["product", "search", "find", "show", "list", ...]
// Query: "Show me products"
// Match: "show" (0.15) + "products" (0.15) = 0.30
```

#### 2. Regex Pattern Matching (0.30 points per pattern)
```csharp
// For "SearchProducts" tool
Patterns:
- @"(?:find|search|show|list|get)\s+(?:me\s+)?(?:products?|items?|things?)"
- @"products?\s+(?:with|by|containing|matching)"
- @"(?:how\s+)?(?:many|count)\s+(?:products|items)"

// Query: "Show me products"
// Matches first pattern: 0.30
```

#### 3. Confidence Score
```
Total Score = Keywords + Regex Matches
Normalized to 0.0 - 1.0
Minimum confidence threshold: varies by tool
```

### Parameter Extraction

The system automatically extracts structured parameters from natural language:

#### Numeric Values
```
"Show me products over $100"
? { minValue: "100" }

"Between $50 and $200"
? { minValue: "50", maxValue: "200" }
```

#### Sorting
```
"Sort by price"
? { sortBy: "price" }

"Most expensive first"
? { sortBy: "price", sortOrder: "desc" }

"Cheapest first"
? { sortBy: "price", sortOrder: "asc" }
```

## Current Limitations & Future Enhancements

### Current (Heuristic-Based)
? Works without external dependencies  
? Fast and lightweight  
? No API keys needed  
? Limited understanding of complex queries  
? Can't handle ambiguous requests well  

### Future (With LLM Integration)

To upgrade to full LLM support (OpenAI, Azure OpenAI):

1. **Install NuGet package**
   ```bash
   dotnet add package Azure.AI.OpenAI
   # or
   dotnet add package OpenAI
   ```

2. **Create `LlmQueryRouterService` replacing heuristic version**
   ```csharp
   public class LlmQueryRouterService : ILlmQueryRouterService
   {
       private readonly OpenAIClient _client;
       private readonly ILlmPromptBuilder _promptBuilder;
       
       public async Task<LlmRoutingResult> RouteQueryAsync(string query, ...)
       {
           var prompt = _promptBuilder.BuildRoutingPrompt(query);
           var response = await _client.GetChatCompletionsAsync(
               deploymentId: "gpt-4",
               chatCompletionOptions: ...
           );
           // Parse response to extract tool + parameters
       }
   }
   ```

3. **Update DI Registration**
   ```csharp
   // In Program.cs
   services.AddSingleton<ILlmQueryRouterService, LlmQueryRouterService>();
   // No code changes needed in DashBoard!
   ```

4. **Configure API Key**
   ```json
   {
     "AzureOpenAi": {
       "Endpoint": "https://your-resource.openai.azure.com/",
       "ApiKey": "your-api-key",
       "DeploymentId": "gpt-4"
     }
   }
   ```

## Troubleshooting

### Query Not Recognized
**Problem**: "Query doesn't match any tool"

**Solution**:
- Rephrase using keywords from the appropriate tool
- Try: "Show me products" instead of "Give me items"
- See "Example Queries" section above

### Low Confidence Score
**Problem**: Tool selected with < 50% confidence

**Solution**:
- Make query more specific
- Include relevant keywords
- Use structured terms (e.g., "products" vs "things")

### Wrong Tool Selected
**Problem**: System selected the wrong tool

**Solution**:
1. Select the correct tool manually from the dropdown
2. Type query as tool parameter
3. Click Execute

## Configuration

### Tuning Heuristic Patterns

Edit `HeuristicQueryRouterService.cs`:

```csharp
private static readonly Dictionary<string, ToolPattern> ToolPatterns = new()
{
    {
        "SearchProducts", new ToolPattern
        {
            Keywords = new[] { "product", "search", "find", ... },
            RegexPatterns = new[] { @"...", @"...", ... },
            MinConfidence = 0.6,  // ? Adjust threshold
            Description = "Search and retrieve products"
        }
    },
    // Add more patterns...
};
```

### Adding New Tools

1. Add tool name and pattern to `ToolPatterns` dictionary
2. Define keywords specific to that tool
3. Add regex patterns that match queries for that tool
4. Set minimum confidence threshold
5. Rebuild and restart

Example:
```csharp
{
    "MyNewTool", new ToolPattern
    {
        Keywords = new[] { "analyze", "report", "summary", ... },
        RegexPatterns = new[] { @"(?:analyze|report).*data", ... },
        MinConfidence = 0.65,
        Description = "Custom analysis tool"
    }
}
```

## Implementation Details

### Key Classes

| Class | Location | Purpose |
|-------|----------|---------|
| `ILlmQueryRouterService` | Interface | Contract for query routing |
| `HeuristicQueryRouterService` | Implementation | Keyword/regex-based routing |
| `LlmRoutingResult` | DTO | Results from routing |
| `DashBoard.ExecuteAiQueryButton_Click` | UI Logic | Entry point |
| `DashBoard.ProcessNaturalLanguageQueryAsync` | UI Logic | Query processing |

### Data Flow

```
User Input
   ?
ExecuteAiQueryButton_Click
   ? (No tool selected?)
ProcessNaturalLanguageQueryAsync
   ?
ILlmQueryRouterService.RouteQueryAsync
   ?
HeuristicQueryRouterService
   ?? FindBestMatchingTool (returns tool + confidence)
   ?? ExtractParameters (returns dict)
   ?? IMcpClientService.ExecuteToolAsync
       ?
       Result Display
```

## Example: Tracing "Show me products over $100"

1. **User types**: `"Show me products over $100"`
2. **No tool selected** (dropdown on default)
3. **ProcessNaturalLanguageQueryAsync called**
4. **RouteQueryAsync analysis**:
   - Query lowercase: `"show me products over $100"`
   - Check SearchProducts tool:
     - Keywords match: "show" (0.15) + "products" (0.15) = 0.30
     - Regex match: Pattern 1 matches "Show me products" = 0.30
     - Total: 0.60 (meets 0.60 threshold ?)
   - Check other tools: fail threshold
   - **Result**: SearchProducts selected (0.60 confidence)
5. **ExtractParameters**:
   - Find "$100" ? `minValue: "100"`
   - Find "over" context ? numeric threshold
   - Result: `{ minValue: "100", query: "Show me products over $100" }`
6. **Execute**:
   - Call `ExecuteToolAsync("SearchProducts", params)`
   - Receive results
7. **Display**:
   - Show routing analysis (tool, confidence, reason)
   - Show formatted results

---

**Happy querying!** ??

For LLM integration questions, see the "Future Enhancements" section above.
