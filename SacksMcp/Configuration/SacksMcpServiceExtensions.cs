using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using McpServer.Core.Configuration;
using McpServer.Database.Configuration;
using Sacks.Configuration;
using Sacks.DataAccess.Data;

namespace SacksMcp.Configuration;

/// <summary>
/// Extension methods for configuring SacksMcp services in any hosting environment
/// (Console, Windows Service, Azure Function, etc.).
/// </summary>
public static class SacksMcpServiceExtensions
{
    /// <summary>
    /// Adds all SacksMcp services including MCP server, database context, and configuration.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="config">The centralized configuration singleton.</param>
    /// <param name="configureLogging">Optional action to configure logging (e.g., for stderr in console apps).</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSacksMcpServices(
        this IServiceCollection services,
        SacksConfigurationOptions config,
        Action<ILoggingBuilder>? configureLogging = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(config);

        // Register configuration singletons
        services.AddSingleton(config);
        services.AddSingleton(config.Database);
        services.AddSingleton(config.McpServer);

        // Configure MCP server with tools from this assembly
        services.AddMcpServer()
            .WithStdioServerTransport()
            .WithToolsFromAssembly(typeof(SacksMcpServiceExtensions).Assembly);

        // Configure SacksDbContext with options from config singleton
        services.AddDbContext<SacksDbContext>(options =>
        {
            var dbOptions = config.Database;
            
            options.UseSqlServer(dbOptions.ConnectionString, sqlOptions =>
            {
                sqlOptions.CommandTimeout(dbOptions.CommandTimeout);
                if (dbOptions.RetryOnFailure)
                {
                    sqlOptions.EnableRetryOnFailure(dbOptions.MaxRetryCount);
                }
            });
            
            if (dbOptions.EnableSensitiveDataLogging)
            {
                options.EnableSensitiveDataLogging();
            }
            
            if (dbOptions.EnableDetailedErrors)
            {
                options.EnableDetailedErrors();
            }
        });

        // Configure MCP server extended options from config singleton
        services.Configure<McpServerExtendedOptions>(options =>
        {
            options.ServerName = config.McpServer.ServerName;
            options.Version = config.McpServer.Version;
            options.MaxConcurrentTools = config.McpServer.MaxConcurrentTools;
            options.ToolTimeoutSeconds = config.McpServer.ToolTimeoutSeconds;
            options.EnableDetailedLogging = config.McpServer.EnableDetailedLogging;
        });

        // Map centralized database options to McpServer.Database.Configuration.DatabaseOptions
        services.Configure<McpServer.Database.Configuration.DatabaseOptions>(options =>
        {
            options.ConnectionString = config.Database.ConnectionString;
            options.Provider = config.Database.Provider;
            options.CommandTimeout = config.Database.CommandTimeout;
            options.EnableSensitiveDataLogging = config.Database.EnableSensitiveDataLogging;
            options.EnableDetailedErrors = config.Database.EnableDetailedErrors;
            options.MaxRetryAttempts = config.Database.MaxRetryCount;
        });

        // Apply custom logging configuration if provided
        if (configureLogging != null)
        {
            services.AddLogging(configureLogging);
        }

        return services;
    }
}
