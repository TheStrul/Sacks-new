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

            // MariaDB connection string
            var mySqlConnectionString = "Server=localhost;Port=3306;Database=SacksProductsDb;Uid=root;Pwd=;";

            // Use Pomelo provider which supports ServerVersion.AutoDetect and MariaDB
            optionsBuilder.UseMySql(
                mySqlConnectionString,
                ServerVersion.AutoDetect(mySqlConnectionString),
                options => {
                    options.MigrationsAssembly("SacksDataLayer");
                });

            return new SacksDbContext(optionsBuilder.Options);
        }
    }
}
