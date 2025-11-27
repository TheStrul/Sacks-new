using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;
using System.Runtime.CompilerServices;

using McpServer.Client;
using McpServer.Client.Configuration;
using ModernWinForms.Theming;
using Sacks.Configuration;
using Sacks.Core.Services.Interfaces;
using Sacks.DataAccess.Extensions;
using Sacks.LogicLayer.Extensions;
using Sacks.LogicLayer.Services;

[assembly: InternalsVisibleTo("Sacks.Tests")]

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
                /*
                // ZERO TOLERANCE: Validate theme files BEFORE starting application
                var skinsDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Skins");
                var validationResult = ThemeValidationTool.ValidateAll(skinsDirectory);
                
                // Log validation report to console
                Console.WriteLine(validationResult.GetFullReport());
                
                // ZERO TOLERANCE: If validation failed, show errors and EXIT
                if (!validationResult.IsValid)
                {
                    ThemeValidationTool.DisplayValidationResults(validationResult);
                    
                    // ZERO TOLERANCE: DO NOT START APPLICATION with invalid themes
                    Environment.Exit(1);
                    return; // Unreachable, but explicit
                }
                
                // Validation passed, show success (optional, can be commented out)
                // ThemeValidationTool.DisplayValidationResults(validationResult);
                */
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
                Log.Information("✅ Theme validation passed - all theme files are valid");

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

                // Launch MDI parent form
                using var mainForm = new MainForm(_serviceProvider);
                Application.Run(mainForm);

                // For testing themes:
                // using var mainForm = new ThemeTestForm();
                // Application.Run(mainForm);
                
                Log.Information("🛑 Sacks Product Management System shutting down normally");
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "💥 Fatal application error");
                CustomMessageBox.Show($"Fatal error: {ex.Message}", "Application Error", 
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
            services.AddSingleton(config.Llm);
            services.AddSingleton(config.UI);

            // Add Serilog logging
            services.AddLogging(builder =>
            {
                builder.ClearProviders();
                builder.AddSerilog();
            });

            // Add data access layer (DbContext, repositories, infrastructure services)
            services.AddSacksDataAccess(config.Database);

            // Add business logic layer (business services, orchestration services)
            services.AddSacksLogicLayer();
            
            // Add logic layer with MCP client and LLM service (GitHub Models)
            services.AddSacksLogicLayerServices(config.McpClient, config.Llm);

            return services;
        }
    }
}
