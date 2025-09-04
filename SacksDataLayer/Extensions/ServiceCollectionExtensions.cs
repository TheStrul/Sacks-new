using Microsoft.Extensions.DependencyInjection;
using SacksDataLayer.Services.Interfaces;
using SacksDataLayer.Services.Implementations;
using SacksDataLayer.Configuration;
using SacksDataLayer.FileProcessing.Services;

namespace SacksDataLayer.Extensions
{
    /// <summary>
    /// Extension methods for registering dynamic product services
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds dynamic product configuration services to the service collection
        /// </summary>
        public static IServiceCollection AddDynamicProductServices(this IServiceCollection services)
        {
            // Register configuration manager as singleton
            services.AddSingleton<ProductPropertyConfigurationManager>();
            
            // Register property normalizer as singleton (it's stateless)
            services.AddSingleton<PropertyNormalizer>();
            
            return services;
        }

        /// <summary>
        /// Register file processing services for handling file imports and processing
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <returns>Service collection for chaining</returns>
        public static IServiceCollection AddFileProcessingServices(this IServiceCollection services)
        {
            services.AddScoped<IFileValidationService, FileValidationService>();
            services.AddScoped<ISupplierConfigurationService, SupplierConfigurationService>();
            services.AddScoped<IFileProcessingDatabaseService, FileProcessingDatabaseService>();
            services.AddScoped<IFileProcessingService, FileProcessingService>();
            services.AddScoped<SupplierConfigurationManager>();
            return services;
        }


        /// <summary>
        /// Register Unit of Work for transaction management across repositories
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <returns>Service collection for chaining</returns>
        public static IServiceCollection AddUnitOfWork(this IServiceCollection services)
        {
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            return services;
        }
    }
}
