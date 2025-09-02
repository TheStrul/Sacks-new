using Microsoft.Extensions.DependencyInjection;
using SacksDataLayer.Services.Interfaces;
using SacksDataLayer.Services.Implementations;
using SacksDataLayer.Configuration;

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
            services.AddScoped<IFileProcessingBatchService, FileProcessingBatchService>();
            services.AddScoped<IFileProcessingService, FileProcessingService>();
            return services;
        }

        /// <summary>
        /// Register performance monitoring and structured logging services
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <returns>Service collection for chaining</returns>
        public static IServiceCollection AddPerformanceMonitoring(this IServiceCollection services)
        {
            services.AddSingleton<IPerformanceMonitoringService, PerformanceMonitoringService>();
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
