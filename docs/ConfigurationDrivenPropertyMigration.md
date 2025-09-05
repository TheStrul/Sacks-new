# Configuration-Driven Property Management Migration Guide

## Overview

This migration replaces hardcoded property mappings with a flexible JSON configuration system. The changes affect:

1. **PropertyNormalizer** → **ConfigurationBasedPropertyNormalizer**
2. **DescriptionPropertyExtractor** → **ConfigurationBasedDescriptionPropertyExtractor** 
3. **PerfumeFilterModel** → **ProductFilterModel** (dynamic)
4. **ConfigurationBasedNormalizer** (updated to use new configuration)

## Key Benefits

- **No more hardcoded property mappings** - all mappings are in JSON configuration files
- **Multi-language support** - easily add new language mappings without code changes
- **Dynamic filtering/sorting** - filter and sort options adapt to configuration
- **Easier maintenance** - property mappings can be updated without redeployment
- **Better testability** - different configurations for different test scenarios
- **Product type flexibility** - different configurations for different product types

## Migration Steps

### 1. Update Service Registration

**Before:**
```csharp
services.AddDynamicProductServices();
```

**After:**
```csharp
// Option A: Let services load configuration on demand
services.AddDynamicProductServices();

// Option B: Specify configuration files at startup
services.AddDynamicProductServices(
    propertyConfigPath: "Configuration/perfume-properties.json",
    normalizationConfigPath: "Configuration/perfume-property-normalization.json"
);
```

### 2. Update PropertyNormalizer Usage

**Before:**
```csharp
public class MyService
{
    private readonly PropertyNormalizer _normalizer;
    
    public MyService(PropertyNormalizer normalizer)
    {
        _normalizer = normalizer;
    }
}
```

**After:**
```csharp
public class MyService
{
    private readonly ConfigurationBasedPropertyNormalizer _normalizer;
    
    public MyService(ConfigurationBasedPropertyNormalizer normalizer)
    {
        _normalizer = normalizer;
    }
}
```

### 3. Update DescriptionPropertyExtractor Usage

**Before:**
```csharp
var extractor = new DescriptionPropertyExtractor(normalizer);
```

**After:**
```csharp
// Via DI
private readonly ConfigurationBasedDescriptionPropertyExtractor _extractor;

// Or create from configuration file
var extractor = await ConfigurationBasedDescriptionPropertyExtractor
    .CreateFromConfigurationAsync("Configuration/perfume-property-normalization.json");
```

### 4. Update Filter Models

**Before:**
```csharp
var filter = new PerfumeFilterModel
{
    Gender = "Men",
    Size = "100ml",
    Brand = "Chanel"
};
```

**After:**
```csharp
var filter = new ProductFilterModel();
filter.SetPropertyFilter("Gender", "Men");
filter.SetPropertyFilter("Size", "100ml");
filter.SetPropertyFilter("Brand", "Chanel");

// Or create from configuration
var filter = ProductFilterModel.CreateFromConfiguration(config, filterValues);
```

### 5. Update ConfigurationBasedNormalizer

The `ConfigurationBasedNormalizer` needs to be updated to use the new `ConfigurationBasedPropertyNormalizer` instead of the hardcoded `PropertyNormalizer`.

**In constructor:**
```csharp
public ConfigurationBasedNormalizer(
    SupplierConfiguration configuration, 
    ConfigurationBasedPropertyNormalizer? propertyNormalizer = null)
{
    _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    
    // Load normalization configuration if normalizer not provided
    if (propertyNormalizer == null)
    {
        var configManager = new PropertyNormalizationConfigurationManager();
        var normConfig = configManager.LoadConfigurationAsync("Configuration/perfume-property-normalization.json").Result;
        propertyNormalizer = new ConfigurationBasedPropertyNormalizer(normConfig);
    }
    
    _descriptionExtractor = new ConfigurationBasedDescriptionPropertyExtractor(propertyNormalizer._configuration);
}
```

## Configuration File Structure

### Property Normalization Configuration

**File:** `SacksDataLayer/Configuration/perfume-property-normalization.json`

```json
{
  "version": "1.0",
  "productType": "Perfume",
  "keyMappings": {
    "gender": "Gender",
    "size": "Size",
    "concentration": "Concentration"
  },
  "valueMappings": {
    "Gender": {
      "m": "Men",
      "w": "Women",
      "u": "Unisex"
    }
  },
  "extractionPatterns": {
    "Size": [
      {
        "pattern": "(\\d+)\\s*ml",
        "groupIndex": 1,
        "priority": 1
      }
    ]
  },
  "filterableProperties": [...],
  "sortableProperties": [...]
}
```

## Backward Compatibility

- Old classes are marked with `[Obsolete]` but still functional
- Migration can be done gradually - both systems can coexist
- Conversion methods provided: `ToProductFilterModel()`, `ToProductResult()`

## Testing

Create test-specific configuration files for different scenarios:

```csharp
[Test]
public async Task TestWithCustomConfiguration()
{
    var config = new ProductPropertyNormalizationConfiguration
    {
        ProductType = "TestProduct",
        KeyMappings = new() { ["testkey"] = "TestKey" }
    };
    
    var normalizer = new ConfigurationBasedPropertyNormalizer(config);
    var result = normalizer.NormalizeKey("testkey");
    
    Assert.AreEqual("TestKey", result);
}
```

## Next Steps

1. **Deploy configuration files** to appropriate locations
2. **Update service registrations** in Program.cs/Startup.cs
3. **Gradually migrate** from old to new classes
4. **Add product-specific configurations** as needed
5. **Remove obsolete classes** once migration is complete

## Configuration Management

- Configuration files should be deployed with the application
- Consider using environment-specific configurations (dev/staging/prod)
- Monitor for configuration loading errors at startup
- Implement configuration validation
- Consider configuration change notifications for runtime updates
