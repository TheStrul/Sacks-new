# SacksMcp.Tests

Comprehensive test suite for SacksMcp production readiness.

## Test Structure

```
SacksMcp.Tests/
├── Unit/                    # Fast, isolated unit tests with mocked dependencies
│   ├── Tools/              # ProductTools, OfferTools, SupplierTools tests
│   ├── Validation/         # Input validation tests
│   └── Serialization/      # JSON formatting tests
├── Integration/            # Tests with real SQL Server via Testcontainers
│   ├── Database/           # Actual database query tests
│   └── Configuration/      # Configuration loading tests
├── E2E/                    # End-to-end MCP protocol tests
├── Performance/            # Benchmarks and load tests
│   ├── Benchmarks/         # BenchmarkDotNet micro-benchmarks
│   └── LoadTests/          # NBomber load/stress tests
├── Security/               # Security and penetration tests
├── Reliability/            # Fault tolerance and cancellation tests
├── Fixtures/               # Test fixtures (DatabaseFixture)
├── Helpers/                # Test utilities (builders, mocks, assertions)
└── TestData/               # SQL seed scripts and test data
```

## Running Tests

### All Tests
```powershell
dotnet test
```

### By Category
```powershell
# Unit tests only (fast)
dotnet test --filter Category=Unit

# Integration tests (requires Docker for Testcontainers)
dotnet test --filter Category=Integration

# E2E tests
dotnet test --filter Category=E2E

# Security tests
dotnet test --filter Category=Security
```

### With Coverage
```powershell
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

### Specific Test Class
```powershell
dotnet test --filter FullyQualifiedName~ProductToolsTests
```

## Performance Benchmarks

```powershell
cd Performance/Benchmarks
dotnet run -c Release
```

## Test Data Builders

Use fluent builders for creating test data:

```csharp
// Single product
var product = new TestProductBuilder()
    .WithName("Test Perfume")
    .WithEan("1234567890123")
    .Build();

// Multiple products
var products = TestProductBuilder.BuildMany(100, builder =>
{
    builder.WithRandomName().WithRandomEan();
});
```

## Coverage Targets

- **Tools**: 95%+
- **Validation**: 100%
- **Overall**: 90%+

## CI/CD Integration

Tests run automatically on:
- Every commit (unit tests)
- Pull requests (unit + integration)
- Main branch (full suite including E2E)

## Dependencies

- **xUnit**: Test framework
- **Moq**: Mocking framework
- **FluentAssertions**: Readable assertions
- **Testcontainers**: SQL Server in Docker
- **BenchmarkDotNet**: Performance benchmarking
- **Bogus**: Fake data generation
- **AutoFixture**: Test data generation
