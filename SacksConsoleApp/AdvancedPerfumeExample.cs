using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using SacksDataLayer.Data;
using SacksDataLayer.Services.Interfaces;
using SacksDataLayer.Services.Implementations;
using SacksDataLayer.Models;
using SacksDataLayer.Configuration;
using SacksDataLayer.Extensions;

namespace SacksConsoleApp
{
    /// <summary>
    /// Advanced example demonstrating data normalization and intelligent filtering
    /// </summary>
    public class AdvancedPerfumeExample
    {
        private readonly IPerfumeProductService _perfumeService;
        private readonly DataNormalizationService _normalizationService;
        private readonly PropertyNormalizer _normalizer;
        private readonly ILogger<AdvancedPerfumeExample> _logger;

        public AdvancedPerfumeExample(
            IPerfumeProductService perfumeService,
            DataNormalizationService normalizationService,
            PropertyNormalizer normalizer,
            ILogger<AdvancedPerfumeExample> logger)
        {
            _perfumeService = perfumeService ?? throw new ArgumentNullException(nameof(perfumeService));
            _normalizationService = normalizationService ?? throw new ArgumentNullException(nameof(normalizationService));
            _normalizer = normalizer ?? throw new ArgumentNullException(nameof(normalizer));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task RunAdvancedExamplesAsync()
        {
            Console.WriteLine("🧠 Advanced Perfume Data Manipulation Examples");
            Console.WriteLine("==============================================\n");

            // Step 1: Analyze current data quality
            await AnalyzeDataQualityAsync();

            // Step 2: Show normalization capabilities
            await DemonstrateNormalizationAsync();

            // Step 3: Run normalization (dry run)
            await RunNormalizationDryRunAsync();

            // Step 4: Show intelligent filtering
            await DemonstrateIntelligentFilteringAsync();

            // Step 5: Show multi-language support
            await DemonstrateMultiLanguageSupportAsync();
        }

        private async Task AnalyzeDataQualityAsync()
        {
            Console.WriteLine("📊 Data Quality Analysis:");
            Console.WriteLine("-------------------------");

            try
            {
                var analysis = await _normalizationService.AnalyzeNormalizationImpactAsync(sampleSize: 500);

                Console.WriteLine($"Sample size: 500 products");
                Console.WriteLine($"Products that would benefit from normalization: {analysis.ProductsWithChanges}");
                Console.WriteLine($"Products already normalized: {analysis.ProductsWithoutChanges}");
                Console.WriteLine($"Products with errors: {analysis.ErrorProducts}");
                Console.WriteLine();

                if (analysis.KeyMappings.Any())
                {
                    Console.WriteLine("Key normalization examples:");
                    foreach (var mapping in analysis.KeyMappings.Take(5))
                    {
                        Console.WriteLine($"  '{mapping.Key}' → '{mapping.Value}'");
                    }
                    Console.WriteLine();
                }

                if (analysis.ValueMappings.Any())
                {
                    Console.WriteLine("Value normalization examples:");
                    foreach (var mapping in analysis.ValueMappings.Take(5))
                    {
                        var parts = mapping.Key.Split(':');
                        Console.WriteLine($"  {parts[0]}: '{parts[1]}' → '{mapping.Value}'");
                    }
                    Console.WriteLine();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during data quality analysis");
                Console.WriteLine($"Error: {ex.Message}\n");
            }
        }

        private Task DemonstrateNormalizationAsync()
        {
            Console.WriteLine("🔧 Normalization Demonstration:");
            Console.WriteLine("-------------------------------");

            // Show how various inputs get normalized
            var testInputs = new Dictionary<string, object?>
            {
                ["family"] = "W", // Should become Gender: Women
                ["con"] = "EDT", // Should become Concentration: EDT
                ["vol"] = "100ml", // Should become Size: 100ml
                ["marca"] = "Dior", // Should become Brand: Dior
                ["sexe"] = "Femme", // Should become Gender: Women (French)
                ["type"] = "Eau de Toilette" // Should become Concentration: EDT
            };

            Console.WriteLine("Raw input → Normalized output:");
            var normalized = _normalizer.NormalizeProperties(testInputs);
            
            foreach (var original in testInputs)
            {
                var normalizedKey = _normalizer.NormalizeKey(original.Key);
                var normalizedValue = _normalizer.NormalizeValue(normalizedKey, original.Value?.ToString() ?? "");
                Console.WriteLine($"  {original.Key}: '{original.Value}' → {normalizedKey}: '{normalizedValue}'");
            }
            Console.WriteLine();
            
            return Task.CompletedTask;
        }

        private async Task RunNormalizationDryRunAsync()
        {
            Console.WriteLine("🧪 Normalization Dry Run (first 50 products):");
            Console.WriteLine("----------------------------------------------");

            try
            {
                var summary = await _normalizationService.NormalizeAllProductsAsync(
                    batchSize: 50, 
                    dryRun: true);

                Console.WriteLine($"Would process: {summary.ProcessedProducts} products");
                Console.WriteLine($"Would change: {summary.ChangedProducts} products");
                Console.WriteLine($"Already correct: {summary.UnchangedProducts} products");
                Console.WriteLine($"Errors: {summary.ErrorProducts} products");

                if (summary.PropertyChanges.Any())
                {
                    Console.WriteLine("\nExample changes that would be made:");
                    foreach (var change in summary.PropertyChanges.Take(5))
                    {
                        Console.WriteLine($"  {change.OriginalKey}: '{change.OriginalValue}' → {change.NormalizedKey}: '{change.NormalizedValue}'");
                    }
                }
                Console.WriteLine();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during normalization dry run");
                Console.WriteLine($"Error: {ex.Message}\n");
            }
        }

        private async Task DemonstrateIntelligentFilteringAsync()
        {
            Console.WriteLine("🔍 Intelligent Filtering (handles variations automatically):");
            Console.WriteLine("-----------------------------------------------------------");

            try
            {
                // Client can search for "Women" and it will find "W", "Femme", "Mujer", etc.
                var filter = new PerfumeFilterModel { Gender = "Women" };
                var sort = new PerfumeSortModel();

                var results = await _perfumeService.SearchPerfumeProductsAsync(filter, sort, pageSize: 5);

                Console.WriteLine($"Searching for Gender = 'Women' found {results.TotalCount} products");
                Console.WriteLine("(This includes products with 'W', 'Femme', 'Mujer', 'Female', etc.)");
                
                foreach (var product in results.Items)
                {
                    Console.WriteLine($"  - {product.Name} (Gender: {product.Gender})");
                }
                Console.WriteLine();

                // Same for concentration
                var edtFilter = new PerfumeFilterModel { Concentration = "EDT" };
                var edtResults = await _perfumeService.SearchPerfumeProductsAsync(edtFilter, sort, pageSize: 3);

                Console.WriteLine($"Searching for Concentration = 'EDT' found {edtResults.TotalCount} products");
                Console.WriteLine("(This includes 'EDT', 'Eau de Toilette', 'E.D.T.', etc.)");
                
                foreach (var product in edtResults.Items)
                {
                    Console.WriteLine($"  - {product.Name} (Concentration: {product.Concentration})");
                }
                Console.WriteLine();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during intelligent filtering demo");
                Console.WriteLine($"Error: {ex.Message}\n");
            }
        }

        private Task DemonstrateMultiLanguageSupportAsync()
        {
            Console.WriteLine("🌍 Multi-Language Support:");
            Console.WriteLine("--------------------------");

            // Show that the system understands multiple languages
            var languageExamples = new[]
            {
                ("English", "Women", "EDT", "Brand"),
                ("French", "Femmes", "Eau de Toilette", "Marque"),  
                ("Spanish", "Mujeres", "EDT", "Marca"),
                ("Italian", "Donne", "EDT", "Marchio")
            };

            Console.WriteLine("All of these search terms would find the same products:");
            foreach (var (language, gender, concentration, brandKey) in languageExamples)
            {
                var normalizedGender = _normalizer.NormalizeValue("Gender", gender);
                var normalizedConcentration = _normalizer.NormalizeValue("Concentration", concentration);
                var normalizedBrandKey = _normalizer.NormalizeKey(brandKey);
                
                Console.WriteLine($"  {language}: {gender} → {normalizedGender}, {concentration} → {normalizedConcentration}");
            }
            Console.WriteLine();

            Console.WriteLine("Your clients can use familiar terms in their language!");
            Console.WriteLine("The system automatically handles the normalization behind the scenes.");
            Console.WriteLine();
            
            return Task.CompletedTask;
        }

        /// <summary>
        /// Configure services for advanced examples
        /// </summary>
        public static void ConfigureAdvancedServices(IServiceCollection services, string connectionString)
        {
            // Configure logging
            services.AddLogging(builder => builder.AddConsole());

            // Configure database
            services.AddDbContext<SacksDbContext>(options =>
                options.UseSqlServer(connectionString));

            // Add perfume services (includes PropertyNormalizer)
            services.AddPerfumeServices();

            // Add normalization service
            services.AddScoped<DataNormalizationService>();

            // Add example class
            services.AddTransient<AdvancedPerfumeExample>();
        }
    }
}
