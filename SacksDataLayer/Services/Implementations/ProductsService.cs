using SacksDataLayer.FileProcessing.Models;
using SacksDataLayer.Repositories.Interfaces;
using SacksDataLayer.Services.Interfaces;

namespace SacksDataLayer.Services.Implementations
{
    /// <summary>
    /// Service implementation for managing products
    /// </summary>
    public class ProductsService : IProductsService
    {
        private readonly IProductsRepository _repository;

        public ProductsService(IProductsRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public async Task<ProductEntity?> GetProductAsync(int id)
        {
            return await _repository.GetByIdAsync(id);
        }

        public async Task<ProductEntity?> GetProductBySKUAsync(string sku)
        {
            return await _repository.GetBySKUAsync(sku);
        }

        public async Task<(IEnumerable<ProductEntity> Products, int TotalCount)> GetProductsAsync(int pageNumber = 1, int pageSize = 50)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 50;
            if (pageSize > 1000) pageSize = 1000; // Limit max page size

            var skip = (pageNumber - 1) * pageSize;
            var products = await _repository.GetPagedAsync(skip, pageSize);
            var totalCount = await _repository.GetCountAsync();

            return (products, totalCount);
        }

        public async Task<ProductEntity> CreateProductAsync(ProductEntity product, string? createdBy = null)
        {
            if (product == null)
                throw new ArgumentNullException(nameof(product));

            // Validate required fields
            if (string.IsNullOrWhiteSpace(product.Name))
                throw new ArgumentException("Product name is required", nameof(product.Name));

            // Check for duplicate SKU if provided
            if (!string.IsNullOrWhiteSpace(product.SKU))
            {
                var existingProduct = await _repository.GetBySKUAsync(product.SKU);
                if (existingProduct != null)
                    throw new InvalidOperationException($"Product with SKU '{product.SKU}' already exists");
            }

            return await _repository.CreateAsync(product, createdBy);
        }

        public async Task<ProductEntity> UpdateProductAsync(ProductEntity product, string? modifiedBy = null)
        {
            if (product == null)
                throw new ArgumentNullException(nameof(product));

            // Validate required fields
            if (string.IsNullOrWhiteSpace(product.Name))
                throw new ArgumentException("Product name is required", nameof(product.Name));

            // Check if product exists
            var existingProduct = await _repository.GetByIdAsync(product.Id);
            if (existingProduct == null)
                throw new InvalidOperationException($"Product with ID {product.Id} not found");

            // Check for duplicate SKU if changed
            if (!string.IsNullOrWhiteSpace(product.SKU) && product.SKU != existingProduct.SKU)
            {
                var duplicateProduct = await _repository.GetBySKUAsync(product.SKU);
                if (duplicateProduct != null)
                    throw new InvalidOperationException($"Product with SKU '{product.SKU}' already exists");
            }

            return await _repository.UpdateAsync(product, modifiedBy);
        }

        public async Task<bool> DeleteProductAsync(int id)
        {
            return await _repository.DeleteAsync(id);
        }

        public async Task<IEnumerable<ProductEntity>> SearchProductsByNameAsync(string searchTerm)
        {
            return await _repository.SearchByNameAsync(searchTerm);
        }

        public async Task<IEnumerable<ProductEntity>> GetProductsBySourceFileAsync(string sourceFile)
        {
            return await _repository.GetBySourceFileAsync(sourceFile);
        }

        public async Task<ProductEntity> CreateOrUpdateProductAsync(ProductEntity product, string? userContext = null)
        {
            if (product == null)
                throw new ArgumentNullException(nameof(product));

            ProductEntity? existingProduct = null;
            
            // Try to find existing product by SKU
            if (!string.IsNullOrWhiteSpace(product.SKU))
            {
                existingProduct = await _repository.GetBySKUAsync(product.SKU);
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
                    
                    // Try to find existing product by SKU
                    if (!string.IsNullOrWhiteSpace(product.SKU))
                    {
                        existingProduct = await _repository.GetBySKUAsync(product.SKU);
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
                    Console.WriteLine($"Error processing product {product.Name} (SKU: {product.SKU}): {ex.Message}");
                    errors++;
                }
            }

            return (created, updated, errors);
        }

        public async Task<int> GetProductCountAsync()
        {
            return await _repository.GetCountAsync();
        }

        public async Task<Dictionary<string, int>> GetProcessingStatisticsAsync()
        {
            var processingStats = await _repository.GetProcessingStatisticsAsync();
            
            // Convert ProcessingMode enum keys to string keys
            return processingStats.ToDictionary(
                kvp => kvp.Key.ToString(), 
                kvp => kvp.Value
            );
        }
    }
}
