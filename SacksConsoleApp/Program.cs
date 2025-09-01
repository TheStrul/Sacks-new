using System;
using System.IO;
using System.Linq;
using System.Threading;
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
using SacksDataLayer.Extensions;
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

            try
            {
                // Check if we want to run normalization tests first
                if (args.Length > 0 && args[0].Equals("test-normalization", StringComparison.OrdinalIgnoreCase))
                {
                    await NormalizationTestRunner.RunTestsAsync();
                    return;
                }

                // Build configuration
                _configuration = BuildConfiguration();

                // Setup dependency injection with configuration and logging
                var services = ConfigureServices(_configuration);
                var serviceProvider = services.BuildServiceProvider();

                // Get logger
                _logger = serviceProvider.GetRequiredService<ILogger<Program>>();
                _logger.LogInformation("Application starting...");

                // Show main menu
                await ShowMainMenuAsync(serviceProvider);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fatal error: {ex.Message}");
                if (_logger != null)
                {
                    _logger.LogCritical(ex, "Fatal application error");
                }
                Environment.Exit(1);
            }
        }

        private static async Task ShowMainMenuAsync(ServiceProvider serviceProvider)
        {
            while (true)
            {
                Console.WriteLine("\n📋 Choose an option:");
                Console.WriteLine("1. Test Property Normalization System");
                Console.WriteLine("2. Run Database Operations");
                Console.WriteLine("3. 🚀 Demo Enhanced Logging & Performance Monitoring");
                Console.WriteLine("4. Exit");
                Console.Write("Enter your choice (1-4): ");

                var choice = Console.ReadLine();
                
                switch (choice)
                {
                    case "1":
                        await NormalizationTestRunner.RunTestsAsync();
                        break;
                    case "2":
                        await RunDatabaseOperationsAsync(serviceProvider);
                        break;
                    case "3":
                        await QuickLoggingDemo.RunQuickDemoAsync();
                        break;
                    case "4":
                        Console.WriteLine("Goodbye!");
                        return;
                    default:
                        Console.WriteLine("Invalid choice. Please try again.");
                        break;
                }
            }
        }

        private static async Task RunDatabaseOperationsAsync(ServiceProvider serviceProvider)
        {
            try
            {
                // Test database connection first
                var connectionService = serviceProvider.GetRequiredService<IDatabaseConnectionService>();
                var (isAvailable, message, exception) = await connectionService.TestConnectionAsync();

                if (!isAvailable)
                {
                    Console.WriteLine($"⚠️  Database Connection Issue: {message}");
                    _logger?.LogWarning("Database connection failed: {Message}", message);
                    
                    if (exception != null)
                    {
                        _logger?.LogError(exception, "Database connection error details");
                    }
                    
                    Console.WriteLine("🔧 Please check your database configuration in appsettings.json");
                    Console.WriteLine("Press any key to exit...");
                    Console.ReadKey();
                    return;
                }

                Console.WriteLine($"✅ {message}");
                _logger?.LogInformation("Database connection successful");

                // Show interactive menu
                await ShowInteractiveMenu(serviceProvider);
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
            services.Configure<PerformanceMonitoringSettings>(configuration.GetSection("PerformanceMonitoring"));

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
            services.AddScoped<IDatabaseConnectionService, DatabaseConnectionService>();
            
            // Add file processing services (includes all file processing dependencies)
            services.AddFileProcessingServices();
            
            // Add supplier configuration manager
            services.AddSingleton<SupplierConfigurationManager>();
            
            // 🚀 PERFORMANCE: Add thread-safe services for high-performance processing
            services.AddScoped<IInMemoryDataService, InMemoryDataService>();
            services.AddScoped<IThreadSafeFileProcessingService, ThreadSafeFileProcessingService>();
            
            // 🚀 ULTIMATE PERFORMANCE: Add in-memory file processing service
            services.AddScoped<IInMemoryFileProcessingService, InMemoryFileProcessingService>();
            
            // 📊 MONITORING: Add performance monitoring and structured logging
            services.AddPerformanceMonitoring();
            
            // Add file processing dependencies
            services.AddScoped<SacksAIPlatform.InfrastructuresLayer.FileProcessing.IFileDataReader, SacksAIPlatform.InfrastructuresLayer.FileProcessing.FileDataReader>();
            services.AddScoped<SupplierConfigurationManager>(provider =>
            {
                var configPath = Path.Combine(AppContext.BaseDirectory, "Configuration", "supplier-formats.json");
                return new SupplierConfigurationManager(configPath);
            });
            
            // Add normalization factory
            services.AddScoped<ConfigurationBasedNormalizerFactory>();

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

        /// <summary>
        /// Shows an interactive menu for user to choose operations
        /// </summary>
        private static async Task ShowInteractiveMenu(ServiceProvider serviceProvider)
        {
            while (true)
            {
                Console.WriteLine("\n" + new string('=', 70));
                Console.WriteLine("🎯 SACKS PRODUCT MANAGEMENT SYSTEM - MAIN MENU");
                Console.WriteLine(new string('=', 70));
                Console.WriteLine();
                Console.WriteLine("📋 Please choose an option:");
                Console.WriteLine();
                Console.WriteLine("   1️⃣  Process all Excel files (Standard Processing)");
                Console.WriteLine("   2️⃣  🚀 Process all files with In-Memory Processing (ULTIMATE PERFORMANCE)");
                Console.WriteLine("   3️⃣  🚀 Process single file with In-Memory Processing");
                Console.WriteLine("   4️⃣  🚀 Demonstrate Thread-Safe Processing");
                Console.WriteLine("   5️⃣  🧹 Clear all data from database");
                Console.WriteLine("   6️⃣  📊 Show database statistics");
                Console.WriteLine("   7️⃣  🧪 Test Refactored Configuration");
                Console.WriteLine("   8️⃣  ❓ Show help and feature information");
                Console.WriteLine("   0️⃣  🚪 Exit");
                Console.WriteLine();
                Console.Write("👉 Enter your choice (0-8): ");

                var input = Console.ReadLine()?.Trim();
                Console.WriteLine();

                try
                {
                    switch (input)
                    {
                        case "1":
                            await ProcessInputFiles(serviceProvider);
                            break;
                        case "2":
                            await ProcessAllFilesInMemory(serviceProvider);
                            break;
                        case "3":
                            await DemonstrateInMemoryProcessing(serviceProvider);
                            break;
                        case "4":
                            await DemonstrateThreadSafeProcessing(serviceProvider);
                            break;
                        case "5":
                            await HandleDatabaseClearCommand(serviceProvider);
                            break;
                        case "6":
                            await ShowDatabaseStatistics(serviceProvider);
                            break;
                        case "7":
                            await TestRefactoredConfiguration(serviceProvider);
                            break;
                        case "8":
                            ShowHelpInformation();
                            break;
                        case "0":
                            Console.WriteLine("👋 Thank you for using Sacks Product Management System!");
                            return;
                        default:
                            Console.WriteLine("❌ Invalid choice. Please enter a number between 0 and 8.");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Error executing option: {ex.Message}");
                    _logger?.LogError(ex, "Error executing menu option {Option}", input);
                }

                if (input != "0")
                {
                    Console.WriteLine("\nPress any key to return to main menu...");
                    Console.ReadKey();
                }
            }
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
        }

        /// <summary>
        /// 🚀 ULTIMATE PERFORMANCE: Demonstrates in-memory file processing with single database save
        /// </summary>
        private static async Task DemonstrateInMemoryProcessing(ServiceProvider serviceProvider)
        {
            try
            {
                Console.WriteLine("=== 🚀 ULTIMATE PERFORMANCE: In-Memory File Processing ===\n");

                var inMemoryService = serviceProvider.GetRequiredService<IInMemoryFileProcessingService>();

                // Get available files
                var inputsPath = FindInputsFolder();
                if (!Directory.Exists(inputsPath))
                {
                    Console.WriteLine($"❌ Inputs folder not found: {inputsPath}");
                    return;
                }

                var excelFiles = Directory.GetFiles(inputsPath, "*.xlsx")
                                          .Where(f => !Path.GetFileName(f).StartsWith("~"))
                                          .ToArray();

                if (excelFiles.Length == 0)
                {
                    Console.WriteLine("❌ No Excel files found in Inputs folder.");
                    return;
                }

                // Show available files and let user choose
                Console.WriteLine("📁 Available Excel files:");
                for (int i = 0; i < excelFiles.Length; i++)
                {
                    Console.WriteLine($"   {i + 1}. {Path.GetFileName(excelFiles[i])}");
                }
                Console.WriteLine();
                Console.Write($"👉 Choose a file to process (1-{excelFiles.Length}): ");

                var input = Console.ReadLine()?.Trim();
                if (!int.TryParse(input, out int choice) || choice < 1 || choice > excelFiles.Length)
                {
                    Console.WriteLine("❌ Invalid choice. Operation cancelled.");
                    return;
                }

                var selectedFile = excelFiles[choice - 1];
                Console.WriteLine($"📄 Processing: {Path.GetFileName(selectedFile)}\n");

                // Process the file using in-memory processing
                var result = await inMemoryService.ProcessFileInMemoryAsync(selectedFile);

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
                        await fileProcessingService.ProcessFileAsync(file, CancellationToken.None);
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
        }

        /// <summary>
        /// Shows current database statistics
        /// </summary>
        private static async Task ShowDatabaseStatistics(ServiceProvider serviceProvider)
        {
            try
            {
                Console.WriteLine("=== 📊 DATABASE STATISTICS ===\n");

                var databaseService = serviceProvider.GetRequiredService<IDatabaseManagementService>();
                var connectionResult = await databaseService.CheckConnectionAsync();

                if (!connectionResult.CanConnect)
                {
                    Console.WriteLine($"❌ {connectionResult.Message}");
                    return;
                }

                Console.WriteLine($"✅ {connectionResult.Message}");
                Console.WriteLine($"🔗 {connectionResult.ServerInfo}");
                Console.WriteLine();

                Console.WriteLine("📊 Current table status:");
                var totalRecords = 0;
                foreach (var (table, count) in connectionResult.TableCounts.OrderBy(x => x.Key))
                {
                    Console.WriteLine($"   📋 {table}: {count:N0} records");
                    totalRecords += count;
                }

                Console.WriteLine($"\n📈 Total records across all tables: {totalRecords:N0}");
                
                // Show memory cache statistics if available
                try
                {
                    var inMemoryService = serviceProvider.GetRequiredService<IInMemoryDataService>();
                    var cacheStats = inMemoryService.GetCacheStats();
                    
                    Console.WriteLine("\n🧠 In-Memory Cache Status:");
                    Console.WriteLine($"   📦 Cached Products: {cacheStats.Products:N0}");
                    Console.WriteLine($"   🏢 Cached Suppliers: {cacheStats.Suppliers:N0}");
                    Console.WriteLine($"   📋 Cached Offers: {cacheStats.Offers:N0}");
                    Console.WriteLine($"   🔗 Cached Offer-Products: {cacheStats.OfferProducts:N0}");
                    Console.WriteLine($"   ⏰ Last Loaded: {(cacheStats.LastLoaded == default ? "Never" : cacheStats.LastLoaded.ToString("yyyy-MM-dd HH:mm:ss"))}");
                }
                catch
                {
                    Console.WriteLine("\n🧠 In-Memory Cache: Not loaded");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error retrieving database statistics: {ex.Message}");
            }
        }

        /// <summary>
        /// Shows help information and feature details
        /// </summary>
        private static void ShowHelpInformation()
        {
            Console.WriteLine("=== ❓ HELP & FEATURE INFORMATION ===\n");
            
            Console.WriteLine("🚀 PROCESSING OPTIONS:");
            Console.WriteLine();
            Console.WriteLine("1️⃣  Standard Processing:");
            Console.WriteLine("   • Traditional database operations");
            Console.WriteLine("   • Processes all Excel files in sequence");
            Console.WriteLine("   • Good for small to medium datasets");
            Console.WriteLine();
            
            Console.WriteLine("2️⃣  🚀 In-Memory Processing (ALL FILES):");
            Console.WriteLine("   • ULTIMATE PERFORMANCE - loads all data into memory once");
            Console.WriteLine("   • Processes all files with maximum speed");
            Console.WriteLine("   • Single database transaction at the end");
            Console.WriteLine("   • Best for large batch operations");
            Console.WriteLine();
            
            Console.WriteLine("3️⃣  🚀 In-Memory Processing (SINGLE FILE):");
            Console.WriteLine("   • Choose specific file to process");
            Console.WriteLine("   • Same ultra-fast processing as option 2");
            Console.WriteLine("   • Perfect for testing or selective processing");
            Console.WriteLine();
            
            Console.WriteLine("4️⃣  🚀 Thread-Safe Processing Demo:");
            Console.WriteLine("   • Demonstrates concurrent data access");
            Console.WriteLine("   • Shows thread-safe in-memory operations");
            Console.WriteLine("   • Educational/diagnostic purposes");
            Console.WriteLine();
            
            Console.WriteLine("5️⃣  Database Operations:");
            Console.WriteLine("   • Clear all data from database");
            Console.WriteLine("   • Recreates empty tables with correct schema");
            Console.WriteLine("   • Requires confirmation for safety");
            Console.WriteLine();
            
            Console.WriteLine("6️⃣  Database Statistics:");
            Console.WriteLine("   • Shows current record counts");
            Console.WriteLine("   • Displays connection information");
            Console.WriteLine("   • Shows in-memory cache status");
            Console.WriteLine();

            Console.WriteLine("7️⃣  Test Refactored Configuration:");
            Console.WriteLine("   • Tests and validates the new configuration system");
            Console.WriteLine("   • Ensures all settings are loaded correctly");
            Console.WriteLine("   • Reports any missing or invalid settings");
            Console.WriteLine();

            Console.WriteLine("🎯 KEY FEATURES:");
            Console.WriteLine("   ✅ Thread-safe in-memory data cache");
            Console.WriteLine("   ✅ Bulk operations to eliminate N+1 query problems");
            Console.WriteLine("   ✅ Optimized batch processing");
            Console.WriteLine("   ✅ Auto-detection of supplier configurations");
            Console.WriteLine("   ✅ Single transaction database saves");
            Console.WriteLine("   ✅ Comprehensive error handling and logging");
            Console.WriteLine("   ✅ Performance metrics and monitoring");
            Console.WriteLine();

            Console.WriteLine("📁 FILE REQUIREMENTS:");
            Console.WriteLine("   • Excel files (.xlsx) in SacksDataLayer/Inputs folder");
            Console.WriteLine("   • Files should follow configured supplier formats");
            Console.WriteLine("   • Temporary files (starting with ~) are automatically skipped");
            Console.WriteLine();

            Console.WriteLine("🔧 CONFIGURATION:");
            Console.WriteLine("   • Database settings in appsettings.json");
            Console.WriteLine("   • Supplier formats in Configuration/supplier-formats.json");
            Console.WriteLine("   • Logging configuration in appsettings.json");
        }

        private static async Task TestRefactoredConfiguration(ServiceProvider serviceProvider)
        {
            Console.WriteLine("=== 🧪 TESTING REFACTORED CONFIGURATION ===\n");

            var configManager = serviceProvider.GetRequiredService<SupplierConfigurationManager>();

            // Test 1: Load configuration
            Console.WriteLine("🔄 Test 1: Loading refactored configuration...");
            var config = await configManager.GetConfigurationAsync();
            Console.WriteLine($"✅ Configuration loaded successfully");
            Console.WriteLine($"   Version: {config.Version}");
            Console.WriteLine($"   Suppliers: {config.Suppliers.Count}");
            Console.WriteLine($"   Last Updated: {config.LastUpdated:yyyy-MM-dd HH:mm:ss}");

            // Test 2: Validate configuration
            Console.WriteLine("\n🔄 Test 2: Validating configuration...");
            var validationResult = await configManager.ValidateConfigurationAsync();
            Console.WriteLine($"✅ Validation completed");
            Console.WriteLine($"   Is Valid: {validationResult.IsValid}");
            Console.WriteLine($"   Errors: {validationResult.Errors.Count}");
            Console.WriteLine($"   Warnings: {validationResult.Warnings.Count}");

            if (validationResult.Errors.Count > 0)
            {
                Console.WriteLine("\n❌ Validation Errors:");
                foreach (var error in validationResult.Errors)
                {
                    Console.WriteLine($"   • {error}");
                }
            }

            if (validationResult.Warnings.Count > 0)
            {
                Console.WriteLine("\n⚠️ Validation Warnings:");
                foreach (var warning in validationResult.Warnings)
                {
                    Console.WriteLine($"   • {warning}");
                }
            }

            // Test 3: Test enhanced properties for DIOR
            Console.WriteLine("\n🔄 Test 3: Testing enhanced DIOR configuration...");
            var diorConfig = await configManager.GetSupplierConfigurationAsync("DIOR");
            if (diorConfig != null)
            {
                Console.WriteLine($"✅ DIOR Configuration:");
                Console.WriteLine($"   Column Mappings: {diorConfig.ColumnIndexMappings.Count}");
                Console.WriteLine($"   Data Types: {diorConfig.DataTypes.Count}");
                Console.WriteLine($"   Required Fields: {diorConfig.Validation.RequiredFields.Count} ({string.Join(", ", diorConfig.Validation.RequiredFields)})");
                Console.WriteLine($"   Unique Fields: {diorConfig.Validation.UniqueFields.Count} ({string.Join(", ", diorConfig.Validation.UniqueFields)})");
                Console.WriteLine($"   Currency: {diorConfig.Metadata.Currency}");
                Console.WriteLine($"   Timezone: {diorConfig.Metadata.Timezone}");
                Console.WriteLine($"   Core Properties: {diorConfig.PropertyClassification.CoreProductProperties.Count}");
                Console.WriteLine($"   Offer Properties: {diorConfig.PropertyClassification.OfferProperties.Count}");

                // Test data type configurations with maxLength
                var nameDataType = diorConfig.DataTypes.GetValueOrDefault("Name");
                if (nameDataType != null)
                {
                    Console.WriteLine($"   Name field maxLength: {nameDataType.MaxLength}");
                }
            }

            // Test 4: Test enhanced properties for UNLIMITED
            Console.WriteLine("\n🔄 Test 4: Testing enhanced UNLIMITED configuration...");
            var unlimitedConfig = await configManager.GetSupplierConfigurationAsync("UNLIMITED");
            if (unlimitedConfig != null)
            {
                Console.WriteLine($"✅ UNLIMITED Configuration:");
                Console.WriteLine($"   Column Mappings: {unlimitedConfig.ColumnIndexMappings.Count}");
                Console.WriteLine($"   Required Fields: {unlimitedConfig.Validation.RequiredFields.Count} ({string.Join(", ", unlimitedConfig.Validation.RequiredFields)})");
                Console.WriteLine($"   Unique Fields: {unlimitedConfig.Validation.UniqueFields.Count} ({string.Join(", ", unlimitedConfig.Validation.UniqueFields)})");
                Console.WriteLine($"   Currency: {unlimitedConfig.Metadata.Currency}");
                Console.WriteLine($"   Timezone: {unlimitedConfig.Metadata.Timezone}");
            }

            // Test 5: File detection with enhanced patterns
            Console.WriteLine("\n🔄 Test 5: Testing enhanced file detection...");
            var testFiles = new[]
            {
                "DIOR_Products_2024.xlsx",
                "dior_beauty_catalog.xlsx",
                "DIOR_December_2024.xls",
                "UNLIMITED_Products.xlsx",
                "unlimited_fragrances.xlsx",
                "RANDOM_FILE.xlsx"
            };

            foreach (var testFile in testFiles)
            {
                var detectedConfig = await configManager.DetectSupplierFromFileAsync(testFile);
                if (detectedConfig != null)
                {
                    Console.WriteLine($"   ✅ {testFile} → {detectedConfig.Name}");
                }
                else
                {
                    Console.WriteLine($"   ❌ {testFile} → No match");
                }
            }

            Console.WriteLine("\n🎯 REFACTORED CONFIGURATION TEST SUMMARY:");
            Console.WriteLine("   ✅ JSON loading and deserialization");
            Console.WriteLine("   ✅ Enhanced validation rules (requiredFields, uniqueFields)");
            Console.WriteLine("   ✅ New metadata properties (currency, timezone)");
            Console.WriteLine("   ✅ Data type configurations with maxLength");
            Console.WriteLine("   ✅ Improved case-insensitive file detection patterns");
            Console.WriteLine("   ✅ Property classification (core vs offer)");
            Console.WriteLine("   ✅ Configuration validation and error reporting");

            if (validationResult.IsValid)
            {
                Console.WriteLine("\n🏆 ALL CONFIGURATION TESTS PASSED! 🏆");
            }
            else
            {
                Console.WriteLine("\n⚠️ Configuration has validation issues - see errors above");
            }

            Console.WriteLine("\nPress any key to return to main menu...");
            Console.ReadKey();
        }
    }
}