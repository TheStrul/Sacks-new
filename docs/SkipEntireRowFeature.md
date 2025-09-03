# SkipEntireRow Feature Documentation

## Overview

The `SkipEntireRow` property has been added to the `ColumnValidationConfiguration` class to provide fine-grained control over row processing behavior when validation fails.

## Configuration

Add the `skipEntireRow` property to any column's validation configuration:

```json
{
  "validation": {
    "isRequired": true,
    "allowedValues": ["Electronics", "Clothing", "Books", "Beauty"],
    "skipEntireRow": true
  }
}
```

## Behavior

- **`skipEntireRow: true`**: When validation fails for this column, the entire row is skipped and no product is created
- **`skipEntireRow: false`** (default): When validation fails for this column, only that column is skipped, but the row continues processing

## Use Cases

### Critical Fields
Set `skipEntireRow: true` for fields that are essential for product creation:
- Product Category (must be from approved list)
- Required Price information
- Mandatory product identifiers

### Optional Fields  
Set `skipEntireRow: false` for fields that can be missing without invalidating the product:
- Optional descriptions
- Non-critical metadata
- Supplementary information

## Examples

### Example 1: Category Validation
```json
"A": {
  "targetProperty": "ProductCategory",
  "validation": {
    "allowedValues": ["Electronics", "Clothing", "Books"],
    "skipEntireRow": true
  }
}
```
**Result**: Row with category "Toys" → Entire row skipped

### Example 2: Brand Validation
```json
"B": {
  "targetProperty": "Brand", 
  "validation": {
    "validationPatterns": ["^[A-Za-z ]+$"],
    "skipEntireRow": false
  }
}
```
**Result**: Row with brand "123Invalid!" → Only brand column skipped, product still created

### Example 3: Required Price
```json
"C": {
  "targetProperty": "Price",
  "validation": {
    "isRequired": true,
    "skipEntireRow": true
  }
}
```
**Result**: Row with missing price → Entire row skipped

## Technical Implementation

The feature works through:
1. `ValidationResult` class that captures both validation status and skip behavior
2. Enhanced `ValidateValueAsync` method that returns structured validation results
3. Updated row processing logic that respects the `SkipEntireRow` flag

## Validation Types Supported

The `skipEntireRow` property works with all validation types:
- `isRequired`: Skip row if required field is missing
- `allowedValues`: Skip row if value not in allowed list  
- `validationPatterns`: Skip row if value doesn't match regex patterns

## Default Behavior

- **Default value**: `false` (maintain backward compatibility)
- **Backward compatibility**: Existing configurations continue to work unchanged
- **Graceful degradation**: If property is missing, defaults to column-level skipping
