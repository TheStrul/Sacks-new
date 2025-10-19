# SacksApp Logic Refactoring Guide

## Executive Summary
This document outlines business logic currently residing in the SacksApp (UI layer) and provides 
best practices for moving it to appropriate lower layers (SacksLogicLayer and SacksDataLayer).

---

## Current Architecture Issues

### ? Problems Identified

1. **Direct DbContext Usage in Forms**
   - `SqlQueryForm.cs` directly uses `SacksDbContext` for queries
   - `OffersForm.cs` performs CRUD operations directly on DbContext
   - **Impact**: Tight coupling, difficult to test, violates separation of concerns

2. **Business Logic in UI Layer**
   - File discovery and filtering in `DashBoard.cs`
   - SQL query building in `SqlQueryForm.cs`
   - Data validation in form dialogs
   - **Impact**: Logic cannot be reused, difficult to unit test

3. **Configuration Management in Forms**
   - Path resolution logic in `DashBoard.cs`
   - Direct configuration file manipulation in `LookupEditorForm.cs`
   - **Impact**: Configuration changes require UI changes

4. **Data Export Logic in Forms**
   - Excel export implementation in `SqlQueryForm.cs`
   - **Impact**: Cannot export from batch processes or APIs

---

## ? Recommended Layered Architecture

```
???????????????????????????????????????????????????????????
?  SacksApp (Presentation Layer)                          ?
?  - WinForms UI                                           ?
?  - Event handlers                                        ?
?  - Data binding                                          ?
?  - User input validation (format only)                   ?
?  ? NO business logic                                    ?
?  ? NO data access                                       ?
???????????????????????????????????????????????????????????
                 ? Depends on
                 ?
???????????????????????????????????????????????????????????
?  SacksLogicLayer (Business Logic Layer)                 ?
?  - Services (business operations)                        ?
?  - Domain logic                                          ?
?  - Validation rules                                      ?
?  - DTOs for data transfer                                ?
?  - Transaction coordination                              ?
?  ? NO UI references                                     ?
?  ? NO direct DbContext usage                            ?
???????????????????????????????????????????????????????????
                 ? Depends on
                 ?
???????????????????????????????????????????????????????????
?  SacksDataLayer (Data Access Layer)                     ?
?  - DbContext                                             ?
?  - Repositories                                          ?
?  - Entities                                              ?
?  - Database migrations                                   ?
?  - Unit of Work pattern                                  ?
???????????????????????????????????????????????????????????
```

---

## ?? Specific Refactoring Recommendations

### 1. DashBoard.cs Refactoring

#### Current Logic (? Bad):
```csharp
// File discovery logic in UI
var files = Directory.GetFiles(inputsPath, "*.xls*")
    .Where(f => !Path.GetFileName(f).StartsWith("~"))
    .ToArray();

foreach (var file in files)
{
    await fileProcessingService.ProcessFileAsync(file, CancellationToken.None);
}
```

#### Recommended Approach (? Good):
```csharp
// In SacksLogicLayer/Services/Interfaces/IFileDiscoveryService.cs
public interface IFileDiscoveryService
{
    Task<IReadOnlyList<string>> DiscoverFilesAsync(
        string[] patterns, 
        bool excludeTemporary = true, 
        CancellationToken cancellationToken = default);
}

// In DashBoard.cs
var fileDiscovery = _serviceProvider.GetRequiredService<IFileDiscoveryService>();
var files = await fileDiscovery.DiscoverFilesAsync(new[] { "*.xls*" }, true, cancellationToken);

foreach (var file in files)
{
    await fileProcessingService.ProcessFileAsync(file, cancellationToken);
}
```

**Benefits:**
- File discovery logic can be unit tested independently
- Can be reused in batch processes, console apps, APIs
- UI only coordinates, doesn't contain logic

---

### 2. SqlQueryForm.cs Refactoring (?? Most Complex)

#### Current Issues:
- 700+ lines of SQL building logic
- Filter management and validation
- Direct SQL execution
- Excel export logic
- Grid state persistence

#### Recommended Approach:

**A. Move SQL Building to Service**
```csharp
// In SacksLogicLayer/Services/Interfaces/IQueryBuilderService.cs
public interface IQueryBuilderService
{
    (string Sql, Dictionary<string, object> Parameters) BuildQuery(
        IEnumerable<string> selectedColumns,
        IEnumerable<FilterCondition> filters,
        bool groupByProduct = false);
}

// In SqlQueryForm.cs
var queryBuilder = _serviceProvider.GetRequiredService<IQueryBuilderService>();
var (sql, parameters) = queryBuilder.BuildQuery(selectedColumns, _filters, groupByProduct);
```

**B. Move Data Export to Service**
```csharp
// In SacksLogicLayer/Services/Interfaces/IDataExportService.cs
public interface IDataExportService
{
    Task ExportToExcelAsync(DataTable data, string filePath, CancellationToken cancellationToken);
}

// In SqlQueryForm.cs
var exportService = _serviceProvider.GetRequiredService<IDataExportService>();
await exportService.ExportToExcelAsync(dataTable, filePath, cancellationToken);
```

**C. Move Filter Validation to Validator**
```csharp
// In SacksLogicLayer/Validators/FilterValidator.cs
public class FilterValidator
{
    public ValidationResult Validate(FilterCondition filter)
    {
        // Validation logic here
    }
}
```

---

### 3. OffersForm.cs Refactoring

#### Current Logic (? Bad):
```csharp
// Direct DbContext usage in UI
var entity = new Offer { ... };
_db.SupplierOffers.Add(entity);
await _db.SaveChangesAsync(ct);
```

#### Recommended Approach (? Good):
```csharp
// Create IOffersService in SacksLogicLayer
public interface IOffersService
{
    Task<OfferDto> CreateOfferAsync(CreateOfferRequest request, CancellationToken cancellationToken);
    Task<OfferDto> UpdateOfferAsync(int offerId, UpdateOfferRequest request, CancellationToken cancellationToken);
    Task<bool> DeleteOfferAsync(int offerId, CancellationToken cancellationToken);
}

// In OffersForm.cs
var offersService = _serviceProvider.GetRequiredService<IOffersService>();
var request = new CreateOfferRequest 
{ 
    SupplierId = supplierId,
    OfferName = dlg.OfferName,
    Currency = dlg.Currency,
    Description = dlg.Description
};
var result = await offersService.CreateOfferAsync(request, cancellationToken);
```

**Benefits:**
- Business rules centralized in service layer
- DTOs prevent over-posting vulnerabilities
- Can add authorization, validation, logging in service layer
- Transactional integrity handled by service

---

### 4. LookupEditorForm.cs Refactoring

#### Current Logic (? Bad):
```csharp
// Direct configuration manipulation
_suppliersConfig.Lookups[_lookupName] = dict;
await _suppliersConfig.Save();
```

#### Recommended Approach (? Good):
```csharp
// Enhance ISupplierConfigurationService
public interface ISupplierConfigurationService
{
    Task<ValidationResult> ValidateLookupAsync(string lookupName, Dictionary<string, string> entries);
    Task SaveLookupAsync(string lookupName, Dictionary<string, string> entries, CancellationToken cancellationToken);
}

// In LookupEditorForm.cs
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

## ?? Implementation Roadmap

### Phase 1: Foundation (Week 1)
1. ? Create service interfaces (DONE - files created above)
2. ? Define DTOs for data transfer
3. Create service implementations in SacksLogicLayer

### Phase 2: Core Refactoring (Week 2-3)
1. Implement `IFileDiscoveryService`
2. Implement `IQueryBuilderService` 
3. Implement `IDataExportService`
4. Implement `IOffersService`

### Phase 3: UI Updates (Week 4)
1. Update `DashBoard.cs` to use services
2. Update `SqlQueryForm.cs` to use services
3. Update `OffersForm.cs` to use services
4. Update `LookupEditorForm.cs` to use services

### Phase 4: Testing & Validation (Week 5)
1. Unit tests for all new services
2. Integration tests for critical paths
3. Remove old logic from UI layer
4. Code review and cleanup

---

## ?? Design Patterns to Follow

### 1. **Repository Pattern** (Already in use ?)
- Continue using `ITransactional*Repository` interfaces
- Keep data access in SacksDataLayer

### 2. **Service Layer Pattern** (Implement)
- Create service interfaces in `SacksLogicLayer/Services/Interfaces`
- Implement in `SacksLogicLayer/Services/Implementations`
- Register in DI container

### 3. **DTO Pattern** (Implement)
- Create DTOs in `SacksLogicLayer/DTOs`
- Never expose entities directly to UI
- Use AutoMapper or manual mapping

### 4. **Unit of Work Pattern** (Already in use ?)
- Continue using `IUnitOfWork` for transactions
- Services should coordinate multiple repository operations

### 5. **Dependency Injection** (Already in use ?)
- Continue registering services in `Program.cs`
- Forms receive services via constructor injection

---

## ?? Testing Strategy

### Unit Tests (SacksLogicLayer.Tests)
```csharp
public class FileDiscoveryServiceTests
{
    [Fact]
    public async Task DiscoverFilesAsync_WithExcelPattern_ReturnsMatchingFiles()
    {
        // Arrange
        var service = new FileDiscoveryService(mockConfig, mockLogger);
        
        // Act
        var files = await service.DiscoverFilesAsync(new[] { "*.xlsx" });
        
        // Assert
        Assert.NotEmpty(files);
        Assert.All(files, f => Assert.EndsWith(".xlsx", f));
    }
}
```

### Integration Tests (Sacks.Tests)
```csharp
public class OffersServiceIntegrationTests : IClassFixture<DatabaseFixture>
{
    [Fact]
    public async Task CreateOfferAsync_ValidRequest_CreatesOffer()
    {
        // Arrange
        var service = _fixture.GetService<IOffersService>();
        
        // Act
        var result = await service.CreateOfferAsync(request, CancellationToken.None);
        
        // Assert
        Assert.NotNull(result);
        Assert.NotEqual(0, result.Id);
    }
}
```

---

## ? Benefits Summary

### Before Refactoring:
- ? 50% of business logic in UI layer
- ? Difficult to test
- ? Cannot reuse logic in other contexts
- ? Tight coupling between layers
- ? Changes require UI changes

### After Refactoring:
- ? 100% business logic in service layer
- ? Comprehensive unit test coverage
- ? Logic reusable in APIs, batch jobs, console apps
- ? Loose coupling via interfaces
- ? UI changes don't affect business logic

---

## ?? References

- **Layered Architecture**: https://learn.microsoft.com/en-us/dotnet/architecture/modern-web-apps-azure/common-web-application-architectures
- **Repository Pattern**: https://learn.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/infrastructure-persistence-layer-design
- **Service Layer Pattern**: https://martinfowler.com/eaaCatalog/serviceLayer.html
- **DTO Pattern**: https://learn.microsoft.com/en-us/aspnet/web-api/overview/data/using-web-api-with-entity-framework/part-5

---

## ?? Next Steps

1. Review this document with the team
2. Prioritize services to implement
3. Create feature branches for each service
4. Implement with unit tests
5. Update UI to use new services
6. Remove old logic
7. Celebrate improved architecture! ??

---

*Generated for Struly-Dear by GitHub Copilot*
*Date: 2024*
