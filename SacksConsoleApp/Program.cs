using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SacksConsoleApp;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SacksDataLayer.Data;
using SacksDataLayer.Services.Interfaces;
using SacksDataLayer.Services.Implementations;
using SacksDataLayer.Repositories.Interfaces;
using SacksDataLayer.Repositories.Implementations;

namespace SacksConsoleApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("=== Sacks Product Management System ===");
            Console.WriteLine();

            // Setup dependency injection
            var services = ConfigureServices();
            var serviceProvider = services.BuildServiceProvider();

            try
            {
                // Check if command line argument for clearing database
                if (args.Length > 0 && args[0].Equals("clear", StringComparison.OrdinalIgnoreCase))
                {
                    await HandleDatabaseClearCommand(serviceProvider);
                    return;
                }

                await ProcessInputFiles(serviceProvider);
            }
            finally
            {
                serviceProvider.Dispose();
            }
        }

        private static ServiceCollection ConfigureServices()
        {
            var services = new ServiceCollection();

            // Add DbContext
            services.AddDbContext<SacksDbContext>(options =>
                options.UseMySql("Server=localhost;Database=SacksDB;User=sacks_user;Password=sacks_password;",
                    new MySqlServerVersion(new Version(10, 11, 0))));

            // Add repositories
            services.AddScoped<IProductsRepository, ProductsRepository>();
            services.AddScoped<ISuppliersRepository, SuppliersRepository>();
            services.AddScoped<ISupplierOffersRepository, SupplierOffersRepository>();
            services.AddScoped<IOfferProductsRepository, OfferProductsRepository>();

            // Add business services
            services.AddScoped<IProductsService, ProductsService>();
            services.AddScoped<ISuppliersService, SuppliersService>();
            services.AddScoped<ISupplierOffersService, SupplierOffersService>();
            services.AddScoped<IOfferProductsService, OfferProductsService>();

            // Add application services
            services.AddScoped<IDatabaseManagementService, DatabaseManagementService>();
            services.AddScoped<IFileProcessingService, FileProcessingService>();

            return services;
        }

        private static async Task HandleDatabaseClearCommand(ServiceProvider serviceProvider)
        {
            var databaseService = serviceProvider.GetRequiredService<IDatabaseManagementService>();

            // First check connection
            Console.WriteLine("🔍 Checking database connection...");
            var connectionResult = await databaseService.CheckConnectionAsync();
            
            if (!connectionResult.CanConnect)
            {
                Console.WriteLine($"❌ {connectionResult.Message}");
                if (connectionResult.Errors.Count > 0)
                {
                    foreach (var error in connectionResult.Errors)
                    {
                        Console.WriteLine($"   Error: {error}");
                    }
                }
                return;
            }

            Console.WriteLine($"✅ {connectionResult.Message}");
            Console.WriteLine($"   {connectionResult.ServerInfo}");

            // Show current table counts
            Console.WriteLine("\n📊 Current table status:");
            foreach (var (table, count) in connectionResult.TableCounts)
            {
                Console.WriteLine($"   {table}: {count:N0} records");
            }

            Console.WriteLine();
            Console.Write("⚠️  Are you sure you want to clear ALL data from the database? (y/N): ");
            var confirmation = Console.ReadLine();
            
            if (confirmation?.ToLower() == "y" || confirmation?.ToLower() == "yes")
            {
                Console.WriteLine("\n🧹 Clearing database...");
                var clearResult = await databaseService.ClearAllDataAsync();

                if (clearResult.Success)
                {
                    Console.WriteLine($"✅ {clearResult.Message}");
                    Console.WriteLine($"⏱️  Operation completed in {clearResult.ElapsedMilliseconds:N0}ms");
                    
                    if (clearResult.DeletedCounts.Count > 0)
                    {
                        Console.WriteLine("\n📋 Deletion summary:");
                        foreach (var (table, count) in clearResult.DeletedCounts)
                        {
                            Console.WriteLine($"   {table}: {count:N0} records deleted");
                        }
                    }
                }
                else
                {
                    Console.WriteLine($"❌ {clearResult.Message}");
                    if (clearResult.Errors.Count > 0)
                    {
                        foreach (var error in clearResult.Errors)
                        {
                            Console.WriteLine($"   Error: {error}");
                        }
                    }
                }
            }
            else
            {
                Console.WriteLine("Operation cancelled.");
            }
        }

        private static async Task ProcessInputFiles(ServiceProvider serviceProvider)
        {
            Console.WriteLine("🚀 Analyzing All Inputs...");
            Console.WriteLine("\n" + new string('=', 50));

            var fileProcessingService = serviceProvider.GetRequiredService<IFileProcessingService>();

            // Get the solution directory using a robust method that works from both VS and command line
            var inputsPath = FindInputsFolder();
            if (Directory.Exists(inputsPath))
            {
                var files = Directory.GetFiles(inputsPath, "*.xlsx")
                                    .Where(f => !Path.GetFileName(f).StartsWith("~")) // Skip temp files
                                    .ToArray();

                if (files.Length > 0)
                {
                    Console.WriteLine($"📁 Found {files.Length} Excel file(s) in Inputs folder:");
                    foreach (var file in files)
                    {
                        Console.WriteLine($"   - {Path.GetFileName(file)}");
                        await fileProcessingService.ProcessFileAsync(file);
                        Console.WriteLine();
                    }
                }
                else
                {
                    Console.WriteLine("❌ No Excel files found in Inputs folder.");
                }
            }
            else
            {
                Console.WriteLine("❌ Inputs folder not found.");
            }

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }

        private static string FindInputsFolder()
        {
            // Try different strategies to find the Inputs folder
            var currentDirectory = Environment.CurrentDirectory;

            // Strategy 1: Check if we're running from project folder (dotnet run)
            var strategy1 = Path.Combine(currentDirectory, "..", "SacksDataLayer", "Inputs");
            if (Directory.Exists(strategy1))
            {
                return Path.GetFullPath(strategy1);
            }

            // Strategy 2: Check if we're running from bin folder (Visual Studio)
            var strategy2 = Path.Combine(currentDirectory, "..", "..", "..", "..", "SacksDataLayer", "Inputs");
            if (Directory.Exists(strategy2))
            {
                return Path.GetFullPath(strategy2);
            }

            // Strategy 3: Search upward for solution file, then go to SacksDataLayer/Inputs
            var searchDir = new DirectoryInfo(currentDirectory);
            while (searchDir != null)
            {
                var solutionFile = searchDir.GetFiles("*.sln").FirstOrDefault();
                if (solutionFile != null)
                {
                    var solutionInputsPath = Path.Combine(searchDir.FullName, "SacksDataLayer", "Inputs");
                    if (Directory.Exists(solutionInputsPath))
                    {
                        return solutionInputsPath;
                    }
                }
                searchDir = searchDir.Parent;
            }

            // Strategy 4: Fallback - return a non-existent path so we can show a helpful error
            return Path.Combine(currentDirectory, "SacksDataLayer", "Inputs");
        }
    }
}