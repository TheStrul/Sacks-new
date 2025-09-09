# Subtitle Handling Feature Documentation

## Overview

The SubtitleHandling feature enables the system to detect and process special "subtitle" rows in Excel files that contain metadata or grouping information that should be applied to subsequent data rows. This is particularly useful for files where brand information, categories, or other grouping data appears in separate rows above the actual product data.

## Configuration

Subtitle handling is configured per supplier in the `supplier-formats.json` file under the `subtitleHandling` section.

### Configuration Structure

```json
{
  "subtitleHandling": {
    "enabled": true,
    "action": "parse",
    "detectionRules": [
      {
        "name": "BrandSubtitle",
        "description": "Detects brand information from subtitle rows in column A",
        "detectionMethod": "columnCount",
        "expectedColumnCount": 1,
        "applyToSubsequentRows": true,
        "validationPatterns": []
      }
    ],
    "fallbackAction": "skip"
  }
}
```

### Configuration Properties

#### SubtitleRowHandlingConfiguration

| Property | Type | Description | Default |
|----------|------|-------------|---------|
| `enabled` | boolean | Whether subtitle handling is enabled for this supplier | `true` |
| `action` | string | Action to take when subtitle rows are detected: "parse" or "skip" | `"parse"` |
| `detectionRules` | SubtitleDetectionRule[] | Array of rules for detecting subtitle rows | `[]` |
| `fallbackAction` | string | Action to take when no rules match: "skip" or "ignore" | `"skip"` |

#### SubtitleDetectionRule

| Property | Type | Description | Default |
|----------|------|-------------|---------|
| `name` | string | Name identifier for the rule | `""` |
| `description` | string | Human-readable description of the rule | `null` |
| `detectionMethod` | string | Method for detection: "columnCount", "pattern", or "hybrid" | `"columnCount"` |
| `expectedColumnCount` | int | Expected number of non-empty columns for columnCount method | `1` |
| `applyToSubsequentRows` | boolean | Whether extracted data should be applied to following rows | `true` |
| `validationPatterns` | string[] | Regex patterns for pattern-based detection | `[]` |

## Detection Methods

### 1. Column Count Detection (`"columnCount"`)

Detects subtitle rows based on the number of non-empty columns. This is useful when subtitle rows have fewer columns than data rows.

**Example**: A row with only one non-empty column in a file that typically has 4+ columns per data row.

### 2. Pattern Detection (`"pattern"`)

Detects subtitle rows using regex patterns that match the content of the row.

**Example**: Rows that start with specific text patterns or contain only uppercase text.

### 3. Hybrid Detection (`"hybrid"`)

Combines both column count and pattern detection. A row must match both criteria to be considered a subtitle row.

## Processing Actions

### Parse Action (`"parse"`)

When a subtitle row is detected and action is set to "parse":

1. The row is marked as `IsSubtitleRow = true`
2. Data is extracted based on the rule name
3. Extracted data is stored in the row's `SubtitleData` dictionary
4. If `applyToSubsequentRows = true`, the data is applied to following non-subtitle rows

### Skip Action (`"skip"`)

When action is set to "skip", subtitle rows are identified but their content is ignored, and they are filtered out during processing.

## Data Extraction

The system includes built-in extraction logic for common subtitle types:

### Brand Subtitle (`"BrandSubtitle"`)
Extracts brand information from the first non-empty cell and stores it as `Brand`.

### Category Subtitle (`"CategorySubtitle"`)
Extracts category information from the first non-empty cell and stores it as `Category`.

### Generic Extraction
For other rule names, uses the rule name as the key and the first non-empty cell as the value.

## Usage Examples

### Example 1: Brand Grouping

For an Excel file structure like:
```
| CHANEL        |           |     |               |
| Product Name  | Price     | Qty | EAN           |
|               | 120.00    | 5   | 1234567890123 |
|               | 85.50     | 3   | 1234567890124 |
| DIOR          |           |     |               |
|               | 95.00     | 7   | 1234567890125 |
```

Configuration:
```json
{
  "subtitleHandling": {
    "enabled": true,
    "action": "parse",
    "detectionRules": [
      {
        "name": "BrandSubtitle",
        "detectionMethod": "columnCount",
        "expectedColumnCount": 1,
        "applyToSubsequentRows": true
      }
    ]
  }
}
```

Result: Products will automatically have the correct brand applied based on their position in the file.

### Example 2: Pattern-Based Detection

For subtitle rows that follow a specific pattern:

```json
{
  "detectionRules": [
    {
      "name": "CategorySubtitle",
      "detectionMethod": "pattern",
      "validationPatterns": ["^[A-Z ]+:$"],
      "applyToSubsequentRows": true
    }
  ]
}
```

This would detect rows like "PERFUMES:" or "COSMETICS:" as category subtitles.

## Integration Points

### File Processing Pipeline

1. **FileDataReader**: Reads Excel file and applies subtitle processing if configured
2. **SubtitleRowProcessor**: Detects and processes subtitle rows according to configuration
3. **ConfigurationNormalizer**: Applies subtitle data to product entities during normalization
4. **FileProcessingService**: Orchestrates the entire process

### Data Flow

```
Excel File ? FileDataReader ? SubtitleRowProcessor ? ConfigurationNormalizer ? Database
```

### Key Classes

- `SubtitleRowProcessor`: Main processing logic
- `RowData`: Enhanced with subtitle properties (`IsSubtitleRow`, `SubtitleRuleName`, `SubtitleData`)
- `SubtitleRowHandlingConfiguration`: Configuration model
- `SubtitleDetectionRule`: Rule definition model

## Error Handling

- Invalid regex patterns are logged as warnings and ignored
- Unrecognized detection methods default to false (no match)
- Missing or invalid configuration disables subtitle processing
- Processing continues even if subtitle extraction fails

## Performance Considerations

- Subtitle processing adds minimal overhead to file reading
- Detection rules are evaluated in order - place most specific rules first
- Pattern matching uses compiled regex for better performance
- Memory usage is optimized by processing rows sequentially

## Logging

The system provides comprehensive logging for subtitle processing:

- Debug: Detection attempts and results
- Info: Summary of subtitle rows processed
- Warning: Invalid patterns or configuration issues
- Trace: Detailed data extraction and application

## Testing

Unit tests are provided in `SubtitleRowProcessorTests.cs` covering:

- Disabled configuration handling
- Brand subtitle detection and extraction
- Data row filtering
- Various detection method scenarios

## Future Enhancements

Potential future improvements:
- Custom extraction logic through configuration
- Support for multi-column subtitle data
- Conditional application based on data row content
- Integration with external mapping services
- Performance optimizations for large files