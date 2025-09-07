using SacksDataLayer.FileProcessing.Configuration;
using SacksDataLayer.Services.Interfaces;
using SacksDataLayer.Repositories.Interfaces;
using SacksDataLayer.Entities;
using SacksDataLayer.Data;
using SacksDataLayer.Exceptions;
using SacksDataLayer.Models;
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
        /// Inserts or updates a supplier offer and all its products/offer-products in a single transaction
        /// This method handles the database state-dependent operations
        /// </summary>
        public async Task<FileProcessingResult> InsertOrUpdateSupplierOfferAsync(
            SupplierOfferAnnex analysisOffer,
            SupplierOfferAnnex dbOffer,
            SupplierConfiguration supplierConfig,
            string? createdBy = null,
            CancellationToken cancellationToken = default)
        {
            if (analysisOffer == null) throw new ArgumentNullException(nameof(analysisOffer));
            if (dbOffer == null) throw new ArgumentNullException(nameof(dbOffer));
            if (supplierConfig == null) throw new ArgumentNullException(nameof(supplierConfig));
            
            cancellationToken.ThrowIfCancellationRequested();
            
            var result = new FileProcessingResult();
            var processorName = createdBy ?? "FileProcessor";
            
            try
            {
                _logger.LogInformation("Starting insert/update operations for {ProductCount} products", 
                    analysisOffer.OfferProducts.Count);

                _logger.LogDebug("InsertOrUpdateSupplierOfferAsync called with {ProductCount} products for offer ID: {OfferId}", 
                    analysisOffer.OfferProducts.Count, dbOffer.Id);

                // Step 1: Get all unique EANs from the analysis result
                var productEANs = analysisOffer.OfferProducts
                    .Select(op => op.Product.EAN)
                    .Where(ean => !string.IsNullOrWhiteSpace(ean))
                    .Distinct()
                    .ToList();

                // Step 2: Bulk lookup existing products by EAN
                var existingProducts = await _context.Products
                    .Where(p => productEANs.Contains(p.EAN))
                    .ToDictionaryAsync(p => p.EAN, p => p, cancellationToken);

                _logger.LogDebug("Found {ExistingCount} existing products out of {TotalCount} EANs", 
                    existingProducts.Count, productEANs.Count);

                var productsToCreate = new List<ProductEntity>();
                var productsToUpdate = new List<ProductEntity>();
                var offerProductsToCreate = new List<OfferProductAnnex>();

                // Step 3: Process each product offer from analysis
                foreach (var analysisOfferProduct in analysisOffer.OfferProducts)
                {
                    var analysisProduct = analysisOfferProduct.Product;
                    ProductEntity dbProduct;

                    if (!string.IsNullOrWhiteSpace(analysisProduct.EAN) && 
                        existingProducts.TryGetValue(analysisProduct.EAN, out var existingProduct))
                    {
                        // Update existing product
                        existingProduct.Name = analysisProduct.Name;
                        existingProduct.DynamicProperties = analysisProduct.DynamicProperties;
                        existingProduct.ModifiedAt = DateTime.UtcNow;
                        
                        productsToUpdate.Add(existingProduct);
                        dbProduct = existingProduct;
                        result.ProductsUpdated++;
                    }
                    else
                    {
                        // Create new product (including those without EANs)
                        analysisProduct.CreatedAt = DateTime.UtcNow;
                        analysisProduct.Id = 0; // Ensure it's treated as new
                        
                        productsToCreate.Add(analysisProduct);
                        dbProduct = analysisProduct;
                        result.ProductsCreated++;
                    }

                    // Create offer-product relationship
                    var offerProduct = new OfferProductAnnex
                    {
                        OfferId = dbOffer.Id,
                        Product = dbProduct, // Navigation property will handle ID assignment
                        Price = analysisOfferProduct.Price,
                        Quantity = analysisOfferProduct.Quantity,
                        CreatedAt = DateTime.UtcNow,
                        OfferProperties = analysisOfferProduct.OfferProperties
                    };
                    offerProduct.SerializeOfferProperties();

                    offerProductsToCreate.Add(offerProduct);
                }

                // Step 4: Bulk database operations
                if (productsToCreate.Any())
                {
                    await _context.Products.AddRangeAsync(productsToCreate, cancellationToken);
                    _logger.LogInformation("Added {Count} new products", productsToCreate.Count);
                }

                if (productsToUpdate.Any())
                {
                    _context.Products.UpdateRange(productsToUpdate);
                    _logger.LogInformation("Updated {Count} existing products", productsToUpdate.Count);
                }

                if (offerProductsToCreate.Any())
                {
                    await _context.OfferProducts.AddRangeAsync(offerProductsToCreate, cancellationToken);
                    result.OfferProductsCreated = offerProductsToCreate.Count;
                    _logger.LogInformation("Added {Count} offer-product relationships", offerProductsToCreate.Count);
                }

                _logger.LogDebug("Final result - Products Created={ProductsCreated}, Updated={ProductsUpdated}, OfferProducts Created={OfferProductsCreated}, Errors={Errors}", 
                    result.ProductsCreated, result.ProductsUpdated, result.OfferProductsCreated, result.Errors);

                _logger.LogInformation("Completed insert/update operations: {ProductsCreated} products created, {ProductsUpdated} updated, " +
                    "{OfferProductsCreated} offer-products created, {Errors} errors",
                    result.ProductsCreated, result.ProductsUpdated, result.OfferProductsCreated, result.Errors);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to insert/update supplier offer: {OfferId}", dbOffer.Id);
                result.Errors += analysisOffer.OfferProducts.Count; // Count all items as errors
                result.ErrorMessages.Add($"Insert/update operations failed: {ex.Message}");
                return result;
            }
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
        /// Ensures no duplicate offer exists - deletes existing offer if found (dev mode).
        /// In development mode, deletes existing offer if found to allow re-processing.
        /// </summary>
        public async Task EnsureOfferCanBeProcessedAsync(
            int supplierId, 
            string fileName, 
            string supplierName, 
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("File name cannot be null or empty", nameof(fileName));
            
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                // Create the expected offer name using the same logic as CreateOfferFromFileAsync
                var expectedOfferName = $"{supplierName} - {fileName}";
                
                
                var existingOffers = await _supplierOffersService.GetOffersBySupplierAsync(supplierId);
                var existingOffer = existingOffers.FirstOrDefault(o => o.OfferName!.Equals(expectedOfferName, StringComparison.OrdinalIgnoreCase));

                if (existingOffer != null)
                {
                    _logger.LogWarning("🔄 REPROCESSING: Existing offer found - deleting and replacing: {SupplierName} - {OfferName}", supplierName, expectedOfferName);
                    _logger.LogInformation("Deleting existing offer ID: {OfferId} with {ProductCount} products", 
                        existingOffer.Id, existingOffer.OfferProducts?.Count ?? 0);
                    
                    // 🔧 FIX: Clear change tracker before deletion to prevent tracking conflicts
                    _context.ChangeTracker.Clear();
                    
                    // Delete the existing offer (cascade will automatically delete all OfferProducts)
                    var deleteSuccess = await _supplierOffersService.DeleteOfferAsync(existingOffer.Id);
                    
                    if (deleteSuccess)
                    {
                        _logger.LogInformation("✅ Successfully deleted existing offer: {OfferName}", expectedOfferName);
                        
                        // Clear change tracker again after deletion to ensure clean state
                        _context.ChangeTracker.Clear();
                        _logger.LogDebug("Cleared change tracker after offer deletion to ensure clean state");
                    }
                    else
                    {
                        _logger.LogError("❌ Failed to delete existing offer: {OfferName}", expectedOfferName);
                        throw new InvalidOperationException($"Failed to delete existing offer '{expectedOfferName}' for supplier '{supplierName}'");
                    }
                }
                
                _logger.LogDebug("Offer validation passed for: {SupplierName} - {FileName}", supplierName, fileName);
            }
            catch (Exception ex) when (!(ex is InvalidOperationException))
            {
                _logger.LogError(ex, "Failed to validate/replace offer for supplier: {SupplierName}, file: {FileName}", 
                    supplierName, fileName);
                throw;
            }
        }

        /// <summary>
        /// Creates a new offer for the file processing session
        /// </summary>
        public async Task<SupplierOfferAnnex> CreateOfferAsync(
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
        public async Task<FileProcessingResult> ProcessProductAsync(
            List<OfferProductAnnex> products,
            SupplierOfferAnnex offer,
            SupplierConfiguration supplierConfig,
            string? createdBy = null,
            CancellationToken cancellationToken = default)
        {
            if (products == null) throw new ArgumentNullException(nameof(products));
            if (offer == null) throw new ArgumentNullException(nameof(offer));
            if (supplierConfig == null) throw new ArgumentNullException(nameof(supplierConfig));
            
            cancellationToken.ThrowIfCancellationRequested();
            
            var result = new FileProcessingResult();
            var processorName = createdBy ?? "FileProcessor";
            
            try
            {
                _logger.LogInformation("Processing batch of {ProductCount} products for offer: {OfferName}", 
                    products.Count, offer.OfferName);

                // 🔧 FIX: Clear EF change tracker to prevent entity tracking conflicts when reprocessing files
                _context.ChangeTracker.Clear();
                _logger.LogDebug("Cleared EF change tracker to prevent entity tracking conflicts during reprocessing");

                // Filter out products without EAN - this is normal, not an error
                var validProducts = products.Where(p => !string.IsNullOrWhiteSpace(p.Product.EAN)).ToList();
                var skippedCount = products.Count - validProducts.Count;

                if (skippedCount > 0)
                {
                    _logger.LogInformation("Skipped {SkippedCount} records without EAN (normal behavior)", skippedCount);
                }

                if (!validProducts.Any())
                {
                    _logger.LogWarning("No valid products found in batch");
                    return result;
                }

                // Step 1: Prepare products for bulk create/update
                var coreProperties = supplierConfig.GetCoreProductProperties();
                var productsForBulkOperation = PrepareProductsForBulkOperation(validProducts, coreProperties);

                // Step 2: Bulk create/update products with proper entity state management
                var res = await _productsService.BulkCreateOrUpdateProductsOptimizedAsync(
                    productsForBulkOperation, processorName);

                result.ProductsCreated += res.Created;
                result.ProductsUpdated += res.Updated;
                result.Wornings += res.Warnings;
                result.ProductsNoChanged += res.NoChanges;
                result.Errors += res.Errors;

                // Step 3: Process offer-products
                _logger.LogInformation("About to process offer-products for {ProductCount} products", validProducts.Count);
                await ProcessOfferProductsAsync(validProducts, offer, supplierConfig, processorName, result, cancellationToken);
                _logger.LogInformation("Completed offer-products processing");

                _logger.LogInformation("Completed batch processing: {ProductsCreated} products created, {ProductsUpdated} updated, " +
                    "{OfferProductsCreated} offer-products created,  {Errors} errors",
                    result.ProductsCreated, result.ProductsUpdated, result.OfferProductsCreated,  result.Errors);

                _logger.LogDebug("Final result being returned - Products Created={ProductsCreated}, Updated={ProductsUpdated}, OfferProducts Created={OfferProductsCreated}, Errors={Errors}", 
                    result.ProductsCreated, result.ProductsUpdated, result.OfferProductsCreated, result.Errors);

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

        private List<ProductEntity> PrepareProductsForBulkOperation(List<OfferProductAnnex> validProducts, List<string> coreProperties)
        {
            var productsForBulkOperation = new List<ProductEntity>();

            foreach (var productData in validProducts)
            {
                var productEntity = new ProductEntity
                {
                    Name = productData.Product.Name,
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
            List<OfferProductAnnex> validProducts,
            SupplierOfferAnnex offer,
            SupplierConfiguration supplierConfig,
            string processorName,
            FileProcessingResult result,
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

                var offerProductsToCreate = new List<OfferProductAnnex>();

                _logger.LogDebug("ProcessOfferProductsAsync called with {ProductCount} products", validProducts.Count);
                _logger.LogDebug("Current result counts BEFORE: Products Created={ProductsCreated}, Updated={ProductsUpdated}, OfferProducts Created={OfferProductsCreated} ", 
                    result.ProductsCreated, result.ProductsUpdated, result.OfferProductsCreated);

                foreach (var productData in validProducts)
                {
                    if (savedProducts.TryGetValue(productData.Product.EAN, out var savedProduct))
                    {
                        // No need to check if offer product already exists - we have no support in update offer
                        // Create new offer product
                        var newOfferProduct = new OfferProductAnnex
                        {
                            OfferId = offer.Id,
                            ProductId = savedProduct.Id,
                            Description = productData.Description,
                            Price = productData.Price,
                            CreatedAt = DateTime.UtcNow
                        };

                        // Copy dynamic properties
                        foreach (var prop in productData.OfferProperties)
                        {
                            newOfferProduct.SetOfferProperty(prop.Key, prop.Value);
                        }

                        offerProductsToCreate.Add(newOfferProduct);
                    }
                }

                // Bulk create new offer products
                if (offerProductsToCreate.Any())
                {
                    await _context.OfferProducts.AddRangeAsync(offerProductsToCreate, cancellationToken);
                    result.OfferProductsCreated += offerProductsToCreate.Count;
                }


                _logger.LogDebug("OfferProducts processing completed. Created {CreatedCount}", 
                    offerProductsToCreate.Count);
                _logger.LogDebug("Current result counts AFTER: Products Created={ProductsCreated}, Updated={ProductsUpdated}, OfferProducts Created={OfferProductsCreated}", 
                    result.ProductsCreated, result.ProductsUpdated, result.OfferProductsCreated);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process offer products for offer: {OfferId}", offer.Id);
                throw;
            }
        }

        /// <summary>
        /// Clears the EF change tracker to prevent entity tracking conflicts during reprocessing
        /// </summary>
        public Task ClearChangeTrackerAsync(CancellationToken cancellationToken = default)
        {
            _context.ChangeTracker.Clear();
            _logger.LogDebug("EF change tracker cleared successfully");
            return Task.CompletedTask;
        }
    }
}
