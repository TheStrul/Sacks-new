using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Sacks.Configuration;
using Sacks.Core.Repositories.Interfaces;
using Sacks.Core.Services.Interfaces;
using Sacks.DataAccess.Data;
using Sacks.DataAccess.Repositories.Implementations;
using Sacks.DataAccess.Services;

namespace Sacks.DataAccess.Extensions;

/// <summary>
/// Extension methods for configuring data access services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Sacks data access services (DbContext, repositories, infrastructure services)
    /// </summary>
    public static IServiceCollection AddSacksDataAccess(this IServiceCollection services, DatabaseOptions dbOptions)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(dbOptions);

        // Add DbContext
        services.AddDbContext<SacksDbContext>(options =>
        {
            options.UseSqlServer(dbOptions.ConnectionString, sqlOptions =>
            {
                if (dbOptions.RetryOnFailure)
                {
                    sqlOptions.EnableRetryOnFailure(dbOptions.MaxRetryCount);
                }
                sqlOptions.CommandTimeout(dbOptions.CommandTimeout);
            });

            if (dbOptions.EnableSensitiveDataLogging)
            {
                options.EnableSensitiveDataLogging();
            }
        });

        // Add repositories
        services.AddScoped<ITransactionalProductsRepository, TransactionalProductsRepository>();
        services.AddScoped<ITransactionalSuppliersRepository, TransactionalSuppliersRepository>();
        services.AddScoped<ITransactionalSupplierOffersRepository, TransactionalSupplierOffersRepository>();
        services.AddScoped<ITransactionalOfferProductsRepository, TransactionalOfferProductsRepository>();

        // Add infrastructure services (services that directly use DbContext)
        services.AddScoped<IDatabaseConnectionService, DatabaseConnectionService>();
        services.AddScoped<IDatabaseManagementService, DatabaseManagementService>();
        services.AddScoped<IQueryBuilderService, QueryBuilderService>();
        services.AddScoped<IProductOffersQueryService, ProductOffersQueryService>();
        services.AddScoped<IOfferProductDataService, OfferProductDataService>();
        services.AddScoped<IFileProcessingDatabaseService, FileProcessingDatabaseService>();

        return services;
    }
}
