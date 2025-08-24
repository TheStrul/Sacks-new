# Sacks Product Normalization System - Complete Instructions

## 🎯 Project Overview

**Purpose**: Inventory management and supplier integration system that normalizes Excel files from different suppliers into a unified structure for data managers and business analysts.

**Team Structure**: 
- **Project Leader**: You (avist)
- **Developer**: GitHub Copilot (AI Assistant)

This is a **configuration-driven** Excel file normalization system that converts supplier data into unified `ProductEntity` objects. The key architectural principle is **JSON configuration over code** - new suppliers are added via JSON configuration, not C# classes.

### Core Data Flow

```text
Excel Files → FileDataReader → ConfigurationBasedNormalizer → ProductEntity → (Future: Local Database)
```

**📊 Key Insight from DIOR Analysis**: The system successfully processed 4,074 products from the DIOR 2025.xlsx file, revealing the need for a **two-stage processing approach** to handle both product catalog creation and supplier pricing analysis effectively.

## 📋 Project Goals & Phases

### Phase 1: Data Normalization Foundation ⚠️ *CURRENT PHASE*

- **COMPLETED**: Initial DIOR file analysis (4,074 products processed successfully)
- **IN PROGRESS**: Configure two-stage processing workflow
  - Stage 1: Unified product catalog creation (ignore pricing/stock)
  - Stage 2: Supplier pricing/stock analysis (link to catalog)
- Normalize ALL existing supplier files to unified ProductEntity structure
- Ensure configuration-based approach works for all current suppliers
- Validate data integrity and completeness

### Phase 2: Customer BI Consultation
- Present well-defined data layer to customer
- Gather BI requirements based on normalized data structure
- Plan reporting and analytics features

### Phase 3: Production Deployment
- Deploy to local PC environment with free database (MySQL/SQLite)
- Support adding new suppliers via JSON configuration only

## 🎯 Two-Stage Data Processing Workflow

The system operates with **two distinct analysis modes** depending on the business purpose:

### 🗃️ Stage 1: Unified Product Catalog Creation

**Purpose**: Build a master product database with all products from all suppliers

- **Primary Goal**: Product identification and core product attributes
- **Focus Areas**:
  - Product Name (standardized)
  - Category/Family classification
  - Product specifications (Size, Unit, Capacity, etc.)
  - Product identification codes (EAN, UPC, etc.)
- **Explicitly Ignore**:
  - Pricing information
  - Stock levels
  - Supplier-specific reference codes
  - Commercial/availability data
- **Output**: Clean, deduplicated unified product catalog
- **Business Value**: Master data foundation for all downstream processes

### 💰 Stage 2: Supplier Stock & Pricing Analysis

**Purpose**: Track supplier-specific commercial data and link to unified products

- **Primary Goal**: Commercial intelligence and supplier comparison
- **Focus Areas**:
  - Current pricing from each supplier
  - Stock availability levels
  - Supplier-specific SKUs/item codes
  - Commercial terms and conditions
- **Links To**: Stage 1 unified product catalog (via product matching)
- **Output**: Supplier pricing/stock data with references to master catalog
- **Business Value**: Price comparison, supplier selection, inventory planning

### 🔄 Processing Mode Selection

When analyzing a file, the system must determine:

- **Mode 1**: "Is this for updating our unified product catalog?"
- **Mode 2**: "Is this for analyzing supplier stock/pricing offers?"

This distinction affects:

- Which columns are prioritized during processing
- How data validation is performed
- What gets stored vs. ignored
- How duplicate detection works

## 🏗️ Project-Specific Architecture

### Configuration-Based Design

- **JSON Configuration over Code**: New suppliers added via JSON, not C# classes
- **Single Normalizer Pattern**: One `ConfigurationBasedNormalizer` handles all suppliers
- **Dynamic Properties**: Unlimited custom fields via `Dictionary<string, object?>`
- **File Detection**: Pattern matching, header keywords, required columns

## 📁 Current Project Structure

```
Sacks-New/
├── SacksDataLayer/              # Core normalization library
│   ├── Entity.cs                # Base entity with audit fields
│   ├── ProductEntity.cs         # Product with dynamic properties
│   ├── Configuration/           # JSON-based supplier configs
│   │   ├── supplier-formats.json
│   │   ├── SupplierConfigurationModels.cs
│   │   └── Normalizers/
│   ├── FileProcessing/          # Excel file processing
│   └── Services/                # Business logic services
└── SacksConsoleApp/             # Testing and demonstration
```

## Key Components & Patterns

### 1. Entity Design Pattern

- **Base Class**: `Entity.cs` provides audit fields (CreatedAt, ModifiedAt, IsDeleted, etc.)
- **Product Entity**: `ProductEntity.cs` extends Entity with:
  - Static properties: Name, Description, SKU
  - **Dynamic Properties**: `Dictionary<string, object?>` for unlimited custom fields
  - JSON serialization for database storage via `DynamicPropertiesJson`

### 2. Configuration-Based Architecture

- **Central Config**: `Configuration/supplier-formats.json` contains ALL supplier definitions
- **Detection Logic**: File pattern matching, header keywords, required columns
- **Column Mapping**: Excel columns → ProductEntity properties (including dynamic ones)
- **Data Types**: Type conversion rules (string, decimal, int, bool, datetime)

### 3. Normalizer Pattern

- **Interface**: `ISupplierProductNormalizer` defines contract
- **Single Implementation**: `ConfigurationBasedNormalizer` handles ALL suppliers via JSON config
- **Factory**: `ConfigurationBasedNormalizerFactory` creates normalizers from configuration
- **Manager**: `SupplierConfigurationManager` loads/manages JSON configurations

## 🤝 Project-Specific Collaboration Rules

### Special Approval Requirements for This Project

⚠️ **REQUIRES EXPLICIT APPROVAL** (beyond general rules):

- **Modifying core entities** (`Entity.cs`, `ProductEntity.cs`)
- **Altering existing supplier configurations** in `supplier-formats.json`
- **Changing file processing interfaces/contracts** (`ISupplierProductNormalizer`, `IFileDataReader`)
- **Modifying existing normalizer logic** in `ConfigurationBasedNormalizer`

### Project Leader Responsibilities

🎯 **For This Project** (avist):

1. **Test normalized data output** with actual supplier Excel files
2. **Validate supplier configurations** work correctly
3. **Approve changes** to core normalization logic
4. **Decide on new supplier formats** and their business requirements

## 📊 Supplier Management Strategy

### Current Approach

- **Configuration-Driven**: Add suppliers via `supplier-formats.json`
- **No Code Changes**: New suppliers should only require JSON updates
- **Flexible Mapping**: Support for column mapping and data transformations

### Adding New Suppliers (Post-Production Goal)

```json
{
  "name": "NewSupplier",
  "detection": { /* file detection rules */ },
  "columnMappings": { /* Excel column to ProductEntity mapping */ },
  "dataTypes": { /* data transformation rules */ }
}
```

## 💾 Data Storage Strategy

### Target Environment

- **Platform**: Local PC deployment
- **Database**: Free solution (MySQL, SQLite, or PostgreSQL)
- **Storage**: Local file system for Excel inputs and database

### Data Flow

```text
## 📊 JSON Configuration Structure

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

## 🔧 Development Workflow

### 1. Analyzing New Requirements

- Review existing code structure
- Identify impact areas
- Propose solution approach
- Get approval for significant changes

### 2. Implementation Process

- Create/modify code following quality standards
- Ensure backward compatibility
- Test with existing supplier files
- Document changes in code comments

### 3. Testing Strategy

- Test with all current supplier Excel files
- Validate ProductEntity output
- Ensure JSON configuration works correctly
- Verify no data loss during normalization

## 📋 Current Tasks & Priorities

### Immediate Focus (Phase 1)

1. **Validate Current Normalization**: Ensure all existing suppliers work correctly
2. **Data Quality**: Check for data loss or transformation issues
3. **Configuration Completeness**: Verify all supplier formats are properly configured
4. **Error Handling**: Robust handling of malformed Excel files

### Next Steps

1. **Database Integration**: Connect ProductEntity to local database
2. **Batch Processing**: Handle multiple files efficiently
3. **Validation Reports**: Generate data quality reports
4. **Configuration Tools**: Possibly create tools to help configure new suppliers

## 🔄 Adding New Suppliers

### Method 1: Manual JSON Editing

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

### Method 2: Interactive Console Application

```bash
dotnet run
# Select option 4: "Add new supplier configuration"
# Follow the guided prompts
```

### Method 3: File Analysis and Auto-Generation

```bash
dotnet run
# Select option 2: "Analyze file and suggest configuration"
# Provide file path and supplier name
# Review and save the auto-generated configuration
```

## ⚙️ Configuration Features

### File Detection

- **Filename patterns**: `*DIOR*`, `*Chanel*`, etc.
- **Header keywords**: Look for specific terms in column headers
- **Required columns**: Must have certain columns to match
- **Priority system**: Higher priority normalizers are tried first

### Data Types & Transformations

- **Built-in types**: `string`, `decimal`, `int`, `bool`, `datetime`
- **Transformations**: `removeSymbols`, `trim`, `lowercase`, `parseDecimal`
- **Default values**: Fallback values for missing data
- **Format specifications**: Currency, date formats, etc.

### Validation Rules

- **Required fields**: Ensure critical data is present
- **Field validations**: Length limits, regex patterns, numeric ranges
- **Error handling**: Configure max errors per file

### Smart Column Mapping

The system automatically recognizes common column patterns:

| Pattern Keywords | Maps To | Type |
|-----------------|---------|------|
| "name", "product name", "title" | Name | Core Property |
| "description", "desc", "details" | Description | Core Property |
| "sku", "code", "product code" | SKU | Core Property |
| "price", "cost", "unit price" | Price | Dynamic Property |
| "category", "type", "group" | Category | Dynamic Property |
| "stock", "quantity", "inventory" | StockQuantity | Dynamic Property |

## 🖥️ Console Application Features

### Interactive Menu System

1. **Process files from folder** - Batch process Excel files
2. **Analyze file and suggest configuration** - Auto-generate configs
3. **View current configurations** - See all loaded suppliers
4. **Add new supplier configuration** - Guided config creation
5. **Configuration management** - Validate, reload, export

### File Analysis

- Automatically detects columns and data types
- Suggests appropriate mappings and transformations
- Provides sample data preview
- Generates complete JSON configuration

### Real-time Validation

- Validates configurations on startup
- Shows errors, warnings, and statistics
## 💡 Usage Examples

### Basic Processing

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

### Configuration Management

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

## 🎯 Benefits of Configuration-Based Approach

### For Users

- 🚫 **No Programming Required** - Add suppliers by editing JSON
- 🔄 **Hot Reload** - Changes take effect immediately
- 📋 **Template-Based** - Copy and modify existing configurations
- 🤖 **Auto-Generation** - Analyze files to create configurations
- ✅ **Validation** - Built-in configuration validation

### For Developers

- 🛠️ **Maintainable** - No more normalizer classes to maintain
- 🎛️ **Flexible** - Rich configuration options for any file format
- 📈 **Scalable** - Add unlimited suppliers without code changes
- ⚡ **Fast** - Single normalizer handles all formats efficiently
- 🐛 **Debuggable** - Easy to troubleshoot with JSON configs

### For Operations

- 🎯 **Centralized** - All supplier formats in one file
- 📊 **Versioned** - Track changes with version control
- ✅ **Validated** - Automatic configuration validation
- 📈 **Monitored** - Built-in statistics and health checks

## ⚠️ Critical Developer Guidelines

### Code Modification Rules

**REQUIRES APPROVAL** before changing:

- Core entities (`Entity.cs`, `ProductEntity.cs`)
- Existing supplier configurations in `supplier-formats.json`
- File processing interfaces/contracts
- Existing normalizer logic

**FREE TO CREATE**:

- New services and utilities
- New configuration models
- Additional validation logic
- Database integration components

### Configuration-First Approach

When adding supplier support:

1. **Never** create new normalizer classes
2. **Always** extend `supplier-formats.json`
3. Use `ConfigurationBasedNormalizer` which adapts to JSON config
4. Test with actual supplier Excel files in `SacksDataLayer/Inputs/`

### Dynamic Properties Usage

```csharp
// Setting dynamic properties
product.SetDynamicProperty("CommercialLine", "Luxury");
product.SetDynamicProperty("Family", "Fragrance");

// Column mapping in JSON maps to both static and dynamic properties
"Commercial Line": "CommercialLine" // → DynamicProperties["CommercialLine"]
"Product Name": "Name"              // → Static Name property
```

## 🚀 Project-Specific Communication

### When Working on This Project

- **Be specific** about which supplier files or configurations are involved
- **Mention data validation** results when testing changes
- **Reference Excel file examples** from `SacksDataLayer/Inputs/`
- **Consider impact** on existing supplier configurations

### Testing Requirements for This Project

- **Test with actual supplier Excel files** in `SacksDataLayer/Inputs/`
- **Validate ProductEntity output** has correct dynamic properties
- **Ensure JSON configuration** loads without errors
- **Verify no data loss** during normalization process

## 🛠️ Development Workflows

### Testing Configuration Changes

```bash
# Build and test console app
dotnet build SacksConsoleApp
dotnet run --project SacksConsoleApp

# Test with sample files in SacksDataLayer/Inputs/
```

### File Processing Pipeline

- `FileDataReader`: Handles .xlsx/.csv files using ExcelDataReader
- Returns `FileData` with `RowData` collections
- `ConfigurationBasedNormalizer.CanHandle()`: Determines supplier match
- `NormalizeAsync()`: Applies column mappings and data type conversions

## 🎛️ Project Context

### Current Phase

**Phase 1**: Data normalization foundation - ensure ALL existing suppliers work correctly via configuration

### Dependencies

- `ExcelDataReader 3.7.0`: Excel file processing
- System.Text.Json: Configuration serialization
- No database dependencies yet (Phase 2)

### Namespace Conventions

- Core entities: `SacksDataLayer`
- File processing: `SacksAIPlatform.InfrastructuresLayer.FileProcessing`
- Configuration: `SacksDataLayer.FileProcessing.Configuration`
- Services: `SacksDataLayer.FileProcessing.Services`

## 🐛 Debugging Tips

- Check `supplier-formats.json` for syntax errors with JSON validation
- Use `SupplierConfigurationManager.GetConfigurationAsync()` to verify config loading
- Test file detection with `ConfigurationBasedNormalizer.CanHandle()`
- Validate dynamic properties are set correctly via `ProductEntity.DynamicProperties`
- Sample Excel files are in `SacksDataLayer/Inputs/` for testing

## 🚀 Advanced Features

- **Priority-based detection** for handling overlapping patterns
- **Custom transformations** for complex data processing
- **Metadata tracking** for supplier information and contact details
- **Batch validation** for quality assurance
- **Configuration statistics** for monitoring and optimization
- **Sample data export** for testing and validation

---

## 🤖 Quick Reference for Copilot

**Current Priority**: Phase 1 - Normalize all existing supplier files  
**Approval Needed**: Changes to existing Entity classes, existing JSON configs, existing file processing logic  
**Free to Create**: New services, new models, new configurations, new utility classes  
**Code Style**: Clean, simple, modular, object-oriented C#  
**Database**: Plan for local free database (MySQL/SQLite)  

This configuration-driven approach makes the system much more flexible and user-friendly, allowing business users to add new supplier formats without requiring developer intervention!

---

*This instruction file consolidates all project documentation. Last updated: August 25, 2025*
