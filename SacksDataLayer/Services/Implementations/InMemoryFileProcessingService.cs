using SacksDataLayer.Configuration;
using SacksDataLayer.Entities;
using SacksDataLayer.Services.Interfaces;
using SacksDataLayer.FileProcessing.Services;
using SacksDataLayer.FileProcessing.Configuration;
using SacksAIPlatform.InfrastructuresLayer.FileProcessing;
using System.Diagnostics;

namespace SacksDataLayer.Services.Implementations
{
    /// <summary>
    /// 🚀 ULTIMATE PERFORMANCE: In-memory file processing service that loads all data into memory,
    /// processes everything in-memory, and saves only at the end in a single transaction
    /// </summary>
    public class InMemoryFileProcessingService : IInMemoryFileProcessingService
    {
        private readonly IInMemoryDataService _inMemoryDataService;
        private readonly IFileDataReader _fileDataReader;
        private readonly SupplierConfigurationManager _configManager;

        public InMemoryFileProcessingService(
            IInMemoryDataService inMemoryDataService,
            IFileDataReader fileDataReader,
            SupplierConfigurationManager configManager)
        {
            _inMemoryDataService = inMemoryDataService ?? throw new ArgumentNullException(nameof(inMemoryDataService));
            _fileDataReader = fileDataReader ?? throw new ArgumentNullException(nameof(fileDataReader));
            _configManager = configManager ?? throw new ArgumentNullException(nameof(configManager));
        }

        /// <summary>
        /// 🚀 ULTIMATE PERFORMANCE: Processes a file completely in memory and saves all changes in a single transaction at the end
        /// </summary>
        public async Task<InMemoryProcessingResult> ProcessFileInMemoryAsync(string filePath, CancellationToken cancellationToken = default)
        {
            var result = new InMemoryProcessingResult();
            var totalStopwatch = Stopwatch.StartNew();

            try
            {
                Console.WriteLine("=== 🚀 ULTIMATE PERFORMANCE: In-Memory File Processing ===\n");
                Console.WriteLine($"📁 Processing file: {Path.GetFileName(filePath)}");

                // Step 1: Load all data into memory
                Console.WriteLine("🔄 Step 1: Loading all database data into memory...");
                var loadStopwatch = Stopwatch.StartNew();
                await _inMemoryDataService.LoadAllDataAsync(cancellationToken);
                loadStopwatch.Stop();
                result.LoadDataDurationMs = loadStopwatch.ElapsedMilliseconds;

                var initialStats = _inMemoryDataService.GetCacheStats();
                result.TotalProductsInMemory = initialStats.Products;
                result.TotalSuppliersInMemory = initialStats.Suppliers;
                result.TotalOffersInMemory = initialStats.Offers;
                result.TotalOfferProductsInMemory = initialStats.OfferProducts;

                Console.WriteLine($"✅ Data loaded in {result.LoadDataDurationMs}ms:");
                Console.WriteLine($"   📦 Products: {result.TotalProductsInMemory:N0}");
                Console.WriteLine($"   🏢 Suppliers: {result.TotalSuppliersInMemory:N0}");
                Console.WriteLine($"   📋 Offers: {result.TotalOffersInMemory:N0}");
                Console.WriteLine($"   🔗 Offer-Products: {result.TotalOfferProductsInMemory:N0}");

                // Step 2: Process file in memory
                Console.WriteLine("\n🔄 Step 2: Processing file data in memory...");
                var processStopwatch = Stopwatch.StartNew();

                // Read file data
                var fileData = await _fileDataReader.ReadFileAsync(filePath);
                result.ProcessedRecords = fileData.DataRows.Count;

                // Auto-detect supplier configuration
                var supplierConfig = await _configManager.DetectSupplierFromFileAsync(filePath);
                if (supplierConfig == null)
                {
                    throw new InvalidOperationException($"Could not auto-detect supplier configuration for file: {Path.GetFileName(filePath)}");
                }

                Console.WriteLine($"✅ Auto-detected supplier: {supplierConfig.Name}");
                Console.WriteLine($"   📋 Processing {result.ProcessedRecords:N0} records");

                // Process supplier and offer in memory
                var supplier = await ProcessSupplierInMemoryAsync(supplierConfig);
                var offer = await ProcessOfferInMemoryAsync(supplier.Id, filePath, supplierConfig);

                // Process products in memory
                await ProcessProductsInMemoryAsync(fileData, offer, supplierConfig, result);

                processStopwatch.Stop();
                result.ProcessingDurationMs = processStopwatch.ElapsedMilliseconds;

                // Step 3: Save all changes to database in single transaction
                Console.WriteLine("\n💾 Step 3: Saving all changes to database in single transaction...");
                var saveStopwatch = Stopwatch.StartNew();

                var (productsCreated, productsUpdated, suppliersCreated, offersCreated, offerProductsCreated, offerProductsUpdated) = 
                    await _inMemoryDataService.SaveAllChangesToDatabaseAsync(cancellationToken);

                saveStopwatch.Stop();
                result.SaveDataDurationMs = saveStopwatch.ElapsedMilliseconds;

                // Update result statistics
                result.ProductsCreated = productsCreated;
                result.ProductsUpdated = productsUpdated;
                result.SuppliersCreated = suppliersCreated;
                result.OffersCreated = offersCreated;
                result.OfferProductsCreated = offerProductsCreated;
                result.OfferProductsUpdated = offerProductsUpdated;

                totalStopwatch.Stop();
                result.TotalDurationMs = totalStopwatch.ElapsedMilliseconds;
                result.Success = true;

                // Display results
                Console.WriteLine($"✅ Database save completed in {result.SaveDataDurationMs}ms");
                Console.WriteLine("\n📊 Processing Results:");
                Console.WriteLine($"   🏢 Suppliers created: {result.SuppliersCreated}");
                Console.WriteLine($"   📋 Offers created: {result.OffersCreated}");
                Console.WriteLine($"   📦 Products created: {result.ProductsCreated}");
                Console.WriteLine($"   🔄 Products updated: {result.ProductsUpdated}");
                Console.WriteLine($"   🔗 Offer-products created: {result.OfferProductsCreated}");
                Console.WriteLine($"   🔄 Offer-products updated: {result.OfferProductsUpdated}");

                Console.WriteLine("\n⏱️ Performance Breakdown:");
                Console.WriteLine($"   📥 Data loading: {result.LoadDataDurationMs}ms");
                Console.WriteLine($"   🔄 In-memory processing: {result.ProcessingDurationMs}ms");
                Console.WriteLine($"   💾 Database save: {result.SaveDataDurationMs}ms");
                Console.WriteLine($"   ⏱️ Total time: {result.TotalDurationMs}ms");

                result.Message = "File processed successfully with ultimate performance!";
                Console.WriteLine($"\n✅ {result.Message}");

                return result;
            }
            catch (Exception ex)
            {
                totalStopwatch.Stop();
                result.TotalDurationMs = totalStopwatch.ElapsedMilliseconds;
                result.Success = false;
                result.Message = $"Error processing file: {ex.Message}";
                result.Errors.Add(ex.Message);

                Console.WriteLine($"❌ {result.Message}");
                
                // Clear any pending changes on error
                _inMemoryDataService.ClearPendingChanges();

                return result;
            }
        }

        /// <summary>
        /// Processes supplier in memory
        /// </summary>
        private Task<SupplierEntity> ProcessSupplierInMemoryAsync(SupplierConfiguration supplierConfig)
        {
            // Check if supplier exists in memory
            var existingSupplier = _inMemoryDataService.GetSupplierByName(supplierConfig.Name);
            
            if (existingSupplier != null)
            {
                Console.WriteLine($"   ✅ Using existing supplier: {existingSupplier.Name} (ID: {existingSupplier.Id})");
                return Task.FromResult(existingSupplier);
            }

            // Create new supplier in memory
            var newSupplier = new SupplierEntity
            {
                Id = GenerateTemporaryId("supplier"),
                Name = supplierConfig.Name,
                Description = supplierConfig.Description,
                CreatedAt = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow
            };

            _inMemoryDataService.AddOrUpdateSupplierInMemory(newSupplier);
            Console.WriteLine($"   ➕ Created new supplier: {newSupplier.Name} (ID: {newSupplier.Id})");
            
            return Task.FromResult(newSupplier);
        }

        /// <summary>
        /// Processes offer in memory
        /// </summary>
        private Task<SupplierOfferEntity> ProcessOfferInMemoryAsync(int supplierId, string filePath, SupplierConfiguration supplierConfig)
        {
            var fileName = Path.GetFileName(filePath);
            var offerName = $"{supplierConfig.Name} - {fileName}";

            // Create new offer in memory
            var newOffer = new SupplierOfferEntity
            {
                Id = GenerateTemporaryId("offer"),
                SupplierId = supplierId,
                OfferName = offerName,
                Description = $"Processed from file: {fileName}",
                CreatedAt = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow
            };

            _inMemoryDataService.AddOrUpdateOfferInMemory(newOffer);
            Console.WriteLine($"   ➕ Created new offer: {newOffer.OfferName} (ID: {newOffer.Id})");
            
            return Task.FromResult(newOffer);
        }

        /// <summary>
        /// Processes products in memory
        /// </summary>
        private Task ProcessProductsInMemoryAsync(
            FileData fileData, 
            SupplierOfferEntity offer, 
            SupplierConfiguration supplierConfig,
            InMemoryProcessingResult result)
        {
            int productCreateCount = 0, productUpdateCount = 0;
            int offerProductCreateCount = 0, offerProductUpdateCount = 0;

            foreach (var row in fileData.DataRows)
            {
                try
                {
                    // Map row data to product using supplier configuration
                    var productData = MapRowToProduct(row, supplierConfig);
                    if (productData == null) continue;

                    // Process product
                    var product = ProcessProductInMemory(productData, ref productCreateCount, ref productUpdateCount);
                    
                    // Process offer product
                    ProcessOfferProductInMemory(product, offer, productData, ref offerProductCreateCount, ref offerProductUpdateCount);
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"Error processing row: {ex.Message}");
                    Console.WriteLine($"   ⚠️ Error processing row: {ex.Message}");
                }
            }

            Console.WriteLine($"   📦 Products: {productCreateCount} created, {productUpdateCount} updated");
            Console.WriteLine($"   🔗 Offer-products: {offerProductCreateCount} created, {offerProductUpdateCount} updated");
            
            return Task.CompletedTask;
        }

        /// <summary>
        /// Maps a file row to product data
        /// </summary>
        private ProductData? MapRowToProduct(RowData row, SupplierConfiguration supplierConfig)
        {
            try
            {
                var productData = new ProductData();

                // Map standard fields using supplier configuration column mappings
                foreach (var mapping in supplierConfig.ColumnMappings)
                {
                    var columnName = mapping.Key;
                    var propertyName = mapping.Value;
                    
                    // Find cell by column name (this is simplified - in real scenario we'd need column header mapping)
                    var cellValue = GetCellValueByColumnName(row, columnName);
                    if (!string.IsNullOrWhiteSpace(cellValue))
                    {
                        switch (propertyName?.ToLower())
                        {
                            case "name":
                                productData.Name = cellValue;
                                break;
                            case "ean":
                                productData.EAN = cellValue;
                                break;
                            case "description":
                                productData.Description = cellValue;
                                break;
                            default:
                                // Store as dynamic property
                                productData.DynamicProperties[propertyName ?? columnName] = cellValue;
                                break;
                        }
                    }
                }

                // If using column index mappings, process those too
                if (supplierConfig.Transformation.UseColumnIndexMapping)
                {
                    foreach (var indexMapping in supplierConfig.ColumnIndexMappings)
                    {
                        if (int.TryParse(indexMapping.Key, out int columnIndex) && columnIndex < row.Cells.Count)
                        {
                            var cellValue = row.Cells[columnIndex]?.Value?.ToString() ?? string.Empty;
                            var propertyName = indexMapping.Value;
                            
                            if (!string.IsNullOrWhiteSpace(cellValue))
                            {
                                switch (propertyName?.ToLower())
                                {
                                    case "name":
                                        productData.Name = cellValue;
                                        break;
                                    case "ean":
                                        productData.EAN = cellValue;
                                        break;
                                    case "description":
                                        productData.Description = cellValue;
                                        break;
                                    default:
                                        if (!string.IsNullOrWhiteSpace(propertyName))
                                        {
                                            productData.DynamicProperties[propertyName] = cellValue;
                                        }
                                        break;
                                }
                            }
                        }
                    }
                }

                // Validation: must have EAN
                if (string.IsNullOrWhiteSpace(productData.EAN))
                {
                    return null; // Skip rows without EAN
                }

                return productData;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error mapping row to product: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Helper method to get cell value by column name
        /// In real implementation, this would use header mapping
        /// </summary>
        private string GetCellValueByColumnName(RowData row, string columnName)
        {
            // Simplified implementation - in practice you'd map column names to indices
            // For now, just return the first cell if it exists
            if (row.Cells.Count > 0)
            {
                return row.Cells[0]?.Value?.ToString() ?? string.Empty;
            }
            return string.Empty;
        }

        /// <summary>
        /// Processes a single product in memory
        /// </summary>
        private ProductEntity ProcessProductInMemory(ProductData productData, ref int createCount, ref int updateCount)
        {
            var existingProduct = _inMemoryDataService.GetProductByEAN(productData.EAN);
            
            if (existingProduct != null)
            {
                // Update existing product in memory
                existingProduct.Name = productData.Name;
                existingProduct.Description = productData.Description;
                existingProduct.DynamicProperties = productData.DynamicProperties;
                existingProduct.ModifiedAt = DateTime.UtcNow;
                
                _inMemoryDataService.AddOrUpdateProductInMemory(existingProduct);
                updateCount++;
                return existingProduct;
            }
            else
            {
                // Create new product in memory
                var newProduct = new ProductEntity
                {
                    Id = GenerateTemporaryId("product"),
                    Name = productData.Name,
                    Description = productData.Description,
                    EAN = productData.EAN,
                    DynamicProperties = productData.DynamicProperties,
                    CreatedAt = DateTime.UtcNow,
                    ModifiedAt = DateTime.UtcNow
                };

                _inMemoryDataService.AddOrUpdateProductInMemory(newProduct);
                createCount++;
                return newProduct;
            }
        }

        /// <summary>
        /// Processes a single offer product in memory
        /// </summary>
        private void ProcessOfferProductInMemory(
            ProductEntity product, 
            SupplierOfferEntity offer, 
            ProductData productData, 
            ref int createCount, 
            ref int updateCount)
        {
            // Check if offer product already exists in memory
            var existingOfferProducts = _inMemoryDataService.GetOfferProductsByOfferId(offer.Id);
            var existingOfferProduct = existingOfferProducts.FirstOrDefault(op => op.ProductId == product.Id);

            if (existingOfferProduct != null)
            {
                // Update existing offer product
                existingOfferProduct.ProductProperties = productData.DynamicProperties;
                existingOfferProduct.ModifiedAt = DateTime.UtcNow;
                
                _inMemoryDataService.AddOrUpdateOfferProductInMemory(existingOfferProduct);
                updateCount++;
            }
            else
            {
                // Create new offer product
                var newOfferProduct = new OfferProductEntity
                {
                    Id = GenerateTemporaryId("offerproduct"),
                    OfferId = offer.Id,
                    ProductId = product.Id,
                    ProductProperties = productData.DynamicProperties,
                    CreatedAt = DateTime.UtcNow,
                    ModifiedAt = DateTime.UtcNow
                };

                _inMemoryDataService.AddOrUpdateOfferProductInMemory(newOfferProduct);
                createCount++;
            }
        }

        /// <summary>
        /// Generates temporary IDs for new entities (will be replaced by database on save)
        /// </summary>
        private int GenerateTemporaryId(string entityType)
        {
            // Use negative IDs to avoid conflicts with existing data
            // Database will assign proper IDs on save
            return -(int)(DateTime.UtcNow.Ticks % int.MaxValue);
        }

        /// <summary>
        /// Helper class for mapping file data to products
        /// </summary>
        private sealed class ProductData
        {
            public string Name { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public string EAN { get; set; } = string.Empty;
            public Dictionary<string, object?> DynamicProperties { get; set; } = new();
        }
    }
}
