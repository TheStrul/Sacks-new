using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using McpServer.Core.Configuration;
using McpServer.Database.Configuration;
using Sacks.Configuration;
using SacksDataLayer.Data;

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
    /// <param name="configuration">The configuration instance to read settings from.</param>
    /// <param name="configureLogging">Optional action to configure logging (e.g., for stderr in console apps).</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSacksMcpServices(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<ILoggingBuilder>? configureLogging = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // Configure MCP server with tools from this assembly
        services.AddMcpServer()
            .WithStdioServerTransport()
            .WithToolsFromAssembly(typeof(SacksMcpServiceExtensions).Assembly);

        // Configure SacksDbContext with options from centralized configuration
        services.AddDbContext<SacksDbContext>(options =>
        {
            var dbOptions = configuration.GetOptions<Sacks.Configuration.DatabaseOptions>("Sacks:Database");
            
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

        // Configure MCP server extended options from centralized configuration
        var mcpServerOptions = configuration.GetOptions<Sacks.Configuration.McpServerOptions>("Sacks:McpServer");
        services.Configure<McpServerExtendedOptions>(options =>
        {
            options.ServerName = mcpServerOptions.ServerName;
            options.Version = mcpServerOptions.Version;
            options.MaxConcurrentTools = mcpServerOptions.MaxConcurrentTools;
            options.ToolTimeoutSeconds = mcpServerOptions.ToolTimeoutSeconds;
            options.EnableDetailedLogging = mcpServerOptions.EnableDetailedLogging;
        });

        // Map centralized database options to McpServer.Database.Configuration.DatabaseOptions
        var sacksDbOptions = configuration.GetOptions<Sacks.Configuration.DatabaseOptions>("Sacks:Database");
        services.Configure<McpServer.Database.Configuration.DatabaseOptions>(options =>
        {
            options.ConnectionString = sacksDbOptions.ConnectionString;
            options.Provider = sacksDbOptions.Provider;
            options.CommandTimeout = sacksDbOptions.CommandTimeout;
            options.EnableSensitiveDataLogging = sacksDbOptions.EnableSensitiveDataLogging;
            options.EnableDetailedErrors = sacksDbOptions.EnableDetailedErrors;
            options.MaxRetryAttempts = sacksDbOptions.MaxRetryCount;
        });

        // Apply custom logging configuration if provided
        if (configureLogging != null)
        {
            services.AddLogging(configureLogging);
        }

        return services;
    }
}
