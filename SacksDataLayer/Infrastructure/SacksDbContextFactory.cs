using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using SacksDataLayer.Data;
using System;

namespace SacksDataLayer.Infrastructure
{
    /// <summary>
    /// Design-time factory for SacksDbContext to support EF Core migrations
    /// </summary>
    public class SacksDbContextFactory : IDesignTimeDbContextFactory<SacksDbContext>
    {
        public SacksDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<SacksDbContext>();
            
            // Use a default connection string for migrations with retry logic
            // This will be overridden in actual application with proper connection string
            optionsBuilder.UseSqlServer(
                "Server=(localdb)\\mssqllocaldb;Database=SacksProductsDb;Trusted_Connection=true;TrustServerCertificate=true;",
                options => {
                    options.MigrationsAssembly("SacksDataLayer");
                    options.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(30),
                        errorNumbersToAdd: null);
                });

            return new SacksDbContext(optionsBuilder.Options);
        }
    }
}
