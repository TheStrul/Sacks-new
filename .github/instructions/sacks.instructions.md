# Sacks Product Normalization System - Complete Instructions

## 🎯 Project Overview

**Purpose**: Inventory management and supplier integration system that normalizes Excel files from different suppliers into a unified relational structure for data managers and business analysts.

This is a **configuration-driven** Excel file normalization system that converts supplier data into a **relational database structure** with proper entity separation. The key architectural principle is **JSON configuration over code** - new suppliers are added via JSON configuration, not C# classes.

### Core Data Flow

```text
Excel Files → FileDataReader → ConfigurationBasedNormalizer → Relational Entities → Database
```


## 🏗️ Relational Database Architecture

### Entity Structure (4-Table Design)

#### 1. **ProductEntity** - Core Product Data
```csharp
- Id, Name, Description, SKU
- DynamicProperties (JSON) - core product attributes only
- Navigation: OfferProducts collection
```

#### 2. **SupplierEntity** - Supplier Information  
```csharp
- Id, Name, ContactInfo, Industry, Region
- Navigation: SupplierOffers collection
```

#### 3. **SupplierOfferEntity** - Offer Catalogs/Price Lists
```csharp
- Id, SupplierId, OfferName, Description
- Currency, ValidFrom, ValidTo, IsActive
- OfferType, Version
- Navigation: Supplier, OfferProducts collection
```

#### 4. **OfferProductEntity** - Product-Offer Junction Table
```csharp
- Id, OfferId, ProductId
- Price, Discount, ListPrice, Capacity
- MinimumOrderQuantity, IsAvailable
- ProductPropertiesJson - offer-specific product data
- Navigation: Offer, Product
```

## 📋 Project Goals & Phases

### Phase 1: Data Normalization Foundation ✅ *COMPLETED*

- ✅ **Database Architecture**: Implemented 4-table relational design with proper normalization
- ✅ **Property Separation**: Core vs offer properties correctly classified
- ✅ **Entity Framework Integration**: Full EF Core 9.0 implementation with migrations
- ✅ **Configuration System**: JSON-based supplier configuration framework

### Phase 2: Data Processing Enhancement ✅ *COMPLETED*

- ✅ **ConfigurationBasedNormalizer Updated**: Now creates relational entities instead of flat ProductEntity objects
- ✅ **SupplierOffer and OfferProduct Logic**: Proper relational entity creation implemented
- ✅ **Property Classification**: Core vs offer properties correctly separated during processing
- ✅ **ProcessingModes Configuration**: Added to supplier-formats.json for proper mode handling
- ✅ **NormalizationResult Enhanced**: Now contains ProductEntity, SupplierOfferEntity, and OfferProductEntity
- ✅ **Backward Compatibility**: Legacy code integration maintained
- ✅ **Comprehensive Testing**: Full validation of relational architecture

**Known Issue**: `SupplierConfigurationManager.GetSupplierConfigurationAsync("DIOR")` may return null in some cases. Production code should include robust workarounds that manually search the suppliers list as a fallback.
````````
### Phase 3: Customer BI Consultation
- Present well-defined relational data layer to customer
- Gather BI requirements based on normalized structure
- Plan reporting and analytics features

### Phase 4: Production Deployment
- Deploy to local PC environment with Entity Framework
- Support adding new suppliers via JSON configuration only

## 📁 Current Project Structure

```
Sacks-New/
├── SacksDataLayer/              # Core normalization library
│   ├── Entity.cs                # Base entity with audit fields
│   ├── ProductEntity.cs         # Core product data
│   ├── SupplierEntity.cs        # Supplier information
│   ├── SupplierOfferEntity.cs   # Offer catalogs/price lists
│   ├── OfferProductEntity.cs    # Product-offer junction table
│   ├── Data/
│   │   └── SacksDbContext.cs    # Entity Framework context
│   ├── Configuration/           # JSON-based supplier configs
│   │   ├── supplier-formats.json
│   │   ├── SupplierConfigurationModels.cs
│   │   └── Normalizers/
│   ├── FileProcessing/          # Excel file processing
│   ├── Repositories/            # Data access layer
│   └── Services/                # Business logic services
└── SacksConsoleApp/             # Testing and demonstration
    └── Program.cs
```

## Key Components & Patterns

### 1. Entity Design Pattern

- **Base Class**: `Entity.cs` provides audit fields (Id, CreatedAt, UpdatedAt, IsDeleted)
- **Product Entity**: Core product data with `DynamicProperties` for unlimited custom fields
- **Supplier Entity**: Basic supplier information and metadata
- **SupplierOffer Entity**: Catalog/price list metadata (eliminates duplication)
- **OfferProduct Entity**: Junction table with product-specific pricing and terms

### 2. Configuration-Based Architecture

- **Central Config**: `Configuration/supplier-formats.json` contains ALL supplier definitions
- **Property Classification**: Separates core product vs offer-specific properties
- **Detection Logic**: File pattern matching, header keywords, required columns
- **Column Mapping**: Excel columns → Entity properties (with proper classification)
- **Data Types**: Type conversion rules (string, decimal, int, bool, datetime)

### 3. Normalizer Pattern

- **Interface**: `ISupplierProductNormalizer` defines contract
- **Single Implementation**: `ConfigurationBasedNormalizer` handles ALL suppliers via JSON config
- **Factory**: `ConfigurationBasedNormalizerFactory` creates normalizers from configuration
- **Manager**: `SupplierConfigurationManager` loads/manages JSON configurations

### 4. Repository Pattern

- **SupplierOffersRepository**: Handles catalog and product-offer queries
- **Proper Entity Framework Queries**: Uses Include() for navigation properties
- **Junction Table Queries**: Correctly navigates through OfferProducts

## 🔧 Database Integration

### Entity Framework Core 9.0

```csharp
// DbContext with proper relationships
public class SacksDbContext : DbContext
{
    public DbSet<ProductEntity> Products { get; set; }
    public DbSet<SupplierEntity> Suppliers { get; set; }
    public DbSet<SupplierOfferEntity> SupplierOffers { get; set; }
    public DbSet<OfferProductEntity> OfferProducts { get; set; }
}
```

### Key Features

- **Development Database Management**: Schema recreation and flexible development workflow
- **Audit Fields**: Automatic CreatedAt/UpdatedAt tracking
- **Proper Indexes**: Performance optimization for lookups
- **Foreign Key Constraints**: Data integrity enforcement
- **JSON Serialization**: Dynamic properties stored as JSON

### Connection String

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=SacksProductsDb;Trusted_Connection=true;"
  }
}
```

## 📊 Property Classification System

### Supplier Configuration Example

```json
{
  "name": "ExampleSupplier",
  "propertyClassification": {
    "coreProductProperties": [
      "Category", "Size", "Unit", "EAN", "CommercialLine", "Family", "PricingItemName"
    ],
    "offerProperties": [
      "Price", "Capacity"
    ]
  }
}
```

### Processing Logic

1. **Core Properties** → `ProductEntity.DynamicProperties`
   - Stored once per product (deduplicated)
   - Shared across all supplier offers

2. **Offer Properties** → `OfferProductEntity` 
   - Stored per product-offer combination
   - Allows different pricing/terms per supplier

## 🤝 Project-Specific Collaboration Rules

### Special Approval Requirements for This Project

⚠️ **REQUIRES EXPLICIT APPROVAL** (beyond general rules):

- **Modifying core entities** (any Entity.cs files)
- **Altering existing supplier configurations** in `supplier-formats.json`
- **Changing database schema** or Entity Framework configurations
- **Modifying existing normalizer logic** in `ConfigurationBasedNormalizer`

### Project Leader Responsibilities

🎯 **For This Project** (avist):

1. **Test normalized data output** with actual supplier Excel files
2. **Validate relational structure** works correctly
3. **Approve changes** to core normalization logic
4. **Decide on property classifications** for new suppliers

## 💾 Data Storage Strategy

### Target Environment

- **Platform**: Local PC deployment
- **Database**: SQL Server LocalDB (free, integrated with Visual Studio)
- **ORM**: Entity Framework Core 9.0
- **Storage**: Local file system for Excel inputs and database files

### Data Relationships

```text
Suppliers (1) ←→ (Many) SupplierOffers (1) ←→ (Many) OfferProducts (Many) ←→ (1) Products

Example:
Supplier → "Supplier 2025 Catalog" → Multiple OfferProducts → Multiple Products
```

## 📊 JSON Configuration Structure

The `supplier-formats.json` file contains all supplier configurations with property classification:

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
        "requiredColumns": ["Item Name", "PRICE"],
        "priority": 10
      },
      "columnMappings": {
        "Item Name": "Name",
        "Item Code": "SKU", 
        "PRICE": "Price",
        "Category": "Category",
        "Size": "Size",
        "Capacity": "Capacity"
      },
      "propertyClassification": {
        "coreProductProperties": [
          "Category", "Size", "Unit", "EAN", "CommercialLine", "Family"
        ],
        "offerProperties": [
          "Price", "Capacity"
        ]
      },
      "dataTypes": {
        "PRICE": {
          "type": "decimal",
          "format": "currency",
          "transformations": ["removeSymbols", "parseDecimal"]
        }
      }
    }
  ]
}
```

## 🔄 Adding New Suppliers

### Method 1: Manual JSON Editing

Add new supplier with property classification:

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
    "Type": "Category",
    "Volume": "Capacity"
  },
  "propertyClassification": {
    "coreProductProperties": ["Category", "Brand", "Collection"],
    "offerProperties": ["Price", "Capacity", "Discount"]
  }
}
```

### Configuration Features

- **File Detection**: Filename patterns, header keywords, required columns
- **Data Types & Transformations**: Built-in type conversion and data cleaning
- **Property Classification**: Automatic separation of core vs offer properties
- **Validation Rules**: Field requirements, data quality checks

## 🎯 Current Processing Workflow

### 1. File Detection
```csharp
var normalizer = factory.GetNormalizer(fileData);
// Uses patterns, keywords, and required columns
```

### 2. Property Classification  
```csharp
// Core properties → ProductEntity.DynamicProperties
product.SetDynamicProperty("Category", "Fragrance");

// Offer properties → OfferProductEntity
offerProduct.Price = 125.50m;
offerProduct.SetProductProperty("Capacity", "100ml");
```

### 3. Entity Creation
```csharp
// Create relational structure
var supplier = new SupplierEntity { Name = "DIOR" };
var offer = new SupplierOfferEntity { 
    OfferName = "DIOR 2025", 
    Currency = "EUR" 
};
var product = new ProductEntity { Name = "J'adore" };
var offerProduct = new OfferProductEntity { 
    Price = 125.50m,
    Capacity = "100ml"
};
```

## 💡 Usage Examples

### Basic Processing with Relational Architecture

```csharp
var configManager = new SupplierConfigurationManager();
var factory = new ConfigurationBasedNormalizerFactory(configManager);
var service = new EnhancedProductNormalizationService(fileReader, factory);

var result = await service.NormalizeFileAsync("supplier_catalog.xlsx");

// Access relational data
foreach (var product in result.Products)
{
    // Core product properties
    var category = product.GetDynamicProperty<string>("Category");
    
    // Offer-specific data through navigation
    foreach (var offerProduct in product.OfferProducts)
    {
        var price = offerProduct.Price;
        var supplier = offerProduct.Offer.Supplier.Name;
        var capacity = offerProduct.GetProductProperty<string>("Capacity");
    }
}
```

### Repository Usage

```csharp
// Find active offers for a product from specific supplier
var offer = await offersRepo.GetActiveOfferAsync(productId, supplierId);

// Get all offers containing a specific product
var offers = await offersRepo.GetByProductIdAsync(productId);

// Get all offers from a supplier
var supplierOffers = await offersRepo.GetBySupplierIdAsync(supplierId);
```

## 🎯 Benefits of Relational Architecture

### Data Integrity
- ✅ **No Duplication**: Catalog metadata stored once per offer
- ✅ **Referential Integrity**: Foreign key constraints prevent orphaned data
- ✅ **Audit Trail**: Full tracking of when entities were created/modified

### Performance  
- ✅ **Proper Indexing**: Optimized queries with Entity Framework indexes
- ✅ **Efficient Joins**: Relational queries instead of JSON parsing
- ✅ **Selective Loading**: Include() only needed navigation properties

### Flexibility
- ✅ **Multiple Catalogs**: Suppliers can have multiple active offers
- ✅ **Version Control**: Track catalog versions and validity periods  
- ✅ **Currency Support**: Different currencies per catalog
- ✅ **Scalability**: Clean separation supports complex business scenarios

### Business Intelligence
- ✅ **Price Comparisons**: Easy supplier pricing analysis
- ✅ **Catalog Management**: Track offer lifecycle and validity
- ✅ **Product Analytics**: Core product data separated from pricing
- ✅ **Supplier Analytics**: Performance metrics per supplier/catalog

## ⚠️ Critical Developer Guidelines

### Code Modification Rules

**REQUIRES APPROVAL** before changing:
- Core entities (ProductEntity, SupplierEntity, SupplierOfferEntity, OfferProductEntity)
- Existing supplier configurations in `supplier-formats.json`
- Database schema or Entity Framework configurations
- Existing normalizer logic or property classification

**FREE TO CREATE**:
- New services and utilities
- New repository methods
- Additional validation logic
- New configuration models
- Database query optimizations

### Property Classification Guidelines

When adding new suppliers:

1. **Analyze column meanings** - Is this core product data or supplier-specific?
2. **Core Properties** - Data that describes the product itself (Category, Size, EAN)
3. **Offer Properties** - Data specific to this supplier's offering (Price, Capacity, Terms)
4. **Test classification** - Verify data goes to correct entity in processing

### Database Management Strategy

#### Development Environment (Current Phase) 🔧
- **Database Recreation**: Database can be deleted and recreated as needed during development
- **Schema Management**: Use `EnsureCreated()` or `EnsureDeleted()` for rapid development iterations
- **No Migration Tracking**: Focus on entity design over formal migration management
- **LocalDB Flexibility**: Take advantage of LocalDB's easy reset capabilities
- **Rapid Testing**: Delete database when schema changes are needed for testing

#### Production Environment (Future Phase) 🚀
- **Formal Migrations**: Will implement proper EF Core migration tracking before production
- **Schema Versioning**: Use `Add-Migration` and `Update-Database` commands
- **Data Preservation**: Protect production data with careful migration strategies
- **Rollback Support**: Maintain migration history for rollback capabilities

#### Current Implementation Note
The existing code uses `context.Database.MigrateAsync()` but this should be considered **development scaffolding**. 
For rapid development, you can safely:
- Delete the LocalDB database files when schema changes are needed
- Use `EnsureCreated()` for simpler development database initialization
- Ignore formal migration files until production deployment phase
