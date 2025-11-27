# Supplier Configuration Creation Guide

## Purpose
This guide provides step-by-step instructions for creating a complete and accurate supplier JSON configuration file from an Excel input file. Follow this process to achieve 100% parsing accuracy.

**Dual Usage:**
- 📖 **Human Reference**: Detailed instructions for manually creating supplier configurations
- 🤖 **AI LLM Context**: When loaded into `ILlmQueryRouterService` via `DomainContext`, the AI can use this knowledge to automatically generate complete supplier JSON configurations from Excel files

**AI Integration:**
When this file is configured as `DomainContext` in `sacks-config.json`, the LLM service will:
1. Load this guide into its system prompt
2. Understand the complete supplier configuration structure
3. Follow the waterfall pattern and best practices
4. Generate accurate JSON configurations when given Excel file analysis
5. Validate configurations against the checklist automatically

## Prerequisites
- Access to the supplier's Excel file (`.xlsx` or `.xls`)
- Understanding of the supplier's data format
- Access to `supplier-formats.json` (contains shared lookup tables)

---

## Step 1: Initial File Analysis

### 1.1 Extract File Structure
Analyze the Excel file to determine:
- **Total rows**: Count of all rows in the file
- **Header row index**: Row number containing column headers (1-based)
- **Data start row index**: First row with actual data (1-based)
- **Column count**: Total number of columns with data
- **File name pattern**: Pattern for detection (e.g., `chk*.xls*`, `POL*.xls*`)

### 1.2 Identify Subtitle Rows (if any)
Check if the file contains subtitle/header rows that separate data sections:
- **Brand headers**: Single-column rows with brand names (e.g., "ACQUA DI PARMA", "ADIDAS")
- **Type headers**: Category separators
- **Detection method**: Usually identified by having only 1 non-empty cell
- **Behavior**: These values should be applied to subsequent data rows

### 1.3 Map Column Contents
For each column (A, B, C, ...), document:
- Column letter (Excel notation)
- Column name/header
- Data type (text, number, mixed)
- Sample values (first 10-15 rows)
- Purpose (EAN, Description, Price, Quantity, etc.)

**Example:**
```
Column A: EAN (numeric) - "8411061081778", "8411061081266"
Column B: Code (text) - "Q-08-404-02", "Q-08-404-01"
Column C: Description (text) - "A. Banderas Black Seduction Edt Spray"
Column F: Size (numeric) - "50", "100", "200"
Column G: Unit (text) - "ml", "oz"
```

---

## Step 2: Description Column Deep Analysis

The **Description column** is the most critical for extracting product properties. Perform a detailed analysis:

### 2.1 Extract Sample Descriptions
Get 20-30 sample descriptions that represent the variety in the file:
```
1. "A. Banderas Black Seduction Edt Spray"
2. "D&G Devotion Intense Wom EDP (100ml)"
3. "THE CROWN (W) 3.4oz / 100ML EDP - (LFP)"
4. "212 3.4 EDT L (100010) - Spain"
```

### 2.2 Identify Extractable Properties
For each description, identify what can be extracted:

#### Brand
- Location: Beginning, middle, end?
- Format: Full name, abbreviation, with/without separators?
- Examples: "A. Banderas", "D&G", "ACQUA DI PARMA"

#### Product Name
- What remains after extracting all other properties
- Should be the core product identifier

#### Size & Unit
- Format: "100ml", "3.4 oz", "100 ml", "3.4oz / 100ML"
- Multiple units present? (oz AND ml)
- Pattern: `\d+(?:\.\d+)?\s*(?:ml|oz|fl\s*oz)`

#### Gender
- Keywords: "M", "W", "L", "Men", "Women", "Wom", "Male", "Female", "Unisex"
- Format: Standalone, in parentheses, as suffix?
- Examples: "(W)", "For Men", "Wom"

#### Concentration
- Keywords: "EDP", "EDT", "EDC", "Parfum", "Cologne", "Eau de"
- Case sensitivity: Usually case-insensitive
- Examples: "EDP", "Edt", "EDT SPRAY"

#### Type/Category
- Keywords: "Spray", "Set", "Mini", "Gift Set", "Samples"
- Examples: "EDT SPRAY", "GIFTSET", "Mini Set"

#### Country of Origin (COO)
- Location: Usually at end with separator
- Format: "- Spain", "(USA)", "Made in France"
- Examples: "- Spain", "- USA"

#### Reference/Code
- Format: Parentheses, brackets, after separator
- Examples: "(100010)", "[CODE123]", "REF: ABC"

### 2.3 Determine Extraction Order (Waterfall Pattern)
Properties must be extracted in the correct sequence using the **waterfall/chain-of-responsibility** pattern:

**Standard Order:**
1. **Remove non-data elements** (parentheses with codes, special characters)
2. **Extract Brand** (from lookup or pattern)
3. **Extract Size & Unit** (regex pattern, remove from text)
4. **Extract Concentration** (from lookup, remove from text)
5. **Extract Gender** (from lookup, remove from text)
6. **Extract Type** (from lookup, remove from text)
7. **Extract COO** (if present, remove from text)
8. **Assign remaining text to Product Name**

**Key Principle:** Each step should:
- Read from the `.Clean` output of the previous step
- Extract the desired property
- Remove it from the text (using `remove` option)
- Store cleaned text in `{OutputName}.Clean` for next step

---

## Step 3: Build the Parsing Chain

### 3.1 Start with Basic Structure
```json
{
  "Name": "SupplierName",
  "Currency": "USD",
  "ParserConfig": {
    "Settings": {
      "StopOnFirstMatchPerColumn": false,
      "DefaultCulture": "en-US"
    },
    "ColumnRules": []
  }
}
```

### 3.2 Create Column Rules for Each Column

For each column, create an action chain. Example patterns:

#### Simple Assignment (EAN, Price, Quantity, etc.)
```json
{
  "Column": "A",
  "Actions": [
    {
      "Op": "Assign",
      "Input": "Text",
      "Output": "Product.EAN",
      "Assign": true,
      "Condition": null,
      "Parameters": null
    }
  ]
}
```

#### Complex Description Parsing (Waterfall Pattern)
```json
{
  "Column": "C",
  "Actions": [
    {
      "Op": "Assign",
      "Input": "Text",
      "Output": "Offer.Description",
      "Assign": true,
      "Condition": null,
      "Parameters": null
    },
    {
      "Op": "Find",
      "Input": "Text",
      "Output": "Brands",
      "Assign": false,
      "Condition": null,
      "Parameters": {
        "Pattern": "lookup:Brand",
        "Options": "first,ignorecase,remove"
      }
    },
    {
      "Op": "Map",
      "Input": "Brands",
      "Output": "Product.Brand",
      "Assign": true,
      "Condition": "Brands.Valid==true",
      "Parameters": {
        "Table": "Brand"
      }
    },
    {
      "Op": "Find",
      "Input": "Brands.Clean",
      "Output": "Sizes",
      "Assign": false,
      "Condition": null,
      "Parameters": {
        "Pattern": "(?i)(?<size>\\d+(?:\\.\\d+)?\\s*(?:ml|oz|fl\\s*oz))",
        "Options": "first,remove"
      }
    },
    {
      "Op": "Find",
      "Input": "Sizes",
      "Output": "Product.Size",
      "Assign": true,
      "Condition": "Sizes.Valid==true",
      "Parameters": {
        "Pattern": "(?<num>\\d+(?:\\.\\d+)?)",
        "Options": "first"
      }
    },
    {
      "Op": "Find",
      "Input": "Sizes.Clean",
      "Output": "Concentrations",
      "Assign": false,
      "Condition": null,
      "Parameters": {
        "Pattern": "lookup:Concentration",
        "Options": "first,ignorecase,remove"
      }
    },
    {
      "Op": "Map",
      "Input": "Concentrations",
      "Output": "Product.Concentration",
      "Assign": true,
      "Condition": "Concentrations.Valid==true",
      "Parameters": {
        "Table": "Concentration"
      }
    },
    {
      "Op": "Find",
      "Input": "Concentrations.Clean",
      "Output": "Genders",
      "Assign": false,
      "Condition": null,
      "Parameters": {
        "Pattern": "lookup:Gender",
        "Options": "first,ignorecase,remove"
      }
    },
    {
      "Op": "Map",
      "Input": "Genders",
      "Output": "Product.Gender",
      "Assign": true,
      "Condition": "Genders.Valid==true",
      "Parameters": {
        "Table": "Gender"
      }
    },
    {
      "Op": "Assign",
      "Input": "Genders.Clean",
      "Output": "Product.Name",
      "Assign": true,
      "Condition": null,
      "Parameters": null
    }
  ]
}
```

### 3.3 Handle Special Patterns

#### Split by Delimiter (Column with structured format)
When column contains structured data like `BRAND:GENDER:CODE`:
```json
{
  "Column": "A",
  "Actions": [
    {
      "Op": "Split",
      "Input": "Text",
      "Output": "SplitText",
      "Assign": false,
      "Condition": null,
      "Parameters": {
        "Delimiter": ":"
      }
    },
    {
      "Op": "Assign",
      "Input": "SplitText[0]",
      "Output": "Product.Brand",
      "Assign": true,
      "Condition": "SplitText.Length == 3",
      "Parameters": null
    },
    {
      "Op": "Map",
      "Input": "SplitText[1]",
      "Output": "Product.Gender",
      "Assign": true,
      "Condition": "SplitText.Length == 3",
      "Parameters": {
        "Table": "Gender"
      }
    },
    {
      "Op": "Assign",
      "Input": "SplitText[2]",
      "Output": "Offer.Ref",
      "Assign": true,
      "Condition": "SplitText.Length == 3",
      "Parameters": null
    }
  ]
}
```

#### Extract from Parentheses
When data is in parentheses like `(100ml)` or `(CODE123)`:
```json
{
  "Op": "Find",
  "Input": "Text",
  "Output": "Parenthesis",
  "Assign": false,
  "Condition": null,
  "Parameters": {
    "Pattern": "\\((?<content>[^)]+)\\)",
    "Options": "first,remove"
  }
}
```

#### Dynamic Pattern from Variable
When you need to remove a previously extracted value (like Brand):
```json
{
  "Op": "Find",
  "Input": "Text",
  "Output": "TextNoBrand",
  "Assign": false,
  "Condition": "Product.Brand != null",
  "Parameters": {
    "PatternKey": "Product.Brand",
    "Options": "first,ignorecase,remove"
  }
}
```

#### Conditional Extraction
Only extract if condition is met:
```json
{
  "Op": "Find",
  "Input": "Text",
  "Output": "Product.Size",
  "Assign": true,
  "Condition": "Product.Type 2 != 'Set' && Product.Type 2 != 'Mini Set'",
  "Parameters": {
    "Pattern": "(?<num>\\d+(?:\\.\\d+)?)",
    "Options": "first,remove"
  }
}
```

---

## Step 4: Configure File Structure

### 4.1 Basic File Structure
```json
"FileStructure": {
  "DataStartRowIndex": 2,
  "ExpectedColumnCount": 10,
  "HeaderRowIndex": 1,
  "Detection": {
    "FileNamePatterns": [
      "supplierName*.xls*"
    ]
  }
}
```

**Important:**
- `DataStartRowIndex`: First row with actual data (NOT header)
- `ExpectedColumnCount`: Count ALL columns with data (even if not all are parsed)
- `HeaderRowIndex`: Row with column names
- `FileNamePatterns`: Case-insensitive patterns for file detection

### 4.2 Subtitle Handling (if needed)
If the file has brand/type headers as single-column rows:
```json
"SubtitleHandling": {
  "Enabled": true,
  "Action": "parse",
  "DetectionRules": [
    {
      "Name": "Brand",
      "Description": "Rows with only a single non-empty cell denote a brand header",
      "DetectionMethod": "columnCount",
      "ExpectedColumnCount": 1,
      "ApplyToSubsequentRows": true,
      "ValidationPatterns": []
    }
  ],
  "FallbackAction": "skip",
  "Transforms": [
    {
      "SourceKey": "Brand",
      "Mode": "removePrefix",
      "Pattern": "COSMETICS",
      "IgnoreCase": true
    }
  ],
  "Assignments": [
    {
      "SourceKey": "Brand",
      "TargetProperty": "Product.Brand",
      "LookupTable": "Brand",
      "Overwrite": true
    }
  ]
}
```

---

## Step 5: Validation & Testing

### 5.1 Required Property Mappings
Ensure the following are extracted:
- ✅ `Product.EAN` (required, numeric, unique identifier)
- ✅ `Product.Brand` (from description, subtitle, or separate column)
- ✅ `Product.Name` (core product name after all removals)
- ✅ `Product.Size` (numeric value)
- ✅ `Product.Units` (ml, oz, etc.)
- ✅ `Product.Concentration` (EDP, EDT, etc.)
- ✅ `Product.Gender` (M, W, U)
- ✅ `Offer.Price` (numeric)
- ✅ `Offer.Quantity` or `Offer.Stock` (numeric)
- ⚠️ `Product.Type 1` or `Product.Type 2` (optional but recommended)
- ⚠️ `Product.COO` (Country of Origin, optional)
- ⚠️ `Offer.Ref` or `Offer.Description` (optional)

### 5.2 Validation Checklist

#### File Structure Validation
- [ ] `DataStartRowIndex` points to first data row (not header)
- [ ] `ExpectedColumnCount` matches actual column count
- [ ] `HeaderRowIndex` is correct (usually 1)
- [ ] `FileNamePatterns` will match actual files

#### Parsing Chain Validation
- [ ] Each step reads from previous step's `.Clean` output
- [ ] All regex patterns are properly escaped (use `\\` for backslash)
- [ ] Lookup table names exist in `supplier-formats.json`
- [ ] Conditions use correct syntax (`==`, `!=`, `Product.Field`)
- [ ] `Assign: true` for final output properties
- [ ] `Assign: false` for intermediate variables

#### Waterfall Chain Validation (Description Column)
- [ ] Properties extracted in correct order
- [ ] Each extraction uses `remove` option to clean text
- [ ] Final step assigns remaining text to `Product.Name`
- [ ] No step overwrites another unless intended

#### Special Cases
- [ ] Dual units (oz + ml) handled appropriately
- [ ] Parentheses content extracted before main parsing
- [ ] Brand removed before size extraction
- [ ] Subtitle values correctly applied to data rows

### 5.3 Test Against Sample Data
Run parser against 10-20 sample rows and verify:
1. All required fields populated
2. `Product.Name` is clean (no leftover tokens)
3. Numeric fields contain only numbers
4. Gender/Concentration mapped to canonical values
5. No parsing errors or null values for required fields

---

## Step 6: Common Patterns & Best Practices

### 6.1 Regex Patterns Library

#### Extract Numeric Size
```json
"Pattern": "(?<num>\\d+(?:\\.\\d+)?)"
```

#### Extract Size with Unit
```json
"Pattern": "(?i)(?<size>\\d+(?:\\.\\d+)?\\s*(?:ml|oz|fl\\s*oz))"
```

#### Extract from Parentheses
```json
"Pattern": "\\((?<content>[^)]+)\\)"
```

#### Extract Country Code
```json
"Pattern": "lookup:COO"
```

#### Extract Gender Keywords
```json
"Pattern": "lookup:Gender"
```

#### Extract First Word (Fallback Brand)
```json
"Pattern": "^\\s*(\\S+)(?=\\s|$)"
```

### 6.2 Action Types Reference

| Action | Purpose | Key Parameters |
|--------|---------|----------------|
| `Assign` | Copy value to output | `Input`, `Output`, `Assign` |
| `Find` | Extract with regex or lookup | `Pattern`, `Options` (all/first/last, remove, ignorecase) |
| `Map` | Lookup value in table | `Table` name |
| `Split` | Split by delimiter | `Delimiter` |
| `Switch` | Conditional value mapping | `When:X`, `Default` |
| `Convert` | Unit conversion | `FromUnit`, `ToUnit`, `Factor` |
| `Concat` | Join multiple values | `Keys`, `Separator` |
| `Clear` | Set to empty | None |

### 6.3 Common Mistakes to Avoid

❌ **Wrong:** Reading from wrong cleaned variable
```json
"Input": "Concentrations.Clean"  // Should be "Genders.Clean"
```

❌ **Wrong:** Using lookup for numeric extraction
```json
"Pattern": "lookup:Size"  // Should use regex for numbers
```

❌ **Wrong:** Not escaping backslashes in regex
```json
"Pattern": "\d+"  // Should be "\\d+"
```

❌ **Wrong:** Incorrect DataStartRowIndex
```json
"DataStartRowIndex": 8  // Actual data starts at row 2
```

❌ **Wrong:** Missing `.Clean` reference
```json
"Input": "Sizes"  // Should be "Sizes.Clean" for next step
```

❌ **Wrong:** Not using `remove` option in waterfall
```json
"Options": "first,ignorecase"  // Should include "remove"
```

✅ **Correct:** Proper waterfall chain
```json
// Step 1: Extract brand
"Input": "Text",
"Output": "Brands",
"Options": "first,ignorecase,remove"

// Step 2: Extract size from cleaned text
"Input": "Brands.Clean",  // ✅ Uses .Clean
"Output": "Sizes",
"Options": "first,remove"

// Step 3: Continue with cleaned text
"Input": "Sizes.Clean",  // ✅ Uses .Clean
```

---

## Step 7: Final Configuration Template

```json
{
  "Name": "SupplierName",
  "Currency": "USD",
  "ParserConfig": {
    "Settings": {
      "StopOnFirstMatchPerColumn": false,
      "DefaultCulture": "en-US"
    },
    "ColumnRules": [
      {
        "Column": "A",
        "Actions": [
          {
            "Op": "Assign",
            "Input": "Text",
            "Output": "Product.EAN",
            "Assign": true,
            "Condition": null,
            "Parameters": null
          }
        ]
      },
      {
        "Column": "C",
        "Actions": [
          {
            "Op": "Assign",
            "Input": "Text",
            "Output": "Offer.Description",
            "Assign": true,
            "Condition": null,
            "Parameters": null
          },
          // ... waterfall chain here
          {
            "Op": "Assign",
            "Input": "FinalCleaned.Clean",
            "Output": "Product.Name",
            "Assign": true,
            "Condition": null,
            "Parameters": null
          }
        ]
      }
      // ... more columns
    ]
  },
  "FileStructure": {
    "DataStartRowIndex": 2,
    "ExpectedColumnCount": 10,
    "HeaderRowIndex": 1,
    "Detection": {
      "FileNamePatterns": [
        "supplierName*.xls*"
      ]
    }
  },
  "SubtitleHandling": null
}
```

---

## Process Summary (Quick Reference)

1. **Analyze File**: Extract structure, identify columns, get sample data
2. **Analyze Description**: Identify all extractable properties and their patterns
3. **Design Waterfall**: Determine extraction order (Brand → Size → Concentration → Gender → Type → Name)
4. **Build Rules**: Create action chains for each column
5. **Configure Structure**: Set file detection and structure parameters
6. **Validate**: Test against sample data, verify all required fields
7. **Commit**: Save configuration file as `supplier-{Name}.json`

---

## Reference Files

- **Lookup Tables**: `SacksApp/Configuration/supplier-formats.json`
- **Example Configs**: 
  - Simple: `supplier-Chk.json`
  - Complex with subtitle: `supplier-MB.json`
  - Waterfall pattern: `supplier-PCA.json`
- **Parser Code**: `ParsingEngine/Engine.cs`, `ParsingEngine/Actions/`
- **Validation Code**: `Sacks.Core/Models/SupplierConfigurationModels.cs`

---

## Notes

- All dictionaries (Lookups, inner tables) MUST use `StringComparer.OrdinalIgnoreCase`
- Configuration updates preserve object identity (in-place updates)
- Always call `ParserConfig.DoMergeLoookUpTables` after updating lookups
- No backward compatibility needed - breaking changes are acceptable
- Use structured logging with `ILogger<T>` when debugging parsing issues
- File detection is case-insensitive substring match on supplier name

---

## AI LLM Integration Instructions

**For the AI Assistant reading this as DomainContext:**

When a user provides an Excel file for supplier configuration creation:

1. **File Analysis Phase**
   - Request PowerShell analysis: `Import-Excel <path> | Select-Object -First 20 | Format-Table`
   - Extract: row counts, column structure, sample data
   - Document FileStructure parameters (DataStartRowIndex, ExpectedColumnCount, HeaderRowIndex)

2. **Description Analysis Phase**
   - Extract 20-30 sample descriptions from the main description column
   - Identify extractable properties: Brand, Size, Unit, Gender, Concentration, Type, COO
   - Determine property patterns (regex or lookup)
   - Design waterfall extraction order

3. **Configuration Generation Phase**
   - Generate complete JSON following the template in Step 7
   - Build waterfall chain with correct `.Clean` references
   - Use proper regex escaping (`\\d+` not `\d+`)
   - Apply validation rules from Section 5.2

4. **Validation Phase**
   - Check all items in Section 5.2 checklist
   - Verify no common mistakes from Section 6.3
   - Confirm all required properties mapped

5. **Output**
   - Provide complete `supplier-{Name}.json` file
   - Include brief summary of parsing strategy
   - Highlight any edge cases or limitations

**Response Format:**
```json
{
  "Name": "SupplierName",
  "Currency": "USD",
  ...
}
```

Followed by:
- ✅ Parsing strategy summary
- ⚠️ Edge cases identified
- 📊 Expected extraction coverage (e.g., "95-100% for all properties")
