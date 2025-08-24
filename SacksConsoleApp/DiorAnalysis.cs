using SacksDataLayer;
using SacksDataLayer.FileProcessing.Configuration;
using SacksDataLayer.FileProcessing.Services;
using SacksDataLayer.FileProcessing.Normalizers;
using SacksDataLayer.FileProcessing.Interfaces;
using SacksDataLayer.FileProcessing.Models;
using SacksAIPlatform.InfrastructuresLayer.FileProcessing;
using System.Text.Json;

namespace SacksConsoleApp
{
    public class DiorAnalysis
    {
        public static async Task AnalyzeDiorFile()
        {
            Console.WriteLine("=== DIOR Configuration vs File Analysis ===\n");

            try
            {
                // Initialize services
                var configPath = Path.Combine("..", "SacksDataLayer", "Configuration", "supplier-formats.json");
                var configManager = new SupplierConfigurationManager(configPath);
                var fileReader = new FileDataReader();

                // Load DIOR configuration
                var diorConfig = await configManager.GetSupplierConfigurationAsync("DIOR");
                if (diorConfig == null)
                {
                    Console.WriteLine("❌ DIOR configuration not found!");
                    return;
                }

                // Load DIOR file
                var diorFilePath = Path.Combine("..", "SacksDataLayer", "Inputs", "DIOR 2025.xlsx");
                if (!File.Exists(diorFilePath))
                {
                    Console.WriteLine($"❌ DIOR file not found: {diorFilePath}");
                    return;
                }

                Console.WriteLine("📁 Reading DIOR Excel file...");
                var fileData = await fileReader.ReadFileAsync(diorFilePath);
                
                // Get file headers
                var headerRow = fileData.dataRows.FirstOrDefault(r => r.HasData);
                if (headerRow == null)
                {
                    Console.WriteLine("❌ No header row found in file!");
                    return;
                }

                var fileHeaders = headerRow.Cells.Select(c => c.Value?.Trim() ?? "").Where(h => !string.IsNullOrEmpty(h)).ToList();
                
                Console.WriteLine($"\n📊 Analysis Results:");
                Console.WriteLine($"   📁 File: {Path.GetFileName(diorFilePath)}");
                Console.WriteLine($"   📋 File Headers: {fileHeaders.Count}");
                Console.WriteLine($"   🗂️ Configured Mappings: {diorConfig.ColumnMappings.Count}");
                Console.WriteLine($"   📄 Total Rows: {fileData.RowCount}");
                
                Console.WriteLine($"\n📋 Headers found in Excel file:");
                for (int i = 0; i < fileHeaders.Count; i++)
                {
                    Console.WriteLine($"   [{i:D2}] {fileHeaders[i]}");
                }
                
                Console.WriteLine($"\n🗂️ DIOR Configuration Column Mappings:");
                foreach (var mapping in diorConfig.ColumnMappings)
                {
                    var found = fileHeaders.Any(h => string.Equals(h, mapping.Key, StringComparison.OrdinalIgnoreCase));
                    var status = found ? "✓" : "❌";
                    Console.WriteLine($"   {status} {mapping.Key} → {mapping.Value}");
                }
                
                Console.WriteLine($"\n🔍 Headers in file but not mapped in configuration:");
                var unmappedHeaders = fileHeaders.Where(h => 
                    !diorConfig.ColumnMappings.Keys.Any(k => string.Equals(k, h, StringComparison.OrdinalIgnoreCase))
                ).ToList();
                
                if (unmappedHeaders.Any())
                {
                    foreach (var header in unmappedHeaders)
                    {
                        Console.WriteLine($"   ⚠️ {header}");
                    }
                }
                else
                {
                    Console.WriteLine("   ✓ All file headers are mapped in configuration");
                }
                
                Console.WriteLine($"\n🔍 Required columns verification:");
                foreach (var required in diorConfig.Detection.RequiredColumns)
                {
                    var found = fileHeaders.Any(h => string.Equals(h, required, StringComparison.OrdinalIgnoreCase));
                    var status = found ? "✓" : "❌";
                    Console.WriteLine($"   {status} {required}");
                }

                // Test the normalizer
                Console.WriteLine($"\n🧪 Testing DIOR normalizer...");
                var normalizer = new ConfigurationBasedNormalizer(diorConfig);
                var canHandle = normalizer.CanHandle(Path.GetFileName(diorFilePath), fileData.dataRows.Take(5));
                Console.WriteLine($"   Can handle file: {(canHandle ? "✓ Yes" : "❌ No")}");

                if (canHandle)
                {
                    // Test processing mode recommendation
                    var recommendedMode = normalizer.RecommendProcessingMode(Path.GetFileName(diorFilePath), fileData.dataRows.Take(5));
                    Console.WriteLine($"   Recommended processing mode: {recommendedMode}");

                    // Test both processing modes
                    await TestProcessingMode(normalizer, fileData, ProcessingMode.UnifiedProductCatalog, diorFilePath);
                    await TestProcessingMode(normalizer, fileData, ProcessingMode.SupplierCommercialData, diorFilePath);
                }

                Console.WriteLine("\n✅ Analysis completed successfully!");
                
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error during analysis: {ex.Message}");
                Console.WriteLine($"   Stack trace: {ex.StackTrace}");
            }

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }

        private static async Task TestProcessingMode(ConfigurationBasedNormalizer normalizer, FileData fileData, ProcessingMode mode, string filePath)
        {
            Console.WriteLine($"\n🔄 Testing {mode} mode...");
            
            var context = new ProcessingContext
            {
                Mode = mode,
                SourceFileName = Path.GetFileName(filePath),
                SupplierName = normalizer.SupplierName,
                ProcessingIntent = $"Testing {mode} processing workflow"
            };

            var result = await normalizer.NormalizeAsync(fileData, context);
            
            Console.WriteLine($"   ✅ Processing completed!");
            Console.WriteLine($"   📊 Statistics:");
            Console.WriteLine($"      • Products created: {result.Statistics.ProductsCreated}");
            Console.WriteLine($"      • Products skipped: {result.Statistics.ProductsSkipped}");
            Console.WriteLine($"      • Processing time: {result.Statistics.ProcessingTime.TotalMilliseconds:F0}ms");
            
            if (mode == ProcessingMode.UnifiedProductCatalog)
            {
                Console.WriteLine($"      • Unique products identified: {result.Statistics.UniqueProductsIdentified}");
                Console.WriteLine($"      • Missing core attributes: {result.Statistics.MissingCoreAttributes}");
            }
            else
            {
                Console.WriteLine($"      • Pricing records processed: {result.Statistics.PricingRecordsProcessed}");
                Console.WriteLine($"      • Stock records processed: {result.Statistics.StockRecordsProcessed}");
                Console.WriteLine($"      • Orphaned commercial records: {result.Statistics.OrphanedCommercialRecords}");
            }

            if (result.Errors.Any())
            {
                Console.WriteLine($"      • Errors: {result.Errors.Count}");
                foreach (var error in result.Errors.Take(3))
                {
                    Console.WriteLine($"        - {error}");
                }
            }

            if (result.Products.Any())
            {
                Console.WriteLine($"\n   📦 Sample {mode} products:");
                foreach (var product in result.Products.Take(2))
                {
                    Console.WriteLine($"      • {product.Name} (SKU: {product.SKU})");
                    if (product.DynamicProperties.Any())
                    {
                        var relevantProps = mode == ProcessingMode.UnifiedProductCatalog
                            ? product.DynamicProperties.Where(p => !p.Key.Contains("Price") && !p.Key.Contains("Stock"))
                            : product.DynamicProperties.Where(p => p.Key.Contains("Price") || p.Key.Contains("Stock"));
                            
                        foreach (var prop in relevantProps.Take(3))
                        {
                            Console.WriteLine($"         {prop.Key}: {prop.Value}");
                        }
                    }
                }
            }
        }
    }
}
