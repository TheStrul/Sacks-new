using SacksDataLayer.FileProcessing.Configuration;
using SacksDataLayer.FileProcessing.Services;
using SacksDataLayer.FileProcessing.Normalizers;
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
using System.Collections.Generic;

namespace SacksDataLayer.Services.Implementations
{
    /// <summary>
    /// Service implementation for unified file processing operations
    /// </summary>
    public class FileProcessingService : IFileProcessingService
    {
        private readonly SacksDbContext _context;
        private readonly IProductsService _productsService;
        private readonly ISuppliersService _suppliersService;
        private readonly ISupplierOffersService _supplierOffersService;
        private readonly IOfferProductsService _offerProductsService;

        public FileProcessingService(
            SacksDbContext context,
            IProductsService productsService,
            ISuppliersService suppliersService,
            ISupplierOffersService supplierOffersService,
            IOfferProductsService offerProductsService)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _productsService = productsService ?? throw new ArgumentNullException(nameof(productsService));
            _suppliersService = suppliersService ?? throw new ArgumentNullException(nameof(suppliersService));
            _supplierOffersService = supplierOffersService ?? throw new ArgumentNullException(nameof(supplierOffersService));
            _offerProductsService = offerProductsService ?? throw new ArgumentNullException(nameof(offerProductsService));
        }

        /// <summary>
        /// Processes a file (Excel, CSV, etc.) and imports data based on supplier configuration
        /// </summary>
        public async Task ProcessFileAsync(string filePath)
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

                // Ensure database is created (dev environment)
                Console.WriteLine("🔧 Ensuring database exists and is up-to-date...");
                await _context.Database.EnsureCreatedAsync();
                Console.WriteLine("✅ Database ready!");

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
                Console.WriteLine("\n🔄 Processing file as Supplier Offer...\n");

                // Process as SupplierOffer (suppliers + offers + offer-products)
                await ProcessSupplierOfferToDatabase(normalizer, fileData, filePath, supplierConfig);

                Console.WriteLine("\n✅ File processing completed successfully!");
                
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error during processing: {ex.Message}");
                Console.WriteLine($"   Stack trace: {ex.StackTrace}");
            }
        }

        private async Task<SupplierConfiguration?> DetectSupplierAsync(SupplierConfigurationManager configManager, string fileName)
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

        private async Task ProcessSupplierOfferToDatabase(
            ConfigurationBasedNormalizer normalizer,
            FileData fileData,
            string filePath,
            SupplierConfiguration supplierConfig)
        {
            try
            {
                Console.WriteLine($"📦 Processing file as Supplier Offer...");
                
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();

                // Create processing context for commercial mode
                var context = new ProcessingContext
                {
                    SourceFileName = Path.GetFileName(filePath),
                    ProcessingDate = DateTime.UtcNow
                };

                // Step 1: Create or get supplier
                Console.WriteLine($"   🏢 Creating/finding supplier: {supplierConfig.Name}");
                var supplier = await _suppliersService.CreateOrGetSupplierFromConfigAsync(
                    supplierConfig.Name,
                    supplierConfig.Description,
                    supplierConfig.Metadata?.Industry,
                    supplierConfig.Metadata?.Region,
                    "FileProcessor");

                Console.WriteLine($"   ✅ Supplier ready: {supplier.Name} (ID: {supplier.Id})");

                // Step 2: Create new offer for this file processing session
                Console.WriteLine($"   📋 Creating offer for file: {Path.GetFileName(filePath)}");
                var offer = await _supplierOffersService.CreateOfferFromFileAsync(
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
                                var existingProduct = await _productsService.GetProductBySKUAsync(productData.SKU);
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
                                        await _productsService.UpdateProductAsync(existingProduct);
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

                                    product = await _productsService.CreateProductAsync(newProduct);
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
                                var offerProduct = await _offerProductsService.CreateOrUpdateOfferProductAsync(
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
                Console.WriteLine($"\n   📈 Supplier Offer Results:");
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
                Console.WriteLine($"   ❌ Failed to process Supplier Offer: {ex.Message}");
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
    }
}
