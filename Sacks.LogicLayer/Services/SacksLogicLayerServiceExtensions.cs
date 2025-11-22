using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using McpServer.Client;
using McpServer.Client.Configuration;
using McpServer.Client.Llm;
using Sacks.Configuration;
using Sacks.Core.Services.Interfaces;
using Sacks.Core.Services.Implementations;
using Sacks.LogicLayer.Services.Interfaces;
using Sacks.LogicLayer.Services.Implementations;

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
        Sacks.Configuration.McpClientOptions mcpClientOptions,
        Sacks.Configuration.LlmOptions llmOptions)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(mcpClientOptions);
        ArgumentNullException.ThrowIfNull(llmOptions);

        // Register generic MCP client
        services.AddMcpClient(new McpServer.Client.Configuration.McpClientOptions
        {
            ServerUrl = mcpClientOptions.ServerUrl,
            ToolTimeoutSeconds = mcpClientOptions.ToolTimeoutSeconds,
            MaxRetryAttempts = mcpClientOptions.MaxRetryAttempts,
            RetryDelayMilliseconds = mcpClientOptions.RetryDelayMilliseconds
        });

        // Register adapter for backwards compatibility
        services.AddScoped<IMcpClientService, McpClientServiceAdapter>();

        // Register LLM service based on provider
        services.Configure<McpServer.Client.Configuration.LlmOptions>(options =>
        {
            options.Provider = llmOptions.Provider;
            options.Endpoint = llmOptions.Endpoint;
            options.ApiKey = llmOptions.ApiKey;
            options.ModelName = llmOptions.ModelName;
            options.MaxTokens = llmOptions.MaxTokens;
            options.Temperature = llmOptions.Temperature;
            options.TimeoutSeconds = llmOptions.TimeoutSeconds;
        });

        // Register LLM service implementation based on provider
        if (llmOptions.Provider.Equals("GitHub", StringComparison.OrdinalIgnoreCase))
        {
            services.AddScoped<ILlmService, GitHubModelsLlmService>();
        }
        else if (llmOptions.Provider.Equals("None", StringComparison.OrdinalIgnoreCase))
        {
            // No LLM service - would need a NullLlmService or throw error
            throw new InvalidOperationException("LLM provider 'None' is not supported yet. Set Provider to 'GitHub' in configuration.");
        }
        else
        {
            throw new InvalidOperationException($"Unsupported LLM provider: {llmOptions.Provider}. Currently only 'GitHub' is supported.");
        }

        // Register query router that uses LLM service
        services.AddScoped<ILlmQueryRouterService, LlmQueryRouterService>();

        return services;
    }
}
