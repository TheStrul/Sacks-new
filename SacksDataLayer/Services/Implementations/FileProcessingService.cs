using SacksDataLayer.FileProcessing.Configuration;
using SacksDataLayer.FileProcessing.Services;
using SacksDataLayer.FileProcessing.Normalizers;
using SacksDataLayer.FileProcessing.Models;
using SacksDataLayer.Data;
using SacksDataLayer.Repositories.Implementations;
using SacksDataLayer.Repositories.Interfaces;
using SacksDataLayer.Services.Implementations;
using SacksDataLayer.Services.Interfaces;
using SacksDataLayer.Entities;
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
                var headerRow = fileData.DataRows.Skip(headerRowIndex).FirstOrDefault(r => r.HasData);
                if (headerRow == null)
                {
                    Console.WriteLine("❌ No header row found in file!");
                    return;
                }

                var fileHeaders = headerRow.Cells.Select(c => c.Value?.Trim()).Where(h => !string.IsNullOrWhiteSpace(h)).ToList();
                Console.WriteLine($"📋 Found {fileHeaders.Count} columns: {string.Join(", ", fileHeaders.Take(5))}{(fileHeaders.Count > 5 ? "..." : "")}");

                // Simple validation - just check column count if specified
                Console.WriteLine($"\n🔍 Validating file structure:");
                if (supplierConfig.Validation.ExpectedColumnCount > 0)
                {
                    var status = fileHeaders.Count == supplierConfig.Validation.ExpectedColumnCount ? "✓" : "❌";
                    Console.WriteLine($"   {status} Expected {supplierConfig.Validation.ExpectedColumnCount} columns, found {fileHeaders.Count}");
                    
                    if (fileHeaders.Count != supplierConfig.Validation.ExpectedColumnCount)
                    {
                        Console.WriteLine($"⚠️  Column count mismatch. Expected {supplierConfig.Validation.ExpectedColumnCount}, found {fileHeaders.Count}");
                    }
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
                
                // Find supplier with matching filename pattern (no priority ordering since Priority was removed)
                foreach (var supplier in allConfigs.Suppliers)
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
                // 🚀 PERFORMANCE OPTIMIZATION: Use larger batches and bulk operations
                const int batchSize = 500; // Increased from 50 to 500
                var productList = result.Products.ToList();
                var totalBatches = (int)Math.Ceiling((double)productList.Count / batchSize);

                if (totalBatches > 0)
                {
                    Console.WriteLine($"   🔄 Processing {productList.Count} commercial records in {totalBatches} batches of {batchSize}...");

                    for (int batchIndex = 0; batchIndex < totalBatches; batchIndex++)
                    {
                        var batch = productList.Skip(batchIndex * batchSize).Take(batchSize).ToList();
                        Console.WriteLine($"   📦 Processing batch {batchIndex + 1}/{totalBatches} ({batch.Count} items)...");

                        try
                        {
                            // 🚀 PERFORMANCE: Process entire batch with optimized bulk operations
                            var batchResults = await ProcessProductBatchOptimizedAsync(batch, offer, supplierConfig);
                            productsCreated += batchResults.productsCreated;
                            productsUpdated += batchResults.productsUpdated;
                            offerProductsCreated += batchResults.offerProductsCreated;
                            offerProductsUpdated += batchResults.offerProductsUpdated;
                            errors += batchResults.errors;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"   ❌ Error processing batch {batchIndex + 1}: {ex.Message}");
                            errors += batch.Count; // Count all items in failed batch as errors
                        }

                        // Reduced delay between batches
                        if (batchIndex < totalBatches - 1)
                        {
                            await Task.Delay(50); // Reduced from 200ms to 50ms
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

        /// <summary>
        /// 🚀 PERFORMANCE OPTIMIZED: Process a batch of products using bulk operations
        /// </summary>
        private async Task<(int productsCreated, int productsUpdated, int offerProductsCreated, int offerProductsUpdated, int errors)> ProcessProductBatchOptimizedAsync(
            List<ProductEntity> batch, 
            SupplierOfferEntity offer, 
            SupplierConfiguration supplierConfig)
        {
            int productsCreated = 0, productsUpdated = 0, offerProductsCreated = 0, offerProductsUpdated = 0, errors = 0;

            // Filter out products without EAN
            var validProducts = batch.Where(p => !string.IsNullOrWhiteSpace(p.EAN)).ToList();
            var invalidCount = batch.Count - validProducts.Count;
            errors += invalidCount;

            if (invalidCount > 0)
            {
                Console.WriteLine($"   ⚠️  Skipped {invalidCount} records without valid EAN");
            }

            if (!validProducts.Any())
                return (productsCreated, productsUpdated, offerProductsCreated, offerProductsUpdated, errors);

            // Step 1: Prepare products for bulk create/update
            var coreProperties = supplierConfig.PropertyClassification?.CoreProductProperties ?? new List<string>();
            var productsForBulkOperation = new List<ProductEntity>();

            foreach (var productData in validProducts)
            {
                var productEntity = new ProductEntity
                {
                    Name = productData.Name,
                    Description = productData.Description,
                    EAN = productData.EAN
                };

                // Add core product properties only
                foreach (var dynProp in productData.DynamicProperties)
                {
                    if (coreProperties.Contains(dynProp.Key, StringComparer.OrdinalIgnoreCase))
                    {
                        productEntity.SetDynamicProperty(dynProp.Key, dynProp.Value);
                    }
                }

                productsForBulkOperation.Add(productEntity);
            }

            // Step 2: 🚀 BULK OPERATION - Create/Update all products in batch
            var (created, updated, bulkErrors) = await _productsService.BulkCreateOrUpdateProductsOptimizedAsync(
                productsForBulkOperation, "FileProcessor");

            productsCreated += created;
            productsUpdated += updated;
            errors += bulkErrors;

            if (created > 0 && productsCreated <= 20)
            {
                Console.WriteLine($"   ➕ Bulk created {created} products");
            }
            if (updated > 0 && productsUpdated <= 20)
            {
                Console.WriteLine($"   🔄 Bulk updated {updated} products");
            }

            // Step 3: 🚀 OPTIMIZED BULK OFFER-PRODUCT PROCESSING
            var offerPropertyNames = supplierConfig.PropertyClassification?.OfferProperties ?? new List<string>();
            
            if (validProducts.Any() && offerPropertyNames.Any())
            {
                // 🚀 PERFORMANCE: Get all products with single bulk lookup
                var eans = validProducts.Select(p => p.EAN).ToList();
                var productLookup = await _productsService.GetProductsByEANsBulkAsync(eans);

                // Prepare offer-products for bulk processing
                var offerProductsToProcess = new List<(int ProductId, Dictionary<string, object?> OfferProperties)>();

                foreach (var productData in validProducts)
                {
                    if (productLookup.TryGetValue(productData.EAN, out var product))
                    {
                        var offerProperties = new Dictionary<string, object?>();
                        foreach (var dynProp in productData.DynamicProperties)
                        {
                            if (offerPropertyNames.Contains(dynProp.Key, StringComparer.OrdinalIgnoreCase))
                            {
                                offerProperties[dynProp.Key] = dynProp.Value;
                            }
                        }
                        offerProductsToProcess.Add((product.Id, offerProperties));
                    }
                }

                // Process offer-products sequentially to avoid DbContext concurrency issues
                // 🚀 PERFORMANCE: Still optimized with bulk product lookup above
                foreach (var (productId, offerProperties) in offerProductsToProcess)
                {
                    try
                    {
                        var offerProduct = await _offerProductsService.CreateOrUpdateOfferProductAsync(
                            offer.Id, productId, offerProperties, "FileProcessor");

                        if (offerProduct.CreatedAt == offerProduct.ModifiedAt)
                            offerProductsCreated++;
                        else
                            offerProductsUpdated++;
                    }
                    catch (Exception ex)
                    {
                        errors++;
                        if (errors <= 3)
                        {
                            Console.WriteLine($"   ❌ Error creating offer-product: {ex.Message}");
                        }
                    }
                }

                if (offerProductsCreated > 0)
                    Console.WriteLine($"   🔗 Bulk created {offerProductsCreated} offer-products");
                if (offerProductsUpdated > 0)
                    Console.WriteLine($"   🔄 Bulk updated {offerProductsUpdated} offer-products");
            }

            return (productsCreated, productsUpdated, offerProductsCreated, offerProductsUpdated, errors);
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
