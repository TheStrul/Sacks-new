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

            // Default connection string for MariaDB (Pomelo)
            var mySqlConnectionString = "Server=localhost;Port=3306;Database=SacksProductsDb;Uid=root;Pwd=;";

            // LocalDB (SQL Server) connection string to make DB visible in Visual Studio SQL Server Object Explorer
            var localDbConnectionString = "Server=(localdb)\\mssqllocaldb;Database=SacksProductsDb;Trusted_Connection=True;";

            // If an argument "localdb" is passed, use SQL Server LocalDB. Otherwise use MariaDB (Pomelo).
            if (args != null && args.Length > 0 && args[0].Equals("localdb", StringComparison.OrdinalIgnoreCase))
            {
                optionsBuilder.UseSqlServer(localDbConnectionString, sqlOptions =>
                {
                    sqlOptions.MigrationsAssembly("SacksDataLayer");
                });
            }
            else
            {
                // Use Pomelo provider which supports ServerVersion.AutoDetect and MariaDB
                optionsBuilder.UseMySql(
                    mySqlConnectionString,
                    ServerVersion.AutoDetect(mySqlConnectionString),
                    options => {
                        options.MigrationsAssembly("SacksDataLayer");
                    });
            }

            return new SacksDbContext(optionsBuilder.Options);
        }
    }
}
