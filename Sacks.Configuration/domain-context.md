# Sacks Product Management System - Domain Context

This file provides domain knowledge and operational instructions for the AI assistant when working with the Sacks product management system.

## System Overview

**Sacks** is a beauty products management system that:
- Imports supplier data from Excel files
- Normalizes product information (Brand, Name, Size, Unit, Concentration, Gender, etc.)
- Uses configurable JSON-based parsing rules
- Stores normalized data in SQL Server via Entity Framework Core
- Provides WinForms UI for data management
- Exposes MCP (Model Context Protocol) server for AI integration

## Key Concepts

### Suppliers
- Each supplier provides Excel files with product offers
- File formats vary: different columns, headers, subtitle rows
- Each supplier has a JSON configuration file: `supplier-{Name}.json`
- Currently configured: Chk, HAND, MB, PCA, POL, WW

### Product Properties
Standard properties extracted from supplier data:
- **Product.EAN**: Unique identifier (numeric)
- **Product.Brand**: Brand name (from lookup or extraction)
- **Product.Name**: Core product name (after all removals)
- **Product.Size**: Numeric value (ml, oz)
- **Product.Units**: Unit of measurement
- **Product.Concentration**: EDP, EDT, EDC, Parfum, etc.
- **Product.Gender**: M (Men), W (Women), U (Unisex)
- **Product.Type 1/Type 2**: Category (Spray, Set, Mini, etc.)
- **Product.COO**: Country of Origin
- **Offer.Price**: Numeric price
- **Offer.Quantity**: Stock quantity
- **Offer.Ref**: Reference code
- **Offer.Description**: Original description text

### Configuration Architecture

#### Supplier Configuration Files (`supplier-{Name}.json`)
Located in: `SacksApp/Configuration/`

Structure:
```json
{
  "Name": "SupplierName",
  "Currency": "USD",
  "ParserConfig": {
    "Settings": {
      "StopOnFirstMatchPerColumn": false,
      "DefaultCulture": "en-US"
    },
    "ColumnRules": [ /* parsing actions per column */ ]
  },
  "FileStructure": {
    "DataStartRowIndex": 2,
    "ExpectedColumnCount": 10,
    "HeaderRowIndex": 1,
    "Detection": {
      "FileNamePatterns": [ "supplier*.xls*" ]
    }
  },
  "SubtitleHandling": { /* optional */ }
}
```

#### Lookup Tables (`supplier-formats.json`)
Shared lookup tables for:
- Brand: Canonical brand names (case-insensitive)
- Gender: M, W, U mappings
- Concentration: EDP, EDT, EDC, etc.
- Size: Size values and variations
- Unit: ml, oz, fl oz, etc.
- COO: Country codes

All dictionaries use `StringComparer.OrdinalIgnoreCase` for case-insensitive matching.

### Parsing Engine

**Waterfall/Chain-of-Responsibility Pattern:**
The description column is parsed sequentially:
1. Extract Brand → remove from text
2. Extract Size & Unit from `.Clean` text → remove
3. Extract Concentration from `.Clean` text → remove
4. Extract Gender from `.Clean` text → remove
5. Extract Type from `.Clean` text → remove
6. Assign remaining `.Clean` text to Product.Name

**Actions:**
- `Assign`: Copy value to output
- `Find`: Extract with regex or lookup table
  - Options: `first|all|last`, `remove`, `ignorecase`
  - `remove` creates `.Clean` property with extracted text removed
- `Map`: Lookup value in shared table
- `Split`: Split by delimiter into array
- `Switch`: Conditional value mapping
- `Convert`: Unit conversion
- `Concat`: Join multiple values
- `Clear`: Set to empty

**Critical Rules:**
- Each waterfall step reads from previous step's `.Clean` output
- Use `remove` option to clean text for next step
- Regex patterns must be properly escaped: `\\d+` not `\d+`
- All property assignments use `Assign: true`
- Intermediate variables use `Assign: false`

## Domain-Specific Knowledge

### Beauty Product Patterns

**Brand Extraction:**
- Usually at beginning: "A. Banderas Black Seduction"
- Sometimes abbreviated: "D&G" for "Dolce & Gabbana"
- Lookup table in `supplier-formats.json` contains 200+ brands

**Size & Unit:**
- Common formats: "100ml", "3.4 oz", "100 ml", "3.4oz / 100ML"
- Regex: `(?i)(?<size>\\d+(?:\\.\\d+)?\\s*(?:ml|oz|fl\\s*oz))`
- Numeric extraction: `(?<num>\\d+(?:\\.\\d+)?)`

**Concentration:**
- Keywords: EDP (Eau de Parfum), EDT (Eau de Toilette), EDC (Eau de Cologne)
- Case-insensitive lookup
- Often appears after brand and before gender

**Gender:**
- Keywords: M, W, U, Men, Women, Wom, Male, Female, Unisex, L (Ladies)
- Format: "(W)", "For Men", "Wom"
- Map variations to canonical: M, W, U

**Type:**
- Keywords: Spray, Set, Mini, Gift Set, Samples, GIFTSET
- Often appears at end of description

**Country of Origin:**
- Format: "- Spain", "(USA)", "Made in France"
- Usually at end with separator

### Common Excel File Patterns

**Subtitle Headers:**
Some files (e.g., MB) have brand headers:
- Single-column rows with brand name: "ACQUA DI PARMA"
- These values apply to subsequent data rows until next subtitle
- `SubtitleHandling.Action: "parse"` extracts and assigns to Product.Brand

**Column Structures:**
- **Simple**: Each column has one property (EAN, Price, Quantity)
- **Complex**: Description column contains multiple properties
- **Structured**: Delimited format like "BRAND:GENDER:CODE"

**Detection:**
- Files matched by `FileNamePatterns`: case-insensitive substring match
- Example: `"chk*.xls*"` matches "chk_2024.xlsx", "CHK_January.xls"

## Supplier Configuration Creation

**Complete instructions available in:**
📄 `.github/supplier-configuration-guide.md`

This comprehensive guide includes:
- Step-by-step file analysis process
- Description column deep analysis (20+ examples)
- Waterfall pattern implementation
- Validation checklist
- Common mistakes and solutions
- Regex pattern library
- Complete JSON template

**When user requests supplier configuration creation:**
1. Read the supplier-configuration-guide.md file
2. Follow its 7-step process
3. Generate complete JSON configuration
4. Validate against checklist in Section 5.2

## Technical Stack

- **Framework**: .NET 10, C# 13
- **Database**: SQL Server (LocalDB) via Entity Framework Core
- **UI**: WinForms (SacksApp project)
- **Parsing**: ParsingEngine library (configuration-driven)
- **Data Layer**: Sacks.DataAccess (EF Core repositories)
- **Logic Layer**: Sacks.LogicLayer (business services)
- **MCP Server**: SacksMcp (exposes tools for AI integration)
- **Configuration**: JSON files in Sacks.Configuration/

## Coding Standards

From `.github/copilot-instructions.md`:

- **Async/Await**: All I/O must be async with `CancellationToken`
- **Nullability**: Enabled; avoid `!` suppression
- **Logging**: Structured with `ILogger<T>`
- **Security**: Parameterized SQL only; no secrets in code
- **Money**: Use `decimal` with explicit precision/scale
- **Time**: Use `DateTime.UtcNow` or NodaTime `Instant`
- **Breaking Changes**: Acceptable; no backward compatibility needed
- **EF Core**: Use `AsNoTracking()` for reads, `AsSplitQuery()` for wide graphs
- **Configuration**: In-place updates preserve object identity

## Common Operations

### Add New Supplier Configuration
1. Analyze Excel file structure (PowerShell ImportExcel or COM)
2. Extract sample descriptions (20-30 rows)
3. Design waterfall extraction pattern
4. Generate `supplier-{Name}.json`
5. Validate against checklist
6. Test with sample data

### Fix Parsing Issues
1. Verify FileStructure (DataStartRowIndex, ExpectedColumnCount)
2. Check waterfall chain: each step reads from `.Clean` of previous
3. Validate regex escaping: `\\d+` not `\d+`
4. Ensure `remove` option used in waterfall steps
5. Check lookup table names exist in `supplier-formats.json`
6. Verify conditions use correct syntax

### Validate Configuration
Run validation in `SacksApp` or check against:
- `DataStartRowIndex|HeaderRowIndex|ExpectedColumnCount` >= 1
- FileNamePatterns non-empty if Detection exists
- SubtitleHandling Action in {`parse`, `skip`}
- All lookup table names exist
- Regex patterns properly escaped
- No `.Clean` reference without `remove` option

## MCP Tools Available

When working through `ILlmQueryRouterService`, these tools are exposed via MCP server:
- **Product queries**: Search by name, brand, EAN, price range
- **Supplier queries**: List suppliers, get supplier details
- **Offer queries**: Find offers by criteria
- **Configuration management**: Read/update supplier configurations
- **System tools**: Record learnings, read domain context

Use these tools for data operations instead of direct database access.

## Response Modes

The LLM service supports two modes (configured in `sacks-config.json`):

**ToolOnly:**
- MUST select a tool for every query
- Always execute a tool call
- Explain reasoning if query doesn't match perfectly

**Conversational (Default):**
- Select tool for product/offer/supplier queries
- Answer directly for greetings, chitchat, general questions
- More natural interaction

## Best Practices

✅ **DO:**
- Read `.github/supplier-configuration-guide.md` for supplier config tasks
- Use waterfall pattern for description parsing
- Validate configurations before saving
- Test with sample data
- Use structured logging
- Apply async/await consistently
- Use `ConfigureAwait(false)` in libraries

❌ **DON'T:**
- Hardcode supplier-specific logic in code
- Use string concatenation for SQL
- Block UI thread in WinForms
- Suppress nullability warnings without reason
- Forget `.Clean` references in waterfall chains
- Use lookup tables for numeric extraction (use regex)

## User Preferences

- Address user as "Struly-Dear"
- Provide concise, factual responses
- Use diffs for code changes
- Keep answers <= 150 lines; use `READY FOR CONTINUE` for longer outputs

---

**Last Updated:** November 27, 2025
**Maintained by:** Human + AI collaborative learning
