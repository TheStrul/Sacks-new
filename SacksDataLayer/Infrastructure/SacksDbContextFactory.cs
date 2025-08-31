using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using SacksDataLayer.Data;
using System;
using System.IO;

namespace SacksDataLayer.Infrastructure
{
    /// <summary>
    /// Design-time factory for SacksDbContext to support EF Core migrations
    /// Uses configuration files to get connection strings
    /// </summary>
    public class SacksDbContextFactory : IDesignTimeDbContextFactory<SacksDbContext>
    {
        public SacksDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<SacksDbContext>();

            // Build configuration from appsettings.json files
            var configuration = new ConfigurationBuilder()
                .SetBasePath(GetConfigurationBasePath())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true)
                .Build();

            // Get connection string from configuration
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException("DefaultConnection connection string is not configured in appsettings.json");
            }

            // Use Pomelo provider which supports ServerVersion.AutoDetect and MariaDB
            optionsBuilder.UseMySql(
                connectionString,
                ServerVersion.AutoDetect(connectionString),
                options => {
                    options.MigrationsAssembly("SacksDataLayer");
                });

            return new SacksDbContext(optionsBuilder.Options);
        }

        private string GetConfigurationBasePath()
        {
            // Try to find the console app directory where appsettings.json is located
            var currentDirectory = Directory.GetCurrentDirectory();
            
            // If we're in SacksDataLayer, go up to find SacksConsoleApp
            if (currentDirectory.EndsWith("SacksDataLayer"))
            {
                var parentDir = Directory.GetParent(currentDirectory)?.FullName;
                var consoleAppDir = Path.Combine(parentDir ?? currentDirectory, "SacksConsoleApp");
                if (Directory.Exists(consoleAppDir))
                {
                    return consoleAppDir;
                }
            }
            
            // Try relative paths
            var possiblePaths = new[]
            {
                Path.Combine(currentDirectory, "..", "SacksConsoleApp"),
                Path.Combine(currentDirectory, "SacksConsoleApp"),
                currentDirectory
            };

            foreach (var path in possiblePaths)
            {
                var fullPath = Path.GetFullPath(path);
                if (File.Exists(Path.Combine(fullPath, "appsettings.json")))
                {
                    return fullPath;
                }
            }

            // Fallback to current directory
            return currentDirectory;
        }
    }
}
