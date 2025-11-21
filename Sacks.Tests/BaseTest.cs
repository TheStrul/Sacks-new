using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Sacks.DataAccess.Data;
using System;
using System.IO;

namespace Sacks.Tests;

/// <summary>
/// Base class for all tests providing common setup and utilities.
/// </summary>
public abstract class BaseTest : IDisposable
{
    protected ILoggerFactory LoggerFactory { get; }
    protected IServiceProvider ServiceProvider { get; }

    protected BaseTest()
    {
        var services = new ServiceCollection();
        
        // Add logging
        services.AddLogging(builder =>
        {
            builder.AddConsole()
                   .SetMinimumLevel(LogLevel.Debug);
        });

        // Add in-memory database for testing
        services.AddDbContext<SacksDbContext>(options =>
            options.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()));

        ServiceProvider = services.BuildServiceProvider();
        LoggerFactory = ServiceProvider.GetRequiredService<ILoggerFactory>();
    }

    protected ILogger<T> GetLogger<T>() => LoggerFactory.CreateLogger<T>();

    protected SacksDbContext CreateDbContext()
    {
        return ServiceProvider.GetRequiredService<SacksDbContext>();
    }

    protected string GetTestDataPath(string fileName)
    {
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        return Path.Combine(baseDir, "TestData", fileName);
    }

    protected string GetTestConfigurationPath(string fileName)
    {
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        return Path.Combine(baseDir, "TestData", "Configurations", fileName);
    }

    protected string GetSampleFilePath(string fileName)
    {
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        return Path.Combine(baseDir, "TestData", "SampleFiles", fileName);
    }

    public virtual void Dispose()
    {
        if (ServiceProvider is IDisposable disposableProvider)
        {
            disposableProvider.Dispose();
        }
        LoggerFactory?.Dispose();
        GC.SuppressFinalize(this);
    }
}
