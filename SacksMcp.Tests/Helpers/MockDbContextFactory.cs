using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Sacks.DataAccess.Data;

namespace SacksMcp.Tests.Helpers;

/// <summary>
/// Factory for creating test DbContext instances for unit testing.
/// Uses EF Core in-memory database for fast, isolated unit tests.
/// </summary>
public static class MockDbContextFactory
{
    /// <summary>
    /// Creates an EF Core in-memory database context.
    /// Each call creates a new isolated database with a unique name.
    /// Caller is responsible for disposing the context.
    /// </summary>
    public static SacksDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<SacksDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var context = new SacksDbContext(options);
        return context;
    }

    /// <summary>
    /// Creates a mock logger for testing.
    /// </summary>
    public static Mock<ILogger<T>> CreateMockLogger<T>()
    {
        return new Mock<ILogger<T>>();
    }
}
