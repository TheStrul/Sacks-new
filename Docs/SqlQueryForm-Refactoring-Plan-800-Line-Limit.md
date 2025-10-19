# SqlQueryForm.cs Refactoring Plan - 800-Line Limit Enforcement

**Date:** 2024  
**Target File:** `SacksApp\SqlQueryForm.cs`  
**Current Size:** 1,591 lines  
**Target Size:** <800 lines  
**Reduction Needed:** 791 lines (50% reduction)

---

## ?? Current State Analysis

### File Metrics
- **Total Lines:** 1,591
- **Over Limit By:** +791 lines (99% over limit)
- **Code Distribution:**
  - SQL Query Building: ~400 lines (25%)
  - Filter Management: ~250 lines (16%)
  - Excel Export: ~150 lines (9%)
  - Grid State Persistence: ~100 lines (6%)
  - Inline Editing/Saving: ~200 lines (13%)
  - UI Event Handlers: ~300 lines (19%)
  - Infrastructure: ~191 lines (12%)

---

## ?? Refactoring Strategy

### Phase 1: Service Interfaces (? ALREADY EXISTS!)

The following interfaces are **already defined** in `SacksLogicLayer\Services\Interfaces`:

1. ? **`IQueryBuilderService`** - SQL query building with filters
2. ? **`IDataExportService`** - Excel export functionality  
3. ? **`IFileDiscoveryService`** - File discovery (not used in SqlQueryForm)

### Phase 2: Create Missing Service Implementations (?? TO DO)

Need to create implementations for:

1. **`QueryBuilderService`** (~400 lines extracted)
   - Location: `SacksLogicLayer\Services\Implementations\QueryBuilderService.cs`
   - Responsibilities:
     - `BuildQuery()` - Main query building logic
     - `BuildBaseWhere()` - Simple WHERE clause construction
     - `BuildCollapsedViewWhere()` - Grouped query WHERE clause
     - `BuildDynamicInnerWhere()` - Dynamic collapse WHERE
     - `BuildPredicate()` - Individual filter predicate building
     - `BuildSimpleQuery()` - SELECT from base view
     - `BuildCollapsedUsingView()` - SELECT from collapsed view
     - `BuildDynamicCollapsed()` - Dynamic ROW_NUMBER() query
     - `GetAvailableColumnsAsync()` - Schema inspection
     - `IsValidColumn()` - Column validation

2. **`DataExportService`** (~150 lines extracted)
   - Location: `SacksLogicLayer\Services\Implementations\DataExportService.cs`
   - Responsibilities:
     - `ExportToExcelAsync()` - Export DataTable to Excel using ClosedXML
     - `PrepareExportData()` - Extract visible columns and rows
     - `FormatWorksheet()` - Apply Excel formatting

3. **`GridStateService`** (~100 lines extracted - NEW INTERFACE NEEDED)
   - Location: `SacksLogicLayer\Services\Implementations\GridStateService.cs`
   - Responsibilities:
     - `SaveGridState()` - Persist column visibility, width, order, sort
     - `LoadGridState()` - Restore grid state
     - `ApplyGridState()` - Apply loaded state to DataGridView

4. **`FilterStateService`** (~100 lines extracted - NEW INTERFACE NEEDED)
   - Location: `SacksLogicLayer\Services\Implementations\FilterStateService.cs`
   - Responsibilities:
     - `SaveFiltersState()` - Persist filter conditions
     - `LoadFiltersState()` - Restore filter conditions

5. **`ProductOfferUpdateService`** (~200 lines extracted - NEW INTERFACE NEEDED)
   - Location: `SacksLogicLayer\Services\Implementations\ProductOfferUpdateService.cs`
   - Responsibilities:
     - `TrySaveCellAsync()` - Save individual cell changes
     - `SaveChangesAsync()` - Batch save modified cells
     - `GetProductOfferAsync()` - Lookup by EAN/Supplier/Offer

---

## ?? Implementation Steps

### Step 1: Create `QueryBuilderService` Implementation

**Lines Saved:** ~400

Extract methods:
- `BuildQuery()` - Entry point
- `BuildBaseWhere()` - Lines 549-568
- `BuildCollapsedViewWhere()` - Lines 570-594
- `BuildDynamicInnerWhere()` - Lines 596-609
- `BuildPredicate()` - Lines 614-663
- `BuildSimpleQuery()` - Lines 669-681
- `BuildCollapsedUsingView()` - Lines 684-691
- `BuildDynamicCollapsed()` - Lines 694-713
- `GetAvailableColumnsAsync()` - Lines 198-232
- Helper methods

### Step 2: Create `DataExportService` Implementation

**Lines Saved:** ~150

Extract methods:
- `ExportToExcelAsync()` - Lines 1050-1124
- Helper for visible column extraction
- Helper for data preparation
- ClosedXML workbook creation and formatting

### Step 3: Create `GridStateService` Implementation

**Lines Saved:** ~100

Extract methods:
- `SaveGridState()` - Lines 1324-1344
- `LoadGridState()` - Lines 1346-1358
- `ApplyLoadedGridState()` - Lines 1240-1286
- JSON serialization/deserialization
- State models

### Step 4: Create `FilterStateService` Implementation

**Lines Saved:** ~100

Extract methods:
- `SaveFiltersState()` - Lines 179-195
- `LoadFiltersState()` - Lines 147-177
- Filter condition persistence models
- JSON serialization/deserialization

### Step 5: Create `ProductOfferUpdateService` Implementation

**Lines Saved:** ~200

Extract methods:
- `TrySaveCellAsync()` - Lines 1416-1499
- `SaveChangesAsync()` - Lines 1538-1586
- Entity lookup logic
- Value type conversion and validation

### Step 6: Update `SqlQueryForm.cs` to Use Services

**Remaining Lines:** ~541 (under 800!)

Keep only:
- Constructor and initialization
- UI event handlers (delegates to services)
- DataGridView event handlers
- Form lifecycle methods
- Simple helper methods

---

## ?? Service Registration (Program.cs)

Add to DI container:

```csharp
// Query and data services
services.AddScoped<IQueryBuilderService, QueryBuilderService>();
services.AddScoped<IDataExportService, DataExportService>();
services.AddScoped<IGridStateService, GridStateService>();
services.AddScoped<IFilterStateService, FilterStateService>();
services.AddScoped<IProductOfferUpdateService, ProductOfferUpdateService>();
```

---

## ?? Expected Results

### Before Refactoring
| Metric | Value |
|--------|-------|
| **Total Lines** | 1,591 |
| **SQL Building** | In UI (400 lines) |
| **Excel Export** | In UI (150 lines) |
| **Grid State** | In UI (100 lines) |
| **Filter State** | In UI (100 lines) |
| **Cell Editing** | In UI (200 lines) |
| **Testability** | ?? Low (UI-coupled) |
| **Reusability** | ?? None |

### After Refactoring
| Metric | Value |
|--------|-------|
| **Total Lines** | ~541 (-66%) |
| **SQL Building** | QueryBuilderService (testable) |
| **Excel Export** | DataExportService (testable) |
| **Grid State** | GridStateService (testable) |
| **Filter State** | FilterStateService (testable) |
| **Cell Editing** | ProductOfferUpdateService (testable) |
| **Testability** | ? High (service-based) |
| **Reusability** | ? High (can use in APIs, batch jobs) |

---

## ? Quality Checklist

### Service Implementation Requirements
- [ ] Implement `IQueryBuilderService` with full SQL building logic
- [ ] Implement `IDataExportService` with Excel export
- [ ] Create and implement `IGridStateService`
- [ ] Create and implement `IFilterStateService`
- [ ] Create and implement `IProductOfferUpdateService`
- [ ] Register all services in DI container
- [ ] Update SqlQueryForm to use services
- [ ] Build succeeds with no warnings
- [ ] SqlQueryForm.cs is under 800 lines
- [ ] All existing functionality preserved

### Code Quality
- [ ] Async methods use CancellationToken
- [ ] Proper null handling (nullable reference types)
- [ ] Structured logging throughout
- [ ] Parameterized SQL only (no string concatenation)
- [ ] Resource disposal with `using` or `await using`
- [ ] Exception handling with specific catch blocks

---

## ?? Success Criteria

1. ? SqlQueryForm.cs is **under 800 lines** (target: ~541 lines)
2. ? All business logic moved to testable services
3. ? Build successful with no errors/warnings
4. ? All existing features work identically
5. ? Services follow .NET 9 / C# 13 best practices
6. ? Comprehensive null-safety
7. ? Async/await patterns throughout

---

## ?? Implementation Order

1. **QueryBuilderService** (Highest impact - 400 lines saved)
2. **ProductOfferUpdateService** (200 lines saved)
3. **DataExportService** (150 lines saved)
4. **GridStateService** (100 lines saved)
5. **FilterStateService** (100 lines saved)
6. **Update SqlQueryForm** (Final integration)
7. **Testing** (Verify all functionality)

---

*Refactoring plan for Struly-Dear by GitHub Copilot*  
*Following .github/copilot-instructions.md rules*

