using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sacks.Configuration;

namespace SacksLogicLayer.Services;

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
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // Map centralized McpClient options to the legacy McpClientOptions
        var mcpClientConfig = configuration.GetOptions<McpClientOptions>("Sacks:McpClient");
        
        services.Configure<Implementations.McpClientOptions>(options =>
        {
            options.ServerExecutablePath = mcpClientConfig.ServerExecutablePath;
            options.ServerArguments = mcpClientConfig.ServerArguments;
            options.ServerWorkingDirectory = mcpClientConfig.ServerWorkingDirectory;
            options.ToolTimeoutSeconds = mcpClientConfig.ToolTimeoutSeconds;
        });

        // Register other logic layer services here as needed
        services.AddScoped<Interfaces.IMcpClientService, Implementations.McpClientService>();

        return services;
    }
}
