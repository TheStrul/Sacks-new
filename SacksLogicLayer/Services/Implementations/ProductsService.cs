using System.Text.Json;

using Microsoft.Extensions.Logging;

using Sacks.Core.Entities;
using Sacks.DataAccess.Repositories.Interfaces;
using SacksLogicLayer.Services.Interfaces;

namespace SacksLogicLayer.Services.Implementations
{
    /// <summary>
    /// Service implementation for managing products
    /// </summary>
    public class ProductsService : IProductsService
    {
        private readonly ITransactionalProductsRepository _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ProductsService> _logger;

        public ProductsService(
            ITransactionalProductsRepository repository, 
            IUnitOfWork unitOfWork,
            ILogger<ProductsService> logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Product?> GetProductAsync(int id)
        {
            return await _repository.GetByIdAsync(id, CancellationToken.None);
        }

        public async Task<Product?> GetProductByEANAsync(string ean)
        {
            return await _repository.GetByEANAsync(ean, CancellationToken.None);
        }

        /// <summary>
        /// 🚀 PERFORMANCE: Get multiple products by EANs in a single database call
        /// </summary>
        public async Task<Dictionary<string, Product>> GetProductsByEANsBulkAsync(IEnumerable<string> eans)
        {
            return await _repository.GetByEANsBulkAsync(eans, CancellationToken.None);
        }

        public async Task<(IEnumerable<Product> Products, int TotalCount)> GetProductsAsync(int pageNumber = 1, int pageSize = 50)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 50;
            if (pageSize > 1000) pageSize = 1000; // Limit max page size

            var skip = (pageNumber - 1) * pageSize;
            var products = await _repository.GetPagedAsync(skip, pageSize, CancellationToken.None);
            var totalCount = await _repository.GetCountAsync(CancellationToken.None);

            return (products, totalCount);
        }

        public async Task<Product> CreateProductAsync(Product product, string? createdBy = null)
        {
            if (product == null)
                throw new ArgumentNullException(nameof(product));

            // Validate required fields
            if (string.IsNullOrWhiteSpace(product.Name))
                throw new ArgumentException("Product name is required", nameof(product));

            return await _unitOfWork.ExecuteInTransactionAsync(async ct =>
            {
                // Check for duplicate EAN if provided
                if (!string.IsNullOrWhiteSpace(product.EAN))
                {
                    var existingProduct = await _repository.GetByEANAsync(product.EAN, ct);
                    if (existingProduct != null)
                        throw new InvalidOperationException($"Product with EAN '{product.EAN}' already exists");
                }

                _repository.Add(product);
                await _unitOfWork.SaveChangesAsync(ct);
                return product;
            });
        }

        public async Task<bool> DeleteProductAsync(int id)
        {
            return await _unitOfWork.ExecuteInTransactionAsync(async ct =>
            {
                var success = await _repository.RemoveByIdAsync(id, ct);
                if (success)
                {
                    await _unitOfWork.SaveChangesAsync(ct);
                }
                return success;
            });
        }

        public async Task<IEnumerable<Product>> SearchProductsByNameAsync(string searchTerm)
        {
            return await _repository.SearchByNameAsync(searchTerm, CancellationToken.None);
        }

        public async Task<IEnumerable<Product>> GetProductsBySourceFileAsync(string sourceFile)
        {
            return await _repository.GetBySourceFileAsync(sourceFile, CancellationToken.None);
        }


        /// <summary>
        /// 🚀 PERFORMANCE OPTIMIZED: Bulk create/update with minimal database calls
        /// NOTE: Does not manage transactions - caller must handle transaction scope
        /// </summary>
        public async Task<ProductImportResult> BulkCreateOrUpdateProductsOptimizedAsync(Offer offer)
        {
            ProductImportResult result = new();
            if (offer == null)
                return result;

            var eans = offer.OfferProducts.Where(p => !string.IsNullOrWhiteSpace(p.Product.EAN)).Select(p => p.Product.EAN).ToList();
            
            // 🚀 SINGLE BULK QUERY instead of N individual queries
            var existingProducts = await _repository.GetByEANsBulkAsync(eans, CancellationToken.None);
            
            // 🔧 FIX: Group products by EAN to handle duplicates in the same offer
            var productsByEAN = offer.OfferProducts
                .Where(p => !string.IsNullOrWhiteSpace(p.Product.EAN))
                .GroupBy(p => p.Product.EAN)
                .ToDictionary(g => g.Key, g => g.ToList());
            
            // Separate products into new and existing for proper EF tracking
            var productsToAdd = new List<Product>();
            var processedEANs = new HashSet<string>();
            
            foreach (var eanGroup in productsByEAN)
            {
                var ean = eanGroup.Key;
                var offerProductsWithSameEAN = eanGroup.Value;
                
                try
                {
                    if (existingProducts.TryGetValue(ean, out var existingProduct))
                    {
                        // 🔧 FIX: Get tracked entity or attach the untracked one
                        var trackedProduct = await GetOrAttachExistingProductAsync(existingProduct);
                        
                        // Use the first product instance to check for changes
                        var firstProduct = offerProductsWithSameEAN.First().Product;

                        if (MergeDynamicProperties(trackedProduct.EAN, trackedProduct.DynamicProperties, firstProduct.DynamicProperties,_logger))
                        {
                            result.Updated++;
                        }
                        else
                        {
                            result.NoChanges++;
                        }

                        // 🔧 FIX: Replace navigation property with tracked entity for ALL offer products with same EAN
                        foreach (var offerProduct in offerProductsWithSameEAN)
                        {
                            offerProduct.Product = trackedProduct;
                            offerProduct.ProductId = trackedProduct.Id;
                        }
                    }
                    else
                    {
                        // 🔧 FIX: Handle multiple offer products with same new EAN
                        var masterProduct = offerProductsWithSameEAN.First().Product;
                        
                        // Ensure new products don't have Id set (let EF generate)
                        masterProduct.Id = 0; // Reset ID for new products
                        masterProduct.CreatedAt = DateTime.UtcNow;
                        masterProduct.ModifiedAt = DateTime.UtcNow;
                        
                        // Only add the product once to EF, even if multiple offer products reference it
                        productsToAdd.Add(masterProduct);
                        result.Created++;
                        
                        // 🔧 FIX: Update all offer products with same EAN to reference the same master product instance
                        foreach (var offerProduct in offerProductsWithSameEAN)
                        {
                            offerProduct.Product = masterProduct;
                            // ProductId will be set after EF assigns the ID during SaveChanges
                        }
                    }
                    
                    processedEANs.Add(ean);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing products with EAN {EAN} (Count: {Count}): {ErrorMessage}",
                        ean, offerProductsWithSameEAN.Count, ex.Message);
                    result.Errors += offerProductsWithSameEAN.Count; // Count all failed instances
                }
            }
            
            // Handle products without EAN (if any)
            var productsWithoutEAN = offer.OfferProducts
                .Where(p => string.IsNullOrWhiteSpace(p.Product.EAN))
                .ToList();
                
            foreach (var productOffer in productsWithoutEAN)
            {
                try
                {
                    productOffer.Product.Id = 0;
                    productOffer.Product.CreatedAt = DateTime.UtcNow;
                    productOffer.Product.ModifiedAt = DateTime.UtcNow;
                    
                    productsToAdd.Add(productOffer.Product);
                    result.Created++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing product without EAN {ProductName}: {ErrorMessage}",
                        productOffer.Product.Name, ex.Message);
                    result.Errors++;
                }
            }
            
            // 🚀 OPTIMIZED: Only add new products - existing ones are already tracked and updated
            if (productsToAdd.Any())
            {
                _repository.AddRange(productsToAdd);
            }
            
            return result;
        }

        /// <summary>
        /// 🔧 FIX: Gets already tracked entity or properly attaches untracked entity to avoid conflicts
        /// </summary>
        private async Task<Product> GetOrAttachExistingProductAsync(Product untrackedProduct)
        {
            // First check if this entity is already being tracked
            var tracked = await _repository.GetByIdAsync(untrackedProduct.Id, CancellationToken.None);
            
            if (tracked != null)
            {
                // Entity is already tracked, return the tracked instance
                return tracked;
            }
            
            // Entity is not tracked, we need to attach it
            // But first ensure it's not conflicting with any tracked entity
            _unitOfWork.ClearTracker(); // Clear any potential conflicts
            
            // Safe to attach after clearing tracker
            _repository.Update(untrackedProduct);
            return untrackedProduct;
        }

        public async Task<int> GetProductCountAsync()
        {
            return await _repository.GetCountAsync(CancellationToken.None);
        }

        public async Task<int> GetTotalCountAsync()
        {
            return await _repository.GetCountAsync(CancellationToken.None);
        }

        /// <summary>
        /// 🔧 MERGE LOGIC: Merges existing and new dynamic properties
        /// New properties overwrite existing ones, but existing properties not in new are preserved
        /// </summary>
        /// <param name="existingProperties">Current dynamic properties</param>
        /// <param name="newProperties">New dynamic properties to merge</param>
        /// <returns>Merged dynamic properties dictionary</returns>
        private static bool MergeDynamicProperties(string EAN,
            Dictionary<string, object?> existingProperties, 
            Dictionary<string, object?> newProperties,
            ILogger logger)
        {
            if (newProperties == null || newProperties.Count == 0)
            {
                return false;
            }

            bool changes = false;
            if (existingProperties == null)
            {
                existingProperties = new Dictionary<string, object?>();
            }
            foreach (var property in newProperties)
            {
                if (existingProperties.ContainsKey(property.Key))
                {
                    //report only if value actually changes
                    if (AreValuesEqual(existingProperties[property.Key], property.Value))
                    {
                        continue;
                    }
                    logger.LogInformation("Product EAN {EAN}: Dynamic property '{Property}' updated from '{OldValue}' to '{NewValue}'",
                        EAN, property.Key, existingProperties[property.Key]?.ToString() ?? "null", property.Value?.ToString() ?? "null");
                }
                changes = true;
                existingProperties[property.Key] = property.Value;
            }
            return changes;
        }

        /// <summary>
        /// Returns a map of property -> { old = ..., new = ... } ONLY for properties that are being updated
        /// (key existed before and new value differs). Newly added properties are not included.
        /// </summary>
        private static Dictionary<string, object?> GetChangedDynamicProperties(
            Dictionary<string, object?> existingProperties,
            Dictionary<string, object?> newProperties)
        {
            existingProperties = existingProperties ?? new Dictionary<string, object?>();
            newProperties = newProperties ?? new Dictionary<string, object?>();

            var changes = new Dictionary<string, object?>();

            foreach (var kvp in existingProperties)
            {
                var key = kvp.Key;
                var oldVal = kvp.Value;

                if (!newProperties.TryGetValue(key, out var incomingVal))
                {
                    // No incoming value for this existing key => not an update (we don't report removals here)
                    continue;
                }

                // Treat empty strings like null per current merge semantics
                if (incomingVal is string s && string.IsNullOrWhiteSpace(s))
                {
                    incomingVal = null;
                }

                // Only report if both old and incoming differ and incoming is not null (i.e., a real update)
                if (!AreValuesEqual(oldVal, incomingVal) && incomingVal is not null)
                {
                    changes[key] = new Dictionary<string, object?>
                    {
                        ["old"] = oldVal,
                        ["new"] = incomingVal
                    };
                }
            }

            return changes;
        }

        private static bool AreValuesEqual(object? a, object? b)
        {
            if (a is null && b is null) return true;
            if (a is null || b is null) return false;

            // Compare string representations for simplicity and cross-JsonElement support
            if (a is JsonElement jeA)
            {
                a = jeA.ToString();
            }
            if (b is JsonElement jeB)
            {
                b = jeB.ToString();
            }

            return string.Equals(a.ToString(), b.ToString(), StringComparison.Ordinal);
        }
    }
}
