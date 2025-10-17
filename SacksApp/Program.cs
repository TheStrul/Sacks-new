using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using SacksDataLayer.Configuration;
using SacksDataLayer.Data;
using SacksDataLayer.Repositories.Interfaces;
using SacksDataLayer.Repositories.Implementations;
using Microsoft.EntityFrameworkCore;
using SacksAIPlatform.InfrastructuresLayer.FileProcessing;
using SacksDataLayer.Services.Interfaces;
using SacksDataLayer.Services.Implementations;
using SacksLogicLayer.Services.Interfaces;
using SacksLogicLayer.Services.Implementations;
using SacksAIPlatform.InfrastructuresLayer.FileProcessing.Services;
using SacksLogicLayer.Services;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Sacks.Tests")]

namespace SacksApp
{
    internal static class Program
    {
        private static ServiceProvider? _serviceProvider;
        private static ILogger<Form>? _logger;

        /// <summary>
        /// Splits a file into multiple files based on the specified lines per file.
        /// </summary>
        /// <param name="fullPath">The full path to the file to split.</param>
        /// <param name="linesPerFile">The number of lines per output file.</param>
        /// <exception cref="ArgumentException">Thrown when fullPath is null or empty, or linesPerFile is less than 1.</exception>
        /// <exception cref="FileNotFoundException">Thrown when the source file does not exist.</exception>
        public static void SplitFile(string fullPath, int linesPerFile)
        {
            if (string.IsNullOrWhiteSpace(fullPath))
            {
                throw new ArgumentException("File path cannot be null or empty.", nameof(fullPath));
            }

            if (linesPerFile < 1)
            {
                throw new ArgumentException("Lines per file must be at least 1.", nameof(linesPerFile));
            }

            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException("Source file not found.", fullPath);
            }

            var directory = Path.GetDirectoryName(fullPath) ?? throw new InvalidOperationException("Could not determine directory from path.");
            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fullPath);
            var extension = Path.GetExtension(fullPath);

            var lines = File.ReadAllLines(fullPath);
            var fileNumber = 1;
            var currentLineCount = 0;
            StreamWriter? writer = null;

            try
            {
                for (int i = 0; i < lines.Length; i++)
                {
                    if (currentLineCount == 0)
                    {
                        writer?.Dispose();
                        var outputPath = Path.Combine(directory, $"{fileNameWithoutExtension}_part{fileNumber}{extension}");
                        writer = new StreamWriter(outputPath);
                        fileNumber++;
                    }

                    writer?.WriteLine(lines[i]);
                    currentLineCount++;

                    if (currentLineCount >= linesPerFile)
                    {
                        currentLineCount = 0;
                    }
                }
            }
            finally
            {
                writer?.Dispose();
            }
        }

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

                // Ensure database and views exist before showing UI
                try
                {
                    var dbInit = _serviceProvider.GetRequiredService<IFileProcessingDatabaseService>();
                    dbInit.EnsureDatabaseReadyAsync(CancellationToken.None).GetAwaiter().GetResult();
                    Log.Information("✅ Database ready (schema + views)");
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Failed to initialize database (EnsureCreated + Views)");
                }

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
                .AddJsonFile("Configuration/appsettings.json", optional: false, reloadOnChange: false)
                .AddEnvironmentVariables();

            // Build initial configuration to read the ConfigurationFiles section
            var baseConfig = configBuilder.Build();
            
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

            // Add Unit of Work
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            // Add business services
            services.AddScoped<IProductsService, ProductsService>();
            services.AddScoped<ISuppliersService, SuppliersService>();
            services.AddScoped<ISupplierOffersService, SupplierOffersService>();
            services.AddScoped<IOfferProductsService, OfferProductsService>();

            // Add application services
            services.AddScoped<IDatabaseManagementService, DatabaseManagementService>();
            services.AddScoped<IDatabaseConnectionService, DatabaseConnectionService>();
            services.AddScoped<IFileProcessingDatabaseService, FileProcessingDatabaseService>();
            services.AddScoped<IFileDataReader, FileDataReader>();
            services.AddScoped<SubtitleRowProcessor>();
            services.AddScoped<SupplierConfigurationManager>();
            services.AddScoped<ISupplierConfigurationService, SupplierConfigurationService>();
            services.AddScoped<IFileProcessingService, FileProcessingService>();

            return services;
        }
    }
}