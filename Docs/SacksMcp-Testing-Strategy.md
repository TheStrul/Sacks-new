# SacksMcp Testing Strategy & Design
**Production Readiness Testing Plan**

**Version:** 1.0  
**Date:** November 21, 2025  
**Status:** Design Phase  
**Target:** 100% Production Ready

---

## Executive Summary

This document defines the comprehensive testing strategy for SacksMcp to achieve production readiness with enterprise-grade quality, security, and reliability.

### Goals
- **100% code coverage** for critical paths (tools, validation, error handling)
- **Zero known defects** in production deployment
- **Performance benchmarks** validated under load
- **Security hardening** verified through penetration testing
- **CI/CD integration** with automated quality gates

---

## 1. Test Pyramid Architecture

```
                    ╔═══════════════╗
                    ║   E2E Tests   ║  (10% - 5-10 tests)
                    ║  MCP Protocol ║
                    ╚═══════════════╝
                  ╔═══════════════════╗
                  ║ Integration Tests ║  (30% - 30-50 tests)
                  ║   Real Database   ║
                  ╚═══════════════════╝
              ╔═══════════════════════════╗
              ║      Unit Tests           ║  (60% - 200+ tests)
              ║   Tools + Validation      ║
              ╚═══════════════════════════╝
```

### Rationale
- **Unit Tests (60%)**: Fast, isolated, comprehensive coverage of business logic
- **Integration Tests (30%)**: Real database scenarios, EF Core behavior, transaction handling
- **E2E Tests (10%)**: Full MCP protocol communication, end-to-end workflows

---

## 2. Test Categories & Coverage Targets

### 2.1 Unit Tests (Target: 200+ tests, 95%+ coverage)

**Scope**: Isolated testing of individual methods without external dependencies

#### 2.1.1 Tool Method Tests
**Coverage per tool method:**
- ✅ Happy path with valid inputs
- ✅ Edge cases (empty results, boundary values)
- ✅ Validation failures (required params, range violations)
- ✅ Null/empty string handling
- ✅ CancellationToken cancellation
- ✅ Limit enforcement (min/max boundaries)

**Tools to cover:**
- `ProductTools` (6 methods × 6 scenarios = 36 tests)
- `OfferTools` (6 methods × 6 scenarios = 36 tests)
- `SupplierTools` (6 methods × 6 scenarios = 36 tests)
- **Subtotal: ~108 tool tests**

#### 2.1.2 Validation Tests
- Required parameter validation (15 tests)
- Range validation with boundaries (20 tests)
- String validation (empty, whitespace, null) (10 tests)
- **Subtotal: 45 validation tests**

#### 2.1.3 Error Handling Tests
- Database exceptions (DbUpdateException, SqlException) (10 tests)
- Timeout scenarios (15 tests)
- Concurrent access conflicts (10 tests)
- **Subtotal: 35 error handling tests**

#### 2.1.4 JSON Serialization Tests
- Success response formatting (5 tests)
- Error response formatting (5 tests)
- Complex object serialization (10 tests)
- **Subtotal: 20 serialization tests**

**Unit Test Total: ~208 tests**

---

### 2.2 Integration Tests (Target: 40-50 tests, 90%+ coverage)

**Scope**: Testing with real SQL Server database using Testcontainers

#### 2.2.1 Database Query Tests (25 tests)
- Actual EF Core queries against real SQL Server
- Complex joins and projections
- `AsNoTracking()` behavior verification
- `AsSplitQuery()` for wide graphs (if needed)
- Index usage and query plan validation
- Connection pooling behavior

#### 2.2.2 Transaction Tests (10 tests)
- Read-only transaction isolation
- Concurrent read scenarios
- Connection timeout handling
- Retry logic on transient failures (3 attempts)

#### 2.2.3 Data Integrity Tests (10 tests)
- Foreign key constraint handling
- NULL value handling in database
- Unicode/special character support
- Large dataset handling (1000+ rows)
- Pagination correctness

#### 2.2.4 Configuration Tests (5 tests)
- Connection string variations
- CommandTimeout enforcement
- EnableSensitiveDataLogging behavior
- EnableDetailedErrors behavior
- MaxRetryAttempts configuration

**Integration Test Total: ~50 tests**

---

### 2.3 End-to-End (E2E) Tests (Target: 8-10 tests, 100% critical paths)

**Scope**: Full MCP server lifecycle with stdio communication

#### 2.3.1 MCP Protocol Tests (5 tests)
- Server startup and tool discovery
- Tool invocation via JSON-RPC over stdio
- Request serialization / response deserialization
- Error responses in MCP format
- Server shutdown and cleanup

#### 2.3.2 Integration Scenarios (3 tests)
- Complete workflow: Search products → Compare prices → Get supplier stats
- Multi-tool call sequence
- Long-running query handling

#### 2.3.3 Configuration Loading (2 tests)
- appsettings.json loading
- Environment variable overrides

**E2E Test Total: ~10 tests**

---

### 2.4 Performance Tests (Target: 15 benchmarks + 5 load tests)

**Scope**: Performance benchmarking and load testing

#### 2.4.1 BenchmarkDotNet Tests (15 benchmarks)
- Query execution time for each tool method
- JSON serialization/deserialization overhead
- EF Core query compilation time
- Memory allocations per operation
- Baseline comparisons (track regressions)

**Targets:**
- Query execution: < 100ms (p95) for simple queries
- Query execution: < 500ms (p95) for complex queries
- Memory per request: < 5MB
- GC collections: Minimal Gen2 collections

#### 2.4.2 Load Tests (5 scenarios)
- Concurrent tool calls (10, 50, 100 concurrent users)
- Connection pool exhaustion testing (max 100 connections)
- Memory leak detection (sustained load for 1 hour)
- CPU usage under load (< 70% utilization)
- Query timeout behavior under contention

**Load Test Targets:**
- **Throughput**: 100 requests/sec sustained
- **Response time**: p95 < 200ms, p99 < 500ms
- **Error rate**: < 0.1% under normal load
- **Max concurrent**: 50 tools executing simultaneously (configured limit: 10, should queue properly)

---

### 2.5 Security Tests (Target: 20+ tests)

**Scope**: Security validation and penetration testing

#### 2.5.1 Input Validation Security (10 tests)
- SQL injection attempts (parameterized queries prevent)
- XSS via JSON responses (proper encoding)
- Command injection in search terms
- Path traversal attempts (N/A for database)
- Buffer overflow attempts (large strings)
- Regex DoS (if using regex patterns)

#### 2.5.2 Data Protection Tests (5 tests)
- No sensitive data in logs (EnableSensitiveDataLogging=false)
- No connection strings in responses
- No stack traces exposed to clients
- PII handling (if applicable)
- Error message sanitization

#### 2.5.3 Access Control Tests (5 tests)
- Read-only operations enforced (no INSERT/UPDATE/DELETE)
- Database user permissions validation
- Tool method authorization (all tools are read-only)
- Rate limiting behavior (if implemented)
- Denial of service resistance (cancellation token handling)

**Security Test Total: ~20 tests**

---

### 2.6 Reliability & Resilience Tests (Target: 15 tests)

#### 2.6.1 Fault Tolerance (8 tests)
- Database connection failure handling
- Network interruption recovery
- Query timeout with retry logic
- Connection pool exhaustion graceful degradation
- Out-of-memory handling
- Thread pool exhaustion scenarios

#### 2.6.2 Cancellation Tests (7 tests)
- CancellationToken propagation through all layers
- Cancellation during query execution
- Cancellation during JSON serialization
- Cleanup after cancellation
- No resource leaks on cancellation
- Multiple concurrent cancellations

**Reliability Test Total: ~15 tests**

---

## 3. Test Project Structure

### 3.1 Project Organization

```
SacksMcp.Tests/
├── SacksMcp.Tests.csproj
├── Unit/
│   ├── Tools/
│   │   ├── ProductToolsTests.cs
│   │   ├── OfferToolsTests.cs
│   │   └── SupplierToolsTests.cs
│   ├── Validation/
│   │   ├── RequiredValidationTests.cs
│   │   └── RangeValidationTests.cs
│   └── Serialization/
│       └── JsonFormattingTests.cs
├── Integration/
│   ├── Database/
│   │   ├── ProductQueriesTests.cs
│   │   ├── OfferQueriesTests.cs
│   │   ├── SupplierQueriesTests.cs
│   │   └── TransactionTests.cs
│   └── Configuration/
│       └── DatabaseOptionsTests.cs
├── E2E/
│   ├── McpProtocolTests.cs
│   └── WorkflowTests.cs
├── Performance/
│   ├── Benchmarks/
│   │   ├── ProductToolsBenchmarks.cs
│   │   ├── OfferToolsBenchmarks.cs
│   │   └── SupplierToolsBenchmarks.cs
│   └── LoadTests/
│       └── ConcurrentLoadTests.cs
├── Security/
│   ├── InputValidationTests.cs
│   ├── DataProtectionTests.cs
│   └── AccessControlTests.cs
├── Reliability/
│   ├── FaultToleranceTests.cs
│   └── CancellationTests.cs
├── Fixtures/
│   ├── DatabaseFixture.cs
│   ├── McpServerFixture.cs
│   └── TestDataBuilder.cs
├── Helpers/
│   ├── MockDbContextFactory.cs
│   ├── TestContainersHelper.cs
│   └── AssertionExtensions.cs
└── TestData/
    ├── SeedData.sql
    └── ExpectedResponses.json
```

### 3.2 Dependencies

```xml
<ItemGroup>
  <!-- Testing Frameworks -->
  <PackageReference Include="xunit" Version="2.9.0" />
  <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2" />
  <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.11.0" />
  
  <!-- Mocking & Assertions -->
  <PackageReference Include="Moq" Version="4.20.72" />
  <PackageReference Include="FluentAssertions" Version="7.0.0" />
  <PackageReference Include="FluentAssertions.Json" Version="7.0.0" />
  
  <!-- Integration Testing -->
  <PackageReference Include="Testcontainers.MsSql" Version="4.0.0" />
  <PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" Version="10.0.0" />
  
  <!-- Performance Testing -->
  <PackageReference Include="BenchmarkDotNet" Version="0.14.0" />
  <PackageReference Include="NBomber" Version="5.10.2" />
  
  <!-- Coverage -->
  <PackageReference Include="coverlet.collector" Version="6.0.2" />
  <PackageReference Include="coverlet.msbuild" Version="6.0.2" />
  
  <!-- Utilities -->
  <PackageReference Include="Bogus" Version="35.6.1" /> <!-- Fake data generation -->
  <PackageReference Include="AutoFixture" Version="4.18.1" />
  <PackageReference Include="AutoFixture.Xunit2" Version="4.18.1" />
</ItemGroup>

<ItemGroup>
  <ProjectReference Include="..\SacksMcp\SacksMcp.csproj" />
  <ProjectReference Include="..\McpServer.Core\McpServer.Core.csproj" />
  <ProjectReference Include="..\McpServer.Database\McpServer.Database.csproj" />
  <ProjectReference Include="..\SacksDataLayer\SacksDataLayer.csproj" />
</ItemGroup>
```

---

## 4. Testing Tools & Technologies

### 4.1 Unit Testing Stack
- **xUnit**: Test framework (preferred for .NET Core)
- **Moq**: Mocking framework for DbContext and dependencies
- **FluentAssertions**: Readable assertions
- **AutoFixture**: Test data generation

### 4.2 Integration Testing Stack
- **Testcontainers**: SQL Server Docker containers for isolated tests
- **EF Core InMemory**: Quick validation tests (limited use)
- **FluentAssertions**: Database result validation

### 4.3 E2E Testing Stack
- **Custom MCP Client**: Stdio-based JSON-RPC communication
- **Process management**: Start/stop SacksMcp.Console
- **JSON validation**: Schema validation for MCP protocol

### 4.4 Performance Testing Stack
- **BenchmarkDotNet**: Micro-benchmarks for individual operations
- **NBomber**: Load testing framework
- **dotnet-counters**: Real-time performance monitoring
- **dotMemory**: Memory profiling (optional)

### 4.5 Code Coverage Tools
- **Coverlet**: Code coverage collector
- **ReportGenerator**: HTML coverage reports
- **SonarQube** (optional): Continuous code quality

### 4.6 Security Testing Tools
- **Manual penetration testing**: SQL injection attempts
- **OWASP dependency check**: Vulnerable package detection
- **Static analysis**: Roslyn analyzers + custom rules

---

## 5. Test Data Strategy

### 5.1 Unit Test Data
- **AutoFixture**: Generate random but valid test objects
- **Hard-coded samples**: Known edge cases
- **Minimal data**: Only what's needed per test

### 5.2 Integration Test Data
- **Seed scripts**: SQL scripts to populate test database
- **Bogus library**: Generate realistic fake data
- **Isolated per test**: Each test gets fresh database via Testcontainers
- **Cleanup**: Automatic disposal after test

### 5.3 Test Data Patterns
```csharp
// Example: Product test data builder
public class TestProductBuilder
{
    private Product _product = new Product
    {
        Id = 1,
        Name = "Test Product",
        EAN = "1234567890123"
    };

    public TestProductBuilder WithName(string name)
    {
        _product.Name = name;
        return this;
    }

    public TestProductBuilder WithoutEAN()
    {
        _product.EAN = null;
        return this;
    }

    public Product Build() => _product;
}
```

---

## 6. CI/CD Integration

### 6.1 GitHub Actions Workflow

```yaml
name: SacksMcp CI/CD

on: [push, pull_request]

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'
      
      # Unit Tests (fast)
      - name: Run Unit Tests
        run: dotnet test --filter Category=Unit --logger trx
      
      # Integration Tests (requires Docker for Testcontainers)
      - name: Run Integration Tests
        run: dotnet test --filter Category=Integration --logger trx
      
      # E2E Tests
      - name: Run E2E Tests
        run: dotnet test --filter Category=E2E --logger trx
      
      # Code Coverage
      - name: Generate Coverage Report
        run: |
          dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura
          reportgenerator -reports:coverage.cobertura.xml -targetdir:coverage-report
      
      # Upload Coverage
      - name: Upload Coverage to Codecov
        uses: codecov/codecov-action@v4
        with:
          file: ./coverage.cobertura.xml
      
      # Quality Gate
      - name: Check Coverage Threshold
        run: |
          dotnet test /p:Threshold=90 /p:ThresholdType=line /p:ThresholdStat=total

  benchmark:
    runs-on: ubuntu-latest
    if: github.event_name == 'push' && github.ref == 'refs/heads/main'
    steps:
      - name: Run Benchmarks
        run: dotnet run --project SacksMcp.Tests/Performance/Benchmarks --configuration Release
      
      - name: Upload Benchmark Results
        uses: actions/upload-artifact@v4
        with:
          name: benchmark-results
          path: BenchmarkDotNet.Artifacts/
```

### 6.2 Quality Gates (Fail Build If)
- ❌ Code coverage < 90% for critical paths
- ❌ Any E2E test fails
- ❌ Performance regression > 20% from baseline
- ❌ Security vulnerability detected (High/Critical)
- ❌ Memory leak detected
- ❌ Any unhandled exception in tests

---

## 7. Test Execution Strategy

### 7.1 Local Development
```powershell
# Run all tests
dotnet test

# Run only unit tests (fast feedback)
dotnet test --filter Category=Unit

# Run with coverage
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover

# Run specific test class
dotnet test --filter FullyQualifiedName~ProductToolsTests
```

### 7.2 Pre-Commit Hooks
```bash
# Run unit tests before commit
dotnet test --filter Category=Unit --no-build
```

### 7.3 Pull Request Checks
- ✅ All unit tests pass
- ✅ All integration tests pass
- ✅ Code coverage maintained or improved
- ✅ No new compiler warnings
- ✅ Benchmarks show no significant regression

### 7.4 Nightly Builds
- 🌙 Full test suite (Unit + Integration + E2E)
- 🌙 Performance benchmarks with historical comparison
- 🌙 Security scans
- 🌙 Load tests with realistic data volumes

---

## 8. Coverage Targets

### 8.1 Line Coverage Targets
- **Tools (ProductTools, OfferTools, SupplierTools)**: 95%+
- **Validation logic**: 100%
- **Error handling**: 90%+
- **Configuration**: 85%+
- **Overall project**: 90%+

### 8.2 Branch Coverage Targets
- **Critical paths**: 90%+
- **Error paths**: 80%+
- **Overall**: 85%+

### 8.3 Excluded from Coverage
- Generated code (if any)
- Third-party integrations (MCP protocol library itself)
- Logging statements (covered by integration tests)

---

## 9. Test Naming Conventions

### 9.1 Unit Tests
**Pattern**: `MethodName_Scenario_ExpectedBehavior`

```csharp
// Examples:
SearchProducts_WithValidSearchTerm_ReturnsMatchingProducts()
SearchProducts_WithEmptySearchTerm_ThrowsArgumentException()
SearchProducts_WithCancelledToken_ThrowsOperationCanceledException()
GetProductByEan_WhenProductNotFound_ReturnsNotFoundResponse()
```

### 9.2 Integration Tests
**Pattern**: `Feature_Scenario_ExpectedOutcome`

```csharp
// Examples:
Database_ComplexJoinQuery_ReturnsCorrectResults()
Database_ConcurrentReads_NoDeadlocks()
Configuration_InvalidConnectionString_ThrowsException()
```

### 9.3 E2E Tests
**Pattern**: `Workflow_Steps_ExpectedResult`

```csharp
// Examples:
McpServer_ToolDiscovery_ReturnsAllTools()
McpServer_SearchProductsWorkflow_ReturnsValidJsonResponse()
McpServer_MultiToolSequence_CompletesSuccessfully()
```

---

## 10. Production Readiness Checklist

### 10.1 Testing Completeness ✅
- [ ] All 18 MCP tools have unit tests (6 scenarios each)
- [ ] All validation logic covered
- [ ] All error paths tested
- [ ] Integration tests with real SQL Server
- [ ] E2E tests with actual MCP protocol
- [ ] Performance benchmarks established
- [ ] Load testing completed (100 req/sec sustained)
- [ ] Security testing completed (no vulnerabilities)
- [ ] Cancellation token handling verified
- [ ] Memory leak testing passed (1 hour sustained load)

### 10.2 Code Quality ✅
- [ ] Code coverage ≥ 90%
- [ ] Branch coverage ≥ 85%
- [ ] Zero compiler warnings
- [ ] All Roslyn analyzers pass
- [ ] No code smells (SonarQube)
- [ ] Documentation complete
- [ ] XML comments on all public APIs

### 10.3 Performance ✅
- [ ] p95 query latency < 200ms
- [ ] p99 query latency < 500ms
- [ ] Throughput ≥ 100 req/sec
- [ ] Memory per request < 5MB
- [ ] No memory leaks detected
- [ ] Connection pool properly sized
- [ ] Query plans optimized (no table scans)

### 10.4 Security ✅
- [ ] No SQL injection vulnerabilities
- [ ] Parameterized queries only
- [ ] No sensitive data in logs
- [ ] Input validation on all parameters
- [ ] Error messages sanitized
- [ ] Read-only database access enforced
- [ ] OWASP dependency check passed
- [ ] Penetration testing completed

### 10.5 Reliability ✅
- [ ] Graceful degradation on database failure
- [ ] Retry logic for transient failures (3 attempts)
- [ ] Proper timeout handling
- [ ] Connection pool exhaustion handled
- [ ] Cancellation token propagation
- [ ] No resource leaks
- [ ] Thread-safe operations

### 10.6 Operations ✅
- [ ] Structured logging implemented
- [ ] Health check endpoint (if applicable)
- [ ] Metrics exposed (if applicable)
- [ ] Configuration validation on startup
- [ ] Clear error messages
- [ ] Deployment documentation
- [ ] Rollback plan defined

### 10.7 Documentation ✅
- [ ] Architecture documentation
- [ ] API documentation (tool descriptions)
- [ ] Deployment guide
- [ ] Troubleshooting guide
- [ ] Performance tuning guide
- [ ] Security hardening guide

---

## 11. Risk Mitigation

### 11.1 High-Risk Areas
1. **Database connection failures** → Retry logic + circuit breaker pattern
2. **Query performance degradation** → Benchmark baselines + alerting
3. **Memory leaks** → Profiling + load testing
4. **SQL injection** → Parameterized queries + validation
5. **Connection pool exhaustion** → Pool size tuning + monitoring

### 11.2 Mitigation Strategies
- **Automated testing** catches regressions before production
- **Performance baselines** detect degradation early
- **Security scanning** in CI/CD pipeline
- **Load testing** validates capacity planning
- **Monitoring** detects issues in production

---

## 12. Success Criteria

### 12.1 Definition of Done for Testing
✅ All test categories implemented (Unit/Integration/E2E/Performance/Security)  
✅ Code coverage ≥ 90%  
✅ All CI/CD quality gates passing  
✅ Performance benchmarks within targets  
✅ Security testing completed with zero high/critical issues  
✅ Load testing validates 100 req/sec sustained  
✅ Documentation complete  

### 12.2 Production Go/No-Go Decision
**GO if:**
- All tests pass ✅
- Coverage targets met ✅
- Performance benchmarks acceptable ✅
- Security review approved ✅
- Operations team trained ✅

**NO-GO if:**
- Any E2E test fails ❌
- Coverage < 85% ❌
- Performance regression > 20% ❌
- High/Critical security issues ❌
- Memory leak detected ❌

---

## 13. Timeline & Effort Estimate

### Phase 1: Setup & Infrastructure (1 week)
- Create SacksMcp.Tests project
- Configure Testcontainers
- Setup CI/CD pipeline
- Establish test data builders

### Phase 2: Unit Tests (2 weeks)
- Implement all tool method tests (~108 tests)
- Validation tests (~45 tests)
- Error handling tests (~35 tests)
- Serialization tests (~20 tests)

### Phase 3: Integration Tests (1.5 weeks)
- Database query tests (~25 tests)
- Transaction tests (~10 tests)
- Data integrity tests (~10 tests)
- Configuration tests (~5 tests)

### Phase 4: E2E & Performance (1.5 weeks)
- MCP protocol tests (~5 tests)
- Workflow tests (~3 tests)
- Benchmarks (~15 benchmarks)
- Load tests (~5 scenarios)

### Phase 5: Security & Reliability (1 week)
- Security tests (~20 tests)
- Reliability tests (~15 tests)
- Penetration testing
- Final validation

**Total Estimated Effort: 7 weeks (1 developer full-time)**

---

## 14. Maintenance & Continuous Improvement

### 14.1 Ongoing Activities
- **Test maintenance**: Update tests when tools change
- **Coverage monitoring**: Track coverage trends
- **Performance baselines**: Update benchmarks quarterly
- **Security scans**: Run monthly vulnerability scans
- **Load testing**: Run quarterly with production-like data

### 14.2 Metrics to Track
- Test execution time (should stay < 5 minutes for CI)
- Flaky test rate (target: < 1%)
- Code coverage trend
- Performance benchmark trends
- Production error rates

---

## Appendix A: Sample Test Code

### A.1 Unit Test Example
```csharp
public class ProductToolsTests
{
    private readonly Mock<SacksDbContext> _mockContext;
    private readonly Mock<ILogger<ProductTools>> _mockLogger;
    private readonly ProductTools _sut;

    public ProductToolsTests()
    {
        _mockContext = new Mock<SacksDbContext>();
        _mockLogger = new Mock<ILogger<ProductTools>>();
        _sut = new ProductTools(_mockContext.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task SearchProducts_WithValidSearchTerm_ReturnsMatchingProducts()
    {
        // Arrange
        var products = new List<Product>
        {
            new() { Id = 1, Name = "Perfume A", EAN = "123" },
            new() { Id = 2, Name = "Perfume B", EAN = "456" }
        }.AsQueryable();

        var mockSet = new Mock<DbSet<Product>>();
        mockSet.As<IQueryable<Product>>().Setup(m => m.Provider).Returns(products.Provider);
        mockSet.As<IQueryable<Product>>().Setup(m => m.Expression).Returns(products.Expression);
        mockSet.As<IQueryable<Product>>().Setup(m => m.ElementType).Returns(products.ElementType);
        mockSet.As<IQueryable<Product>>().Setup(m => m.GetEnumerator()).Returns(products.GetEnumerator());

        _mockContext.Setup(c => c.Products).Returns(mockSet.Object);

        // Act
        var result = await _sut.SearchProducts("Perfume", 50);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("\"count\":2");
        result.Should().Contain("Perfume A");
        result.Should().Contain("Perfume B");
    }

    [Fact]
    public async Task SearchProducts_WithEmptySearchTerm_ThrowsArgumentException()
    {
        // Act & Assert
        await _sut.Invoking(x => x.SearchProducts("", 50))
            .Should().ThrowAsync<ArgumentException>()
            .WithMessage("*searchTerm*");
    }
}
```

### A.2 Integration Test Example
```csharp
public class ProductQueriesIntegrationTests : IClassFixture<DatabaseFixture>
{
    private readonly DatabaseFixture _fixture;

    public ProductQueriesIntegrationTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task SearchProducts_WithRealDatabase_ReturnsCorrectResults()
    {
        // Arrange
        await _fixture.SeedTestDataAsync();
        var tools = new ProductTools(_fixture.DbContext, _fixture.Logger);

        // Act
        var result = await tools.SearchProducts("Test Product", 50);

        // Assert
        result.Should().Contain("\"count\":");
        var json = JsonDocument.Parse(result);
        json.RootElement.GetProperty("count").GetInt32().Should().BeGreaterThan(0);
    }
}

public class DatabaseFixture : IAsyncLifetime
{
    private MsSqlContainer? _container;
    public SacksDbContext DbContext { get; private set; } = null!;
    public ILogger<ProductTools> Logger { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        _container = new MsSqlBuilder().Build();
        await _container.StartAsync();

        var options = new DbContextOptionsBuilder<SacksDbContext>()
            .UseSqlServer(_container.GetConnectionString())
            .Options;

        DbContext = new SacksDbContext(options);
        await DbContext.Database.EnsureCreatedAsync();

        Logger = new Mock<ILogger<ProductTools>>().Object;
    }

    public async Task SeedTestDataAsync()
    {
        // Insert test data
        DbContext.Products.Add(new Product { Name = "Test Product", EAN = "123" });
        await DbContext.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        await DbContext.DisposeAsync();
        if (_container != null)
            await _container.DisposeAsync();
    }
}
```

---

## Conclusion

This comprehensive testing strategy ensures SacksMcp achieves production-grade quality with:
- **268+ automated tests** covering all scenarios
- **90%+ code coverage** with quality gates
- **Performance validated** under realistic load
- **Security hardened** through penetration testing
- **CI/CD integrated** for continuous quality

**Next Steps:**
1. Review and approve this design document ✅
2. Create SacksMcp.Tests project structure ⏭️
3. Implement tests in phases (7-week timeline) ⏭️
4. Validate production readiness checklist ⏭️
5. Deploy to production with confidence 🚀
