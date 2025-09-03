# Sacks Product Normalization System - Complete Instructions

## ⚠️ IMPORTANT DATABASE NOTICE

**NO MIGRATIONS NEEDED UNTIL PRODUCTION IS ANNOUNCED!**

- Database will be auto-created on first run using `EnsureCreatedAsync()`
- All migration files have been removed
- Infrastructure is aligned for automatic database creation
- Current database provider: **SQL Server** (Microsoft.EntityFrameworkCore.SqlServer)

## 🎯 Project Overview

**Purpose**: Inventory management and supplier integration system that normalizes Excel files from different suppliers into a unified relational structure for data managers and business analysts.

This is a **configuration-driven** Excel file normalization system that converts supplier data into a **relational database structure** with proper entity separation. The key architectural principle is **JSON configuration over code** - new suppliers are added via JSON configuration, not C# classes.

### Core Data Flow

```text
Excel Files → FileDataReader → ConfigurationBasedNormalizer → NormalizationResult → Relational Entities → MariaDB
```

## 🏗️ Relational Database Architecture

### Entity Structure (4-Table Design)

#### 1. **ProductEntity** - Core Product Data
```csharp
- Id, Name, Description, EAN
- DynamicPropertiesJson (JSON column) - core product attributes only
- CreatedAt, ModifiedAt (audit fields)
- Navigation: OfferProducts collection
```

#### 2. **SupplierEntity** - Supplier Information  
```csharp
- Id, Name, Description, Industry, Region
- ContactName, ContactEmail, Company, FileFrequency
- Notes
- Navigation: Offers collection
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
- Price, Capacity, Discount, ListPrice, UnitOfMeasure
- MinimumOrderQuantity, MaximumOrderQuantity, IsAvailable
- Notes, ProductPropertiesJson (JSON column) - offer-specific product data
- Navigation: Offer, Product
```

## 📋 Project Goals & Phases

### Phase 1: Data Normalization Foundation ✅ *COMPLETED*

- ✅ **Database Architecture**: Implemented 4-table relational design with proper normalization
- ✅ **Property Separation**: Core vs offer properties correctly classified
- ✅ **Entity Framework Integration**: Full EF Core 9.0 implementation with MariaDB
- ✅ **Configuration System**: JSON-based supplier configuration framework

### Phase 2: Data Processing Enhancement ✅ *COMPLETED*

- ✅ **ConfigurationBasedNormalizer**: Creates relational entities with proper property classification
- ✅ **NormalizationResult**: Enhanced to contain ProductEntity, SupplierOfferEntity, and OfferProductEntity
- ✅ **ProcessingResult**: Contains collection of NormalizationResults plus statistics
- ✅ **Property Classification**: Core vs offer properties correctly separated during processing
- ✅ **Excel Column Mapping**: Column index mappings (A, B, C, etc.) for user-friendly configuration
- ✅ **Relational Entity Creation**: Proper SupplierOffer and OfferProduct creation
- ✅ **JSON Property Storage**: Dynamic properties stored in JSON columns with serialization
- ✅ **Backward Compatibility**: Legacy interfaces maintained
### Phase 3: Production Readiness 🚧 *IN PROGRESS*
- **Service Layer**: Complete service implementations for all repositories
- **File Processing Service**: Unified file processing with automatic supplier detection
- **Database Auto-Creation**: Production-ready database initialization
- **Configuration Management**: Dynamic supplier configuration loading
- **Error Handling**: Comprehensive error handling and logging

### Phase 4: Customer BI Consultation
- Present well-defined relational data layer to customer
- Gather BI requirements based on normalized structure
- Plan reporting and analytics features

### Phase 5: Production Deployment
- Deploy to local PC environment with Entity Framework
- Support adding new suppliers via JSON configuration only

## 📁 Current Project Structure

```
Sacks-New/
├── SacksDataLayer/              # Core normalization library
│   ├── Entities/                # Entity classes with proper namespace organization
│   │   ├── Entity.cs            # Base entity with audit fields (Id, CreatedAt, ModifiedAt)
│   │   ├── ProductEntity.cs     # Core product data with JSON dynamic properties
│   │   ├── SupplierEntity.cs    # Supplier information and metadata
│   │   ├── SupplierOfferEntity.cs # Offer catalogs/price lists
│   │   ├── OfferProductEntity.cs # Product-offer junction table
│   │   └── ApplicationDeploymentEntity.cs # Deployment tracking
│   ├── Data/
│   │   └── SacksDbContext.cs    # Entity Framework context for MariaDB
│   ├── Configuration/           # JSON-based supplier configs
│   │   ├── supplier-formats.json
│   │   ├── SupplierConfigurationModels.cs
│   │   ├── ISupplierProductNormalizer.cs
│   │   └── Normalizers/
│   │       ├── ConfigurationBasedNormalizer.cs
│   │       └── NormalizationResult.cs
│   ├── FileProcessing/          # Excel file processing
│   │   ├── Models/              # FileData, RowData, CellData, ProcessingModels
│   │   ├── Interfaces/          # IFileDataReader, IUnifiedFileProcessor
│   │   └── Implementations/     # FileDataReader
│   ├── Repositories/            # Data access layer
│   │   ├── Interfaces/          # Repository contracts
│   │   └── Implementations/     # EF Core repository implementations
│   └── Services/                # Business logic services
│       ├── ConfigurationBasedNormalizerFactory.cs
│       ├── SupplierConfigurationManager.cs
│       ├── Interfaces/          # Service contracts
│       └── Implementations/     # Service implementations
└── SacksConsoleApp/             # Testing and demonstration
    ├── Program.cs               # Main entry point with DI setup
    └── appsettings.json         # MariaDB connection configuration
```

## Key Components & Patterns

### 1. Entity Design Pattern

- **Base Class**: `Entity.cs` provides audit fields (Id, CreatedAt, ModifiedAt)
- **Product Entity**: Core product data with `DynamicPropertiesJson` for unlimited custom fields
- **Supplier Entity**: Basic supplier information and metadata
- **SupplierOffer Entity**: Catalog/price list metadata (eliminates duplication)
- **OfferProduct Entity**: Junction table with product-specific pricing and terms

### 2. Configuration-Based Architecture

- **Central Config**: `Configuration/supplier-formats.json` contains ALL supplier definitions
- **Property Classification**: Separates core product vs offer-specific properties
- **Detection Logic**: File pattern matching and Excel column index mappings
- **Column Mapping**: Excel columns (A, B, C, etc.) → Entity properties (with proper classification)
- **Data Types**: Type conversion rules (string, decimal, int, bool, datetime)

### 3. Normalizer Pattern

- **Interface**: `ISupplierProductNormalizer` defines contract
- **Single Implementation**: `ConfigurationBasedNormalizer` handles ALL suppliers via JSON config
- **Factory**: `ConfigurationBasedNormalizerFactory` creates normalizers from configuration
- **Manager**: `SupplierConfigurationManager` loads/manages JSON configurations
- **Result**: `NormalizationResult` contains ProductEntity, SupplierOfferEntity, and OfferProductEntity

### 4. Repository Pattern

- **Complete Repository Layer**: All entities have dedicated repositories
- **SupplierOffersRepository**: Handles catalog and product-offer queries
- **Proper Entity Framework Queries**: Uses Include() for navigation properties
- **Junction Table Queries**: Correctly navigates through OfferProducts

## 🔧 Database Integration

### Entity Framework Core 9.0 with MariaDB

```csharp
// DbContext with proper relationships and JSON column support
public class SacksDbContext : DbContext
{
    public DbSet<ProductEntity> Products { get; set; }
    public DbSet<SupplierEntity> Suppliers { get; set; }
    public DbSet<SupplierOfferEntity> SupplierOffers { get; set; }
    public DbSet<OfferProductEntity> OfferProducts { get; set; }
}
```

### Key Features

- **MariaDB Integration**: Using Pomelo.EntityFrameworkCore.MySql provider
- **JSON Column Support**: Dynamic properties stored as JSON in database
- **Development Database Management**: Schema auto-creation using `EnsureCreatedAsync()`
- **Audit Fields**: Automatic CreatedAt/ModifiedAt tracking with UTC timestamps
- **Proper Indexes**: Performance optimization for lookups and relationships
- **Foreign Key Constraints**: Data integrity enforcement with cascade delete
- **Unique Constraints**: Supplier names are unique

### Connection String (MariaDB)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Port=3306;Database=SacksProductsDb;Uid=root;Pwd=;"
  },
  "DatabaseSettings": {
    "Provider": "MariaDB",
    "CommandTimeout": 30,
    "RetryOnFailure": true,
    "MaxRetryCount": 3
  }
}
```

## 📊 Property Classification System

### Supplier Configuration Example (DIOR)

```json
{
  "name": "DIOR",
  "description": "DIOR beauty and fragrance products supplier",
  "detection": {
    "fileNamePatterns": ["DIOR*.xlsx"]
  },
  "columnIndexMappings": {
    "A": "Category",
    "C": "Family", 
    "F": "PricingItemName",
    "H": "Name",
    "M": "EAN",
    "N": "Capacity",
    "O": "Price"
  },
  "propertyClassification": {
    "coreProductProperties": [
      "EAN", "Category", "Family", "CommercialLine", "PricingItemName", "Size", "ItemCode"
    ],
    "offerProperties": [
      "Price", "Capacity"
    ]
  }
}
```

### Processing Logic

1. **Core Properties** → `ProductEntity.DynamicPropertiesJson`
   - Stored once per product (deduplicated)
   - Shared across all supplier offers
   - Serialized to JSON column in database

2. **Offer Properties** → `OfferProductEntity` 
   - Stored per product-offer combination
   - Allows different pricing/terms per supplier
   - Standard properties mapped to dedicated columns
   - Additional properties stored in `ProductPropertiesJson`

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
- **Database**: MariaDB (MySQL-compatible, free, high-performance)
- **ORM**: Entity Framework Core 9.0 with Pomelo provider
- **Storage**: Local database server for Excel inputs and relational data

### Data Relationships

```text
Suppliers (1) ←→ (Many) SupplierOffers (1) ←→ (Many) OfferProducts (Many) ←→ (1) Products

Example:
Supplier → "DIOR 2025 Catalog" → Multiple OfferProducts → Multiple Products
```

## 📊 JSON Configuration Structure

The `supplier-formats.json` file contains all supplier configurations with Excel column index mappings:

```json
{
  "version": "1.0",
  "suppliers": [
    {
      "name": "DIOR",
      "description": "DIOR beauty and fragrance products supplier",
      "detection": {
        "fileNamePatterns": ["DIOR*.xlsx"]
      },
      "columnIndexMappings": {
        "A": "Category",
        "H": "Name",
        "M": "EAN",
        "O": "Price"
      },
      "propertyClassification": {
        "coreProductProperties": ["Category", "Size", "EAN", "Family"],
        "offerProperties": ["Price", "Capacity"]
      },
      "validation": {
        "dataStartRowIndex": 5,
        "expectedColumnCount": 11
      },
      "transformation": {
        "skipEmptyRows": true,
        "trimWhitespace": true
      }
    }
  ]
}
```

## 🔄 Adding New Suppliers

### Method: JSON Configuration

Add new supplier with Excel column index mappings:

```json
{
  "name": "Chanel",
  "description": "Chanel luxury fashion and beauty products",
  "detection": {
    "fileNamePatterns": ["*Chanel*", "*CHANEL*"]
  },
  "columnIndexMappings": {
    "A": "Name",
    "B": "SKU", 
    "C": "Price",
    "D": "Category"
  },
  "propertyClassification": {
    "coreProductProperties": ["Category", "Brand", "Collection"],
    "offerProperties": ["Price", "Capacity", "Discount"]
  }
}
```

### Configuration Features

- **File Detection**: Filename patterns for automatic supplier recognition
- **Excel Column Mapping**: User-friendly A, B, C column references
- **Property Classification**: Automatic separation of core vs offer properties
- **Data Transformation**: Row skipping, whitespace trimming, type conversion
- **Validation Rules**: Data start row, expected column count

## 🎯 Current Processing Workflow

### 1. File Detection & Processing
```csharp
var configManager = new SupplierConfigurationManager();
var factory = new ConfigurationBasedNormalizerFactory(configManager);
var normalizer = await factory.GetNormalizerForFileAsync(fileName);
```

### 2. Relational Entity Creation  
```csharp
var result = await normalizer.NormalizeAsync(fileData, context);

foreach (var normalizationResult in result.NormalizationResults)
{
    var product = normalizationResult.Product;           // Core product data
    var offer = normalizationResult.SupplierOffer;      // Catalog metadata
    var offerProduct = normalizationResult.OfferProduct; // Junction with pricing
}
```

### 3. Property Classification in Action
```csharp
// Core properties → ProductEntity.DynamicPropertiesJson
product.SetDynamicProperty("Category", "Fragrance");

// Offer properties → OfferProductEntity
offerProduct.Price = 125.50m;
offerProduct.SetProductProperty("Capacity", "100ml");
```

## 💡 Usage Examples

### Basic Processing with Relational Architecture

```csharp
var fileProcessingService = serviceProvider.GetRequiredService<IFileProcessingService>();
await fileProcessingService.ProcessFileAsync("DIOR_catalog.xlsx");

// Access via repositories
var offersRepo = serviceProvider.GetRequiredService<ISupplierOffersRepository>();
var offers = await offersRepo.GetBySupplierIdAsync(supplierId);

foreach (var offer in offers)
{
    foreach (var offerProduct in offer.OfferProducts)
    {
        var productName = offerProduct.Product.Name;
        var price = offerProduct.Price;
        var category = offerProduct.Product.GetDynamicProperty<string>("Category");
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

## 🎯 Benefits of Current Architecture

### Data Integrity
- ✅ **No Duplication**: Catalog metadata stored once per offer
- ✅ **Referential Integrity**: Foreign key constraints prevent orphaned data
- ✅ **Audit Trail**: Full tracking of when entities were created/modified
- ✅ **JSON Validation**: Proper serialization/deserialization of dynamic properties

### Performance  
- ✅ **Proper Indexing**: Optimized queries with Entity Framework indexes
- ✅ **Efficient Joins**: Relational queries instead of JSON parsing
- ✅ **Selective Loading**: Include() only needed navigation properties
- ✅ **MariaDB Performance**: High-performance database engine

### Flexibility
- ✅ **Multiple Catalogs**: Suppliers can have multiple active offers
- ✅ **Version Control**: Track catalog versions and validity periods  
- ✅ **Currency Support**: Different currencies per catalog
- ✅ **Excel Column Mapping**: User-friendly A, B, C column references
- ✅ **Dynamic Properties**: Unlimited custom fields via JSON storage

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
- **Database Auto-Creation**: Database automatically created using `EnsureCreatedAsync()`
- **Schema Management**: No formal migrations - schema derived from entities
- **MariaDB Flexibility**: Easy database recreation for schema changes
- **Rapid Testing**: Delete database when schema changes are needed for testing
- **JSON Column Support**: Full utilization of MariaDB JSON features

#### Production Environment (Future Phase) 🚀
- **Formal Migrations**: Will implement proper EF Core migration tracking before production
- **Schema Versioning**: Use `Add-Migration` and `Update-Database` commands
- **Data Preservation**: Protect production data with careful migration strategies
- **Rollback Support**: Maintain migration history for rollback capabilities

#### Current Implementation Note
The existing code uses `context.Database.EnsureCreatedAsync()` which is perfect for development.
For rapid development, you can safely:
- Delete the MariaDB database when schema changes are needed
- Let the application recreate the schema automatically
- Focus on entity design over formal migration management
