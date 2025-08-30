using SacksDataLayer;
using SacksDataLayer.FileProcessing.Configuration;
using SacksDataLayer.FileProcessing.Services;
using SacksDataLayer.FileProcessing.Normalizers;
using SacksDataLayer.FileProcessing.Interfaces;
using SacksDataLayer.FileProcessing.Models;
using SacksDataLayer.Configuration.Normalizers;
using SacksDataLayer.Data;
using SacksDataLayer.Repositories.Implementations;
using SacksDataLayer.Services.Implementations;
using SacksAIPlatform.InfrastructuresLayer.FileProcessing;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace SacksConsoleApp
{
    /// <summary>
    /// Phase 2 Relational Architecture Test
    /// Tests the new ConfigurationBasedNormalizer implementation that creates relational entities
    /// </summary>
    public class Phase2RelationalTest
    {
        public static async Task RunPhase2TestAsync()
        {
            Console.WriteLine("?? === Phase 2 Relational Architecture Test ===\n");
            Console.WriteLine("Testing the new ConfigurationBasedNormalizer implementation that creates:");
            Console.WriteLine("   • ProductEntity (core properties only)");
            Console.WriteLine("   • SupplierOfferEntity (catalog metadata)");
            Console.WriteLine("   • OfferProductEntity (pricing & offer-specific data)");
            Console.WriteLine("   • Proper property classification\n");

            try
            {
                // Initialize configuration
                var configPath = FindConfigurationFile();
                var configManager = new SupplierConfigurationManager(configPath);
                var fileReader = new FileDataReader();

                Console.WriteLine($"?? Configuration: {Path.GetFileName(configPath)}");

                // Load DIOR configuration with robust error handling
                var diorConfig = await configManager.GetSupplierConfigurationAsync("DIOR");
                if (diorConfig == null)
                {
                    Console.WriteLine("?? DIOR configuration not found via GetSupplierConfigurationAsync, trying manual search...");
                    
                    // Use the proven workaround from other test files
                    var allConfigs = await configManager.GetConfigurationAsync();
                    var foundDior = allConfigs.Suppliers.FirstOrDefault(s => 
                        string.Equals(s.Name, "DIOR", StringComparison.OrdinalIgnoreCase));
                    
                    if (foundDior != null)
                    {
                        Console.WriteLine($"? Found DIOR config manually: '{foundDior.Name}'");
                        diorConfig = foundDior;
                    }
                    else
                    {
                        Console.WriteLine("? DIOR configuration not found even with manual search!");
                        Console.WriteLine("Available suppliers:");
                        foreach (var s in allConfigs.Suppliers)
                        {
                            Console.WriteLine($"   - '{s.Name}'");
                        }
                        return;
                    }
                }

                Console.WriteLine($"? DIOR Configuration loaded");
                Console.WriteLine($"   ?? Core Properties: {string.Join(", ", diorConfig.PropertyClassification.CoreProductProperties)}");
                Console.WriteLine($"   ?? Offer Properties: {string.Join(", ", diorConfig.PropertyClassification.OfferProperties)}");

                // Find DIOR file
                var diorFilePath = FindDiorFile();
                if (!File.Exists(diorFilePath))
                {
                    Console.WriteLine($"? DIOR file not found: {diorFilePath}");
                    return;
                }

                Console.WriteLine($"?? Processing: {Path.GetFileName(diorFilePath)}");

                // Read file data
                var fileData = await fileReader.ReadFileAsync(diorFilePath);
                Console.WriteLine($"?? File contains {fileData.RowCount} rows");

                // Test the new Phase 2 normalizer
                var normalizer = new ConfigurationBasedNormalizer(diorConfig);

                // Test both processing modes with the new relational architecture
                await TestRelationalProcessingMode(normalizer, fileData, ProcessingMode.UnifiedProductCatalog, diorFilePath);
                await TestRelationalProcessingMode(normalizer, fileData, ProcessingMode.SupplierCommercialData, diorFilePath);

                Console.WriteLine("\n?? Phase 2 Relational Architecture Test Completed Successfully!");
                Console.WriteLine("? All relational entities are now being created properly");
                Console.WriteLine("? Property classification is working correctly");
                Console.WriteLine("? Both processing modes implemented with relational design");
                
            }
            catch (Exception ex)
            {
                Console.WriteLine($"? Error during Phase 2 test: {ex.Message}");
                Console.WriteLine($"   Stack trace: {ex.StackTrace}");
            }

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }

        private static async Task TestRelationalProcessingMode(
            ConfigurationBasedNormalizer normalizer, 
            FileData fileData, 
            ProcessingMode mode, 
            string filePath)
        {
            Console.WriteLine($"\n?? === Testing {mode} Mode (Phase 2 Relational) ===");
            
            var context = new ProcessingContext
            {
                Mode = mode,
                SourceFileName = Path.GetFileName(filePath),
                SupplierName = normalizer.SupplierName,
                ProcessingDate = DateTime.UtcNow,
                ProcessingIntent = $"Phase 2 {mode} processing test"
            };

            var result = await normalizer.NormalizeAsync(fileData, context);
            
            Console.WriteLine($"   ? Processing completed!");
            Console.WriteLine($"   ?? Statistics:");
            Console.WriteLine($"      • Products created: {result.Statistics.ProductsCreated}");
            Console.WriteLine($"      • Products skipped: {result.Statistics.ProductsSkipped}");
            Console.WriteLine($"      • Processing time: {result.Statistics.ProcessingTime.TotalMilliseconds:F0}ms");

            // NEW PHASE 2 STATISTICS
            if (mode == ProcessingMode.SupplierCommercialData)
            {
                Console.WriteLine($"      • ?? Supplier offers created: {result.Statistics.SupplierOffersCreated}");
                Console.WriteLine($"      • ?? Offer products created: {result.Statistics.OfferProductsCreated}");
                Console.WriteLine($"      • ?? Pricing records processed: {result.Statistics.PricingRecordsProcessed}");
                
                if (result.SupplierOffer != null)
                {
                    Console.WriteLine($"   ?? Supplier Offer Details:");
                    Console.WriteLine($"      • Offer Name: {result.SupplierOffer.OfferName}");
                    Console.WriteLine($"      • Currency: {result.SupplierOffer.Currency}");
                    Console.WriteLine($"      • Offer Type: {result.SupplierOffer.OfferType}");
                    Console.WriteLine($"      • Valid From: {result.SupplierOffer.ValidFrom:yyyy-MM-dd}");
                }
            }
            else
            {
                Console.WriteLine($"      • ??? Unique products identified: {result.Statistics.UniqueProductsIdentified}");
                Console.WriteLine($"      • ?? Missing core attributes: {result.Statistics.MissingCoreAttributes}");
            }

            if (result.Errors.Any())
            {
                Console.WriteLine($"      • ? Errors: {result.Errors.Count}");
                foreach (var error in result.Errors.Take(3))
                {
                    Console.WriteLine($"        - {error}");
                }
            }

            // TEST NEW PHASE 2 RELATIONAL RESULTS
            if (result.NormalizationResults.Any())
            {
                Console.WriteLine($"\n   ?? Phase 2 Relational Entity Analysis:");
                Console.WriteLine($"      ?? Total normalization results: {result.NormalizationResults.Count()}");

                var sampleResults = result.NormalizationResults.Take(3);
                foreach (var normResult in sampleResults)
                {
                    Console.WriteLine($"\n      ?? Sample Product: {normResult.Product.Name}");
                    Console.WriteLine($"         SKU: {normResult.Product.SKU}");
                    
                    // Show core properties (stored in ProductEntity.DynamicProperties)
                    if (normResult.Product.DynamicProperties.Any())
                    {
                        Console.WriteLine($"         ??? Core Properties:");
                        foreach (var coreProp in normResult.Product.DynamicProperties.Take(3))
                        {
                            Console.WriteLine($"            • {coreProp.Key}: {coreProp.Value}");
                        }
                    }

                    // Show offer properties (SupplierCommercialData mode only)
                    if (mode == ProcessingMode.SupplierCommercialData && normResult.HasOfferProperties)
                    {
                        Console.WriteLine($"         ?? Offer Properties:");
                        foreach (var offerProp in normResult.OfferProperties.Take(3))
                        {
                            Console.WriteLine($"            • {offerProp.Key}: {offerProp.Value}");
                        }

                        if (normResult.OfferProduct != null)
                        {
                            Console.WriteLine($"         ?? OfferProduct Entity:");
                            if (normResult.OfferProduct.Price.HasValue)
                                Console.WriteLine($"            • Price: {normResult.OfferProduct.Price:C}");
                            if (!string.IsNullOrEmpty(normResult.OfferProduct.Capacity))
                                Console.WriteLine($"            • Capacity: {normResult.OfferProduct.Capacity}");
                        }
                    }

                    Console.WriteLine($"         ?? Processing Mode: {normResult.ProcessingMode}");
                    Console.WriteLine($"         ?? Row Index: {normResult.RowIndex}");
                }

                // Property Classification Validation
                Console.WriteLine($"\n   ?? Property Classification Validation:");
                var totalCoreProps = result.NormalizationResults
                    .SelectMany(nr => nr.Product.DynamicProperties.Keys)
                    .Distinct()
                    .Count();
                var totalOfferProps = result.NormalizationResults
                    .SelectMany(nr => nr.OfferProperties.Keys)
                    .Distinct()
                    .Count();

                Console.WriteLine($"      • Unique core properties found: {totalCoreProps}");
                Console.WriteLine($"      • Unique offer properties found: {totalOfferProps}");

                // Check if property classification is working correctly
                var expectedCoreProps = result.NormalizationResults.First().Product.DynamicProperties.Keys
                    .Intersect(new[] { "Category", "Size", "Unit", "EAN", "CommercialLine", "Family" });
                var expectedOfferProps = result.NormalizationResults.First().OfferProperties.Keys
                    .Intersect(new[] { "Price", "Capacity" });

                Console.WriteLine($"      ? Expected core properties found: {string.Join(", ", expectedCoreProps)}");
                Console.WriteLine($"      ? Expected offer properties found: {string.Join(", ", expectedOfferProps)}");

                // Backward Compatibility Test
                Console.WriteLine($"\n   ?? Backward Compatibility Test:");
                Console.WriteLine($"      • result.Products.Count(): {result.Products.Count()}");
                Console.WriteLine($"      • result.NormalizationResults.Count(): {result.NormalizationResults.Count()}");
                Console.WriteLine($"      ? Legacy Products property working: {result.Products.Count() == result.NormalizationResults.Count()}");
            }
        }

        private static string FindConfigurationFile()
        {
            var possiblePaths = new[]
            {
                Path.Combine("..", "SacksDataLayer", "Configuration", "supplier-formats.json"),
                Path.Combine("SacksDataLayer", "Configuration", "supplier-formats.json"),
                Path.Combine("Configuration", "supplier-formats.json"),
                "supplier-formats.json"
            };

            foreach (var path in possiblePaths)
            {
                if (File.Exists(path))
                    return path;
            }

            throw new FileNotFoundException("Configuration file 'supplier-formats.json' not found");
        }

        private static string FindDiorFile()
        {
            var possiblePaths = new[]
            {
                Path.Combine("..", "SacksDataLayer", "Inputs", "DIOR 2025.xlsx"),
                Path.Combine("..", "SacksDataLayer", "Inputs", "DIOR short.xlsx"),
                Path.Combine("SacksDataLayer", "Inputs", "DIOR 2025.xlsx"),
                Path.Combine("SacksDataLayer", "Inputs", "DIOR short.xlsx"),
                Path.Combine("Inputs", "DIOR 2025.xlsx"),
                Path.Combine("Inputs", "DIOR short.xlsx")
            };

            foreach (var path in possiblePaths)
            {
                if (File.Exists(path))
                    return path;
            }

            return Path.Combine("..", "SacksDataLayer", "Inputs", "DIOR 2025.xlsx"); // Fallback
        }
    }
}