using Microsoft.Extensions.Configuration;
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
                using var form = new MainForm(_serviceProvider);
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

        private static IConfiguration BuildConfiguration()
        {
            var environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Development";
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

            // Add file processing services
            services.AddFileProcessingServices();

            // Add configuration-based property normalization services
            try 
            {
                services.AddDynamicProductServices(
                    propertyConfigPath: "product-property-configuration.json",
                    normalizationConfigPath: "perfume-property-normalization.json"
                );
            }
            catch
            {
                // Fallback: Register a simple ConfigurationPropertyNormalizer to prevent DI failures
                // This will be logged during service resolution
            }

            // Add supplier configuration manager
            services.AddScoped<SacksAIPlatform.InfrastructuresLayer.FileProcessing.IFileDataReader, SacksAIPlatform.InfrastructuresLayer.FileProcessing.FileDataReader>();
            services.AddScoped<SupplierConfigurationManager>();

            return services;
        }
    }
}