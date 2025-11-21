# Natural Language Query - Quick Reference

## How to Use

### Scenario 1: Natural Language Query (Recommended)
```
1. Leave tool dropdown on "-- Select a tool --" (default)
2. Type query: "Hi" or "Show me products over $100"
3. Click "Execute AI Query"
4. System automatically finds best tool and executes
```

### Scenario 2: Specific Tool Selection
```
1. Select tool from dropdown (e.g., "SearchProducts - Search...")
2. Type parameters: "over $100"
3. Click "Execute AI Query"
4. System executes selected tool with your parameters
```

## Supported Queries by Tool

### SearchProducts
```
"Show me products"
"Find items over $100"
"Products between $50 and $200"
"List products sorted by price"
"How many products are expensive?"
```

### GetSupplierStats
```
"How many suppliers?"
"Show supplier stats"
"Supplier statistics"
"Supplier overview"
"Analyze suppliers"
```

### GetOfferDetails
```
"Show offers"
"List deals"
"What prices?"
"Offer information"
"Deal details"
```

## UI Response

### Example: User types "Show me products over $100"

```
?? Natural Language Query
?? Your question: Show me products over $100

?? Routing Analysis:
   Tool Selected: SearchProducts
   Confidence: 60%
   Reason: 2 keyword match(es); Pattern: (?:find|search|show|list...)

?? Tool Result:
????????????????????????????????????????????????????????????????????????????????
{
  "products": [
    { "id": 1, "name": "Premium Product A", "price": 150.00 },
    { "id": 2, "name": "Luxury Item B", "price": 225.50 }
  ],
  "count": 2,
  "executedAt": "2024-01-15T10:30:00Z"
}
```

## How It Works Behind the Scenes

```
Query Input
    ?
Heuristic Pattern Matching
    ?? Count keyword matches
    ?? Check regex patterns
    ?? Calculate confidence score
    ?? Select best tool
    ?
Parameter Extraction
    ?? Find numeric values ($, prices)
    ?? Detect sorting preferences
    ?? Extract range constraints
    ?? Build parameter dictionary
    ?
Execute Tool
    ?? Call MCP server
    ?? Get JSON results
    ?? Format for display
    ?
Display Results
    ?? Show routing explanation
    ?? Show confidence score
    ?? Show tool output
```

## Troubleshooting

| Issue | Solution |
|-------|----------|
| "Query doesn't match" | Rephrase using keywords: "products", "suppliers", "offers" |
| Low confidence (20-40%) | Be more specific: "Show me products" not "Give me things" |
| Wrong tool selected | Select correct tool manually from dropdown |
| No results | Tool executed but found no matching data (expected) |

## Configuration

### Change Confidence Thresholds
File: `HeuristicQueryRouterService.cs`
```csharp
MinConfidence = 0.6  // Default: 60%
// Lower = more lenient, higher = more strict
```

### Add New Tools
File: `HeuristicQueryRouterService.cs`
```csharp
// In ToolPatterns dictionary:
{
    "YourToolName", new ToolPattern
    {
        Keywords = new[] { "keyword1", "keyword2", ... },
        RegexPatterns = new[] { @"pattern1", @"pattern2", ... },
        MinConfidence = 0.65,
        Description = "Your tool description"
    }
}
```

## Future: LLM Integration

To use OpenAI/Azure OpenAI for smarter query understanding:

1. Install OpenAI NuGet package
2. Create `LlmQueryRouterService` (replaces heuristic)
3. Update `Program.cs` DI registration
4. Add API key to `appsettings.json`

**No UI changes needed!** The interface remains the same.

---

**Status**: ? Ready to use with heuristic routing  
**Can be upgraded**: Yes, to full LLM in future
