using SacksDataLayer;
using SacksDataLayer.FileProcessing.Configuration;
using SacksDataLayer.FileProcessing.Services;
using SacksDataLayer.FileProcessing.Normalizers;
using SacksDataLayer.FileProcessing.Interfaces;
using SacksDataLayer.FileProcessing.Models;
using SacksDataLayer.Data;
using SacksDataLayer.Repositories.Implementations;
using SacksDataLayer.Services.Implementations;
using SacksAIPlatform.InfrastructuresLayer.FileProcessing;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SacksConsoleApp
{
    public class DiorProcessor
    {
        public static async Task ProcessDiorFile()
        {
            Console.WriteLine("=== DIOR File Processing ===\n");

            try
            {
                // Setup SQL Server database connection with retry logic
                var connectionString = @"Server=(localdb)\mssqllocaldb;Database=SacksProductsDb;Trusted_Connection=true;MultipleActiveResultSets=true";
                var options = new DbContextOptionsBuilder<SacksDbContext>()
                    .UseSqlServer(connectionString, sqlOptions => sqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(30),
                        errorNumbersToAdd: null))
                    .Options;

                await using var context = new SacksDbContext(options);
                
                // Ensure database is created and up-to-date
                Console.WriteLine("🔧 Ensuring database exists and is up-to-date...");
                await context.Database.MigrateAsync();
                Console.WriteLine("✅ Database ready!");

                var repository = new ProductsRepository(context);
                var service = new ProductsService(repository);

                // Initialize file processing services
                var configPath = FindConfigurationFile();
                var configManager = new SupplierConfigurationManager(configPath);
                var fileReader = new FileDataReader();

                // Debug: Add diagnostics for configuration loading
                Console.WriteLine($"🔍 Configuration file path: {Path.GetFullPath(configPath)}");
                Console.WriteLine($"📁 Configuration file exists: {File.Exists(configPath)}");
                
                try
                {
                    // First get all configurations to see what's available
                    var allConfigs = await configManager.GetConfigurationAsync();
                    Console.WriteLine($"📋 Found {allConfigs.Suppliers.Count} supplier configurations:");
                    foreach (var supplier in allConfigs.Suppliers)
                    {
                        Console.WriteLine($"   - {supplier.Name} (Priority: {supplier.Detection.Priority})");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Error loading configuration: {ex.Message}");
                    return;
                }

                // Load DIOR configuration
                var diorConfig = await configManager.GetSupplierConfigurationAsync("DIOR");
                if (diorConfig == null)
                {
                    Console.WriteLine("❌ DIOR configuration not found!");
                    
                    // Try case-insensitive search manually
                    var allConfigs = await configManager.GetConfigurationAsync();
                    var foundDior = allConfigs.Suppliers.FirstOrDefault(s => 
                        string.Equals(s.Name, "DIOR", StringComparison.OrdinalIgnoreCase));
                    
                    if (foundDior != null)
                    {
                        Console.WriteLine($"✓ Found DIOR config with name: '{foundDior.Name}'");
                        diorConfig = foundDior;
                    }
                    else
                    {
                        Console.WriteLine("❌ DIOR configuration not found even with case-insensitive search!");
                        Console.WriteLine("Available suppliers:");
                        foreach (var s in allConfigs.Suppliers)
                        {
                            Console.WriteLine($"   - '{s.Name}'");
                        }
                        return;
                    }
                }

                Console.WriteLine($"✅ DIOR configuration loaded successfully!");
                Console.WriteLine($"   - Name: {diorConfig.Name}");
                Console.WriteLine($"   - Description: {diorConfig.Description}");
                Console.WriteLine($"   - Required Columns: {string.Join(", ", diorConfig.Detection.RequiredColumns)}");

                // Load DIOR file
                var diorFilePath = FindDiorFile();
                if (!File.Exists(diorFilePath))
                {
                    Console.WriteLine($"❌ DIOR file not found: {diorFilePath}");
                    return;
                }

                Console.WriteLine("📁 Reading DIOR Excel file...");
                var fileData = await fileReader.ReadFileAsync(diorFilePath);
                
                // Get file headers and validate
                var headerRow = fileData.dataRows.FirstOrDefault(r => r.HasData);
                if (headerRow == null)
                {
                    Console.WriteLine("❌ No header row found in file!");
                    return;
                }

                var fileHeaders = headerRow.Cells.Select(c => c.Value?.Trim()).Where(h => !string.IsNullOrWhiteSpace(h)).ToList();
                Console.WriteLine($"📋 File Headers: {fileHeaders.Count}");

                // Verify required columns exist
                Console.WriteLine($"\n🔍 Required columns verification:");
                var missingColumns = new List<string>();
                foreach (var required in diorConfig.Detection.RequiredColumns)
                {
                    var found = fileHeaders.Any(h => string.Equals(h, required, StringComparison.OrdinalIgnoreCase));
                    var status = found ? "✓" : "❌";
                    Console.WriteLine($"   {status} {required}");
                    
                    if (!found)
                        missingColumns.Add(required);
                }

                if (missingColumns.Any())
                {
                    Console.WriteLine($"❌ Missing required columns: {string.Join(", ", missingColumns)}. Aborting.");
                    return;
                }

                // Initialize normalizer
                var normalizer = new ConfigurationBasedNormalizer(diorConfig);
                var canHandle = normalizer.CanHandle(Path.GetFileName(diorFilePath), fileData.dataRows.Take(5));
                
                if (!canHandle)
                {
                    Console.WriteLine("❌ Normalizer cannot handle this file format.");
                    return;
                }

                Console.WriteLine("🔄 Processing file in both modes...\n");

                // Process in UnifiedProductCatalog mode (products)
                await ProcessFileToDatabase(normalizer, fileData, ProcessingMode.UnifiedProductCatalog, diorFilePath, service, repository, "Product Catalog");
                
                // Process in SupplierCommercialData mode (offers)
                await ProcessFileToDatabase(normalizer, fileData, ProcessingMode.SupplierCommercialData, diorFilePath, service, repository, "Commercial Data");

                Console.WriteLine("\n✅ File processing completed successfully!");
                
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error during processing: {ex.Message}");
                Console.WriteLine($"   Stack trace: {ex.StackTrace}");
            }

            Console.WriteLine("\nPress any key to continue...");
            Console.ReadKey();
        }

        /// <summary>
        /// Finds the configuration file using multiple search strategies
        /// </summary>
        private static string FindConfigurationFile()
        {
            var possiblePaths = new[]
            {
                // First try the copied file in the output directory
                Path.Combine("Configuration", "supplier-formats.json"),
                "supplier-formats.json",
                
                // Then try different relative paths from the current working directory
                Path.Combine("..", "..", "..", "SacksDataLayer", "Configuration", "supplier-formats.json"), // Debug build output
                Path.Combine("..", "..", "..", "..", "SacksDataLayer", "Configuration", "supplier-formats.json"), // Release build output  
                Path.Combine("..", "SacksDataLayer", "Configuration", "supplier-formats.json"), // If running from solution root
                Path.Combine("SacksDataLayer", "Configuration", "supplier-formats.json"), // If running from solution root
            };

            foreach (var path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    Console.WriteLine($"🔍 Found configuration file at: {path}");
                    return path;
                }
            }

            // If none found, try to find it by searching up the directory tree
            var currentDir = Directory.GetCurrentDirectory();
            var dir = new DirectoryInfo(currentDir);
            
            while (dir != null && dir.Parent != null)
            {
                var configPath = Path.Combine(dir.FullName, "SacksDataLayer", "Configuration", "supplier-formats.json");
                if (File.Exists(configPath))
                {
                    Console.WriteLine($"🔍 Found configuration file by searching up directory tree: {configPath}");
                    return configPath;
                }
                dir = dir.Parent;
            }

            // Fallback to the original path
            Console.WriteLine("⚠️ Configuration file not found in any expected location, using fallback path");
            return Path.Combine("..", "SacksDataLayer", "Configuration", "supplier-formats.json");
        }

        /// <summary>
        /// Finds the DIOR input file using multiple search strategies
        /// </summary>
        private static string FindDiorFile()
        {
            var possiblePaths = new[]
            {
                // First try the copied file in the output directory
                Path.Combine("Inputs", "DIOR short.xlsx"),
                "DIOR short.xlsx",
                
                // Try different relative paths and file names
                Path.Combine("..", "..", "..", "SacksDataLayer", "Inputs", "DIOR short.xlsx"),
                Path.Combine("..", "..", "..", "..", "SacksDataLayer", "Inputs", "DIOR short.xlsx"),
                Path.Combine("..", "SacksDataLayer", "Inputs", "DIOR short.xlsx"),
                Path.Combine("SacksDataLayer", "Inputs", "DIOR short.xlsx"),
            };

            foreach (var path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    Console.WriteLine($"🔍 Found DIOR file at: {path}");
                    return path;
                }
            }

            // If none found, try to find it by searching up the directory tree
            var currentDir = Directory.GetCurrentDirectory();
            var dir = new DirectoryInfo(currentDir);
            
            while (dir != null && dir.Parent != null)
            {
                var filePath = Path.Combine(dir.FullName, "SacksDataLayer", "Inputs", "DIOR short.xlsx");
                if (File.Exists(filePath))
                {
                    Console.WriteLine($"🔍 Found DIOR file by searching up directory tree: {filePath}");
                    return filePath;
                }
                dir = dir.Parent;
            }

            // Fallback to the original path
            Console.WriteLine("⚠️ DIOR file not found in any expected location, using fallback path");
            return Path.Combine("..", "SacksDataLayer", "Inputs", "DIOR short.xlsx");
        }

        private static async Task ProcessFileToDatabase(
            ConfigurationBasedNormalizer normalizer, 
            FileData fileData, 
            ProcessingMode mode, 
            string filePath, 
            ProductsService service,
            ProductsRepository repository,
            string modeName)
        {
            Console.WriteLine($"📦 Processing {modeName} mode...");
            
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            try
            {
                // Process the file using the normalizer with ProcessingContext
                var context = new ProcessingContext
                {
                    Mode = mode,
                    SourceFileName = Path.GetFileName(filePath),
                    SupplierName = "DIOR",
                    ProcessingIntent = $"Processing DIOR file in {modeName} mode"
                };

                var result = await normalizer.NormalizeAsync(fileData, context);
                
                if (result.Errors.Any())
                {
                    Console.WriteLine($"   ❌ Failed to process file: {string.Join(", ", result.Errors)}");
                    return;
                }

                Console.WriteLine($"   📊 Processed {result.Products.Count()} products from file");
                
                // Statistics
                int created = 0;
                int updated = 0;
                int skipped = 0;
                int errors = 0;

                // Process products in batches to reduce database pressure
                const int batchSize = 50;
                var productList = result.Products.ToList();
                var totalBatches = (int)Math.Ceiling((double)productList.Count / batchSize);

                Console.WriteLine($"   🔄 Processing {productList.Count} products in {totalBatches} batches of {batchSize}...");

                for (int batchIndex = 0; batchIndex < totalBatches; batchIndex++)
                {
                    var batch = productList.Skip(batchIndex * batchSize).Take(batchSize);
                    Console.WriteLine($"   📦 Processing batch {batchIndex + 1}/{totalBatches}...");

                    foreach (var product in batch)
                    {
                        try
                        {
                            // Skip products without SKU
                            if (string.IsNullOrWhiteSpace(product.SKU))
                            {
                                errors++;
                                Console.WriteLine($"   ❌ Skipping product without SKU: {product.Name}");
                                continue;
                            }

                            // Check if product already exists by SKU
                            var existingProduct = await service.GetProductBySKUAsync(product.SKU);
                            
                            if (existingProduct != null)
                            {
                                // Product exists - check for differences and update if needed
                                bool needsUpdate = false;
                                var updates = new List<string>();

                                // Compare basic properties
                                if (existingProduct.Name != product.Name)
                                {
                                    existingProduct.Name = product.Name;
                                    needsUpdate = true;
                                    updates.Add("Name");
                                }

                                if (existingProduct.Description != product.Description)
                                {
                                    existingProduct.Description = product.Description;
                                    needsUpdate = true;
                                    updates.Add("Description");
                                }

                                // Compare dynamic properties
                                foreach (var dynProp in product.DynamicProperties)
                                {
                                    var existingValue = existingProduct.GetDynamicProperty<object>(dynProp.Key);
                                    if (!object.Equals(existingValue, dynProp.Value))
                                    {
                                        existingProduct.SetDynamicProperty(dynProp.Key, dynProp.Value);
                                        needsUpdate = true;
                                        updates.Add(dynProp.Key);
                                    }
                                }

                                if (needsUpdate)
                                {
                                    await service.UpdateProductAsync(existingProduct, "DiorProcessor");
                                    updated++;
                                    if (updated <= 10) // Only show first 10 updates to avoid spam
                                    {
                                        Console.WriteLine($"   ✏️ Updated: {product.SKU} - {product.Name} ({string.Join(", ", updates)})");
                                    }
                                }
                                else
                                {
                                    skipped++;
                                }
                            }
                            else
                            {
                                // Product doesn't exist - create it
                                await service.CreateProductAsync(product, "DiorProcessor");
                                created++;
                                if (created <= 10) // Only show first 10 creates to avoid spam
                                {
                                    Console.WriteLine($"   ➕ Created: {product.SKU} - {product.Name}");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            errors++;
                            if (errors <= 5) // Only show first 5 errors to avoid spam
                            {
                                Console.WriteLine($"   ❌ Error processing {product.SKU}: {ex.Message}");
                            }
                            
                            // Add a small delay after errors to reduce pressure
                            await Task.Delay(100);
                        }
                    }

                    // Small delay between batches to reduce database pressure
                    if (batchIndex < totalBatches - 1)
                    {
                        await Task.Delay(200);
                    }
                }

                stopwatch.Stop();

                // Final statistics
                Console.WriteLine($"\n   📈 {modeName} Results:");
                Console.WriteLine($"      • Products created: {created}");
                Console.WriteLine($"      • Products updated: {updated}");
                Console.WriteLine($"      • Products skipped: {skipped}");
                if (errors > 0)
                {
                    Console.WriteLine($"      • Errors: {errors}");
                    if (errors > 5)
                        Console.WriteLine($"        (Only first 5 errors shown)");
                }
                Console.WriteLine($"      • Processing time: {stopwatch.ElapsedMilliseconds}ms");
                
                // Get total count from database
                var totalProducts = await repository.GetCountAsync(false);
                Console.WriteLine($"      • Total products in database: {totalProducts}");
                
                // Debug: Show all products in database
                var allProducts = await repository.GetAllAsync(false);
                Console.WriteLine($"      📋 Products in database:");
                var dbProductList = allProducts.ToList();
                for (int i = 0; i < Math.Min(dbProductList.Count, 5); i++)
                {
                    var p = dbProductList[i];
                    Console.WriteLine($"         [{i+1}] SKU: {p.SKU}, Name: {p.Name}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ❌ Failed to process {modeName}: {ex.Message}");
            }
        }
    }
}
