using SacksDataLayer;
using SacksDataLayer.FileProcessing.Configuration;
using SacksDataLayer.FileProcessing.Services;
using SacksDataLayer.FileProcessing.Normalizers;
using SacksDataLayer.FileProcessing.Interfaces;
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
                    Console.WriteLine("\n🔄 Testing normalization process...");
                    var products = await normalizer.NormalizeAsync(fileData);
                    var productList = products.ToList();
                    Console.WriteLine($"   ✅ Products normalized: {productList.Count}");
                    
                    if (productList.Any())
                    {
                        Console.WriteLine("\n📦 Sample normalized products:");
                        foreach (var product in productList.Take(3))
                        {
                            Console.WriteLine($"   • {product.Name} (SKU: {product.SKU})");
                            if (product.DynamicProperties.Any())
                            {
                                Console.WriteLine($"     Dynamic Properties: {product.DynamicProperties.Count}");
                                foreach (var prop in product.DynamicProperties.Take(3))
                                {
                                    Console.WriteLine($"       {prop.Key}: {prop.Value}");
                                }
                                if (product.DynamicProperties.Count > 3)
                                {
                                    Console.WriteLine($"       ... and {product.DynamicProperties.Count - 3} more properties");
                                }
                            }
                            Console.WriteLine();
                        }
                    }
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
    }
}
