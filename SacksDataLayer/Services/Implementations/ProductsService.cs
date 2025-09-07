using SacksDataLayer.FileProcessing.Models;
using SacksDataLayer.Repositories.Interfaces;
using SacksDataLayer.Services.Interfaces;
using SacksDataLayer.Entities;
using Microsoft.Extensions.Logging;

namespace SacksDataLayer.Services.Implementations
{
    /// <summary>
    /// Service implementation for managing products
    /// </summary>
    public class ProductsService : IProductsService
    {
        private readonly IProductsRepository _repository;
        private readonly ILogger<ProductsService> _logger;

        public ProductsService(IProductsRepository repository, ILogger<ProductsService> logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ProductEntity?> GetProductAsync(int id)
        {
            return await _repository.GetByIdAsync(id, CancellationToken.None);
        }

        public async Task<ProductEntity?> GetProductByEANAsync(string ean)
        {
            return await _repository.GetByEANAsync(ean, CancellationToken.None);
        }

        /// <summary>
        /// 🚀 PERFORMANCE: Get multiple products by EANs in a single database call
        /// </summary>
        public async Task<Dictionary<string, ProductEntity>> GetProductsByEANsBulkAsync(IEnumerable<string> eans)
        {
            return await _repository.GetByEANsBulkAsync(eans, CancellationToken.None);
        }

        public async Task<(IEnumerable<ProductEntity> Products, int TotalCount)> GetProductsAsync(int pageNumber = 1, int pageSize = 50)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 50;
            if (pageSize > 1000) pageSize = 1000; // Limit max page size

            var skip = (pageNumber - 1) * pageSize;
            var products = await _repository.GetPagedAsync(skip, pageSize, CancellationToken.None);
            var totalCount = await _repository.GetCountAsync(CancellationToken.None);

            return (products, totalCount);
        }

        public async Task<ProductEntity> CreateProductAsync(ProductEntity product, string? createdBy = null)
        {
            if (product == null)
                throw new ArgumentNullException(nameof(product));

            // Validate required fields
            if (string.IsNullOrWhiteSpace(product.Name))
                throw new ArgumentException("Product name is required", nameof(product));

            // Check for duplicate EAN if provided
            if (!string.IsNullOrWhiteSpace(product.EAN))
            {
                var existingProduct = await _repository.GetByEANAsync(product.EAN, CancellationToken.None);
                if (existingProduct != null)
                    throw new InvalidOperationException($"Product with EAN '{product.EAN}' already exists");
            }

            return await _repository.CreateAsync(product, createdBy, CancellationToken.None);
        }

        public async Task<ProductEntity> UpdateProductAsync(ProductEntity product, string? modifiedBy = null)
        {
            if (product == null)
                throw new ArgumentNullException(nameof(product));

            // Validate required fields
            if (string.IsNullOrWhiteSpace(product.Name))
                throw new ArgumentException("Product name is required", nameof(product));

            // Check if product exists
            var existingProduct = await _repository.GetByIdAsync(product.Id, CancellationToken.None);
            if (existingProduct == null)
                throw new InvalidOperationException($"Product with ID {product.Id} not found");

            // Check for duplicate EAN if changed
            if (!string.IsNullOrWhiteSpace(product.EAN) && product.EAN != existingProduct.EAN)
            {
                var duplicateProduct = await _repository.GetByEANAsync(product.EAN, CancellationToken.None);
                if (duplicateProduct != null)
                    throw new InvalidOperationException($"Product with EAN '{product.EAN}' already exists");
            }

            return await _repository.UpdateAsync(product, modifiedBy, CancellationToken.None);
        }

        public async Task<bool> DeleteProductAsync(int id)
        {
            return await _repository.DeleteAsync(id, CancellationToken.None);
        }

        public async Task<IEnumerable<ProductEntity>> SearchProductsByNameAsync(string searchTerm)
        {
            return await _repository.SearchByNameAsync(searchTerm, CancellationToken.None);
        }

        public async Task<IEnumerable<ProductEntity>> GetProductsBySourceFileAsync(string sourceFile)
        {
            return await _repository.GetBySourceFileAsync(sourceFile, CancellationToken.None);
        }

        public async Task<ProductEntity> CreateOrUpdateProductAsync(ProductEntity product, string? userContext = null)
        {
            if (product == null)
                throw new ArgumentNullException(nameof(product));

            ProductEntity? existingProduct = null;
            
            // Try to find existing product by EAN
            if (!string.IsNullOrWhiteSpace(product.EAN))
            {
                existingProduct = await _repository.GetByEANAsync(product.EAN, CancellationToken.None);
            }

            if (existingProduct != null)
            {
                // Update existing product
                existingProduct.Name = product.Name;
                existingProduct.Description = product.Description;
                existingProduct.DynamicProperties = product.DynamicProperties;

                return await UpdateProductAsync(existingProduct, userContext);
            }
            else
            {
                // Create new product
                return await CreateProductAsync(product, userContext);
            }
        }

        public async Task<(int Created, int Updated, int Errors)> BulkCreateOrUpdateProductsAsync(
            IEnumerable<ProductEntity> products, string? userContext = null)
        {
            if (products == null || !products.Any())
                return (0, 0, 0);

            int created = 0, updated = 0, errors = 0;

            foreach (var product in products)
            {
                try
                {
                    ProductEntity? existingProduct = null;
                    
                    // Try to find existing product by EAN
                    if (!string.IsNullOrWhiteSpace(product.EAN))
                    {
                        existingProduct = await _repository.GetByEANAsync(product.EAN, CancellationToken.None);
                    }

                    if (existingProduct != null)
                    {
                        // Update existing product
                        existingProduct.Name = product.Name;
                        existingProduct.Description = product.Description;
                        existingProduct.DynamicProperties = product.DynamicProperties;
                        
                        await UpdateProductAsync(existingProduct, userContext);
                        updated++;
                    }
                    else
                    {
                        // Create new product
                        await CreateProductAsync(product, userContext);
                        created++;
                    }
                }
                catch (Exception ex)
                {
                    // Log error but continue with other products
                    _logger.LogError(ex, "Error processing product {ProductName} (EAN: {ProductEAN}): {ErrorMessage}", 
                        product.Name, product.EAN, ex.Message);
                    errors++;
                }
            }

            return (created, updated, errors);
        }

        /// <summary>
        /// 🚀 PERFORMANCE OPTIMIZED: Bulk create/update with minimal database calls
        /// </summary>
        public async Task<(int Created, int Updated, int Errors)> BulkCreateOrUpdateProductsOptimizedAsync(
            IEnumerable<ProductEntity> products, string? userContext = null)
        {
            if (products == null || !products.Any())
                return (0, 0, 0);

            var productList = products.ToList();
            var eans = productList.Where(p => !string.IsNullOrWhiteSpace(p.EAN)).Select(p => p.EAN).ToList();
            
            // 🚀 SINGLE BULK QUERY instead of N individual queries
            var existingProducts = await _repository.GetByEANsBulkAsync(eans, CancellationToken.None);
            
            var productsToCreate = new List<ProductEntity>();
            var productsToUpdate = new List<ProductEntity>();
            var processedEANs = new HashSet<string>(StringComparer.OrdinalIgnoreCase); // Track EANs within this batch
            int errors = 0;

            foreach (var product in productList)
            {
                try
                {
                    // Skip if EAN is empty
                    if (string.IsNullOrWhiteSpace(product.EAN))
                    {
                        // For products without EAN, allow them to be created (they won't violate unique constraint)
                        product.CreatedAt = DateTime.UtcNow;
                        productsToCreate.Add(product);
                        continue;
                    }

                    // Check if EAN already processed in this batch
                    if (processedEANs.Contains(product.EAN))
                    {
                        _logger.LogWarning("Skipping duplicate EAN '{ProductEAN}' within the same batch (Product: {ProductName})", 
                            product.EAN, product.Name);
                        errors++;
                        continue;
                    }

                    // Mark EAN as processed
                    processedEANs.Add(product.EAN);

                    if (existingProducts.TryGetValue(product.EAN, out var existingProduct))
                    {
                        // Update existing product
                        existingProduct.Name = product.Name;
                        existingProduct.Description = product.Description;
                        existingProduct.DynamicProperties = product.DynamicProperties;
                        existingProduct.ModifiedAt = DateTime.UtcNow;
                        productsToUpdate.Add(existingProduct);
                    }
                    else
                    {
                        // Create new product
                        product.CreatedAt = DateTime.UtcNow;
                        productsToCreate.Add(product);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing product {ProductName} (EAN: {ProductEAN}): {ErrorMessage}", 
                        product.Name, product.EAN, ex.Message);
                    errors++;
                }
            }

            // 🚀 BULK OPERATIONS instead of individual saves
            if (productsToCreate.Any())
            {
                await _repository.CreateBulkAsync(productsToCreate, userContext, CancellationToken.None);
            }
            
            if (productsToUpdate.Any())
            {
                await _repository.UpdateBulkAsync(productsToUpdate, userContext, CancellationToken.None);
            }

            return (productsToCreate.Count, productsToUpdate.Count, errors);
        }

        public async Task<int> GetProductCountAsync()
        {
            return await _repository.GetCountAsync(CancellationToken.None);
        }

        public async Task<int> GetTotalCountAsync()
        {
            return await _repository.GetCountAsync(CancellationToken.None);
        }
    }
}
