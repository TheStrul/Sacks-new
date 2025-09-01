using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SacksConsoleApp;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SacksDataLayer.Configuration;
using SacksDataLayer.Data;
using SacksDataLayer.Services.Interfaces;
using SacksDataLayer.Services.Implementations;
using SacksDataLayer.Repositories.Interfaces;
using SacksDataLayer.Repositories.Implementations;
using SacksDataLayer.FileProcessing.Interfaces;
using SacksDataLayer.FileProcessing.Services;

namespace SacksConsoleApp
{
    sealed class Program
    {
        private static IConfiguration? _configuration;
        private static ILogger<Program>? _logger;

        static async Task Main(string[] args)
        {
            Console.WriteLine("=== Sacks Product Management System ===");
            Console.WriteLine();

            // Show usage if help requested
            if (args.Length > 0 && (args[0].Equals("help", StringComparison.OrdinalIgnoreCase) || args[0] == "?" || args[0] == "-h"))
            {
                ShowUsage();
                return;
            }

            try
            {
                // Build configuration
                _configuration = BuildConfiguration();

                // Setup dependency injection with configuration and logging
                var services = ConfigureServices(_configuration);
                var serviceProvider = services.BuildServiceProvider();

                // Get logger
                _logger = serviceProvider.GetRequiredService<ILogger<Program>>();
                _logger.LogInformation("Application starting...");

                // Test database connection first
                var connectionService = serviceProvider.GetRequiredService<IDatabaseConnectionService>();
                var (isAvailable, message, exception) = await connectionService.TestConnectionAsync();

                if (!isAvailable)
                {
                    Console.WriteLine($"⚠️  Database Connection Issue: {message}");
                    _logger.LogWarning("Database connection failed: {Message}", message);
                    
                    if (exception != null)
                    {
                        _logger.LogError(exception, "Database connection error details");
                    }
                    
                    Console.WriteLine("🔧 Please check your database configuration in appsettings.json");
                    Console.WriteLine("Press any key to exit...");
                    Console.ReadKey();
                    return;
                }

                Console.WriteLine($"✅ {message}");
                _logger.LogInformation("Database connection successful");

                // Check if command line argument for clearing database
                if (args.Length > 0 && args[0].Equals("clear", StringComparison.OrdinalIgnoreCase))
                {
                    await HandleDatabaseClearCommand(serviceProvider);
                    return;
                }

                // Check if command line argument for thread-safe demo
                if (args.Length > 0 && args[0].Equals("threadsafe", StringComparison.OrdinalIgnoreCase))
                {
                    await DemonstrateThreadSafeProcessing(serviceProvider);
                    return;
                }

                // Check if command line argument for ultimate performance in-memory processing
                if (args.Length > 0 && args[0].Equals("inmemory", StringComparison.OrdinalIgnoreCase))
                {
                    await DemonstrateInMemoryProcessing(serviceProvider, args);
                    return;
                }

                // Check if command line argument for in-memory processing all files
                if (args.Length > 0 && args[0].Equals("inmemoryall", StringComparison.OrdinalIgnoreCase))
                {
                    await ProcessAllFilesInMemory(serviceProvider);
                    return;
                }

                // Process files with our optimized implementation
                await ProcessInputFiles(serviceProvider);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Fatal error: {ex.Message}");
                _logger?.LogCritical(ex, "Fatal application error");
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
            }
        }

        private static IConfiguration BuildConfiguration()
        {
            var environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production";
            
            // Get the directory where the executable is running from (contains appsettings.json)
            var basePath = AppContext.BaseDirectory;
            
            return new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();
        }

        private static ServiceCollection ConfigureServices(IConfiguration configuration)
        {
            var services = new ServiceCollection();

            // Add configuration as singleton
            services.AddSingleton<IConfiguration>(configuration);

            // Add logging
            services.AddLogging(builder =>
            {
                builder.AddConfiguration(configuration.GetSection("Logging"));
                builder.AddConsole();
            });

            // Add configuration options
            services.Configure<DatabaseSettings>(configuration.GetSection("DatabaseSettings"));

            // Get connection string from configuration
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("DefaultConnection string is not configured in appsettings.json");
            }

            // Add DbContext with configuration-based connection string
            services.AddDbContext<SacksDbContext>(options =>
            {
                var dbSettings = configuration.GetSection("DatabaseSettings").Get<DatabaseSettings>() ?? new DatabaseSettings();
                
                options.UseMySql(connectionString, new MySqlServerVersion(new Version(10, 11, 0)), mysqlOptions =>
                {
                    if (dbSettings.RetryOnFailure)
                    {
                        mysqlOptions.EnableRetryOnFailure(dbSettings.MaxRetryCount);
                    }
                    mysqlOptions.CommandTimeout(dbSettings.CommandTimeout);
                });

                if (dbSettings.EnableSensitiveDataLogging)
                {
                    options.EnableSensitiveDataLogging();
                }
            });

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
            services.AddScoped<IDatabaseConnectionService, DatabaseConnectionService>();
            
            // 🚀 PERFORMANCE: Add thread-safe services for high-performance processing
            services.AddScoped<IInMemoryDataService, InMemoryDataService>();
            services.AddScoped<IThreadSafeFileProcessingService, ThreadSafeFileProcessingService>();
            
            // 🚀 ULTIMATE PERFORMANCE: Add in-memory file processing service
            services.AddScoped<IInMemoryFileProcessingService, InMemoryFileProcessingService>();
            
            // Add file processing dependencies
            services.AddScoped<SacksAIPlatform.InfrastructuresLayer.FileProcessing.IFileDataReader, SacksAIPlatform.InfrastructuresLayer.FileProcessing.FileDataReader>();
            services.AddScoped<SupplierConfigurationManager>(provider =>
            {
                var configPath = Path.Combine(AppContext.BaseDirectory, "Configuration", "supplier-formats.json");
                return new SupplierConfigurationManager(configPath);
            });

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

        private static void ShowUsage()
        {
            Console.WriteLine("📖 USAGE:");
            Console.WriteLine("   dotnet run                    - Process all Excel files in Inputs folder");
            Console.WriteLine("   dotnet run clear             - Clear all data from database");
            Console.WriteLine("   dotnet run threadsafe        - 🚀 Demonstrate thread-safe in-memory processing");
            Console.WriteLine("   dotnet run inmemory [file]    - 🚀 ULTIMATE: Process file with in-memory operations & save once");
            Console.WriteLine("   dotnet run inmemoryall        - 🚀 ULTIMATE: Process ALL files with in-memory operations");
            Console.WriteLine("   dotnet run help              - Show this help message");
            Console.WriteLine();
            Console.WriteLine("🚀 NEW FEATURES:");
            Console.WriteLine("   • Thread-safe in-memory data cache for high-performance processing");
            Console.WriteLine("   • Bulk operations to eliminate N+1 query problems");
            Console.WriteLine("   • Optimized batch processing with larger batch sizes");
            Console.WriteLine("   • Safe parallel processing without DbContext concurrency issues");
            Console.WriteLine("   • 🚀 ULTIMATE: In-memory processing with single database save at end");
            Console.WriteLine();
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

        private static async Task DemonstrateThreadSafeProcessing(ServiceProvider serviceProvider)
        {
            Console.WriteLine("=== 🚀 THREAD-SAFE IN-MEMORY DATA SERVICE DEMONSTRATION ===");
            Console.WriteLine();

            try
            {
                // Get the in-memory data service
                var inMemoryService = serviceProvider.GetRequiredService<IInMemoryDataService>();
                var threadSafeProcessor = serviceProvider.GetRequiredService<IThreadSafeFileProcessingService>();

                Console.WriteLine("📊 Demonstrating thread-safe in-memory data loading...");
                
                // Load all data into memory
                var loadStartTime = DateTime.UtcNow;
                await inMemoryService.LoadAllDataAsync();
                var loadTime = DateTime.UtcNow - loadStartTime;

                // Get cache statistics
                var stats = inMemoryService.GetCacheStats();
                Console.WriteLine($"✅ Data loaded in {loadTime.TotalMilliseconds:F0}ms");
                Console.WriteLine($"   📦 Products: {stats.Products:N0}");
                Console.WriteLine($"   🏢 Suppliers: {stats.Suppliers:N0}");
                Console.WriteLine($"   📋 Offers: {stats.Offers:N0}");
                Console.WriteLine($"   🔗 Offer-Products: {stats.OfferProducts:N0}");
                Console.WriteLine($"   ⏰ Last loaded: {stats.LastLoaded:yyyy-MM-dd HH:mm:ss}");

                // Demonstrate thread-safe lookups
                Console.WriteLine("\n🔍 Demonstrating thread-safe data access...");
                
                var allProducts = inMemoryService.GetAllProducts().Take(5).ToList();
                if (allProducts.Any())
                {
                    Console.WriteLine($"   First 5 products:");
                    foreach (var product in allProducts)
                    {
                        Console.WriteLine($"      • {product.Name} (EAN: {product.EAN})");
                    }

                    // Demonstrate bulk EAN lookup
                    var eans = allProducts.Select(p => p.EAN).Where(e => !string.IsNullOrEmpty(e)).ToList();
                    if (eans.Any())
                    {
                        Console.WriteLine($"\n🚀 BULK LOOKUP: Testing bulk EAN lookup for {eans.Count} products...");
                        var bulkStartTime = DateTime.UtcNow;
                        var bulkResults = inMemoryService.GetProductsByEANs(eans);
                        var bulkTime = DateTime.UtcNow - bulkStartTime;
                        Console.WriteLine($"   ✅ Bulk lookup completed in {bulkTime.TotalMilliseconds:F2}ms");
                        Console.WriteLine($"   📊 Found {bulkResults.Count} products");
                    }
                }

                var allSuppliers = inMemoryService.GetAllSuppliers().ToList();
                if (allSuppliers.Any())
                {
                    Console.WriteLine($"\n   Suppliers ({allSuppliers.Count}):");
                    foreach (var supplier in allSuppliers)
                    {
                        Console.WriteLine($"      • {supplier.Name}");
                    }
                }

                // Check if we have test files to process
                var inputsPath = FindInputsFolder();
                if (Directory.Exists(inputsPath))
                {
                    var files = Directory.GetFiles(inputsPath, "*.xlsx")
                                        .Where(f => !Path.GetFileName(f).StartsWith("~"))
                                        .Take(1) // Only process first file for demo
                                        .ToArray();

                    if (files.Length > 0)
                    {
                        Console.WriteLine($"\n🔄 Demonstrating thread-safe file processing with: {Path.GetFileName(files[0])}");
                        await threadSafeProcessor.ProcessSupplierFileThreadSafeAsync(files[0]);
                        
                        // Show updated cache stats
                        var finalStats = inMemoryService.GetCacheStats();
                        Console.WriteLine($"\n📊 Final cache statistics:");
                        Console.WriteLine($"   📦 Products: {finalStats.Products:N0}");
                        Console.WriteLine($"   🏢 Suppliers: {finalStats.Suppliers:N0}");
                        Console.WriteLine($"   📋 Offers: {finalStats.Offers:N0}");
                        Console.WriteLine($"   🔗 Offer-Products: {finalStats.OfferProducts:N0}");
                    }
                    else
                    {
                        Console.WriteLine("\n⚠️  No Excel files found for demonstration");
                    }
                }

                Console.WriteLine("\n✅ Thread-safe demonstration completed!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error during demonstration: {ex.Message}");
                Console.WriteLine($"   Stack trace: {ex.StackTrace}");
            }

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }

        /// <summary>
        /// 🚀 ULTIMATE PERFORMANCE: Demonstrates in-memory file processing with single database save
        /// </summary>
        private static async Task DemonstrateInMemoryProcessing(ServiceProvider serviceProvider, string[] args)
        {
            try
            {
                Console.WriteLine("=== 🚀 ULTIMATE PERFORMANCE: In-Memory File Processing ===\n");

                var inMemoryService = serviceProvider.GetRequiredService<IInMemoryFileProcessingService>();

                // Determine which file to process
                string? filePath = null;
                
                if (args.Length > 1)
                {
                    // Use specified file
                    filePath = args[1];
                    if (!File.Exists(filePath))
                    {
                        // Try relative path from Inputs folder
                        var inputsPath = FindInputsFolder();
                        if (!string.IsNullOrEmpty(inputsPath))
                        {
                            filePath = Path.Combine(inputsPath, args[1]);
                            if (!File.Exists(filePath))
                            {
                                Console.WriteLine($"❌ File not found: {args[1]}");
                                Console.WriteLine("Available files in Inputs folder:");
                                var availableFiles = Directory.GetFiles(inputsPath, "*.xlsx");
                                foreach (var file in availableFiles)
                                {
                                    Console.WriteLine($"   • {Path.GetFileName(file)}");
                                }
                                return;
                            }
                        }
                        else
                        {
                            Console.WriteLine($"❌ File not found: {args[1]}");
                            return;
                        }
                    }
                }
                else
                {
                    // Use first available file from Inputs folder
                    var inputsPath = FindInputsFolder();
                    if (!string.IsNullOrEmpty(inputsPath))
                    {
                        var excelFiles = Directory.GetFiles(inputsPath, "*.xlsx");
                        if (excelFiles.Length > 0)
                        {
                            filePath = excelFiles[0];
                            Console.WriteLine($"📁 Using first available file: {Path.GetFileName(filePath)}");
                        }
                    }
                }

                if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                {
                    Console.WriteLine("❌ No Excel file found to process");
                    Console.WriteLine("Usage: dotnet run inmemory [filename.xlsx]");
                    return;
                }

                // Process the file using in-memory processing
                var result = await inMemoryService.ProcessFileInMemoryAsync(filePath);

                if (result.Success)
                {
                    Console.WriteLine("\n✅ In-memory processing demonstration completed successfully!");
                    Console.WriteLine("\n🎯 KEY BENEFITS OF IN-MEMORY PROCESSING:");
                    Console.WriteLine("   • All data loaded into memory once at start");
                    Console.WriteLine("   • All processing done in-memory (no database calls during processing)");
                    Console.WriteLine("   • Single database transaction at the end");
                    Console.WriteLine("   • Maximum performance and reliability");
                    Console.WriteLine("   • Thread-safe operations");
                }
                else
                {
                    Console.WriteLine($"\n❌ In-memory processing failed: {result.Message}");
                    if (result.Errors.Count > 0)
                    {
                        Console.WriteLine("Errors:");
                        foreach (var error in result.Errors)
                        {
                            Console.WriteLine($"   • {error}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error during in-memory processing demonstration: {ex.Message}");
                Console.WriteLine($"   Stack trace: {ex.StackTrace}");
            }

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
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

        /// <summary>
        /// Processes all input files using the ultimate performance in-memory processing
        /// </summary>
        private static async Task ProcessAllFilesInMemory(ServiceProvider serviceProvider)
        {
            Console.WriteLine("=== 🚀 ULTIMATE PERFORMANCE: Processing ALL Files In-Memory ===\n");

            var inMemoryService = serviceProvider.GetRequiredService<IInMemoryFileProcessingService>();
            var inputsPath = FindInputsFolder();

            if (!Directory.Exists(inputsPath))
            {
                Console.WriteLine($"❌ Inputs folder not found: {inputsPath}");
                return;
            }

            var files = Directory.GetFiles(inputsPath, "*.xlsx")
                                .Where(f => !Path.GetFileName(f).StartsWith("~")) // Skip temp files
                                .ToArray();

            if (files.Length == 0)
            {
                Console.WriteLine("❌ No Excel files found in Inputs folder.");
                return;
            }

            Console.WriteLine($"📁 Found {files.Length} Excel file(s) to process:");
            foreach (var file in files)
            {
                Console.WriteLine($"   - {Path.GetFileName(file)}");
            }
            Console.WriteLine();

            var totalStopwatch = System.Diagnostics.Stopwatch.StartNew();
            var successCount = 0;
            var errorCount = 0;
            var totalProcessedRecords = 0;
            var totalProcessingTime = TimeSpan.Zero;
            var totalSaveTime = TimeSpan.Zero;

            // Process each file
            for (int i = 0; i < files.Length; i++)
            {
                var file = files[i];
                var fileName = Path.GetFileName(file);
                
                Console.WriteLine($"📄 [{i + 1}/{files.Length}] Processing: {fileName}");
                Console.WriteLine(new string('-', 60));

                try
                {
                    var result = await inMemoryService.ProcessFileInMemoryAsync(file);
                    
                    if (result.Success)
                    {
                        successCount++;
                        totalProcessedRecords += result.ProcessedRecords;
                        totalProcessingTime = totalProcessingTime.Add(TimeSpan.FromMilliseconds(result.ProcessingDurationMs));
                        totalSaveTime = totalSaveTime.Add(TimeSpan.FromMilliseconds(result.SaveDataDurationMs));

                        Console.WriteLine($"✅ {fileName} processed successfully!");
                        Console.WriteLine($"   📊 Records: {result.ProcessedRecords:N0}");
                        Console.WriteLine($"   ⏱️ Time: {result.TotalDurationMs}ms");
                    }
                    else
                    {
                        errorCount++;
                        Console.WriteLine($"❌ {fileName} failed:");
                        foreach (var error in result.Errors)
                        {
                            Console.WriteLine($"   • {error}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    errorCount++;
                    Console.WriteLine($"❌ {fileName} failed with exception: {ex.Message}");
                }

                Console.WriteLine();
            }

            totalStopwatch.Stop();

            // Final summary
            Console.WriteLine("🎯 BATCH PROCESSING SUMMARY");
            Console.WriteLine(new string('=', 60));
            Console.WriteLine($"📁 Total files: {files.Length}");
            Console.WriteLine($"✅ Successful: {successCount}");
            Console.WriteLine($"❌ Failed: {errorCount}");
            Console.WriteLine($"📊 Total records processed: {totalProcessedRecords:N0}");
            Console.WriteLine($"⏱️ Total processing time: {totalProcessingTime.TotalSeconds:F1}s");
            Console.WriteLine($"💾 Total database save time: {totalSaveTime.TotalSeconds:F1}s");
            Console.WriteLine($"🏁 Total elapsed time: {totalStopwatch.Elapsed.TotalSeconds:F1}s");
            
            if (successCount > 0)
            {
                var avgProcessingTime = totalProcessingTime.TotalMilliseconds / successCount;
                var avgRecordsPerFile = (double)totalProcessedRecords / successCount;
                Console.WriteLine($"📈 Average per file: {avgProcessingTime:F0}ms, {avgRecordsPerFile:F0} records");
            }

            Console.WriteLine("\n🎉 Batch processing complete!");
            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }
    }
}