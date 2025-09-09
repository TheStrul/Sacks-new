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
            CancellationToken cancellationToken = default)
        {
            if (supplierConfig == null) throw new ArgumentNullException(nameof(supplierConfig));
            
            cancellationToken.ThrowIfCancellationRequested();
            
            try
            {
                var supplier = await _suppliersService.CreateOrGetSupplierByName(
                    supplierConfig.Name);

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
        public async Task<SupplierOfferAnnex> CreateOfferAsync(
            SupplierEntity supplier,
            string offerName,
            DateTime processingDate,
            string currency,
            string description,
            CancellationToken cancellationToken = default)
        {
            if (supplier == null) throw new ArgumentNullException(nameof(supplier));
            if (string.IsNullOrWhiteSpace(offerName)) throw new ArgumentException("File name cannot be null or empty", nameof(offerName));
            
            cancellationToken.ThrowIfCancellationRequested();
            
            try
            {
                await EnsureOfferCanBeProcessedAsync(supplier, offerName, cancellationToken);

                var offer = new SupplierOfferAnnex
                {
                    // ✅ DON'T set SupplierId manually - let EF handle it via navigation
                    Supplier = supplier,  // Set navigation property instead
                    OfferName = offerName,
                    Description = description,
                    Currency = currency,
                    CreatedAt = DateTime.UtcNow,

                };

                // ✅ Add to supplier's collection instead of repository directly
                supplier.Offers.Add(offer);
                // Use new overload that accepts SupplierEntity directly - no database validation needed

                _logger.LogInformation("Offer created: {OfferName} (ID: {OfferId})", 
                    offer.OfferName, offer.Id);
                
                return offer;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create offer for file: {FileName}", offerName);
                throw;
            }
        }

        /// <summary>
        /// Processes a batch of products with optimized bulk operations
        /// </summary>
        public async Task<FileProcessingResult> ProcessOfferAsync(
            SupplierOfferAnnex offer,
            CancellationToken cancellationToken = default)
        {
            if (offer == null) throw new ArgumentNullException(nameof(offer));
            
            cancellationToken.ThrowIfCancellationRequested();
            
            var result = new FileProcessingResult();
            
            try
            {
                // Step 1: Prepare products for bulk create/update
                var distinctProducts = offer.OfferProducts.Select(op => op.Product).Distinct().ToList();


                // Step 2: Bulk create/update products with proper entity state management
                var res = await _productsService.BulkCreateOrUpdateProductsOptimizedAsync(offer);

                result.ProductsCreated += res.Created;
                result.ProductsUpdated += res.Updated;
                result.Wornings += res.Warnings;
                result.ProductsNoChanged += res.NoChanges;
                result.Errors += res.Errors;
                //result.OfferProductsCreated+= res.Created;

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process product batch for offer: {OfferName}", offer.OfferName);
                result.Errors += offer.OfferProducts.Count; // Count all items as errors
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

        /// <summary>
        /// Processes offer-products for the given products and offer
        /// </summary>
        private async Task ProcessOfferProductsAsync(
            SupplierOfferAnnex offer,
            SupplierConfiguration supplierConfig,
            FileProcessingResult result,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(offer);
            ArgumentNullException.ThrowIfNull(supplierConfig);
            ArgumentNullException.ThrowIfNull(result);
            
            _logger.LogInformation("ProcessOfferProductsAsync called with {ProductCount} products for offer {OfferId}",
                 offer.OfferProducts.Count, offer.Id);
            
            try
            {
                // Ensure offer is tracked by EF with its navigation properties loaded
                var trackedOffer = await _context.SupplierOffers
                    .Include(o => o.OfferProducts)
                    .FirstOrDefaultAsync(o => o.Id == offer.Id, cancellationToken);

                if (trackedOffer == null)
                {
                    throw new InvalidOperationException($"Offer with ID {offer.Id} not found in database");
                }

                // Get product lookup by EAN to map offer products to saved products
                var productEANs = offer.OfferProducts.Select(vp => vp.Product.EAN).ToList();
                var savedProducts = await _context.Products
                    .AsNoTracking()
                    .Where(p => productEANs.Contains(p.EAN))
                    .ToDictionaryAsync(p => p.EAN, p => p, cancellationToken);

                var offerProductsToCreate = new List<OfferProductAnnex>();

                foreach (var productData in offer.OfferProducts)
                {
                    if (savedProducts.TryGetValue(productData.Product.EAN, out var savedProduct))
                    {
                        // Create new offer product with proper navigation setup
                        var newOfferProduct = new OfferProductAnnex
                        {
                            OfferId = trackedOffer.Id,
                            ProductId = savedProduct.Id,
                            Description = productData.Description,
                            Price = productData.Price,
                            Quantity = productData.Quantity,
                            CreatedAt = DateTime.UtcNow,
                            Offer = trackedOffer // Set navigation property
                        };

                        // Copy dynamic properties and serialize them
                        foreach (var prop in productData.OfferProperties)
                        {
                            newOfferProduct.SetOfferProperty(prop.Key, prop.Value);
                        }
                        newOfferProduct.SerializeOfferProperties();

                        offerProductsToCreate.Add(newOfferProduct);
                    }
                }

                // Add to navigation collection AND context for proper tracking
                if (offerProductsToCreate.Any())
                {
                    foreach (var offerProduct in offerProductsToCreate)
                    {
                        trackedOffer.OfferProducts.Add(offerProduct);
                    }
                    
                    result.OfferProductsCreated += offerProductsToCreate.Count;
                    
                    _logger.LogDebug("Added {Count} OfferProducts to tracked offer navigation collection", 
                        offerProductsToCreate.Count);
                }

                _logger.LogDebug("OfferProducts processing completed. Created {CreatedCount}", 
                    offerProductsToCreate.Count);
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


        /// <summary>
        /// Ensures no duplicate offer exists - deletes existing offer if found (dev mode).
        /// In development mode, deletes existing offer if found to allow re-processing.
        /// </summary>
        private async Task EnsureOfferCanBeProcessedAsync(
            SupplierEntity supplier,
            string offerName,
            CancellationToken cancellationToken = default)
        {

            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var existingOffers = await _supplierOffersService.GetOffersBySupplierAsync(supplier.Id);
                var existingOffer = existingOffers.FirstOrDefault(o => o.OfferName!.Equals(offerName, StringComparison.OrdinalIgnoreCase));

                if (existingOffer != null)
                {
                    _logger.LogWarning("🔄 REPROCESSING: Existing offer found - deleting and replacing: {SupplierName} - {OfferName}", supplier.Name, offerName);


                    // Delete the existing offer (cascade will automatically delete all OfferProducts)
                    var deleteSuccess = await _supplierOffersService.DeleteOfferAsync(existingOffer.Id);

                    if (deleteSuccess)
                    {
                        _logger.LogInformation("✅ Successfully deleted existing offer: {OfferName}", offerName);

                        // Clear change tracker again after deletion to ensure clean state
                        _logger.LogDebug("Cleared change tracker after offer deletion to ensure clean state");
                    }
                    else
                    {
                        throw new InvalidOperationException($"Failed to delete existing offer '{offerName}' for supplier '{supplier.Name}'");
                    }
                }
            }
            catch (Exception ex) when (!(ex is InvalidOperationException))
            {
                _logger.LogError(ex, "Failed to validate/replace offer for supplier: {SupplierName}, file: {FileName}",
                    supplier.Name, offerName);
                throw;
            }
        }

    }
}
