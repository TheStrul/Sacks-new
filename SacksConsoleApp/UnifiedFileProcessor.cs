using SacksDataLayer;
using SacksDataLayer.FileProcessing.Configuration;
using SacksDataLayer.FileProcessing.Services;
using SacksDataLayer.FileProcessing.Normalizers;
using SacksDataLayer.FileProcessing.Interfaces;
using SacksDataLayer.FileProcessing.Models;
using SacksDataLayer.Data;
using SacksDataLayer.Repositories.Implementations;
using SacksDataLayer.Repositories.Interfaces;
using SacksDataLayer.Services.Implementations;
using SacksDataLayer.Services.Interfaces;
using SacksAIPlatform.InfrastructuresLayer.FileProcessing;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SacksConsoleApp
{
    public class UnifiedFileProcessor
    {
        public static async Task ProcessFileAsync(string filePath)
        {
            Console.WriteLine("=== Unified File Processing ===\n");

            try
            {
                // Validate file exists
                if (!File.Exists(filePath))
                {
                    Console.WriteLine($"❌ File not found: {filePath}");
                    return;
                }

                Console.WriteLine($"📁 Processing file: {Path.GetFileName(filePath)}");

                // Setup MySQL database connection
                var connectionString = @"Server=localhost;Port=3306;Database=SacksProductsDb;Uid=root;Pwd=;";
                var options = new DbContextOptionsBuilder<SacksDbContext>()
                    .UseMySQL(connectionString)
                    .Options;

                await using var context = new SacksDbContext(options);
                
                // Ensure database is created (dev environment)
                Console.WriteLine("🔧 Ensuring database exists and is up-to-date...");
                await context.Database.EnsureCreatedAsync();
                Console.WriteLine("✅ Database ready!");

                // Initialize repositories
                var productsRepository = new ProductsRepository(context);
                var suppliersRepository = new SuppliersRepository(context);
                var supplierOffersRepository = new SupplierOffersRepository(context);
                var offerProductsRepository = new OfferProductsRepository(context);

                // Initialize services
                var productsService = new ProductsService(productsRepository);
                var suppliersService = new SuppliersService(suppliersRepository);
                var supplierOffersService = new SupplierOffersService(supplierOffersRepository, suppliersRepository);
                var offerProductsService = new OfferProductsService(offerProductsRepository, supplierOffersRepository, productsRepository);

                // Initialize file processing services
                var configPath = FindConfigurationFile();
                var configManager = new SupplierConfigurationManager(configPath);
                var fileReader = new FileDataReader();

                Console.WriteLine($"🔍 Configuration file: {Path.GetFileName(configPath)}");

                // Auto-detect supplier from filename
                var fileName = Path.GetFileName(filePath);
                var supplierConfig = await DetectSupplierAsync(configManager, fileName);
                
                if (supplierConfig == null)
                {
                    Console.WriteLine($"❌ No supplier configuration found for file: {fileName}");
                    Console.WriteLine("💡 Add a supplier configuration with matching fileNamePatterns in supplier-formats.json");
                    return;
                }

                Console.WriteLine($"✅ Auto-detected supplier: {supplierConfig.Name}");
                Console.WriteLine($"   Description: {supplierConfig.Description}");

                // Read and process the file
                Console.WriteLine("📖 Reading Excel file...");
                var fileData = await fileReader.ReadFileAsync(filePath);
                
                // Get file headers and validate
                var headerRowIndex = supplierConfig.Transformation?.HeaderRowIndex ?? 0;
                var headerRow = fileData.dataRows.Skip(headerRowIndex).FirstOrDefault(r => r.HasData);
                if (headerRow == null)
                {
                    Console.WriteLine("❌ No header row found in file!");
                    return;
                }

                var fileHeaders = headerRow.Cells.Select(c => c.Value?.Trim()).Where(h => !string.IsNullOrWhiteSpace(h)).ToList();
                Console.WriteLine($"📋 Found {fileHeaders.Count} columns: {string.Join(", ", fileHeaders.Take(5))}{(fileHeaders.Count > 5 ? "..." : "")}");

                // Verify required columns exist
                Console.WriteLine($"\n🔍 Validating required columns:");
                var missingColumns = new List<string>();
                foreach (var required in supplierConfig.Detection.RequiredColumns)
                {
                    var found = fileHeaders.Any(h => string.Equals(h, required, StringComparison.OrdinalIgnoreCase));
                    var status = found ? "✓" : "❌";
                    Console.WriteLine($"   {status} {required}");
                    
                    if (!found)
                        missingColumns.Add(required);
                }

                if (missingColumns.Any())
                {
                    Console.WriteLine($"❌ Missing required columns: {string.Join(", ", missingColumns)}");
                    return;
                }

                // Initialize normalizer and process file
                var normalizer = new ConfigurationBasedNormalizer(supplierConfig);
                Console.WriteLine("\n🔄 Processing file in both modes...\n");

                // Process in UnifiedProductCatalog mode (products only)
                await ProcessProductCatalogToDatabase(normalizer, fileData, filePath, productsService, supplierConfig.Name);
                
                // Process in SupplierCommercialData mode (suppliers + offers + offer-products)
                await ProcessSupplierCommercialDataToDatabase(normalizer, fileData, filePath, 
                    suppliersService, supplierOffersService, offerProductsService, productsService, supplierConfig);

                Console.WriteLine("\n✅ File processing completed successfully!");
                
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error during processing: {ex.Message}");
                Console.WriteLine($"   Stack trace: {ex.StackTrace}");
            }
        }

        private static async Task<SupplierConfiguration?> DetectSupplierAsync(SupplierConfigurationManager configManager, string fileName)
        {
            try
            {
                var allConfigs = await configManager.GetConfigurationAsync();
                
                // Find supplier with matching filename pattern
                foreach (var supplier in allConfigs.Suppliers.OrderByDescending(s => s.Detection.Priority))
                {
                    foreach (var pattern in supplier.Detection.FileNamePatterns)
                    {
                        var regexPattern = pattern.Replace("*", ".*");
                        if (System.Text.RegularExpressions.Regex.IsMatch(fileName, regexPattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                        {
                            return supplier;
                        }
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error detecting supplier: {ex.Message}");
                return null;
            }
        }

        private static async Task ProcessProductCatalogToDatabase(
            ConfigurationBasedNormalizer normalizer,
            FileData fileData,
            string filePath,
            ProductsService productsService,
            string supplierName)
        {
            try
            {
                Console.WriteLine($"📦 Processing Product Catalog mode...");
                
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();

                // Create processing context for catalog mode
                var context = new ProcessingContext
                {
                    Mode = ProcessingMode.UnifiedProductCatalog,
                    SourceFileName = Path.GetFileName(filePath),
                    ProcessingDate = DateTime.UtcNow
                };

                // Normalize products using the configuration-based normalizer
                var result = await normalizer.NormalizeAsync(fileData, context);
                
                if (result.Errors.Any())
                {
                    Console.WriteLine($"   ❌ Processing errors: {string.Join(", ", result.Errors)}");
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

                if (totalBatches > 0)
                {
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
                                var existingProduct = await productsService.GetProductBySKUAsync(product.SKU);
                                
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
                                        await productsService.UpdateProductAsync(existingProduct);
                                        updated++;
                                        if (updated <= 10) // Only show first 10 updates to avoid spam
                                        {
                                            Console.WriteLine($"   ✏️ Updated: {product.SKU} - {product.Name} ({string.Join(", ", updates.Take(3))}{(updates.Count > 3 ? "..." : "")})");
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
                                    await productsService.CreateProductAsync(product);
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
                }

                stopwatch.Stop();

                // Final statistics
                Console.WriteLine($"\n   📈 Product Catalog Results:");
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
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ❌ Failed to process Product Catalog: {ex.Message}");
            }
        }

        private static async Task ProcessSupplierCommercialDataToDatabase(
            ConfigurationBasedNormalizer normalizer,
            FileData fileData,
            string filePath,
            ISuppliersService suppliersService,
            ISupplierOffersService supplierOffersService,
            IOfferProductsService offerProductsService,
            ProductsService productsService,
            SupplierConfiguration supplierConfig)
        {
            try
            {
                Console.WriteLine($"📦 Processing Supplier Commercial Data mode...");
                
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();

                // Create processing context for commercial mode
                var context = new ProcessingContext
                {
                    Mode = ProcessingMode.SupplierCommercialData,
                    SourceFileName = Path.GetFileName(filePath),
                    ProcessingDate = DateTime.UtcNow
                };

                // Step 1: Create or get supplier
                Console.WriteLine($"   🏢 Creating/finding supplier: {supplierConfig.Name}");
                var supplier = await suppliersService.CreateOrGetSupplierFromConfigAsync(
                    supplierConfig.Name,
                    supplierConfig.Description,
                    supplierConfig.Metadata?.Industry,
                    supplierConfig.Metadata?.Region,
                    "FileProcessor");

                Console.WriteLine($"   ✅ Supplier ready: {supplier.Name} (ID: {supplier.Id})");

                // Step 2: Create new offer for this file processing session
                Console.WriteLine($"   📋 Creating offer for file: {Path.GetFileName(filePath)}");
                var offer = await supplierOffersService.CreateOfferFromFileAsync(
                    supplier.Id,
                    Path.GetFileName(filePath),
                    context.ProcessingDate,
                    "USD", // Default currency, could be extracted from config
                    "File Import",
                    "FileProcessor");

                Console.WriteLine($"   ✅ Offer created: {offer.OfferName} (ID: {offer.Id})");

                // Step 3: Normalize products and extract commercial data
                var result = await normalizer.NormalizeAsync(fileData, context);
                
                if (result.Errors.Any())
                {
                    Console.WriteLine($"   ❌ Processing errors: {string.Join(", ", result.Errors)}");
                    return;
                }

                Console.WriteLine($"   📊 Processed {result.Products.Count()} commercial records from file");

                // Statistics
                int productsCreated = 0;
                int productsUpdated = 0;
                int offerProductsCreated = 0;
                int offerProductsUpdated = 0;
                int errors = 0;

                // Process products and create offer-product relationships
                const int batchSize = 50;
                var productList = result.Products.ToList();
                var totalBatches = (int)Math.Ceiling((double)productList.Count / batchSize);

                if (totalBatches > 0)
                {
                    Console.WriteLine($"   🔄 Processing {productList.Count} commercial records in {totalBatches} batches of {batchSize}...");

                    for (int batchIndex = 0; batchIndex < totalBatches; batchIndex++)
                    {
                        var batch = productList.Skip(batchIndex * batchSize).Take(batchSize);
                        Console.WriteLine($"   📦 Processing batch {batchIndex + 1}/{totalBatches}...");

                        foreach (var productData in batch)
                        {
                            try
                            {
                                // Skip products without SKU
                                if (string.IsNullOrWhiteSpace(productData.SKU))
                                {
                                    errors++;
                                    Console.WriteLine($"   ❌ Skipping record without SKU: {productData.Name}");
                                    continue;
                                }

                                // Step 3a: Ensure product exists (create or update core product data)
                                var existingProduct = await productsService.GetProductBySKUAsync(productData.SKU);
                                ProductEntity product;

                                if (existingProduct != null)
                                {
                                    // Update existing product with core properties only
                                    bool needsUpdate = false;
                                    if (existingProduct.Name != productData.Name)
                                    {
                                        existingProduct.Name = productData.Name;
                                        needsUpdate = true;
                                    }
                                    if (existingProduct.Description != productData.Description)
                                    {
                                        existingProduct.Description = productData.Description;
                                        needsUpdate = true;
                                    }

                                    // Update core product properties (not offer properties)
                                    var coreProperties = supplierConfig.PropertyClassification?.CoreProductProperties ?? new List<string>();
                                    foreach (var dynProp in productData.DynamicProperties)
                                    {
                                        if (coreProperties.Contains(dynProp.Key, StringComparer.OrdinalIgnoreCase))
                                        {
                                            var existingValue = existingProduct.GetDynamicProperty<object>(dynProp.Key);
                                            if (!object.Equals(existingValue, dynProp.Value))
                                            {
                                                existingProduct.SetDynamicProperty(dynProp.Key, dynProp.Value);
                                                needsUpdate = true;
                                            }
                                        }
                                    }

                                    if (needsUpdate)
                                    {
                                        await productsService.UpdateProductAsync(existingProduct);
                                        productsUpdated++;
                                    }
                                    product = existingProduct;
                                }
                                else
                                {
                                    // Create new product with core properties only
                                    var newProduct = new ProductEntity
                                    {
                                        Name = productData.Name,
                                        Description = productData.Description,
                                        SKU = productData.SKU
                                    };

                                    // Add core product properties only
                                    var coreProperties = supplierConfig.PropertyClassification?.CoreProductProperties ?? new List<string>();
                                    foreach (var dynProp in productData.DynamicProperties)
                                    {
                                        if (coreProperties.Contains(dynProp.Key, StringComparer.OrdinalIgnoreCase))
                                        {
                                            newProduct.SetDynamicProperty(dynProp.Key, dynProp.Value);
                                        }
                                    }

                                    product = await productsService.CreateProductAsync(newProduct);
                                    productsCreated++;
                                    if (productsCreated <= 10)
                                    {
                                        Console.WriteLine($"   ➕ Created product: {product.SKU} - {product.Name}");
                                    }
                                }

                                // Step 3b: Extract offer properties and create offer-product relationship
                                var offerProperties = new Dictionary<string, object?>();
                                var offerPropertyNames = supplierConfig.PropertyClassification?.OfferProperties ?? new List<string>();
                                
                                foreach (var dynProp in productData.DynamicProperties)
                                {
                                    if (offerPropertyNames.Contains(dynProp.Key, StringComparer.OrdinalIgnoreCase))
                                    {
                                        offerProperties[dynProp.Key] = dynProp.Value;
                                    }
                                }

                                // Create or update offer-product relationship
                                var offerProduct = await offerProductsService.CreateOrUpdateOfferProductAsync(
                                    offer.Id,
                                    product.Id,
                                    offerProperties,
                                    "FileProcessor");

                                if (offerProduct.CreatedAt == offerProduct.ModifiedAt) // Was created
                                {
                                    offerProductsCreated++;
                                    if (offerProductsCreated <= 10)
                                    {
                                        Console.WriteLine($"   🔗 Created offer-product: {product.SKU} -> Offer {offer.Id}");
                                    }
                                }
                                else // Was updated
                                {
                                    offerProductsUpdated++;
                                }
                            }
                            catch (Exception ex)
                            {
                                errors++;
                                if (errors <= 5) // Only show first 5 errors to avoid spam
                                {
                                    Console.WriteLine($"   ❌ Error processing {productData.SKU}: {ex.Message}");
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
                }

                stopwatch.Stop();

                // Final statistics
                Console.WriteLine($"\n   📈 Supplier Commercial Data Results:");
                Console.WriteLine($"      • Supplier: {supplier.Name} (ID: {supplier.Id})");
                Console.WriteLine($"      • Offer: {offer.OfferName} (ID: {offer.Id})");
                Console.WriteLine($"      • Products created: {productsCreated}");
                Console.WriteLine($"      • Products updated: {productsUpdated}");
                Console.WriteLine($"      • Offer-products created: {offerProductsCreated}");
                Console.WriteLine($"      • Offer-products updated: {offerProductsUpdated}");
                if (errors > 0)
                {
                    Console.WriteLine($"      • Errors: {errors}");
                    if (errors > 5)
                        Console.WriteLine($"        (Only first 5 errors shown)");
                }
                Console.WriteLine($"      • Processing time: {stopwatch.ElapsedMilliseconds}ms");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ❌ Failed to process Supplier Commercial Data: {ex.Message}");
                Console.WriteLine($"   Stack trace: {ex.StackTrace}");
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
                {
                    return path;
                }
            }

            throw new FileNotFoundException("Configuration file 'supplier-formats.json' not found in any of the expected locations.");
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
            try
            {
                Console.WriteLine($"📦 Processing {modeName} mode...");
                
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();

                // Create processing context
                var context = new ProcessingContext
                {
                    Mode = mode,
                    SourceFileName = Path.GetFileName(filePath),
                    ProcessingDate = DateTime.UtcNow
                };

                // Normalize products using the configuration-based normalizer
                var result = await normalizer.NormalizeAsync(fileData, context);
                
                if (result.Errors.Any())
                {
                    Console.WriteLine($"   ❌ Processing errors: {string.Join(", ", result.Errors)}");
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

                if (totalBatches > 0)
                {
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
                                        await service.UpdateProductAsync(existingProduct);
                                        updated++;
                                        if (updated <= 10) // Only show first 10 updates to avoid spam
                                        {
                                            Console.WriteLine($"   ✏️ Updated: {product.SKU} - {product.Name} ({string.Join(", ", updates.Take(3))}{(updates.Count > 3 ? "..." : "")})");
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
                                    await service.CreateProductAsync(product);
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
                var totalProducts = await repository.GetCountAsync();
                Console.WriteLine($"      • Total products in database: {totalProducts}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ❌ Failed to process {modeName}: {ex.Message}");
            }
        }
    }
}
