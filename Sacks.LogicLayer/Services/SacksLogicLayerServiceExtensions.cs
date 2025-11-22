using Microsoft.Extensions.DependencyInjection;
using Sacks.Configuration;

namespace Sacks.LogicLayer.Services;

/// <summary>
/// Extension methods for configuring SacksLogicLayer services with centralized configuration.
/// </summary>
public static class SacksLogicLayerServiceExtensions
{
    /// <summary>
    /// Registers SacksLogicLayer services using centralized Sacks configuration.
    /// </summary>
    public static IServiceCollection AddSacksLogicLayerServices(
        this IServiceCollection services,
        McpClientOptions mcpClientOptions)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(mcpClientOptions);

        // Register McpClient options singleton
        services.AddSingleton(mcpClientOptions);

        // Register logic layer services
        services.AddScoped<Interfaces.IMcpClientService, Implementations.McpClientService>();

        return services;
    }
}
