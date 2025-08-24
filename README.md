# Product Normalization System - Configuration-Based Approach

This system allows you to normalize Excel files from different suppliers into a unified ProductEntity format using **JSON configuration files** instead of hardcoded normalizer classes.

## ?? **New Configuration-Based Architecture**

Instead of creating separate C# classes for each supplier, you now simply update a JSON configuration file. This makes adding new suppliers much easier and more maintainable.

## Projects Structure

- **SacksDataLayer**: Core library containing the normalization infrastructure
- **SacksConsoleApp**: Console application for testing and demonstrating the system

## ?? **Key Components**

### **JSON Configuration System**

1. **`supplier-formats.json`**: Central configuration file containing all supplier formats
2. **`SupplierConfigurationManager`**: Manages loading and updating configurations
3. **`ConfigurationBasedNormalizer`**: Single normalizer that adapts based on JSON config
4. **`EnhancedProductNormalizationService`**: Enhanced service with configuration support

### **Core Classes (Unchanged)**

1. **ProductEntity**: Base product class with dynamic properties support
2. **Configuration Models**: Strongly-typed classes for JSON configuration

## ?? **JSON Configuration Structure**

The `supplier-formats.json` file contains all supplier configurations:

```json
{
  "version": "1.0",
  "lastUpdated": "2024-12-19T10:00:00Z",
  "description": "Configuration file for all supplier file formats",
  "suppliers": [
    {
      "name": "DIOR",
      "description": "DIOR beauty and fragrance products supplier",
      "detection": {
        "fileNamePatterns": ["*DIOR*", "*dior*", "*Dior*"],
        "headerKeywords": ["DIOR", "Dior Product", "Beauty Product"],
        "requiredColumns": ["Product Name", "Price"],
        "priority": 10
      },
      "columnMappings": {
        "Product Name": "Name",
        "Description": "Description",
        "SKU": "SKU",
        "Price": "Price",
        "Category": "Category",
        "Commercial Line": "CommercialLine"
      },
      "dataTypes": {
        "Price": {
          "type": "decimal",
          "format": "currency",
          "defaultValue": 0,
          "transformations": ["removeSymbols", "parseDecimal"]
        },
        "StockQuantity": {
          "type": "int",
          "defaultValue": 0,
          "transformations": ["parseInt"]
        }
      },
      "validation": {
        "requiredFields": ["Name"],
        "skipRowsWithoutName": true,
        "maxErrorsPerFile": 100
      },
      "transformation": {
        "headerRowIndex": 0,
        "dataStartRowIndex": 1,
        "skipEmptyRows": true,
        "trimWhitespace": true
      },
      "metadata": {
        "industry": "Beauty & Cosmetics",
        "fileFrequency": "monthly",
        "notes": ["Files typically contain 4000+ products"]
      }
    }
  ]
}
```

## ?? **Adding New Suppliers**

### **Method 1: Manual JSON Editing**

Simply add a new supplier object to the `suppliers` array in `supplier-formats.json`:

```json
{
  "name": "Chanel",
  "description": "Chanel luxury fashion and beauty products",
  "detection": {
    "fileNamePatterns": ["*Chanel*", "*CHANEL*"],
    "headerKeywords": ["Chanel", "CHANEL"],
    "priority": 10
  },
  "columnMappings": {
    "Product": "Name",
    "Product Code": "SKU",
    "Cost": "Price",
    "Type": "Category"
  },
  "dataTypes": {
    "Cost": {
      "type": "decimal",
      "transformations": ["removeSymbols"]
    }
  }
}
```

### **Method 2: Interactive Console Application**

```bash
dotnet run
# Select option 4: "Add new supplier configuration"
# Follow the guided prompts
```

### **Method 3: File Analysis and Auto-Generation**

```bash
dotnet run
# Select option 2: "Analyze file and suggest configuration"
# Provide file path and supplier name
# Review and save the auto-generated configuration
```

## ?? **Configuration Features**

### **File Detection**
- **Filename patterns**: `*DIOR*`, `*Chanel*`, etc.
- **Header keywords**: Look for specific terms in column headers
- **Required columns**: Must have certain columns to match
- **Priority system**: Higher priority normalizers are tried first

### **Data Types & Transformations**
- **Built-in types**: `string`, `decimal`, `int`, `bool`, `datetime`
- **Transformations**: `removeSymbols`, `trim`, `lowercase`, `parseDecimal`
- **Default values**: Fallback values for missing data
- **Format specifications**: Currency, date formats, etc.

### **Validation Rules**
- **Required fields**: Ensure critical data is present
- **Field validations**: Length limits, regex patterns, numeric ranges
- **Error handling**: Configure max errors per file

### **Smart Column Mapping**
The system automatically recognizes common column patterns:

| Pattern Keywords | Maps To | Type |
|-----------------|---------|------|
| "name", "product name", "title" | Name | Core Property |
| "description", "desc", "details" | Description | Core Property |
| "sku", "code", "product code" | SKU | Core Property |
| "price", "cost", "unit price" | Price | Dynamic Property |
| "category", "type", "group" | Category | Dynamic Property |
| "stock", "quantity", "inventory" | StockQuantity | Dynamic Property |

## ??? **Console Application Features**

### **Interactive Menu System**
1. **Process files from folder** - Batch process Excel files
2. **Analyze file and suggest configuration** - Auto-generate configs
3. **View current configurations** - See all loaded suppliers
4. **Add new supplier configuration** - Guided config creation
5. **Configuration management** - Validate, reload, export

### **File Analysis**
- Automatically detects columns and data types
- Suggests appropriate mappings and transformations
- Provides sample data preview
- Generates complete JSON configuration

### **Real-time Validation**
- Validates configurations on startup
- Shows errors, warnings, and statistics
- Helps troubleshoot configuration issues

## ?? **Usage Examples**

### **Basic Processing**
```csharp
var configManager = new SupplierConfigurationManager();
var factory = new ConfigurationBasedNormalizerFactory(configManager);
var service = new EnhancedProductNormalizationService(fileReader, factory);

var result = await service.NormalizeFileAsync("DIOR 2025.xlsx");
foreach (var product in result.Products)
{
    var price = product.GetDynamicProperty<decimal?>("Price");
    var category = product.GetDynamicProperty<string>("Category");
}
```

### **Configuration Management**
```csharp
// Add new supplier
var newConfig = new SupplierConfiguration { /* ... */ };
await service.AddSupplierConfigurationAsync(newConfig);

// Analyze unknown file
var suggestion = await service.AnalyzeFileAndSuggestConfigurationAsync("unknown.xlsx");
if (suggestion.IsValid)
{
    await service.AddSupplierConfigurationAsync(suggestion.SuggestedConfiguration);
}
```

## ? **Benefits of Configuration-Based Approach**

### **For Users:**
- ?? **No Programming Required** - Add suppliers by editing JSON
- ?? **Hot Reload** - Changes take effect immediately
- ?? **Template-Based** - Copy and modify existing configurations
- ?? **Auto-Generation** - Analyze files to create configurations
- ? **Validation** - Built-in configuration validation

### **For Developers:**
- ?? **Maintainable** - No more normalizer classes to maintain
- ?? **Flexible** - Rich configuration options for any file format
- ?? **Scalable** - Add unlimited suppliers without code changes
- ?? **Fast** - Single normalizer handles all formats efficiently
- ?? **Debuggable** - Easy to troubleshoot with JSON configs

### **For Operations:**
- ?? **Centralized** - All supplier formats in one file
- ?? **Versioned** - Track changes with version control
- ??? **Validated** - Automatic configuration validation
- ?? **Monitored** - Built-in statistics and health checks

## ?? **Advanced Features**

- **Priority-based detection** for handling overlapping patterns
- **Custom transformations** for complex data processing
- **Metadata tracking** for supplier information and contact details
- **Batch validation** for quality assurance
- **Configuration statistics** for monitoring and optimization
- **Sample data export** for testing and validation

This configuration-driven approach makes the system much more flexible and user-friendly, allowing business users to add new supplier formats without requiring developer intervention!