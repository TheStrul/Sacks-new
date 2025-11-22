using Microsoft.Extensions.DependencyInjection;

using Sacks.Core.Repositories.Interfaces;
using Sacks.Core.Services.Interfaces;
using Sacks.LogicLayer.Services;
using Sacks.LogicLayer.Services.Implementations;
using Sacks.Core.Services.Interfaces;
using SacksAIPlatform.InfrastructuresLayer.FileProcessing;
using SacksAIPlatform.InfrastructuresLayer.FileProcessing.Services;

namespace Sacks.LogicLayer.Extensions;

/// <summary>
/// Extension methods for configuring business logic services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Sacks business logic services
    /// </summary>
    public static IServiceCollection AddSacksLogicLayer(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Add Unit of Work
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Add business services
        services.AddScoped<IProductsService, ProductsService>();
        services.AddScoped<ISuppliersService, SuppliersService>();
        services.AddScoped<ISupplierOffersService, SupplierOffersService>();
        services.AddScoped<IOfferProductsService, OfferProductsService>();

        // Add application/orchestration services
        services.AddScoped<IFileDataReader, FileDataReader>();
        services.AddScoped<SubtitleRowProcessor>();
        services.AddScoped<SupplierConfigurationManager>();
        services.AddScoped<ISupplierConfigurationService, SupplierConfigurationService>();
        services.AddScoped<IFileProcessingService, FileProcessingService>();

        // Add query and grid management services
        services.AddScoped<IGridStateManagementService, GridStateManagementService>();

        // Add MCP client service
        services.AddSingleton<IMcpClientService, McpClientService>();

        // Add LLM query router service
        services.AddSingleton<ILlmQueryRouterService, HeuristicQueryRouterService>();

        return services;
    }
}
