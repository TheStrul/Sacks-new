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
    }
}
