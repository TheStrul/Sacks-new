# Supplier Configuration Guide

## Overview

This guide explains the structure and logic behind supplier configuration JSON files in the Sacks solution. These files define how to parse Excel files from different suppliers, extracting product information through a declarative rule-based system.

**Target Framework**: .NET 8/9  
**JSON Parser**: `System.Text.Json`  
**Example**: `supplier-PCA.json`

---

## Top-Level Structure

Each supplier configuration file contains:

```json
{
  "Name": "SUPPLIER_CODE",
  "Currency": "USD",
  "ParserConfig": { ... },
  "FileStructure": { ... },
  "SubtitleHandling": { ... } // optional
}
```

### Core Properties

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| `Name` | `string` | ? | Unique supplier identifier (e.g., "PCA", "ACME") |
| `Currency` | `string` | ? | Default currency code (e.g., "USD", "EUR") |
| `ParserConfig` | `object` | ? | Parsing rules for data extraction |
| `FileStructure` | `object` | ? | Excel file layout definition |
| `SubtitleHandling` | `object` | ? | Configuration for subtitle rows (if present) |

---

## FileStructure Configuration

Defines the physical layout of the Excel file.

```json
{
  "FileStructure": {
    "DataStartRowIndex": 2,
    "ExpectedColumnCount": 10,
    "HeaderRowIndex": 1,
    "Detection": {
      "FileNamePatterns": [ "PCA*.xls*" ]
    }
  }
}
```

### Properties

| Property | Type | Required | Validation | Description |
|----------|------|----------|------------|-------------|
| `DataStartRowIndex` | `int` | ? | `>= 1` | **1-based** Excel row where data starts |
| `ExpectedColumnCount` | `int` | ? | `>= 1` | Number of columns expected in each data row |
| `HeaderRowIndex` | `int` | ? | `>= 1` | **1-based** Excel row containing column headers |
| `Detection` | `object` | ? | - | Auto-detection rules for supplier files |
| `Detection.FileNamePatterns` | `string[]` | ? (if `Detection` exists) | Non-empty | Wildcard patterns to match supplier files |

**Notes**:
- Row indices use Excel numbering (first row = 1)
- `Detection` enables automatic supplier identification from filename
- Patterns support wildcards: `*` (any chars), `?` (single char)

---

## ParserConfig Structure

The heart of the configuration: defines how to extract and transform data from Excel cells.

```json
{
  "ParserConfig": {
    "Settings": { ... },
    "ColumnRules": { ... }
  }
}
```

### Settings

Global parsing behavior:

```json
{
  "Settings": {
    "StopOnFirstMatchPerColumn": false,
    "DefaultCulture": "en-US",
    "PreferFirstAssignment": true
  }
}
```

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `StopOnFirstMatchPerColumn` | `bool` | `false` | Stop processing column rules after first match |
| `DefaultCulture` | `string` | `"en-US"` | Culture for number/date parsing |
| `PreferFirstAssignment` | `bool` | `false` | When `true`, first value written to a property wins; later assignments are ignored |

**PreferFirstAssignment Use Case**: Enable when subtitle rows provide baseline values (e.g., Brand) that should only be overridden if explicitly present in data rows.

---

## ColumnRules: Mapping Excel Columns to Actions

Each key represents an Excel column (A, B, C, etc.). Each column contains a list of actions executed sequentially.

```json
{
  "ColumnRules": {
    "A": {
      "Actions": [ ... ],
      "Trace": false
    },
    "B": { ... }
  }
}
```

### RuleConfig Properties

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| `Actions` | `ActionConfig[]` | ? | Sequential list of operations to perform on cell content |
| `Trace` | `bool` | ? | If `true`, emit detailed execution logs for debugging |

---

## Action Types

Actions are executed in order. Each action reads from the **working bag** (a key-value store) and writes results back.

### Common Action Structure

```json
{
  "Op": "ACTION_TYPE",
  "Input": "INPUT_KEY",
  "Output": "OUTPUT_KEY",
  "Assign": true,
  "Condition": "KEY.Valid==true",
  "Parameters": { ... }
}
```

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| `Op` | `string` | ? | Action type (see below) |
| `Input` | `string` | ? | Source key in bag (use `"Text"` for raw cell value) |
| `Output` | `string` | ? | Destination key in bag |
| `Assign` | `bool` | ? | If `true`, write to PropertyBag (prefixed with `assign:`); if `false`, intermediate result only |
| `Condition` | `string` | ? | Execute only if condition is met (e.g., `"Product.Brand.Valid==true"`) |
| `Parameters` | `object` | ? | Action-specific parameters |

---

## Action Types Reference

### 1. **Assign**

Copy a value directly from input to output.

```json
{
  "Op": "Assign",
  "Input": "Text",
  "Output": "Product.EAN",
  "Assign": true,
  "Condition": null,
  "Parameters": null
}
```

**Parameters**: None allowed  
**Use Case**: Direct mapping (e.g., EAN code, SKU)

---

### 2. **Find**

Extract substring(s) using regex or lookup tables.

```json
{
  "Op": "Find",
  "Input": "Text",
  "Output": "Product.Size",
  "Assign": true,
  "Condition": null,
  "Parameters": {
    "Pattern": "(?<num>\\d+(?:[.,]\\d+)?)\\s*ml",
    "Options": "first,ignorecase,remove"
  }
}
```

**Parameters**:

| Key | Required | Description |
|-----|----------|-------------|
| `Pattern` | ? (or `PatternKey`) | Regex pattern or `lookup:TableName` to search lookup entries |
| `PatternKey` | ? | Alternative: reference another bag key containing the pattern |
| `Options` | ? | Comma-separated: `first`/`all`, `ignorecase`, `remove` (remove matched text from `.Clean`) |

**Special Patterns**:
- `lookup:TableName` — searches all keys in the specified lookup table (preserves order)
- Named capture groups extract specific portions: `(?<num>\\d+)` ? extracts to `.num` sub-key

**Output Keys**:
- `Output` — matched value
- `Output.Valid` — `true`/`false` indicating success
- `Output.Clean` — input text with match removed (if `remove` option set)
- `Output[0]`, `Output[1]` — array indexing for `all` option
- `Output.{groupName}` — named capture group result

---

### 3. **Map** / **Mapping**

Normalize a value using a lookup table.

```json
{
  "Op": "Map",
  "Input": "Text",
  "Output": "Product.Gender",
  "Assign": true,
  "Condition": null,
  "Parameters": {
    "Table": "Gender",
    "AddIfNotFound": "true"
  }
}
```

**Parameters**:

| Key | Required | Description |
|-----|----------|-------------|
| `Table` | ? | Lookup table name (must exist in merged lookups) |
| `CaseMode` | ? | Case transformation mode (if supported) |
| `AddIfNotFound` | ? | `"true"` = add unmapped values to lookup; `"false"` (default) = fail silently |

**Lookup Tables**: Case-insensitive dictionaries merged from:
1. Root-level `Lookups` (shared across suppliers)
2. Supplier-specific lookups (override/extend root)

**Use Case**: Standardize variants (e.g., "M", "male", "Man" ? "Men")

---

### 4. **Switch**

Match input against multiple cases and return corresponding value.

```json
{
  "Op": "Switch",
  "Input": "Text",
  "Output": "Product.Decoded",
  "Assign": true,
  "Condition": null,
  "Parameters": {
    "IgnoreCase": "true",
    "When:-": "True",
    "Default": "False"
  }
}
```

**Parameters**:

| Key | Required | Description |
|-----|----------|-------------|
| `IgnoreCase` | ? | `"true"` for case-insensitive matching |
| `When:{text}` | ? | Case definitions: key = match text, value = output |
| `Default` | ? | Fallback value if no case matches |

**Use Case**: Boolean flags, status codes, categorical mappings

---

### 5. **Convert**

Unit conversion with optional snapping.

```json
{
  "Op": "Convert",
  "Input": "assign:Product.Size",
  "Output": "Product.Size",
  "Assign": true,
  "Condition": "Product.Units.Valid==true",
  "Parameters": {
    "Preset": "OzToMl",
    "FromUnit": "oz",
    "ToUnit": "ml",
    "UnitKey": "Product.Units",
    "SetUnit": "true"
  }
}
```

**Parameters**:

| Key | Required | Description |
|-----|----------|-------------|
| `Preset` | ? | Predefined conversion (e.g., `"OzToMl"` = 29.5735 factor) |
| `FromUnit` | ? | Source unit (overrides preset default) |
| `ToUnit` | ? | Target unit (overrides preset default) |
| `UnitKey` | ? | Bag key containing current unit; only convert if matches `FromUnit` |
| `SetUnit` | ? | `"true"` to update `UnitKey` to `ToUnit` after conversion |

**Presets**:
- `OzToMl`: 1 oz = 29.5735 ml with smart rounding (e.g., 3.4 oz ? 100 ml)

**Use Case**: Standardize product sizes to metric

---

### 6. **Split**

Split text by delimiter into indexed array.

```json
{
  "Op": "Split",
  "Input": "Text",
  "Output": "Parts",
  "Assign": false,
  "Condition": null,
  "Parameters": {
    "Delimiter": ":",
    "ExpectedParts": "2",
    "Strict": "true"
  }
}
```

**Parameters**:

| Key | Required | Description |
|-----|----------|-------------|
| `Delimiter` | ? | Split character (default: `":"`) |
| `ExpectedParts` | ? | Number of parts expected (validation) |
| `Strict` | ? | `"true"` = fail if part count doesn't match |

**Output**: `Parts[0]`, `Parts[1]`, etc.

---

### 7. **Concat**

Join multiple bag values into one string.

```json
{
  "Op": "Concat",
  "Input": "Text",
  "Output": "Product.Name",
  "Assign": true,
  "Condition": "assign:Product.Name==",
  "Parameters": {
    "Keys": "TextNoBrand,assign:Product.Gender",
    "Separator": " "
  }
}
```

**Parameters**:

| Key | Required | Description |
|-----|----------|-------------|
| `Keys` | ? | Comma-separated list of bag keys to concatenate |
| `Separator` | ? | String inserted between values (default: `" "`) |

**Use Case**: Construct fallback names, combine descriptive fields

---

### 8. **Clear**

Set output to empty string.

```json
{
  "Op": "Clear",
  "Input": "Text",
  "Output": "TempValue",
  "Assign": false,
  "Condition": null,
  "Parameters": null
}
```

**Parameters**: None allowed  
**Use Case**: Reset intermediate values in multi-step pipelines

---

### 9. **CaseFormat** (if implemented)

Transform text casing.

```json
{
  "Op": "CaseFormat",
  "Input": "Text",
  "Output": "Product.Name",
  "Assign": true,
  "Condition": null,
  "Parameters": {
    "Mode": "title",
    "Culture": "en-US"
  }
}
```

**Parameters**:

| Key | Required | Description |
|-----|----------|-------------|
| `Mode` | ? | `"upper"`, `"lower"`, `"title"` |
| `Culture` | ? | Culture for case conversion |

---

## Action Execution Flow

### Working Bag Lifecycle

1. **Initialization**: Bag seeded with `"Text"` = raw cell value
2. **Static Assigns**: Pre-populate `assign:*` keys from `RuleConfig.Assign` (if any)
3. **Sequential Execution**: Each action reads from bag, writes to bag
4. **Assignment Collection**: Keys prefixed with `assign:` are extracted and written to `PropertyBag`

### Input Sources

| Input Pattern | Source |
|---------------|--------|
| `"Text"` | Raw cell content |
| `"SomeKey"` | Intermediate result from previous action |
| `"assign:Product.Brand"` | Previously assigned value from PropertyBag |
| `"SomeKey.Clean"` | Cleaned version (after `Find` with `remove` option) |
| `"SomeKey[0]"` | Array element (after `Split` or `Find` with `all`) |
| `"SomeKey.Valid"` | Boolean success flag (from `Find`, `Map`, etc.) |

---

## Condition Syntax

Execute action only if condition evaluates to `true`.

### Supported Operators

| Operator | Example | Description |
|----------|---------|-------------|
| `==` | `IsSet.Valid==false` | Equality check |
| `!=` | `Product.Brand!=` | Not equal (empty check) |

### Common Patterns

```json
// Execute only if previous Find succeeded
"Condition": "SizesAndUnits.Valid==true"

// Execute only if value is NOT already set
"Condition": "assign:Product.Name=="

// Execute only if previous extraction failed
"Condition": "IsSet.Valid==false"
```

---

## PCA Supplier Example Walkthrough

### Column G: Complex Multi-Step Extraction

This example demonstrates progressive text cleaning and extraction:

```json
{
  "Op": "Assign",
  "Input": "Text",
  "Output": "Offer.Description",
  "Assign": true
}
```
**Step 1**: Store full description

```json
{
  "Op": "Find",
  "Input": "Text",
  "Output": "TextNoBrand",
  "Assign": false,
  "Parameters": {
    "PatternKey": "Product.Brand",
    "Options": "first,ignorecase,remove"
  }
}
```
**Step 2**: Remove brand name (found in column A) from description

```json
{
  "Op": "Find",
  "Input": "TextNoBrand.Clean",
  "Output": "Product.Type 2",
  "Assign": true,
  "Parameters": {
    "Pattern": "lookup:Type 2",
    "Options": "first,ignorecase,remove"
  }
}
```
**Step 3**: Extract product type from lookup table, remove from text

```json
{
  "Op": "Find",
  "Input": "assign:Product.Type 2",
  "Output": "IsSet",
  "Assign": false,
  "Parameters": {
    "Pattern": "(?i)^(mini\\s*set|set)$",
    "Options": "first"
  }
}
```
**Step 4**: Detect if product is a "set" (changes parsing logic later)

```json
{
  "Op": "Find",
  "Input": "SizesAndUnits[0]",
  "Output": "Product.Size",
  "Assign": true,
  "Condition": "SizesAndUnits.Valid==true",
  "Parameters": {
    "Pattern": "(?i)(?<num>\\d+(?:[.,]\\d+)?)(?=\\s*(?:ml|oz|fl\\s*oz|g|gram)\\b)",
    "Options": "first"
  }
}
```
**Step 5**: Extract numeric size (conditional on previous size detection)

```json
{
  "Op": "Convert",
  "Input": "assign:Product.Size",
  "Output": "Product.Size",
  "Assign": true,
  "Condition": "Product.Units.Valid==true",
  "Parameters": {
    "Preset": "OzToMl",
    "FromUnit": "oz",
    "ToUnit": "ml",
    "UnitKey": "Product.Units",
    "SetUnit": "true"
  }
}
```
**Step 6**: Convert oz ? ml if units were detected as "oz"

---

## Lookup Tables

Shared dictionaries for normalization, stored at root or supplier level.

### Structure

```json
{
  "Lookups": {
    "Gender": {
      "M": "Men",
      "F": "Women",
      "U": "Unisex",
      "male": "Men",
      "female": "Women"
    },
    "Brand": {
      "D&G": "Dolce & Gabbana",
      "YSL": "Yves Saint Laurent"
    }
  }
}
```

### Rules

- **Case-Insensitive**: Both outer and inner dictionaries use `StringComparer.OrdinalIgnoreCase`
- **Merge Behavior**: Supplier-specific lookups override/extend root lookups
- **Validation**: Must call `ParserConfig.DoMergeLoookUpTables()` after updating
- **Usage**: Reference via `"Pattern": "lookup:TableName"` or `"Table": "TableName"`

---

## Subtitle Handling (Optional)

For Excel files with subtitle rows (e.g., category headers).

```json
{
  "SubtitleHandling": {
    "Enabled": true,
    "Action": "parse",
    "DetectionRules": [ ... ],
    "FallbackAction": "skip",
    "Assignments": [ ... ],
    "Transforms": [ ... ]
  }
}
```

### Properties

| Property | Description |
|----------|-------------|
| `Enabled` | Enable subtitle processing |
| `Action` | `"parse"` (extract values) or `"skip"` (ignore row) |
| `DetectionRules` | Rules to identify subtitle rows (by column count or pattern) |
| `FallbackAction` | Action if no rule matches: `"skip"` or `"ignore"` |
| `Assignments` | Map extracted subtitle values to PropertyBag keys (e.g., `Brand ? Product.Brand`) |
| `Transforms` | Regex/text transformations before assignment |

**Use Case**: Extract brand/category from section headers, apply to all subsequent rows.

---

## Validation Rules

The `ValidateConfiguration()` method enforces:

### Root Configuration

- ? Non-empty `Version`
- ? At least one supplier
- ? All lookup tables have non-null keys/values

### FileStructure

- ? `DataStartRowIndex >= 1`
- ? `HeaderRowIndex >= 1`
- ? `ExpectedColumnCount >= 1`
- ? If `Detection` exists, `FileNamePatterns` must be non-empty

### ParserConfig

- ? At least one column rule
- ? Each column has at least one action
- ? All actions have non-empty `Op`, `Input`, `Output`

### Action Parameters

| Op | Required Parameters | Allowed Parameters |
|----|---------------------|---------------------|
| `assign` | None | None |
| `find` | `Pattern` or `PatternKey` | `Pattern`, `Options`, `PatternKey` |
| `split` | None | `Delimiter`, `ExpectedParts`, `Strict` |
| `map` | `Table` | `Table`, `CaseMode`, `AddIfNotFound` |
| `convert` | `Preset` or `FromUnit`+`ToUnit` | `Preset`, `FromUnit`, `ToUnit`, `UnitKey`, `SetUnit` |
| `concat` | `Keys` | `Keys`, `Separator` |
| `clear` | None | None |

**Regex Validation**: All `Pattern` parameters (except `lookup:*` patterns) must be valid regex.

---

## Best Practices

### 1. **Progressive Cleaning**

Chain `Find` actions with `remove` option to progressively strip known patterns:

```json
// Remove brand ? Remove type ? Remove size ? Remaining = product name
```

### 2. **Conditional Assignments**

Use `Condition` to avoid overwriting valid data:

```json
{
  "Op": "Concat",
  "Output": "Product.Name",
  "Condition": "assign:Product.Name==",  // Only if Name is empty
  "Parameters": { "Keys": "Fallback1,Fallback2" }
}
```

### 3. **Intermediate Results**

Use `"Assign": false` for temporary values:

```json
{
  "Op": "Find",
  "Output": "TempMatch",
  "Assign": false  // Don't write to PropertyBag
}
```

### 4. **Lookup Table Usage**

Prefer `lookup:TableName` over hardcoded patterns for maintainability:

```json
// ? Bad: Hardcoded brands
"Pattern": "(?i)(Chanel|Dior|Gucci)"

// ? Good: Centralized lookup
"Pattern": "lookup:Brand"
```

### 5. **Tracing**

Enable `"Trace": true` during development to see step-by-step execution:

```json
{
  "ColumnRules": {
    "G": {
      "Actions": [ ... ],
      "Trace": true  // Emit detailed logs
    }
  }
}
```

---

## Creating a New Supplier Configuration

### Checklist

1. **Analyze Sample File**
   - Row structure (header, data start)
   - Column mapping (what data is in which column?)
   - Special rows (subtitles, totals)?

2. **Define FileStructure**
   ```json
   {
     "DataStartRowIndex": ?,
     "HeaderRowIndex": ?,
     "ExpectedColumnCount": ?,
     "Detection": { "FileNamePatterns": ["SUPPLIER*.xlsx"] }
   }
   ```

3. **Map Columns to PropertyBag Keys**
   - Direct mappings: Use `Assign`
   - Lookups: Use `Map` with appropriate table
   - Extractions: Use `Find` with regex or `lookup:*`

4. **Build Action Chains**
   - Start simple (direct assignments)
   - Add complexity (progressive cleaning)
   - Use conditions to handle variants

5. **Test Incrementally**
   - Enable `"Trace": true` on complex columns
   - Verify intermediate values (`.Clean`, `.Valid`)
   - Check output PropertyBag keys

6. **Validate**
   - Run `ValidateConfiguration()` to catch errors
   - Test with real supplier files
   - Compare parsed results against expected values

---

## Output PropertyBag Structure

After parsing, the `PropertyBag` contains extracted values:

### Standard Keys

| Key Pattern | Example | Description |
|-------------|---------|-------------|
| `Product.*` | `Product.Brand`, `Product.EAN` | Product-level attributes |
| `Offer.*` | `Offer.Price`, `Offer.Quantity` | Offer-level attributes |
| Custom | `MyCustom.Field` | Any namespace supported |

### Reserved Namespaces

- `Product`: Brand, Name, Type, Size, Units, Gender, COO, EAN, Concentration
- `Offer`: Ref, Description, Price, Quantity, case pack

**Access Pattern**:
```csharp
var brand = bag.GetString("Product.Brand");
var price = bag.GetDecimal("Offer.Price");
```

---

## Advanced Patterns

### Pattern: Fallback Cascade

Assign from primary source, fall back to secondary if empty:

```json
[
  { "Op": "Find", "Output": "Product.Name", "Assign": true, "Input": "Text", "Parameters": { "Pattern": "..." } },
  { "Op": "Assign", "Output": "Product.Name", "Assign": true, "Condition": "assign:Product.Name==", "Input": "Text" }
]
```

### Pattern: Multi-Pass Extraction

Extract multiple values from same text:

```json
[
  { "Op": "Find", "Output": "Size1", "Assign": false, "Parameters": { "Pattern": "...", "Options": "first,remove" } },
  { "Op": "Find", "Input": "Size1.Clean", "Output": "Size2", "Assign": false, "Parameters": { "Pattern": "...", "Options": "first,remove" } },
  { "Op": "Concat", "Output": "Product.Sizes", "Assign": true, "Parameters": { "Keys": "Size1,Size2", "Separator": "+" } }
]
```

### Pattern: Conditional Type Detection

Alter parsing logic based on detected product type:

```json
[
  { "Op": "Find", "Output": "IsSet", "Assign": false, "Parameters": { "Pattern": "(?i)\\bset\\b" } },
  { "Op": "Find", "Output": "Product.Size", "Assign": true, "Condition": "IsSet.Valid==false", "Parameters": { ... } },
  { "Op": "Assign", "Output": "Product.Size", "Assign": true, "Condition": "IsSet.Valid==true", "Input": "DefaultSetSize" }
]
```

---

## Serialization Notes

- **Encoder**: `UnsafeRelaxedJsonEscaping` (allows special chars without escaping)
- **Formatting**: `WriteIndented = true` (human-readable)
- **Casing**: PascalCase for properties (`Op`, `Input`, `Output`)
- **Nulls**: Omit null parameters for cleaner JSON

---

## Summary

- **FileStructure**: Defines Excel layout (row indices, column count, detection)
- **ParserConfig**: Contains `Settings` + `ColumnRules`
- **ColumnRules**: Map Excel columns to action chains
- **Actions**: Sequential operations (Assign, Find, Map, Convert, etc.)
- **Lookups**: Case-insensitive dictionaries for normalization
- **Conditions**: Control action execution based on intermediate results
- **PropertyBag**: Final output containing extracted key-value pairs

This declarative approach enables non-developers to configure parsing logic without code changes, supporting rapid onboarding of new suppliers. ??
