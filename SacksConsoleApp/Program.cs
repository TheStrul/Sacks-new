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
using Serilog;
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
                // Build configuration
                _configuration = BuildConfiguration();

                // Initialize Serilog
                Log.Logger = new LoggerConfiguration()
                    .ReadFrom.Configuration(_configuration)
                    .CreateLogger();

                Log.Information("🚀 Sacks Product Management System starting up...");

                // Setup dependency injection with configuration and logging
                var services = ConfigureServices(_configuration);
                var serviceProvider = services.BuildServiceProvider();

                // Get logger
                _logger = serviceProvider.GetRequiredService<ILogger<Program>>();

                // Go directly to database operations (main menu)
                await RunDatabaseOperationsAsync(serviceProvider);
                
                Log.Information("🛑 Sacks Product Management System shutting down normally");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fatal error: {ex.Message}");
                Log.Fatal(ex, "💥 Fatal application error");
                if (_logger != null)
                {
                    _logger.LogCritical(ex, "Fatal application error");
                }
                Environment.Exit(1);
            }
            finally
            {
                // Ensure all logs are flushed
                Log.CloseAndFlush();
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

                _logger?.LogDebug($"✅ {message}");
                _logger?.LogDebug("Database connection successful");

                // Show the main interactive menu
                await ShowMainMenuAsync(serviceProvider);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Fatal error: {ex.Message}");
                _logger?.LogCritical(ex, "Fatal application error");
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
            }
        }

        /// <summary>
        /// Shows the main interactive menu for user to choose operations
        /// </summary>
        private static async Task ShowMainMenuAsync(ServiceProvider serviceProvider)
        {
            while (true)
            {
                Console.WriteLine("\n" + new string('=', 70));
                Console.WriteLine("🎯 SACKS PRODUCT MANAGEMENT SYSTEM - MAIN MENU");
                Console.WriteLine(new string('=', 70));
                Console.WriteLine();
                Console.WriteLine("📋 Please choose an option:");
                Console.WriteLine();
                Console.WriteLine("   1️⃣  Process all Excel files");
                Console.WriteLine("   2️⃣  🧹 Clear all data from database");
                Console.WriteLine("   3️⃣  📊 Show database statistics");
                Console.WriteLine("   4️⃣  🧪 Test Configuration");
                Console.WriteLine("   5️⃣  🔧 Create new supplier configuration (Interactive)");
                Console.WriteLine("   6️⃣  ❓ Show help and feature information"); 
                Console.WriteLine("   7️⃣  🚪 Exit");                
                Console.WriteLine();
                Console.Write("👉 Enter your choice (1-7, or 0 to exit): ");

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
                            await HandleDatabaseClearCommand(serviceProvider);
                            break;
                        case "3":
                            await ShowDatabaseStatistics(serviceProvider);
                            break;
                        case "4":
                            await TestConfiguration(serviceProvider);
                            break;
                        case "5":
                            await CreateSupplierConfigurationInteractively(serviceProvider);
                            break;
                        case "6":
                            ShowHelpInformation();
                            break;
                        case "7":
                        case "0":
                            Console.WriteLine("👋 Thank you for using Sacks Product Management System!");
                            return;
                        default:
                            Console.WriteLine("❌ Invalid choice. Please enter a number between 1-7 (or 0 to exit).");
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

        private static async Task TestConfiguration(ServiceProvider serviceProvider)
        {
            Console.WriteLine("=== 🧪 TESTING CONFIGURATION ===\n");

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

            // Test 3: Test all supplier configurations
            Console.WriteLine("\n🔄 Test 3: Testing all supplier configurations...");
            if (config.Suppliers.Count > 0)
            {
                for (int i = 0; i < config.Suppliers.Count; i++)
                {
                    var supplierConfig = config.Suppliers[i];
                    Console.WriteLine($"\n✅ {i + 1}. {supplierConfig.Name} Configuration:");
                    Console.WriteLine($"   Column Properties: {supplierConfig.ColumnProperties?.Count ?? 0}");
                    Console.WriteLine($"   Column Mappings: {supplierConfig.ColumnIndexMappings?.Count ?? 0}");
                    Console.WriteLine($"   Data Types: {supplierConfig.DataTypes?.Count ?? 0}");
                    Console.WriteLine($"   Required Fields: {supplierConfig.Validation?.RequiredFields.Count ?? 0} ({string.Join(", ", supplierConfig.Validation?.RequiredFields ?? new List<string>())})");
                    Console.WriteLine($"   File Patterns: {string.Join(", ", supplierConfig.Detection?.FileNamePatterns ?? new List<string>())}");
                    Console.WriteLine($"   Industry: {supplierConfig.Metadata?.Industry}");
                    Console.WriteLine($"   Currency: {supplierConfig.Metadata?.Currency}");
                    Console.WriteLine($"   Header Row: {supplierConfig.FileStructure?.HeaderRowIndex}");
                    Console.WriteLine($"   Data Start Row: {supplierConfig.FileStructure?.DataStartRowIndex}");
                }
            }
            else
            {
                Console.WriteLine("❌ No supplier configurations found");
            }

            Console.WriteLine("\n✅ Configuration testing completed!");
        }

        private static async Task CreateSupplierConfigurationInteractively(ServiceProvider serviceProvider)
        {
            Console.WriteLine("=== 🔧 INTERACTIVE SUPPLIER CONFIGURATION CREATOR ===\n");

            var configManager = serviceProvider.GetRequiredService<SupplierConfigurationManager>();

            try
            {
                // Step 1: Show unmatched files in the Inputs folder
                var inputsPath = FindInputsFolder();
                if (!Directory.Exists(inputsPath))
                {
                    Console.WriteLine($"❌ Inputs folder not found at: {inputsPath}");
                    return;
                }

                Console.WriteLine("� Scanning for files without supplier configurations...\n");
                var unmatchedFiles = await configManager.GetUnmatchedFilesAsync(inputsPath);

                if (unmatchedFiles.Count == 0)
                {
                    Console.WriteLine("✅ All Excel files in the Inputs folder already have matching supplier configurations!");
                    Console.WriteLine("💡 To create a configuration for a new file, place it in the Inputs folder first.");
                    return;
                }

                Console.WriteLine($"� Found {unmatchedFiles.Count} file(s) without matching supplier configurations:");
                Console.WriteLine();
                
                for (int i = 0; i < unmatchedFiles.Count; i++)
                {
                    var fileName = Path.GetFileName(unmatchedFiles[i]);
                    Console.WriteLine($"   {i + 1}. {fileName}");
                }
                Console.WriteLine($"   {unmatchedFiles.Count + 1}. Enter custom file path");

                // Step 2: Let user choose which file to configure
                Console.WriteLine();
                Console.Write($"👉 Choose file to configure (1-{unmatchedFiles.Count + 1}): ");
                
                var choice = Console.ReadLine()?.Trim();
                string excelFilePath;

                if (int.TryParse(choice, out int fileIndex))
                {
                    if (fileIndex >= 1 && fileIndex <= unmatchedFiles.Count)
                    {
                        // User selected one of the unmatched files
                        excelFilePath = unmatchedFiles[fileIndex - 1];
                        Console.WriteLine($"✅ Selected file: {Path.GetFileName(excelFilePath)}");
                    }
                    else if (fileIndex == unmatchedFiles.Count + 1)
                    {
                        // User chose custom file path
                        Console.Write("✏️  Enter full path to Excel file: ");
                        excelFilePath = Console.ReadLine()?.Trim() ?? "";
                        
                        if (string.IsNullOrWhiteSpace(excelFilePath) || !File.Exists(excelFilePath))
                        {
                            Console.WriteLine("❌ Invalid file path or file not found.");
                            return;
                        }
                    }
                    else
                    {
                        Console.WriteLine("❌ Invalid choice.");
                        return;
                    }
                }
                else
                {
                    Console.WriteLine("❌ Invalid choice. Please enter a number.");
                    return;
                }

                Console.WriteLine();

                // Step 3: Create supplier configuration interactively
                var newSupplierConfig = await configManager.CreateSupplierConfigurationInteractivelyAsync(excelFilePath);

                // Step 4: Add to existing configuration
                Console.WriteLine("💾 Saving Configuration:");
                var success = await configManager.AddSupplierConfigurationAsync(newSupplierConfig, saveToFile: true);

                if (success)
                {
                    Console.WriteLine();
                    Console.WriteLine("🎉 SUCCESS! Supplier configuration created and saved.");
                    Console.WriteLine();
                    Console.WriteLine("📋 Configuration Summary:");
                    Console.WriteLine($"   • Supplier Name: {newSupplierConfig.Name}");
                    Console.WriteLine($"   • Column Mappings: {newSupplierConfig.ColumnProperties?.Count ?? 0}");
                    Console.WriteLine($"   • File Patterns: {newSupplierConfig.Detection?.FileNamePatterns?.Count ?? 0}");
                    Console.WriteLine($"   • Header Row: {newSupplierConfig.FileStructure?.HeaderRowIndex}");
                    Console.WriteLine($"   • Data Start Row: {newSupplierConfig.FileStructure?.DataStartRowIndex}");
                    Console.WriteLine();
                    Console.WriteLine("💡 You can now process files from this supplier using option 1!");
                }
                else
                {
                    Console.WriteLine("❌ Failed to save supplier configuration.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error creating supplier configuration: {ex.Message}");
                _logger?.LogError(ex, "Error in interactive supplier configuration creation");
            }
        }

        private static IConfiguration BuildConfiguration()
        {
            var environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Development";
            
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

            // Add Serilog logging
            services.AddLogging(builder =>
            {
                builder.ClearProviders();
                builder.AddSerilog();
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
                    
                    // Enable string comparison translations for better LINQ support
                    mysqlOptions.EnableStringComparisonTranslations();
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

            // Add Unit of Work for transaction management
            services.AddUnitOfWork();

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
            // Add file processing dependencies
            services.AddScoped<SacksAIPlatform.InfrastructuresLayer.FileProcessing.IFileDataReader, SacksAIPlatform.InfrastructuresLayer.FileProcessing.FileDataReader>();
            services.AddScoped<SupplierConfigurationManager>();
            
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

        private static async Task ProcessInputFiles(ServiceProvider serviceProvider)
        {
            if (_logger == null)
            {
                throw new InvalidOperationException("Logger not initialized");
            }
            _logger.LogInformation("🚀 Analyzing All Inputs...");
            _logger.LogInformation("\n" + new string('=', 50));

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
                    _logger.LogInformation($"📁 Found {files.Length} Excel file(s) in Inputs folder:");
                    foreach (var file in files)
                    {
                        _logger.LogInformation($"   - {Path.GetFileName(file)}");
                        await fileProcessingService.ProcessFileAsync(file, CancellationToken.None);
                        _logger.LogInformation($"   - Finished processing {Path.GetFileName(file)}");
                    }
                }
                else
                {
                    _logger.LogWarning("❌ No Excel files found in Inputs folder.");
                }
            }
            else
            {
                _logger.LogError("❌ Inputs folder not found.");
            }
        }

        private static string FindInputsFolder()
        {
            // Try different strategies to find the Inputs folder (now at workspace root)
            var currentDirectory = Environment.CurrentDirectory;

            // Strategy 1: Check if we're running from project folder (dotnet run)
            var strategy1 = Path.Combine(currentDirectory, "..", "Inputs");
            if (Directory.Exists(strategy1))
            {
                return Path.GetFullPath(strategy1);
            }

            // Strategy 2: Check if we're running from bin folder (Visual Studio)
            var strategy2 = Path.Combine(currentDirectory, "..", "..", "..", "..", "Inputs");
            if (Directory.Exists(strategy2))
            {
                return Path.GetFullPath(strategy2);
            }

            // Strategy 3: Search upward for solution file, then go to Inputs at workspace root
            var searchDir = new DirectoryInfo(currentDirectory);
            while (searchDir != null)
            {
                var solutionFile = searchDir.GetFiles("*.sln").FirstOrDefault();
                if (solutionFile != null)
                {
                    var solutionInputsPath = Path.Combine(searchDir.FullName, "Inputs");
                    if (Directory.Exists(solutionInputsPath))
                    {
                        return solutionInputsPath;
                    }
                }
                searchDir = searchDir.Parent;
            }

            // Strategy 4: Fallback - return a non-existent path so we can show a helpful error
            return Path.Combine(currentDirectory, "Inputs");
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
            
            Console.WriteLine("🚀 MAIN MENU OPTIONS:");
            Console.WriteLine();
            Console.WriteLine("1️⃣  Process all Excel files (Standard Processing):");
            Console.WriteLine("   • Processes all Excel files from the Inputs folder");
            Console.WriteLine("   • Auto-detects supplier configurations based on filename patterns");
            Console.WriteLine("   • Applies column mappings and data transformations");
            Console.WriteLine("   • Saves processed data to the database");
            Console.WriteLine("   • Handles duplicate detection and data validation");
            Console.WriteLine();
            
            Console.WriteLine("2️⃣  🧹 Clear all data from database:");
            Console.WriteLine("   • Removes all records from all tables");
            Console.WriteLine("   • Maintains table structure and relationships");
            Console.WriteLine("   • Requires confirmation for safety");
            Console.WriteLine("   • Shows deletion summary with record counts");
            Console.WriteLine();
            
            Console.WriteLine("3️⃣  � Show database statistics:");
            Console.WriteLine("   • Displays current record counts for all tables");
            Console.WriteLine("   • Shows database connection information");
            Console.WriteLine("   • Provides total records summary");
            Console.WriteLine("   • Helpful for monitoring data growth");
            Console.WriteLine();
            
            Console.WriteLine("4️⃣  🧪 Test Configuration:");
            Console.WriteLine("   • Validates the supplier configuration system");
            Console.WriteLine("   • Tests configuration loading and parsing");
            Console.WriteLine("   • Shows detailed configuration information");
            Console.WriteLine("   • Reports any configuration errors or warnings");
            Console.WriteLine();

            Console.WriteLine("5️⃣  🔧 Create new supplier configuration (Interactive):");
            Console.WriteLine("   • Guided creation of new supplier configurations");
            Console.WriteLine("   • Analyzes Excel file structure automatically");
            Console.WriteLine("   • Suggests intelligent column mappings");
            Console.WriteLine("   • Interactive column classification (coreProduct/offer)");
            Console.WriteLine("   • Auto-detects data types and validation rules");
            Console.WriteLine("   • Generates filename detection patterns");
            Console.WriteLine("   • Saves configuration to supplier-formats.json");
            Console.WriteLine();

            Console.WriteLine("6️⃣  ❓ Show help and feature information:");
            Console.WriteLine("   • This help screen with detailed feature descriptions");
            Console.WriteLine("   • System requirements and configuration info");
            Console.WriteLine();

            Console.WriteLine("7️⃣  🚪 Exit:");
            Console.WriteLine("   • Safely exits the application");
            Console.WriteLine("   • Ensures all logs are flushed");
            Console.WriteLine();
            
            Console.WriteLine("🎯 KEY FEATURES:");
            Console.WriteLine("   ✅ Auto-detection of supplier configurations");
            Console.WriteLine("   ✅ Intelligent column mapping suggestions");
            Console.WriteLine("   ✅ Interactive configuration creation");
            Console.WriteLine("   ✅ Comprehensive data validation");
            Console.WriteLine("   ✅ Duplicate detection and handling");
            Console.WriteLine("   ✅ Structured logging with Serilog");
            Console.WriteLine("   ✅ MySQL database with Entity Framework Core");
            Console.WriteLine("   ✅ Robust error handling and recovery");
            Console.WriteLine("   ✅ Performance monitoring and metrics");
            Console.WriteLine();

            Console.WriteLine("📁 FILE REQUIREMENTS:");
            Console.WriteLine("   • Excel files (.xlsx) in Inputs folder at workspace root");
            Console.WriteLine("   • Files should follow configured supplier formats");
            Console.WriteLine("   • Temporary files (starting with ~) are automatically skipped");
            Console.WriteLine("   • Supported suppliers: DIOR, UNLIMITED, ACE (and custom)");
            Console.WriteLine();

            Console.WriteLine("🔧 CONFIGURATION FILES:");
            Console.WriteLine("   • appsettings.json - Database and logging configuration");
            Console.WriteLine("   • Configuration/supplier-formats.json - Supplier mappings");
            Console.WriteLine("   • Inputs/ - Folder containing Excel files to process");
            Console.WriteLine();

            Console.WriteLine("💡 GETTING STARTED:");
            Console.WriteLine("   1. Place Excel files in the Inputs folder");
            Console.WriteLine("   2. Use option 5 to create configurations for new suppliers");
            Console.WriteLine("   3. Use option 1 to process all files");
            Console.WriteLine("   4. Use option 3 to monitor processing results");
            Console.WriteLine();

            Console.WriteLine("🆘 TROUBLESHOOTING:");
            Console.WriteLine("   • Check database connection in appsettings.json");
            Console.WriteLine("   • Ensure Excel files are not open in another application");
            Console.WriteLine("   • Use option 4 to validate configuration integrity");
            Console.WriteLine("   • Check logs in the logs/ folder for detailed error information");
        }
        
    }
}