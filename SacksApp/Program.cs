﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
using Microsoft.EntityFrameworkCore;

namespace SacksApp
{
    internal static class Program
    {
        private static ServiceProvider? _serviceProvider;
        private static ILogger<Form>? _logger;

        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            try
            {
                // Initialize configuration and services
                var configuration = BuildConfiguration();
                
                // Handle log file cleanup before initializing Serilog
                HandleLogFileCleanup(configuration);
                
                var services = ConfigureServices(configuration);
                _serviceProvider = services.BuildServiceProvider();

                // Initialize Serilog
                Log.Logger = new LoggerConfiguration()
                    .ReadFrom.Configuration(configuration)
                    .CreateLogger();

                Log.Information("🚀 Sacks Product Management System starting up...");

                _logger = _serviceProvider.GetRequiredService<ILogger<Form>>();

                ApplicationConfiguration.Initialize();

                // Pass service provider to Form1
                using var form = new DashBoard(_serviceProvider);
                Application.Run(form);

                Log.Information("🛑 Sacks Product Management System shutting down normally");
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "💥 Fatal application error");
                MessageBox.Show($"Fatal error: {ex.Message}", "Application Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.Exit(1);
            }
            finally
            {
                Log.CloseAndFlush();
                _serviceProvider?.Dispose();
            }
        }

        /// <summary>
        /// Handles log file cleanup based on configuration settings
        /// </summary>
        private static void HandleLogFileCleanup(IConfiguration configuration)
        {
            var loggingSettings = configuration.GetSection("LoggingSettings").Get<LoggingSettings>();
            if (loggingSettings?.DeleteLogFilesOnStartup != true)
            {
                return;
            }

            try
            {
                var solutionRoot = FindSolutionRoot();
                var deletedCount = 0;
                var errorCount = 0;

                foreach (var logPath in loggingSettings.LogFilePaths)
                {
                    if (string.IsNullOrWhiteSpace(logPath)) continue;

                    var resolvedPath = Path.IsPathRooted(logPath) 
                        ? logPath 
                        : Path.GetFullPath(Path.Combine(solutionRoot, logPath));

                    if (!Directory.Exists(resolvedPath)) continue;

                    var logFiles = Directory.GetFiles(resolvedPath, "*.log", SearchOption.TopDirectoryOnly);
                    
                    foreach (var file in logFiles)
                    {
                        try
                        {
                            File.Delete(file);
                            deletedCount++;
                        }
                        catch (Exception ex)
                        {
                            errorCount++;
                            // Can't use structured logging here as Serilog isn't initialized yet
                            Console.WriteLine($"Warning: Failed to delete log file {file}: {ex.Message}");
                        }
                    }
                }

                if (deletedCount > 0)
                {
                    Console.WriteLine($"Startup: Deleted {deletedCount} log file(s)" + 
                        (errorCount > 0 ? $" ({errorCount} failed)" : ""));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Error during log file cleanup: {ex.Message}");
            }
        }

        /// <summary>
        /// Finds the solution root directory by searching upward from the current executable location
        /// </summary>
        private static string FindSolutionRoot()
        {
            var currentDirectory = new DirectoryInfo(AppContext.BaseDirectory);

            // Search upward for solution file (.sln)
            while (currentDirectory != null)
            {
                var solutionFile = currentDirectory.GetFiles("*.sln").FirstOrDefault();
                if (solutionFile != null)
                {
                    return currentDirectory.FullName;
                }
                currentDirectory = currentDirectory.Parent;
            }

            throw new DirectoryNotFoundException("Solution root directory not found - no .sln file found in directory hierarchy");
        }

        private static IConfiguration BuildConfiguration()
        {
            var environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Development";
            var basePath = AppContext.BaseDirectory;

            // First, load the main appsettings.json
            var configBuilder = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("Configuration/appsettings.json", optional: false, reloadOnChange: false)
                .AddEnvironmentVariables();

            // Build initial configuration to read the ConfigurationFiles section
            var baseConfig = configBuilder.Build();
            
            // Get the configuration file settings
            var configFiles = baseConfig.GetSection("ConfigurationFiles").Get<ConfigurationFileSettings>();
            
            // Add the referenced configuration files if they exist
            if (configFiles != null)
            {
                if (!string.IsNullOrEmpty(configFiles.SupplierFormats))
                    configBuilder.AddJsonFile(configFiles.SupplierFormats, optional: true, reloadOnChange: false);
                    
                if (!string.IsNullOrEmpty(configFiles.ProductPropertyConfiguration))
                    configBuilder.AddJsonFile(configFiles.ProductPropertyConfiguration, optional: true, reloadOnChange: false);
                    
                if (!string.IsNullOrEmpty(configFiles.PerfumePropertyNormalization))
                    configBuilder.AddJsonFile(configFiles.PerfumePropertyNormalization, optional: true, reloadOnChange: false);
            }

            return configBuilder.Build();
        }

        private static ServiceCollection ConfigureServices(IConfiguration configuration)
        {
            var services = new ServiceCollection();

            // Add configuration as singleton
            services.AddSingleton<IConfiguration>(configuration);

            // Add configuration options
            services.Configure<DatabaseSettings>(configuration.GetSection("DatabaseSettings"));
            services.Configure<ConfigurationFileSettings>(configuration.GetSection("ConfigurationFiles"));
            services.Configure<LoggingSettings>(configuration.GetSection("LoggingSettings"));

            // Add Serilog logging
            services.AddLogging(builder =>
            {
                builder.ClearProviders();
                builder.AddSerilog();
            });

            // Get connection string from configuration
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("DefaultConnection string is not configured in Configuration/appsettings.json");
            }

            // Add DbContext with configuration-based connection string
            services.AddDbContext<SacksDbContext>(options =>
            {
                var dbSettings = configuration.GetSection("DatabaseSettings").Get<DatabaseSettings>() ?? new DatabaseSettings();

                options.UseSqlServer(connectionString, sqlOptions =>
                {
                    if (dbSettings.RetryOnFailure)
                    {
                        sqlOptions.EnableRetryOnFailure(dbSettings.MaxRetryCount);
                    }
                    sqlOptions.CommandTimeout(dbSettings.CommandTimeout);
                });

                if (dbSettings.EnableSensitiveDataLogging)
                {
                    options.EnableSensitiveDataLogging();
                }
            });

            // Add repositories
            services.AddScoped<ITransactionalProductsRepository, TransactionalProductsRepository>();
            services.AddScoped<ITransactionalSuppliersRepository, TransactionalSuppliersRepository>();
            services.AddScoped<ITransactionalSupplierOffersRepository, TransactionalSupplierOffersRepository>();
            services.AddScoped<ITransactionalOfferProductsRepository, TransactionalOfferProductsRepository>();

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

            // Add file processing services
            services.AddFileProcessingServices();

            // Add configuration-based property normalization services
            services.AddSingleton<ProductPropertyConfigurationManager>();
            services.AddSingleton<PropertyNormalizationConfigurationManager>();
            
            // Load configuration data from the JSON files via configuration system
            services.AddSingleton<ProductPropertyNormalizationConfiguration>(serviceProvider =>
            {
                var configuration = serviceProvider.GetRequiredService<IConfiguration>();
                
                // The perfume-property-normalization.json file is loaded directly at root level
                // Try to bind from root configuration first
                try
                {
                    var config = configuration.Get<ProductPropertyNormalizationConfiguration>();
                    if (config != null && !string.IsNullOrEmpty(config.ProductType))
                    {
                        return config;
                    }
                }
                catch (Exception ex)
                {
                    var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
                    var logger = loggerFactory?.CreateLogger("ConfigurationLoader");
                    logger?.LogWarning(ex, "Failed to bind ProductPropertyNormalizationConfiguration from root configuration");
                }
                
                // Try to get from the loaded JSON configuration section
                var configSection = configuration.GetSection("PerfumePropertyNormalization");
                if (configSection.Exists())
                {
                    return ConfigurationBinder.Get<ProductPropertyNormalizationConfiguration>(configSection)
                        ?? throw new InvalidOperationException("Failed to bind PerfumePropertyNormalization configuration");
                }
                
                // Fallback to manager's default
                var manager = serviceProvider.GetRequiredService<PropertyNormalizationConfigurationManager>();
                return manager.CreateDefaultPerfumeConfiguration();
            });
            
            // Now we can register these services without factories - DI will auto-resolve
            services.AddScoped<ConfigurationPropertyNormalizer>();
            services.AddScoped<ConfigurationDescriptionPropertyExtractor>();

            // Add supplier configuration manager
            services.AddScoped<SacksAIPlatform.InfrastructuresLayer.FileProcessing.IFileDataReader, SacksAIPlatform.InfrastructuresLayer.FileProcessing.FileDataReader>();
            services.AddScoped<SacksAIPlatform.InfrastructuresLayer.FileProcessing.Services.SubtitleRowProcessor>();
            services.AddScoped<SupplierConfigurationManager>();

            return services;
        }
    }
}