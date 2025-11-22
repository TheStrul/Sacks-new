using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using McpServer.Transport.Http;

namespace McpServer.Transport;

/// <summary>
/// Extension methods for registering transport layers with DI.
/// </summary>
public static class TransportServiceExtensions
{
    /// <summary>
    /// Adds HTTP transport layer to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">Action to configure HTTP transport options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddHttpTransport(
        this IServiceCollection services,
        Action<HttpTransportOptions>? configureOptions = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        if (configureOptions != null)
        {
            services.Configure(configureOptions);
        }

        services.AddSingleton<ITransportLayer, HttpTransportLayer>();
        
        return services;
    }

    /// <summary>
    /// Adds HTTP transport layer to the service collection with specific options instance.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="options">The HTTP transport options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddHttpTransport(
        this IServiceCollection services,
        HttpTransportOptions options)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(options);

        services.AddSingleton<IOptions<HttpTransportOptions>>(Options.Create(options));
        services.AddSingleton<ITransportLayer, HttpTransportLayer>();
        
        return services;
    }
}
