using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SacksDataLayer.Data;
using SacksDataLayer.FileProcessing.Models;
using SacksDataLayer.Repositories.Interfaces;
using System.Linq.Expressions;
using System.Text.Json;

namespace SacksDataLayer.Repositories.Implementations
{
    /// <summary>
    /// Entity Framework implementation of IProductsRepository
    /// </summary>
    public class ProductsRepository : IProductsRepository
    {
        private readonly SacksDbContext _context;

        public ProductsRepository(SacksDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        #region Basic CRUD Operations

        public async Task<ProductEntity?> GetByIdAsync(int id, bool includeDeleted = false)
        {
            var query = includeDeleted ? _context.ProductsWithDeleted : _context.Products;
            return await query.FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<ProductEntity?> GetBySKUAsync(string sku, bool includeDeleted = false)
        {
            if (string.IsNullOrWhiteSpace(sku))
                return null;

            var query = includeDeleted ? _context.ProductsWithDeleted : _context.Products;
            return await query.FirstOrDefaultAsync(p => p.SKU == sku);
        }

        public async Task<IEnumerable<ProductEntity>> GetAllAsync(bool includeDeleted = false)
        {
            var query = includeDeleted ? _context.ProductsWithDeleted : _context.Products;
            return await query.ToListAsync();
        }

        public async Task<IEnumerable<ProductEntity>> GetPagedAsync(int skip, int take, bool includeDeleted = false)
        {
            var query = includeDeleted ? _context.ProductsWithDeleted : _context.Products;
            return await query
                .OrderBy(p => p.CreatedAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync();
        }

        public async Task<ProductEntity> CreateAsync(ProductEntity product, string? createdBy = null)
        {
            if (product == null)
                throw new ArgumentNullException(nameof(product));

            product.CreatedBy = createdBy;
            product.CreatedAt = DateTime.UtcNow;

            _context.Products.Add(product);
            await _context.SaveChangesAsync();
            return product;
        }

        public async Task<ProductEntity> UpdateAsync(ProductEntity product, string? modifiedBy = null)
        {
            if (product == null)
                throw new ArgumentNullException(nameof(product));

            var existingProduct = await _context.Products.FirstOrDefaultAsync(p => p.Id == product.Id);
            if (existingProduct == null)
                throw new InvalidOperationException($"Product with ID {product.Id} not found");

            // Update properties
            existingProduct.Name = product.Name;
            existingProduct.Description = product.Description;
            existingProduct.SKU = product.SKU;
            existingProduct.DynamicPropertiesJson = product.DynamicPropertiesJson;
            existingProduct.UpdateModified(modifiedBy);

            await _context.SaveChangesAsync();
            return existingProduct;
        }

        public async Task<bool> SoftDeleteAsync(int id, string? deletedBy = null)
        {
            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id);
            if (product == null)
                return false;

            product.SoftDelete(deletedBy);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> HardDeleteAsync(int id)
        {
            var product = await _context.ProductsWithDeleted.FirstOrDefaultAsync(p => p.Id == id);
            if (product == null)
                return false;

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RestoreAsync(int id)
        {
            var product = await _context.ProductsWithDeleted.FirstOrDefaultAsync(p => p.Id == id);
            if (product == null || !product.IsDeleted)
                return false;

            product.Restore();
            await _context.SaveChangesAsync();
            return true;
        }

        #endregion

        #region Advanced Query Operations

        public async Task<IEnumerable<ProductEntity>> FindAsync(Expression<Func<ProductEntity, bool>> predicate, bool includeDeleted = false)
        {
            var query = includeDeleted ? _context.ProductsWithDeleted : _context.Products;
            return await query.Where(predicate).ToListAsync();
        }

        public async Task<IEnumerable<ProductEntity>> SearchByNameAsync(string searchTerm, bool includeDeleted = false)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return Enumerable.Empty<ProductEntity>();

            var query = includeDeleted ? _context.ProductsWithDeleted : _context.Products;
            return await query
                .Where(p => EF.Functions.Like(p.Name, $"%{searchTerm}%"))
                .ToListAsync();
        }

        public async Task<IEnumerable<ProductEntity>> GetByProcessingModeAsync(ProcessingMode mode, bool includeDeleted = false)
        {
            var query = includeDeleted ? _context.ProductsWithDeleted : _context.Products;
            var modeString = mode.ToString();
            
            return await query
                .Where(p => p.DynamicPropertiesJson != null && 
                           p.DynamicPropertiesJson.Contains($"\"ProcessingMode\":\"{modeString}\""))
                .ToListAsync();
        }

        public async Task<IEnumerable<ProductEntity>> GetBySourceFileAsync(string sourceFile, bool includeDeleted = false)
        {
            if (string.IsNullOrWhiteSpace(sourceFile))
                return Enumerable.Empty<ProductEntity>();

            var query = includeDeleted ? _context.ProductsWithDeleted : _context.Products;
            
            return await query
                .Where(p => p.DynamicPropertiesJson != null && 
                           p.DynamicPropertiesJson.Contains($"\"SourceFile\":\"{sourceFile}\""))
                .ToListAsync();
        }

        public async Task<IEnumerable<ProductEntity>> SearchByDynamicPropertyAsync(string propertyKey, object? propertyValue = null, bool includeDeleted = false)
        {
            if (string.IsNullOrWhiteSpace(propertyKey))
                return Enumerable.Empty<ProductEntity>();

            var query = includeDeleted ? _context.ProductsWithDeleted : _context.Products;
            
            if (propertyValue == null)
            {
                // Search for products that have the property key
                return await query
                    .Where(p => p.DynamicPropertiesJson != null && 
                               p.DynamicPropertiesJson.Contains($"\"{propertyKey}\""))
                    .ToListAsync();
            }
            else
            {
                // Search for products that have the property key with specific value
                var valueJson = JsonSerializer.Serialize(propertyValue);
                return await query
                    .Where(p => p.DynamicPropertiesJson != null && 
                               p.DynamicPropertiesJson.Contains($"\"{propertyKey}\":{valueJson}"))
                    .ToListAsync();
            }
        }

        #endregion

        #region Bulk Operations

        public async Task<IEnumerable<ProductEntity>> CreateBulkAsync(IEnumerable<ProductEntity> products, string? createdBy = null)
        {
            if (products == null)
                throw new ArgumentNullException(nameof(products));

            var productList = products.ToList();
            var now = DateTime.UtcNow;

            foreach (var product in productList)
            {
                product.CreatedBy = createdBy;
                product.CreatedAt = now;
            }

            _context.Products.AddRange(productList);
            await _context.SaveChangesAsync();
            return productList;
        }

        public async Task<IEnumerable<ProductEntity>> UpdateBulkAsync(IEnumerable<ProductEntity> products, string? modifiedBy = null)
        {
            if (products == null)
                throw new ArgumentNullException(nameof(products));

            var productList = products.ToList();
            var productIds = productList.Select(p => p.Id).ToList();
            
            var existingProducts = await _context.Products
                .Where(p => productIds.Contains(p.Id))
                .ToListAsync();

            var existingProductsDict = existingProducts.ToDictionary(p => p.Id);

            foreach (var product in productList)
            {
                if (existingProductsDict.TryGetValue(product.Id, out var existingProduct))
                {
                    existingProduct.Name = product.Name;
                    existingProduct.Description = product.Description;
                    existingProduct.SKU = product.SKU;
                    existingProduct.DynamicPropertiesJson = product.DynamicPropertiesJson;
                    existingProduct.UpdateModified(modifiedBy);
                }
            }

            await _context.SaveChangesAsync();
            return existingProducts;
        }

        public async Task<int> SoftDeleteBulkAsync(IEnumerable<int> ids, string? deletedBy = null)
        {
            if (ids == null)
                return 0;

            var idList = ids.ToList();
            var products = await _context.Products
                .Where(p => idList.Contains(p.Id))
                .ToListAsync();

            foreach (var product in products)
            {
                product.SoftDelete(deletedBy);
            }

            await _context.SaveChangesAsync();
            return products.Count;
        }

        #endregion

        #region Statistics and Analytics

        public async Task<int> GetCountAsync(bool includeDeleted = false)
        {
            var query = includeDeleted ? _context.ProductsWithDeleted : _context.Products;
            return await query.CountAsync();
        }

        public async Task<int> GetCountByProcessingModeAsync(ProcessingMode mode, bool includeDeleted = false)
        {
            var query = includeDeleted ? _context.ProductsWithDeleted : _context.Products;
            var modeString = mode.ToString();
            
            return await query
                .Where(p => p.DynamicPropertiesJson != null && 
                           p.DynamicPropertiesJson.Contains($"\"ProcessingMode\":\"{modeString}\""))
                .CountAsync();
        }

        public async Task<Dictionary<ProcessingMode, int>> GetProcessingStatisticsAsync(string? sourceFile = null, bool includeDeleted = false)
        {
            var query = includeDeleted ? _context.ProductsWithDeleted : _context.Products;
            
            if (!string.IsNullOrWhiteSpace(sourceFile))
            {
                query = query.Where(p => p.DynamicPropertiesJson != null && 
                                        p.DynamicPropertiesJson.Contains($"\"SourceFile\":\"{sourceFile}\""));
            }

            var products = await query.ToListAsync();
            var statistics = new Dictionary<ProcessingMode, int>();

            foreach (ProcessingMode mode in Enum.GetValues<ProcessingMode>())
            {
                statistics[mode] = 0;
            }

            foreach (var product in products)
            {
                if (product.DynamicPropertiesJson != null)
                {
                    try
                    {
                        var dynamicProps = JsonSerializer.Deserialize<Dictionary<string, object>>(product.DynamicPropertiesJson);
                        if (dynamicProps?.TryGetValue("ProcessingMode", out var modeValue) == true)
                        {
                            if (Enum.TryParse<ProcessingMode>(modeValue.ToString(), out var mode))
                            {
                                statistics[mode]++;
                            }
                        }
                    }
                    catch (JsonException)
                    {
                        // Skip malformed JSON
                    }
                }
            }

            return statistics;
        }

        public async Task<IEnumerable<ProductEntity>> GetByCreatedDateRangeAsync(DateTime startDate, DateTime endDate, bool includeDeleted = false)
        {
            var query = includeDeleted ? _context.ProductsWithDeleted : _context.Products;
            return await query
                .Where(p => p.CreatedAt >= startDate && p.CreatedAt <= endDate)
                .OrderBy(p => p.CreatedAt)
                .ToListAsync();
        }

        #endregion

        #region Transaction Support

        public async Task<IDbContextTransaction> BeginTransactionAsync()
        {
            return await _context.Database.BeginTransactionAsync();
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        #endregion
    }
}
