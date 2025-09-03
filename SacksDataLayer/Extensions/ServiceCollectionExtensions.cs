using Microsoft.Extensions.DependencyInjection;
using SacksDataLayer.Services.Interfaces;
using SacksDataLayer.Services.Implementations;
using SacksDataLayer.Configuration;
using SacksDataLayer.FileProcessing.Services;

namespace SacksDataLayer.Extensions
{
    /// <summary>
    /// Extension methods for registering perfume-specific services
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Register perfume product services for familiar filtering/sorting with normalization
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <returns>Service collection for chaining</returns>
        public static IServiceCollection AddPerfumeServices(this IServiceCollection services)
        {
            services.AddSingleton<PropertyNormalizer>();
            services.AddScoped<IPerfumeProductService, PerfumeProductService>();
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
