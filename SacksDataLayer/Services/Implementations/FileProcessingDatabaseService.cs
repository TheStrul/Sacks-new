using SacksDataLayer.FileProcessing.Configuration;
using SacksDataLayer.Services.Interfaces;
using SacksDataLayer.Entities;
using SacksDataLayer.Data;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SacksDataLayer.Services.Implementations
{
    /// <summary>
    /// Service implementation for database operations during file processing
    /// </summary>
    public class FileProcessingDatabaseService : IFileProcessingDatabaseService
    {
        private readonly SacksDbContext _context;
        private readonly IProductsService _productsService;
        private readonly ISuppliersService _suppliersService;
        private readonly ISupplierOffersService _supplierOffersService;
        private readonly IOfferProductsService _offerProductsService;
        private readonly ILogger<FileProcessingDatabaseService> _logger;

        public FileProcessingDatabaseService(
            SacksDbContext context,
            IProductsService productsService,
            ISuppliersService suppliersService,
            ISupplierOffersService supplierOffersService,
            IOfferProductsService offerProductsService,
            ILogger<FileProcessingDatabaseService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _productsService = productsService ?? throw new ArgumentNullException(nameof(productsService));
            _suppliersService = suppliersService ?? throw new ArgumentNullException(nameof(suppliersService));
            _supplierOffersService = supplierOffersService ?? throw new ArgumentNullException(nameof(supplierOffersService));
            _offerProductsService = offerProductsService ?? throw new ArgumentNullException(nameof(offerProductsService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Ensures the database is ready for file processing operations
        /// </summary>
        public async Task EnsureDatabaseReadyAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            try
            {
                _logger.LogInformation("Ensuring database is ready for file processing operations");
                await _context.Database.EnsureCreatedAsync(cancellationToken);
                _logger.LogInformation("Database is ready for file processing");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to ensure database is ready");
                throw;
            }
        }

        /// <summary>
        /// Creates or retrieves a supplier based on the supplier configuration
        /// </summary>
        public async Task<SupplierEntity> CreateOrGetSupplierAsync(
            SupplierConfiguration supplierConfig, 
            string? createdBy = null, 
            CancellationToken cancellationToken = default)
        {
            if (supplierConfig == null) throw new ArgumentNullException(nameof(supplierConfig));
            
            cancellationToken.ThrowIfCancellationRequested();
            
            try
            {
                _logger.LogInformation("Creating or retrieving supplier: {SupplierName}", supplierConfig.Name);
                
                var supplier = await _suppliersService.CreateOrGetSupplierFromConfigAsync(
                    supplierConfig.Name,
                    supplierConfig.Description,
                    supplierConfig.Metadata?.Industry,
                    supplierConfig.Metadata?.Region,
                    createdBy ?? "FileProcessor");

                _logger.LogInformation("Supplier ready: {SupplierName} (ID: {SupplierId})", 
                    supplier.Name, supplier.Id);
                
                return supplier;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create or get supplier: {SupplierName}", supplierConfig.Name);
                throw;
            }
        }

        /// <summary>
        /// Creates a new offer for the file processing session
        /// </summary>
        public async Task<SupplierOfferEntity> CreateOfferAsync(
            SupplierEntity supplier,
            string fileName,
            DateTime processingDate,
            string currency = "USD",
            string description = "File Import",
            string? createdBy = null,
            CancellationToken cancellationToken = default)
        {
            if (supplier == null) throw new ArgumentNullException(nameof(supplier));
            if (string.IsNullOrWhiteSpace(fileName)) throw new ArgumentException("File name cannot be null or empty", nameof(fileName));
            
            cancellationToken.ThrowIfCancellationRequested();
            
            try
            {
                _logger.LogInformation("Creating offer for file: {FileName} and supplier: {SupplierName}", 
                    fileName, supplier.Name);
                
                var offer = await _supplierOffersService.CreateOfferFromFileAsync(
                    supplier.Id,
                    fileName,
                    processingDate,
                    currency,
                    description,
                    createdBy ?? "FileProcessor");

                _logger.LogInformation("Offer created: {OfferName} (ID: {OfferId})", 
                    offer.OfferName, offer.Id);
                
                return offer;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create offer for file: {FileName}", fileName);
                throw;
            }
        }

        /// <summary>
        /// Processes a batch of products with optimized bulk operations
        /// </summary>
        public async Task<FileProcessingBatchResult> ProcessProductBatchAsync(
            List<OfferProductEntity> products,
            SupplierOfferEntity offer,
            SupplierConfiguration supplierConfig,
            string? createdBy = null,
            CancellationToken cancellationToken = default)
        {
            if (products == null) throw new ArgumentNullException(nameof(products));
            if (offer == null) throw new ArgumentNullException(nameof(offer));
            if (supplierConfig == null) throw new ArgumentNullException(nameof(supplierConfig));
            
            cancellationToken.ThrowIfCancellationRequested();
            
            var result = new FileProcessingBatchResult();
            var processorName = createdBy ?? "FileProcessor";
            
            try
            {
                _logger.LogInformation("Processing batch of {ProductCount} products for offer: {OfferId}", 
                    products.Count, offer.Id);

                // Filter out products without EAN
                var validProducts = products.Where(p => !string.IsNullOrWhiteSpace(p.Product.EAN)).ToList();
                var invalidCount = products.Count - validProducts.Count;
                result.Errors += invalidCount;

                if (invalidCount > 0)
                {
                    var errorMessage = $"Skipped {invalidCount} records without valid EAN";
                    result.ErrorMessages.Add(errorMessage);
                    _logger.LogWarning("Skipped {InvalidCount} records without valid EAN in batch", invalidCount);
                }

                if (!validProducts.Any())
                {
                    _logger.LogWarning("No valid products found in batch");
                    return result;
                }

                // Step 1: Prepare products for bulk create/update
                var coreProperties = supplierConfig.PropertyClassification?.CoreProductProperties ?? new List<string>();
                var productsForBulkOperation = PrepareProductsForBulkOperation(validProducts, coreProperties);

                // Step 2: Bulk create/update products
                var (created, updated, bulkErrors) = await _productsService.BulkCreateOrUpdateProductsOptimizedAsync(
                    productsForBulkOperation, processorName);

                result.ProductsCreated += created;
                result.ProductsUpdated += updated;
                result.Errors += bulkErrors;

                _logger.LogInformation("Bulk processed products: {Created} created, {Updated} updated, {Errors} errors",
                    created, updated, bulkErrors);

                // Step 3: Process offer-products
                _logger.LogInformation("About to process offer-products for {ProductCount} products", validProducts.Count);
                await ProcessOfferProductsAsync(validProducts, offer, supplierConfig, processorName, result, cancellationToken);
                _logger.LogInformation("Completed offer-products processing");

                _logger.LogInformation("Completed batch processing: {ProductsCreated} products created, {ProductsUpdated} updated, " +
                    "{OfferProductsCreated} offer-products created, {OfferProductsUpdated} updated, {Errors} errors",
                    result.ProductsCreated, result.ProductsUpdated, result.OfferProductsCreated, result.OfferProductsUpdated, result.Errors);

                Console.WriteLine($"   🔍 DEBUG: Final result being returned - Products Created={result.ProductsCreated}, Updated={result.ProductsUpdated}, OfferProducts Created={result.OfferProductsCreated}, Updated={result.OfferProductsUpdated}, Errors={result.Errors}");

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process product batch for offer: {OfferId}", offer.Id);
                result.Errors += products.Count; // Count all items as errors
                result.ErrorMessages.Add($"Batch processing failed: {ex.Message}");
                return result;
            }
        }

        private List<ProductEntity> PrepareProductsForBulkOperation(List<OfferProductEntity> validProducts, List<string> coreProperties)
        {
            var productsForBulkOperation = new List<ProductEntity>();

            foreach (var productData in validProducts)
            {
                var productEntity = new ProductEntity
                {
                    Name = productData.Product.Name,
                    Description = productData.Product.Description,
                    EAN = productData.Product.EAN
                };

                // Add core product properties only
                foreach (var dynProp in productData.Product.DynamicProperties)
                {
                    if (coreProperties.Contains(dynProp.Key, StringComparer.OrdinalIgnoreCase))
                    {
                        productEntity.SetDynamicProperty(dynProp.Key, dynProp.Value);
                    }
                }

                productsForBulkOperation.Add(productEntity);
            }

            return productsForBulkOperation;
        }

        

        private List<(int ProductId, Dictionary<string, object?> OfferProperties)> PrepareOfferProductsForProcessing(
            List<ProductEntity> validProducts,
            Dictionary<string, ProductEntity> productLookup,
            List<string> offerPropertyNames)
        {
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

            return offerProductsToProcess;
        }

        /// <summary>
        /// Processes offer-products for the given products and offer
        /// </summary>
        private async Task ProcessOfferProductsAsync(
            List<OfferProductEntity> validProducts,
            SupplierOfferEntity offer,
            SupplierConfiguration supplierConfig,
            string processorName,
            FileProcessingBatchResult result,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("ProcessOfferProductsAsync called with {ProductCount} products for offer {OfferId}", 
                validProducts.Count, offer.Id);
            
            try
            {
                // Get product lookup by EAN to map offer products to saved products
                var productEANs = validProducts.Select(vp => vp.Product.EAN).ToList();
                var savedProducts = await _context.Products
                    .Where(p => productEANs.Contains(p.EAN))
                    .ToDictionaryAsync(p => p.EAN, p => p, cancellationToken);

                var offerProductsToCreate = new List<OfferProductEntity>();
                var offerProductsToUpdate = new List<OfferProductEntity>();

                Console.WriteLine($"   🔍 DEBUG: ProcessOfferProductsAsync called with {validProducts.Count} products");
                Console.WriteLine($"   🔍 DEBUG: Current result counts BEFORE: Products Created={result.ProductsCreated}, Updated={result.ProductsUpdated}, OfferProducts Created={result.OfferProductsCreated}, Updated={result.OfferProductsUpdated}");

                foreach (var productData in validProducts)
                {
                    if (savedProducts.TryGetValue(productData.Product.EAN, out var savedProduct))
                    {
                        // Check if offer product already exists
                        var existingOfferProduct = await _context.OfferProducts
                            .FirstOrDefaultAsync(op => op.OfferId == offer.Id && op.ProductId == savedProduct.Id, cancellationToken);

                        if (existingOfferProduct != null)
                        {
                            // Update existing offer product
                            existingOfferProduct.Price = productData.Price;
                            existingOfferProduct.Capacity = productData.Capacity;
                            existingOfferProduct.Discount = productData.Discount;
                            existingOfferProduct.ListPrice = productData.ListPrice;
                            existingOfferProduct.UnitOfMeasure = productData.UnitOfMeasure;
                            existingOfferProduct.MinimumOrderQuantity = productData.MinimumOrderQuantity;
                            existingOfferProduct.MaximumOrderQuantity = productData.MaximumOrderQuantity;
                            existingOfferProduct.IsAvailable = productData.IsAvailable;
                            existingOfferProduct.UpdateModified();

                            // Update dynamic properties
                            existingOfferProduct.ProductProperties.Clear();
                            foreach (var prop in productData.ProductProperties)
                            {
                                existingOfferProduct.SetProductProperty(prop.Key, prop.Value);
                            }

                            offerProductsToUpdate.Add(existingOfferProduct);
                        }
                        else
                        {
                            // Create new offer product
                            var newOfferProduct = new OfferProductEntity
                            {
                                OfferId = offer.Id,
                                ProductId = savedProduct.Id,
                                Price = productData.Price,
                                Capacity = productData.Capacity,
                                Discount = productData.Discount,
                                ListPrice = productData.ListPrice,
                                UnitOfMeasure = productData.UnitOfMeasure,
                                MinimumOrderQuantity = productData.MinimumOrderQuantity,
                                MaximumOrderQuantity = productData.MaximumOrderQuantity,
                                IsAvailable = productData.IsAvailable,
                                CreatedAt = DateTime.UtcNow
                            };

                            // Copy dynamic properties
                            foreach (var prop in productData.ProductProperties)
                            {
                                newOfferProduct.SetProductProperty(prop.Key, prop.Value);
                            }

                            offerProductsToCreate.Add(newOfferProduct);
                        }
                    }
                }

                // Bulk create new offer products
                if (offerProductsToCreate.Any())
                {
                    await _context.OfferProducts.AddRangeAsync(offerProductsToCreate, cancellationToken);
                    result.OfferProductsCreated += offerProductsToCreate.Count;
                }

                // Update counts for modified offer products
                result.OfferProductsUpdated += offerProductsToUpdate.Count;

                Console.WriteLine($"   🔍 DEBUG: OfferProducts processing completed. Created {offerProductsToCreate.Count}, Updated {offerProductsToUpdate.Count}");
                Console.WriteLine($"   🔍 DEBUG: Current result counts AFTER: Products Created={result.ProductsCreated}, Updated={result.ProductsUpdated}, OfferProducts Created={result.OfferProductsCreated}, Updated={result.OfferProductsUpdated}");

                _logger.LogInformation("Processed offer products: {Created} created, {Updated} updated", 
                    offerProductsToCreate.Count, offerProductsToUpdate.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process offer products for offer: {OfferId}", offer.Id);
                throw;
            }
        }
    }
}
