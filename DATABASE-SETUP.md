# Database Setup Information

## ⚠️ IMPORTANT: NO MIGRATIONS UNTIL PRODUCTION

**Database Auto-Creation Policy:**
- NO Entity Framework migrations are needed until production is announced
- Database schema will be automatically created on first application run
- All migration files have been removed from the project
- Infrastructure is aligned for automatic database creation using `EnsureCreatedAsync()`

## How It Works

The application automatically creates the database schema when:
1. **First application run** - When any database operation is performed
2. **Database clear operation** - When running `dotnet run --project SacksConsoleApp -- clear`
3. **File processing** - When processing Excel files for the first time

## Technical Implementation

- Uses Entity Framework's `Database.EnsureCreatedAsync()` method
- Database schema is generated from current entity models
- No migration history is tracked (suitable for development/testing)
- Schema changes are applied immediately when entity models change

## Current Entity Structure

All entities inherit from base `Entity` class with these audit fields:
- `Id` (int) - Primary key
- `CreatedAt` (DateTime) - Auto-set on creation
- `ModifiedAt` (DateTime?) - Auto-updated on changes

### Entity Classes:
1. **ProductEntity** - Core product data with dynamic properties (JSON)
2. **SupplierEntity** - Supplier information and contact details
3. **SupplierOfferEntity** - Supplier catalogs/price lists
4. **OfferProductEntity** - Junction table with product-specific pricing

## Development Notes

- Database will be recreated fresh on each schema change
- Existing data will be preserved only if schema changes are compatible
- For production, proper migrations will be implemented
- Current approach is optimal for rapid development and testing
