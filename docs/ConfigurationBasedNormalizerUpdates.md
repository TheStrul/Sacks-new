# Required Updates to ConfigurationBasedNormalizer

## Summary of Changes Needed

The `ConfigurationBasedNormalizer` class needs to be updated to use the new configuration-driven approach instead of hardcoded property mappings.

## Key Changes Required

### 1. Constructor Update

**Current Issue:** Uses hardcoded `PropertyNormalizer` and `DescriptionPropertyExtractor`

**Required Change:**
```csharp
public ConfigurationBasedNormalizer(
    SupplierConfiguration configuration, 
    ConfigurationBasedPropertyNormalizer? propertyNormalizer = null)
{
    _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    
    // Initialize data type converters
    _dataTypeConverters = InitializeDataTypeConverters();
    
    // Use provided normalizer or create from configuration
    if (propertyNormalizer != null)
    {
        _propertyNormalizer = propertyNormalizer;
        _descriptionExtractor = new ConfigurationBasedDescriptionPropertyExtractor(propertyNormalizer._configuration);
    }
    else
    {
        // Load default configuration
        var configManager = new PropertyNormalizationConfigurationManager();
        var normConfig = configManager.LoadConfigurationAsync("Configuration/perfume-property-normalization.json").Result;
        _propertyNormalizer = new ConfigurationBasedPropertyNormalizer(normConfig);
        _descriptionExtractor = new ConfigurationBasedDescriptionPropertyExtractor(normConfig);
    }
}
```

### 2. Field Declarations Update

**Add these fields:**
```csharp
private readonly ConfigurationBasedPropertyNormalizer _propertyNormalizer;
private readonly ConfigurationBasedDescriptionPropertyExtractor _descriptionExtractor;
```

**Remove:**
```csharp
private readonly DescriptionPropertyExtractor? _descriptionExtractor;
```

### 3. Usage Updates Throughout the Class

**Property Normalization:**
Replace any direct property normalization calls with the new normalizer:

```csharp
// Instead of hardcoded mappings, use:
var normalizedProperties = _propertyNormalizer.NormalizeProperties(properties);
```

**Description Property Extraction:**
The existing usage should work as the interface remains the same:

```csharp
var extractedProperties = _descriptionExtractor.ExtractPropertiesFromDescription(description);
```

## Benefits of These Changes

1. **Removes hardcoded property mappings** from the normalizer
2. **Enables runtime configuration changes** without code deployment
3. **Supports multiple product types** with different configurations
4. **Maintains backward compatibility** with existing supplier configurations
5. **Improves testability** with configurable property mappings

## Implementation Priority

1. **High Priority:** Update constructor to accept `ConfigurationBasedPropertyNormalizer`
2. **High Priority:** Replace hardcoded property normalization logic
3. **Medium Priority:** Add configuration file loading fallback
4. **Low Priority:** Add configuration validation and error handling

## Testing Considerations

- Ensure existing supplier configurations continue to work
- Test with different property normalization configurations
- Verify that extracted properties are correctly normalized
- Test fallback behavior when configuration files are missing
