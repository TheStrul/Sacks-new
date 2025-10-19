# SacksApp Logic Inventory - Detailed Analysis

## Summary for Struly-Dear

This document provides a comprehensive inventory of all business logic found in the SacksApp project, 
categorized by file and complexity level.

---

## 1. DashBoard.cs - Dashboard Main Form

### ?? Location: `SacksApp\DashBoard.cs`
### ?? Complexity: **MEDIUM**
### ?? Lines of Logic: **~150 lines**

#### Business Logic Found:

1. **File Discovery Logic** (Lines ~55-75)
   - Pattern: `Directory.GetFiles(inputsPath, "*.xls*")`
   - Filtering temporary files: `!Path.GetFileName(f).StartsWith("~")`
   - **Move to**: `IFileDiscoveryService` in SacksLogicLayer

2. **Path Resolution Logic** (Lines ~80-120)
   - `GetInputDirectoryFromConfiguration()` - resolves relative/absolute paths
   - `FindSolutionRoot()` - traverses directory tree to find .sln file
   - Directory creation logic
   - **Move to**: `IPathResolutionService` in SacksLogicLayer

3. **File Processing Orchestration** (Lines ~48-85)
   - Loops through files and calls processing service
   - **Move to**: `IBatchProcessingService` in SacksLogicLayer

4. **Database Operations** (Lines ~127-170)
   - Calls `IDatabaseManagementService.ClearAllDataAsync()`
   - Calls `IDatabaseManagementService.CheckConnectionAsync()`
   - **Keep**: These are already delegated to services ?

#### Recommendations:
```csharp
// ? Current (Bad):
var files = Directory.GetFiles(inputsPath, "*.xls*")
    .Where(f => !Path.GetFileName(f).StartsWith("~"))
    .ToArray();

foreach (var file in files)
{
    await fileProcessingService.ProcessFileAsync(file, CancellationToken.None);
}

// ? Refactored (Good):
var fileDiscoveryService = _serviceProvider.GetRequiredService<IFileDiscoveryService>();
var files = await fileDiscoveryService.DiscoverFilesAsync(
    patterns: new[] { "*.xls*" }, 
    excludeTemporary: true, 
    cancellationToken);

var batchService = _serviceProvider.GetRequiredService<IBatchProcessingService>();
await batchService.ProcessFilesAsync(files, cancellationToken);
```

---

## 2. SqlQueryForm.cs - Advanced Query Tool

### ?? Location: `SacksApp\SqlQueryForm.cs`
### ?? Complexity: **VERY HIGH**
### ?? Lines of Logic: **~1,500 lines** (LARGEST OFFENDER)

#### Business Logic Found:

### A. SQL Query Building Logic (Lines ~200-600)
1. **WHERE Clause Construction**
   - `BuildBaseWhere()` - builds parameterized WHERE clause
   - `BuildCollapsedViewWhere()` - builds WHERE for grouped queries
   - `BuildDynamicInnerWhere()` - builds subquery WHERE clause
   - **Complexity**: HIGH - handles 11 different filter operators
   - **Move to**: `IQueryBuilderService.BuildQuery()`

2. **Filter Predicate Building**
   - `BuildPredicate()` - converts FilterCondition to SQL
   - Handles: Equals, NotEquals, GreaterThan, Contains, StartsWith, EndsWith, IsEmpty, IsNotEmpty
   - **Move to**: `IQueryBuilderService.BuildPredicate()`

3. **Query Assembly**
   - `BuildSimpleQuery()` - SELECT from base view
   - `BuildCollapsedUsingView()` - SELECT from pre-collapsed view
   - `BuildDynamicCollapsed()` - Dynamic ROW_NUMBER() query
   - **Move to**: `IQueryBuilderService.BuildQuery()`

### B. Filter Management Logic (Lines ~150-400)
1. **Filter Validation**
   - Type conversion validation
   - Operator compatibility checks
   - **Move to**: `FilterValidator` class

2. **Filter State Persistence**
   - `SaveFiltersState()` - JSON serialization
   - `LoadFiltersState()` - JSON deserialization
   - **Move to**: `IFilterStateService`

### C. Database Schema Inspection (Lines ~100-150)
1. **Column Discovery**
   - `LoadAvailableColumns()` - queries database schema
   - Uses: `SqlConnection`, `SqlCommand`, `GetSchemaTable()`
   - **Move to**: `ISchemaService.GetAvailableColumns()`

### D. Data Export Logic (Lines ~800-950)
1. **Excel Export**
   - `ExportResultsToExcelAsync()` - exports DataTable to Excel
   - Uses: ClosedXML library
   - **Move to**: `IDataExportService.ExportToExcelAsync()`

2. **Data Preparation**
   - Column filtering and ordering
   - Row-by-row data extraction
   - **Move to**: `IDataExportService`

### E. Inline Editing & Persistence (Lines ~600-800)
1. **Cell Value Persistence**
   - `TrySaveCellAsync()` - saves individual cell changes to database
   - Direct `DbContext` usage
   - Complex EAN/Supplier/Offer lookup logic
   - **Move to**: `IProductOfferUpdateService.UpdateCellAsync()`

2. **Change Tracking**
   - `DataRow.RowState` monitoring
   - `SaveChangesAsync()` - batch saves modified cells
   - **Move to**: `IProductOfferUpdateService.SaveChangesAsync()`

### F. Grid State Persistence (Lines ~950-1050)
1. **State Serialization**
   - `SaveGridState()` - saves column visibility, width, order, sort
   - `LoadGridState()` - restores grid state
   - Uses: JSON serialization to AppData folder
   - **Move to**: `IGridStateService`

#### Recommendations:
```csharp
// ? Current (Bad) - 1,500 lines of mixed logic in UI:
private string BuildBaseWhere(List<SqlParameter> parameters) { /* 100 lines */ }
private string BuildPredicate(...) { /* 50 lines */ }
private async Task ExportResultsToExcelAsync(...) { /* 150 lines */ }
// ... and much more

// ? Refactored (Good) - Delegate to services:
var queryBuilder = _serviceProvider.GetRequiredService<IQueryBuilderService>();
var (sql, parameters) = queryBuilder.BuildQuery(selectedColumns, _filters, groupByProduct);

var exportService = _serviceProvider.GetRequiredService<IDataExportService>();
await exportService.ExportToExcelAsync(dataTable, filePath, cancellationToken);

var updateService = _serviceProvider.GetRequiredService<IProductOfferUpdateService>();
await updateService.SaveChangesAsync(changedCells, cancellationToken);
```

---

## 3. OffersForm.cs - Offers CRUD Manager

### ?? Location: `SacksApp\OffersForm.cs`
### ?? Complexity: **MEDIUM**
### ?? Lines of Logic: **~250 lines**

#### Business Logic Found:

1. **Direct DbContext CRUD Operations** (Lines ~60-180)
   - `LoadSuppliersAsync()` - queries Suppliers table
   - `ReloadOffersAsync()` - queries SupplierOffers table
   - `AddOfferAsync()` - creates new Offer entity
   - `EditSelectedOfferAsync()` - updates Offer entity
   - `DeleteSelectedOfferAsync()` - deletes Offer entity
   - **All use direct DbContext** - `_db.SupplierOffers.Add()`, `_db.SaveChangesAsync()`
   - **Move to**: `IOffersService` (already created interface)

2. **Validation Logic in Dialog** (Lines ~200-250)
   - `OfferEditDialog` class validates:
     - OfferName is not empty
     - Currency is 3-letter code
   - **Move to**: `OfferValidator` class in SacksLogicLayer

#### Recommendations:
```csharp
// ? Current (Bad):
var entity = new Offer { SupplierId = sel.Id, OfferName = dlg.OfferName, ... };
_db.SupplierOffers.Add(entity);
await _db.SaveChangesAsync(ct);

// ? Refactored (Good):
var offersService = _serviceProvider.GetRequiredService<IOffersService>();
var request = new CreateOfferRequest 
{ 
    SupplierId = sel.Id,
    OfferName = dlg.OfferName,
    Currency = dlg.Currency,
    Description = dlg.Description
};
var result = await offersService.CreateOfferAsync(request, cancellationToken);
```

---

## 4. LookupEditorForm.cs - Lookup Configuration Editor

### ?? Location: `SacksApp\LookupEditorForm.cs`
### ?? Complexity: **MEDIUM**
### ?? Lines of Logic: **~400 lines**

#### Business Logic Found:

1. **Lookup Data Manipulation** (Lines ~150-250)
   - `LoadAsync()` - loads lookup entries from configuration
   - `SaveAsync()` - saves lookup entries to configuration file
   - Direct `ISuppliersConfiguration` manipulation
   - **Move to**: Enhanced `ISupplierConfigurationService`

2. **Validation Logic** (Lines ~250-300)
   - Empty key detection
   - Duplicate key detection (case-insensitive)
   - **Move to**: `LookupValidator` class

3. **Data Sorting Logic** (Lines ~80-150)
   - `SortEntries()` - manual BindingList sorting
   - Column header click handling
   - **Move to**: Helper class or keep in UI (acceptable)

4. **Lookup Creation Logic** (Lines ~120-180)
   - Creating new lookup tables dynamically
   - Name validation
   - **Move to**: `ISupplierConfigurationService.CreateLookupAsync()`

#### Recommendations:
```csharp
// ? Current (Bad):
_suppliersConfig.Lookups[_lookupName] = dict;
await _suppliersConfig.Save();

// ? Refactored (Good):
var configService = _serviceProvider.GetRequiredService<ISupplierConfigurationService>();
var validation = await configService.ValidateLookupAsync(_lookupName, dict);
if (!validation.IsValid)
{
    MessageBox.Show(string.Join("\n", validation.Errors));
    return;
}
await configService.SaveLookupAsync(_lookupName, dict, cancellationToken);
```

---

## 5. TestPattern.cs - Action Testing Tool

### ?? Location: `SacksApp\TestPattern.cs`
### ?? Complexity: **MEDIUM-HIGH**
### ?? Lines of Logic: **~350 lines**

#### Business Logic Found:

1. **Action Parameter Building** (Lines ~80-200)
   - `BuildFindOptionsString()` - constructs options for Find action
   - Complex parameter dictionary construction for different action types
   - **Move to**: `ActionConfigBuilder` class in ParsingEngine or SacksLogicLayer

2. **Lookup Table Loading** (Lines ~200-220)
   - `LoadLookupsAsync()` - loads configuration and merges lookups
   - **Already delegated to service** ? (but could be improved)

3. **Action Execution** (Lines ~220-300)
   - `OnRunClick()` - orchestrates action testing
   - Calls `ActionTestRunner.Run()` with constructed parameters
   - **Move to**: `IActionTestService` interface

#### Recommendations:
```csharp
// ? Current (Bad):
var parameters = new Dictionary<string, string>();
if (op == "find")
{
    var opts = BuildFindOptionsString();
    if (!string.IsNullOrWhiteSpace(opts)) parameters["Options"] = opts;
    // ... more parameter building
}
var result = ActionTestRunner.Run(op, inputText, inputKey, outputKey, ...);

// ? Refactored (Good):
var testService = _serviceProvider.GetRequiredService<IActionTestService>();
var request = new ActionTestRequest
{
    Operation = op,
    InputText = inputText,
    InputKey = inputKey,
    OutputKey = outputKey,
    Parameters = parameters,
    Lookups = await _configService.GetMergedLookupsAsync()
};
var result = await testService.ExecuteTestAsync(request, cancellationToken);
```

---

## Summary Statistics

| File | Lines of Logic | Complexity | Priority |
|------|----------------|------------|----------|
| **SqlQueryForm.cs** | ~1,500 | VERY HIGH | ?? **CRITICAL** |
| **LookupEditorForm.cs** | ~400 | MEDIUM | ?? HIGH |
| **TestPattern.cs** | ~350 | MEDIUM-HIGH | ?? HIGH |
| **OffersForm.cs** | ~250 | MEDIUM | ?? MEDIUM |
| **DashBoard.cs** | ~150 | MEDIUM | ?? MEDIUM |
| **TOTAL** | **~2,650 lines** | | |

---

## Services to Create (Priority Order)

### ?? Critical Priority
1. **IQueryBuilderService** - Extract SQL building from SqlQueryForm (saves ~600 lines)
2. **IDataExportService** - Extract Excel export logic (saves ~150 lines)
3. **IProductOfferUpdateService** - Extract inline editing logic (saves ~200 lines)

### ?? High Priority
4. **IOffersService** - Extract CRUD operations from OffersForm (saves ~250 lines)
5. **ISupplierConfigurationService** (Enhanced) - Better lookup management (saves ~200 lines)
6. **IFilterStateService** - Extract filter persistence (saves ~100 lines)

### ?? Medium Priority
7. **IFileDiscoveryService** - Extract file discovery (saves ~50 lines)
8. **IPathResolutionService** - Extract path resolution (saves ~100 lines)
9. **IActionTestService** - Extract action testing (saves ~150 lines)
10. **IGridStateService** - Extract grid state persistence (saves ~100 lines)

---

## Estimated Refactoring Effort

| Phase | Services | Effort | Lines Saved |
|-------|----------|--------|-------------|
| **Phase 1** | IQueryBuilderService, IDataExportService, IProductOfferUpdateService | 2-3 weeks | ~950 lines |
| **Phase 2** | IOffersService, Enhanced ISupplierConfigurationService | 1-2 weeks | ~450 lines |
| **Phase 3** | Remaining services | 1-2 weeks | ~600 lines |
| **Phase 4** | Testing & cleanup | 1 week | N/A |
| **TOTAL** | 10 services | **5-8 weeks** | **~2,000 lines** |

---

## Expected Outcomes

### Before Refactoring:
- ? **2,650 lines** of business logic in UI layer
- ? **0% unit test coverage** of business logic (can't test UI)
- ? Logic **cannot be reused** outside WinForms
- ? **High coupling** - UI changes break business logic

### After Refactoring:
- ? **<200 lines** of orchestration code in UI layer (90% reduction)
- ? **>80% unit test coverage** of business logic
- ? Logic **reusable** in APIs, console apps, batch jobs
- ? **Loose coupling** - business logic independent of UI

---

## Next Steps for Struly-Dear

1. ? **Review this document** - Understand the scope
2. ? **Prioritize services** - Start with SqlQueryForm refactoring
3. ?? **Create feature branches** - One per service
4. ?? **Implement services** - TDD approach (tests first)
5. ?? **Update UI** - Use new services
6. ??? **Remove old logic** - Clean up UI layer
7. ?? **Verify tests pass** - Ensure no regressions
8. ?? **Merge to main** - Celebrate improved architecture!

---

*This analysis was performed by GitHub Copilot for Struly-Dear*
*All file paths are relative to solution root*
