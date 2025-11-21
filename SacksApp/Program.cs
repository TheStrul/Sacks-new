using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Sacks.Core.Configuration;
using Sacks.DataAccess.Data;
using Sacks.DataAccess.Repositories.Interfaces;
using Sacks.DataAccess.Repositories.Implementations;
using Microsoft.EntityFrameworkCore;
using SacksAIPlatform.InfrastructuresLayer.FileProcessing;
using SacksLogicLayer.Services.Interfaces;
using SacksLogicLayer.Services.Implementations;
using SacksAIPlatform.InfrastructuresLayer.FileProcessing.Services;
using SacksLogicLayer.Services;
using System.Runtime.CompilerServices;
using Sacks.Configuration;

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
                // Load centralized configuration singleton
                var config = ConfigurationLoader.Instance;
                
                // Handle log file cleanup before initializing Serilog
                HandleLogFileCleanup(config.Logging);
                
                var services = ConfigureServices(config);
                _serviceProvider = services.BuildServiceProvider();

                // Initialize Serilog with simple file sink
                Log.Logger = new LoggerConfiguration()
                    .MinimumLevel.Information()
                    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
                    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", Serilog.Events.LogEventLevel.Warning)
                    .WriteTo.File("logs/sacks-.log",
                        rollingInterval: RollingInterval.Day,
                        rollOnFileSizeLimit: true,
                        fileSizeLimitBytes: 10485760,
                        retainedFileCountLimit: 10,
                        outputTemplate: "[{Timestamp:HH:mm:ss.fff} {Level:u3}]: {Message:lj}{NewLine}{Exception}")
                    .Enrich.FromLogContext()
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
        private static void HandleLogFileCleanup(LoggingOptions loggingSettings)
        {
            if (!loggingSettings.DeleteLogFilesOnStartup || loggingSettings.LogFilePaths.Length == 0)
            {
                return;
            }

            try
            {
                var deletedCount = 0;
                var errorCount = 0;

                foreach (var logPath in loggingSettings.LogFilePaths)
                {
                    if (string.IsNullOrWhiteSpace(logPath)) continue;

                    var resolvedPath = Path.IsPathRooted(logPath) 
                        ? logPath 
                        : Path.GetFullPath(logPath);

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

        private static ServiceCollection ConfigureServices(SacksConfigurationOptions config)
        {
            var services = new ServiceCollection();

            // Register configuration singleton
            services.AddSingleton(config);
            services.AddSingleton(config.Database);
            services.AddSingleton(config.FileProcessing);
            services.AddSingleton(config.ConfigurationFiles);
            services.AddSingleton(config.Logging);
            services.AddSingleton(config.McpClient);
            services.AddSingleton(config.UI);

            // Add Serilog logging
            services.AddLogging(builder =>
            {
                builder.ClearProviders();
                builder.AddSerilog();
            });

            // Get database options from config singleton
            var dbOptions = config.Database;
            
            // Add DbContext with centralized configuration
            services.AddDbContext<SacksDbContext>(options =>
            {
                options.UseSqlServer(dbOptions.ConnectionString, sqlOptions =>
                {
                    if (dbOptions.RetryOnFailure)
                    {
                        sqlOptions.EnableRetryOnFailure(dbOptions.MaxRetryCount);
                    }
                    sqlOptions.CommandTimeout(dbOptions.CommandTimeout);
                });

                if (dbOptions.EnableSensitiveDataLogging)
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

            // Add query and data management services
            services.AddScoped<IQueryBuilderService, QueryBuilderService>();
            services.AddScoped<IProductOffersQueryService, ProductOffersQueryService>();
            services.AddScoped<IOfferProductDataService, OfferProductDataService>();
            services.AddScoped<IGridStateManagementService, GridStateManagementService>();

            // Add MCP client service
            services.AddSingleton<IMcpClientService, McpClientService>();

            // Add LLM query router service (heuristic-based, can be replaced with LLM later)
            services.AddSingleton<ILlmQueryRouterService, HeuristicQueryRouterService>();

            return services;
        }
    }
}
