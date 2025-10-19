# ParsingEngine Actions Quick Reference

## All Supported Operations (Alphabetical)

| Operation | Purpose | Key Parameters | Example Use Case |
|-----------|---------|----------------|------------------|
| **assign** | Copy value from one key to another | None | Copy raw column value to output |
| **case** / **switch** | Conditional value mapping (if-else) | `IgnoreCase`, `Default`, `When:<value>` | Map "-" to "Unknown", "M" to "Male" |
| **caseformat** | Transform text casing | `Mode`, `Culture` | Convert "HELLO WORLD" ? "Hello World" |
| **clear** | Remove a property | None | Clear temporary values |
| **concat** | Concatenate multiple values | `Keys`, `Separator` | Combine "Brand" + "Type" ? "BrandType" |
| **convert** | Unit conversion with snapping | `Preset`, `FromUnit`, `ToUnit`, `UnitKey`, `SetUnit` | Convert 1.7oz ? 50ml (snapped) |
| **find** | Regex pattern matching/extraction | `Pattern`, `Options`, `PatternKey` | Extract digits, remove uppercase words |
| **map** / **mapping** | Lookup table transformation | `Table`, `CaseMode`, `AddIfNotFound` | Normalize "Male" ? "M" via lookup |
| **split** | Split string by delimiter | `Delimiter`, `ExpectedParts`, `Strict` | Split "A:B:C" ? ["A", "B", "C"] |

---

## Operation Details

### 1. **assign** - Simple Value Copy
```json
{
  "Op": "assign",
  "Input": "ColumnA",
  "Output": "Product.Name",
  "Assign": true
}
```
- **Purpose**: Copy value without transformation
- **Parameters**: None allowed
- **Returns**: Always true if input exists

---

### 2. **switch** / **case** - Conditional Mapping
```json
{
  "Op": "switch",
  "Input": "Gender",
  "Output": "Product.Gender",
  "Parameters": {
    "IgnoreCase": "true",
    "When:M": "Male",
    "When:F": "Female",
    "When:U": "Unisex",
    "When:-": "Unknown",
    "Default": "Unisex"
  }
}
```
- **Purpose**: Map input values to output values (like a switch statement)
- **Parameters**:
  - `IgnoreCase` (optional): `true` or `false` (default: `false`)
  - `When:<inputValue>` (multiple): Condition ? Result mapping
  - `Default` (optional): Fallback value if no match
- **Validation**: Requires at least one `When:` or a `Default`
- **Returns**: `true` if match found or default used, `false` otherwise

**Common Patterns:**
```json
// Handle special characters
"When:-": "Unknown",
"When:N/A": "NotApplicable"

// Case-insensitive matching
"IgnoreCase": "true",
"When:yes": "Y",
"When:no": "N"

// Catch-all with default
"Default": "Unknown"
```

---

### 3. **caseformat** - Text Casing
```json
{
  "Op": "caseformat",
  "Input": "Name",
  "Output": "Product.Name",
  "Parameters": {
    "Mode": "title",
    "Culture": "en-US"
  }
}
```
- **Purpose**: Change text capitalization
- **Parameters**:
  - `Mode`: `title`, `upper`, or `lower` (default: `title`)
  - `Culture` (optional): Culture code like "en-US"
- **Examples**:
  - `title`: "hello world" ? "Hello World"
  - `upper`: "hello" ? "HELLO"
  - `lower`: "HELLO" ? "hello"

---

### 4. **clear** - Remove Property
```json
{
  "Op": "clear",
  "Input": "TempValue",
  "Output": "TempValue"
}
```
- **Purpose**: Remove a property from the bag
- **Parameters**: None allowed
- **Use Case**: Clean up temporary variables

---

### 5. **concat** - String Concatenation
```json
{
  "Op": "concat",
  "Input": "Ignored",
  "Output": "Product.FullName",
  "Parameters": {
    "Keys": "Brand,Type,Size",
    "Separator": " "
  }
}
```
- **Purpose**: Combine multiple bag values into one
- **Parameters**:
  - `Keys` (required): Comma-separated list of keys to concatenate
  - `Separator` (optional): String to insert between values (default: empty)
- **Example**: Brand="Acme", Type="Widget", Size="Large" ? "Acme Widget Large"

---

### 6. **convert** - Unit Conversion
```json
{
  "Op": "convert",
  "Input": "Size",
  "Output": "Product.Size",
  "Parameters": {
    "Preset": "OzToMl",
    "SetUnit": "true"
  }
}
```
- **Purpose**: Convert units (e.g., oz to ml) with smart snapping
- **Parameters**:
  - `Preset` (optional): Predefined conversion (e.g., `OzToMl`)
  - `FromUnit` (optional): Source unit (overrides preset)
  - `ToUnit` (optional): Target unit (overrides preset)
  - `UnitKey` (optional): Bag key containing unit info
  - `SetUnit` (optional): Whether to append unit to output (default: `true`)
- **Validation**: Requires either `Preset` or `FromUnit`/`ToUnit` pair
- **Example**: "1.7 oz" ? "50ml" (snaps to common bottle size)

---

### 7. **find** - Pattern Matching
```json
{
  "Op": "find",
  "Input": "Description",
  "Output": "ExtractedValue",
  "Parameters": {
    "Pattern": "\\d+ml",
    "Options": "first,ignorecase"
  }
}
```
- **Purpose**: Extract or remove text using regex patterns
- **Parameters**:
  - `Pattern` (required): Regex pattern OR `Lookup:<TableName>` for multi-pattern
  - `PatternKey` (optional): Bag key containing pattern
  - `Options` (optional): Comma-separated: `first`, `last`, `all`, `ignorecase`, `remove`
- **Options Explained**:
  - `first`: Return first match only (default)
  - `last`: Return last match only
  - `all`: Return all matches
  - `ignorecase`: Case-insensitive matching
  - `remove`: Remove matched text, store in `.Clean`
- **Output Keys**:
  - `Output[0]`, `Output[1]`, ... : Matched values
  - `Output.Length`: Number of matches
  - `Output.Clean`: Original text with matches removed (if `remove` option)
  - `Output.Valid`: `true` if any match found

**Common Patterns:**
```json
// Extract numbers
"Pattern": "\\d+"

// Extract size with unit
"Pattern": "\\d+(\\.\\d+)?\\s*(ml|oz|g)"

// Remove uppercase words
"Pattern": "\\b\\p{Lu}+\\b",
"Options": "all,remove"

// Multi-pattern from lookup
"Pattern": "Lookup:Sizes"
```

---

### 8. **map** / **mapping** - Lookup Table
```json
{
  "Op": "map",
  "Input": "RawGender",
  "Output": "Product.Gender",
  "Parameters": {
    "Table": "Gender",
    "AddIfNotFound": "false"
  }
}
```
- **Purpose**: Transform values using a lookup table
- **Parameters**:
  - `Table` (required): Name of lookup table to use
  - `CaseMode` (optional): `exact`, `upper`, or `lower`
  - `AddIfNotFound` (optional): Add missing keys to lookup (default: `false`)
- **Validation**: Table must exist in merged lookups
- **Returns**: `false` if input not found and no AddIfNotFound

**Example Lookup Table:**
```json
{
  "Lookups": {
    "Gender": {
      "M": "Male",
      "F": "Female",
      "Male": "Male",
      "Female": "Female",
      "U": "Unisex"
    }
  }
}
```

---

### 9. **split** - String Splitting
```json
{
  "Op": "split",
  "Input": "Dimensions",
  "Output": "Parts",
  "Parameters": {
    "Delimiter": "x",
    "ExpectedParts": "3",
    "Strict": "false"
  }
}
```
- **Purpose**: Split a string by delimiter
- **Parameters**:
  - `Delimiter` (required): Character(s) to split on
  - `ExpectedParts` (optional): Expected number of parts for validation
  - `Strict` (optional): Fail if part count doesn't match expected
- **Output Keys**:
  - `Output[0]`, `Output[1]`, ... : Individual parts
  - `Output.Length`: Number of parts
  - `Output.Valid`: `true` if split successful
- **Example**: "10x20x30" split by "x" ? ["10", "20", "30"]

---

## Common Patterns & Workflows

### Pattern 1: Normalize Gender
```json
[
  {
    "Op": "find",
    "Input": "Text",
    "Output": "TempGender",
    "Parameters": {
      "Pattern": "\\b(Men|Women|Unisex|Male|Female)\\b",
      "Options": "first,ignorecase"
    }
  },
  {
    "Op": "map",
    "Input": "TempGender",
    "Output": "Product.Gender",
    "Parameters": {
      "Table": "Gender"
    }
  }
]
```

### Pattern 2: Extract & Convert Size
```json
[
  {
    "Op": "find",
    "Input": "Description",
    "Output": "TempSize",
    "Parameters": {
      "Pattern": "\\d+(\\.\\d+)?\\s*(ml|oz)",
      "Options": "first"
    }
  },
  {
    "Op": "convert",
    "Input": "TempSize",
    "Output": "Product.Size",
    "Parameters": {
      "Preset": "OzToMl"
    }
  }
]
```

### Pattern 3: Clean Brand Names
```json
[
  {
    "Op": "find",
    "Input": "RawBrand",
    "Output": "CleanedBrand",
    "Parameters": {
      "Pattern": "\\b(INC|LLC|LTD|CO|CORP)\\b",
      "Options": "all,remove,ignorecase"
    }
  },
  {
    "Op": "map",
    "Input": "CleanedBrand.Clean",
    "Output": "Product.Brand",
    "Parameters": {
      "Table": "Brands"
    }
  }
]
```

### Pattern 4: Handle Missing Values
```json
{
  "Op": "switch",
  "Input": "Value",
  "Output": "Product.Field",
  "Parameters": {
    "When:-": "Unknown",
    "When:N/A": "NotApplicable",
    "When:": "Empty",
    "Default": "{Value}"
  }
}
```

---

## Validation Rules Summary

| Operation | Must Have | Optional | Cannot Have |
|-----------|-----------|----------|-------------|
| assign | None | None | Any parameters |
| switch | 1+ `When:` OR `Default` | `IgnoreCase` | Unknown params |
| caseformat | None | `Mode`, `Culture` | Unknown params |
| clear | None | None | Any parameters |
| concat | `Keys` | `Separator` | Unknown params |
| convert | `Preset` OR `FromUnit` | `ToUnit`, `UnitKey`, `SetUnit` | Unknown params |
| find | `Pattern` OR `PatternKey` | `Options` | Unknown params |
| map | `Table` (existing) | `CaseMode`, `AddIfNotFound` | Unknown params |
| split | `Delimiter` | `ExpectedParts`, `Strict` | Unknown params |

---

*Quick Reference for Struly-Dear*
*All operations are case-insensitive in Op field*
