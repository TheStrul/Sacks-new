using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using McpServer.Client.Configuration;

namespace McpServer.Client;

/// <summary>
/// Extension methods for registering MCP client services with dependency injection.
/// </summary>
public static class McpClientServiceExtensions
{
    /// <summary>
    /// Adds HTTP-based MCP client to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">Action to configure MCP client options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddMcpClient(
        this IServiceCollection services,
        Action<McpClientOptions>? configureOptions = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        if (configureOptions != null)
        {
            services.Configure(configureOptions);
        }

        services.AddSingleton<IMcpClient, HttpMcpClient>();
        
        return services;
    }

    /// <summary>
    /// Adds HTTP-based MCP client to the service collection with specific options instance.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="options">The MCP client options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddMcpClient(
        this IServiceCollection services,
        McpClientOptions options)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(options);

        services.AddSingleton<IOptions<McpClientOptions>>(Options.Create(options));
        services.AddSingleton<IMcpClient, HttpMcpClient>();
        
        return services;
    }
}
