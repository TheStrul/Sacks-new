using SacksDataLayer.FileProcessing.Configuration;
using SacksDataLayer.FileProcessing.Models; // ProcessingContext
using SacksDataLayer.Services.Interfaces;
using SacksDataLayer.Entities;
using SacksDataLayer.Data;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

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
                await _context.Database.EnsureCreatedAsync(cancellationToken);
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

                SupplierOfferAnnex offer;

                if (supplier.Id > 0)
                {
                    // Supplier exists in DB - create offer using SupplierId to avoid attaching a detached Supplier
                    offer = new SupplierOfferAnnex
                    {
                        SupplierId = supplier.Id,
                        OfferName = offerName,
                        Description = description,
                        Currency = currency,
                        CreatedAt = DateTime.UtcNow
                    };
                }
                else
                {
                    // Supplier is new / unpersisted - set navigation so EF will insert both
                    offer = new SupplierOfferAnnex
                    {
                        Supplier = supplier,
                        OfferName = offerName,
                        Description = description,
                        Currency = currency,
                        CreatedAt = DateTime.UtcNow
                    };
                }

                // Add the offer using the SupplierOffersService which will add it to the repository
                // (transaction scope / SaveChanges will be handled by the caller)
                var createdOffer = await _supplierOffersService.CreateOfferAsync(offer);


                return createdOffer;
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
        public async Task ProcessOfferAsync(
            ProcessingContext context,
            CancellationToken cancellationToken = default)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            var offer = context.ProcessingResult.SupplierOffer ?? throw new ArgumentNullException("context.ProcessingResult.SupplierOffer");
            
            cancellationToken.ThrowIfCancellationRequested();
            // use ProcessingStatistics on the provided context
            var stats = context.ProcessingResult.Statistics;
            
            try
            {
                // Step 1: Bulk create/update products with proper entity state management
                var res = await _productsService.BulkCreateOrUpdateProductsOptimizedAsync(offer);

                // Map product service results directly into ProcessingStatistics
                stats.ProductsCreated += res.Created;
                stats.ProductsUpdated += res.Updated;
                stats.ProductsSkipped += res.NoChanges;
                stats.ErrorCount += res.Errors;
                // For warnings, increment by returned warnings
                stats.WarningCount += res.Warnings;

                // Important: persist changes now so new Product IDs are generated and available
                await _context.SaveChangesAsync(cancellationToken);

                // Step 2: Now create OfferProducts referencing persisted ProductIds
                await ProcessOfferProductsAsync(context, cancellationToken);

                // Caller will SaveChanges as part of transaction
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process product batch for offer: {OfferName}", offer.OfferName);

                // Count all items as errors in the statistics
                stats.ErrorCount += offer.OfferProducts.Count;

                return;
            }
        }

        /// <summary>
        /// Processes offer-products for the given products and offer
        /// </summary>
        private async Task ProcessOfferProductsAsync(
            ProcessingContext context,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(context);
            var offer = context.ProcessingResult.SupplierOffer ?? throw new ArgumentNullException("context.ProcessingResult.SupplierOffer");
            var supplierConfig = context.SupplierConfiguration;
            var stats = context.ProcessingResult.Statistics;

            
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
                    // Map offer-products created into the processing context statistics
                    stats.OfferProductsCreated += offerProductsToCreate.Count;

                    _logger.LogDebug("Added {Count} OfferProducts to tracked offer navigation collection", offerProductsToCreate.Count);
                }

                _logger.LogDebug("OfferProducts processing completed. Created {CreatedCount}", offerProductsToCreate.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process offer products for offer: {OfferId}", offer.Id);
                // Map error into context statistics
                context.ProcessingResult.Statistics.ErrorCount += 1;
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
