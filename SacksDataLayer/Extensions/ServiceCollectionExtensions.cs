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
            // Register configuration managers as singleton
            services.AddSingleton<ProductPropertyConfigurationManager>();
            services.AddSingleton<PropertyNormalizationConfigurationManager>();
            
            // Register configuration-based services as scoped (they need configuration loaded)
            services.AddScoped<ConfigurationBasedPropertyNormalizer>();
            services.AddScoped<ConfigurationBasedDescriptionPropertyExtractor>();
            
            return services;
        }

        /// <summary>
        /// Adds dynamic product configuration services with specific configuration files
        /// </summary>
        public static IServiceCollection AddDynamicProductServices(
            this IServiceCollection services, 
            string propertyConfigPath, 
            string normalizationConfigPath)
        {
            // Register configuration managers as singleton
            services.AddSingleton<ProductPropertyConfigurationManager>();
            services.AddSingleton<PropertyNormalizationConfigurationManager>();
            
            // Register configuration-based services with factory pattern
            services.AddScoped<ConfigurationBasedPropertyNormalizer>(serviceProvider =>
            {
                var manager = serviceProvider.GetRequiredService<PropertyNormalizationConfigurationManager>();
                var config = manager.LoadConfigurationAsync(normalizationConfigPath).GetAwaiter().GetResult();
                return new ConfigurationBasedPropertyNormalizer(config);
            });
            
            services.AddScoped<ConfigurationBasedDescriptionPropertyExtractor>(serviceProvider =>
            {
                var manager = serviceProvider.GetRequiredService<PropertyNormalizationConfigurationManager>();
                var config = manager.LoadConfigurationAsync(normalizationConfigPath).GetAwaiter().GetResult();
                return new ConfigurationBasedDescriptionPropertyExtractor(config);
            });
            
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
            services.AddScoped<DataNormalizationService>();
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
