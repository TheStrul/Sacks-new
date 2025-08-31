using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SacksDataLayer.Data;
using SacksDataLayer.FileProcessing.Models;
using SacksDataLayer.Repositories.Interfaces;
using System.Linq.Expressions;

namespace SacksDataLayer.Repositories.Implementations
{
    /// <summary>
    /// Repository implementation for managing products
    /// </summary>
    public class ProductsRepository : IProductsRepository
    {
        private readonly SacksDbContext _context;

        public ProductsRepository(SacksDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<ProductEntity?> GetByIdAsync(int id)
        {
            return await _context.Products
                .Include(p => p.OfferProducts)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<ProductEntity?> GetBySKUAsync(string sku)
        {
            if (string.IsNullOrWhiteSpace(sku))
                return null;

            return await _context.Products
                .Include(p => p.OfferProducts)
                .FirstOrDefaultAsync(p => p.SKU == sku);
        }

        public async Task<IEnumerable<ProductEntity>> GetAllAsync()
        {
            return await _context.Products
                .Include(p => p.OfferProducts)
                .ToListAsync();
        }

        public async Task<IEnumerable<ProductEntity>> GetByNameAsync(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return Enumerable.Empty<ProductEntity>();

            return await _context.Products
                .Include(p => p.OfferProducts)
                .Where(p => p.Name.Contains(name))
                .ToListAsync();
        }

        public async Task<ProductEntity> CreateAsync(ProductEntity product, string? createdBy = null)
        {
            if (product == null)
                throw new ArgumentNullException(nameof(product));

            product.CreatedAt = DateTime.UtcNow;
            
            _context.Products.Add(product);
            await _context.SaveChangesAsync();
            return product;
        }

        public async Task<ProductEntity> UpdateAsync(ProductEntity product, string? modifiedBy = null)
        {
            if (product == null)
                throw new ArgumentNullException(nameof(product));

            var existingProduct = await _context.Products.FindAsync(product.Id);
            if (existingProduct == null)
                throw new InvalidOperationException($"Product with ID {product.Id} not found");

            // Update basic properties
            existingProduct.Name = product.Name;
            existingProduct.Description = product.Description;
            existingProduct.SKU = product.SKU;
            existingProduct.UpdatedAt = DateTime.UtcNow;
            existingProduct.DynamicProperties = product.DynamicProperties;

            await _context.SaveChangesAsync();
            return existingProduct;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
                return false;

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsAsync(int id)
        {
            return await _context.Products.AnyAsync(p => p.Id == id);
        }

        public async Task<bool> SKUExistsAsync(string sku)
        {
            if (string.IsNullOrWhiteSpace(sku))
                return false;

            return await _context.Products.AnyAsync(p => p.SKU == sku);
        }

        public async Task<IEnumerable<ProductEntity>> BulkCreateAsync(IEnumerable<ProductEntity> products)
        {
            if (products == null || !products.Any())
                return Enumerable.Empty<ProductEntity>();

            var productsToAdd = products.ToList();
            foreach (var product in productsToAdd)
            {
                product.CreatedAt = DateTime.UtcNow;
            }

            _context.Products.AddRange(productsToAdd);
            await _context.SaveChangesAsync();
            return productsToAdd;
        }

        public async Task<IEnumerable<ProductEntity>> SearchAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return Enumerable.Empty<ProductEntity>();

            var lowerSearchTerm = searchTerm.ToLower();
            return await _context.Products
                .Include(p => p.OfferProducts)
                .Where(p => 
                    p.Name.ToLower().Contains(lowerSearchTerm) ||
                    (p.Description != null && p.Description.ToLower().Contains(lowerSearchTerm)) ||
                    (p.SKU != null && p.SKU.ToLower().Contains(lowerSearchTerm)))
                .ToListAsync();
        }

        // Additional interface methods
        public async Task<IEnumerable<ProductEntity>> GetPagedAsync(int skip, int take)
        {
            return await _context.Products
                .Include(p => p.OfferProducts)
                .Skip(skip)
                .Take(take)
                .ToListAsync();
        }

        public async Task<IEnumerable<ProductEntity>> FindAsync(Expression<Func<ProductEntity, bool>> predicate)
        {
            return await _context.Products
                .Include(p => p.OfferProducts)
                .Where(predicate)
                .ToListAsync();
        }

        public async Task<IEnumerable<ProductEntity>> SearchByNameAsync(string searchTerm)
        {
            return await GetByNameAsync(searchTerm); // Reuse existing implementation
        }

        public async Task<IEnumerable<ProductEntity>> GetBySourceFileAsync(string sourceFile)
        {
            if (string.IsNullOrWhiteSpace(sourceFile))
                return Enumerable.Empty<ProductEntity>();

            return await _context.Products
                .Include(p => p.OfferProducts)
                .Where(p => p.DynamicPropertiesJson != null && p.DynamicPropertiesJson.Contains($"SourceFile\":\"{sourceFile}\""))
                .ToListAsync();
        }

        public async Task<IEnumerable<ProductEntity>> SearchByDynamicPropertyAsync(string propertyKey, object? propertyValue = null)
        {
            if (string.IsNullOrWhiteSpace(propertyKey))
                return Enumerable.Empty<ProductEntity>();

            var query = _context.Products.Include(p => p.OfferProducts);

            if (propertyValue == null)
            {
                return await query
                    .Where(p => p.DynamicPropertiesJson != null && p.DynamicPropertiesJson.Contains($"\"{propertyKey}\""))
                    .ToListAsync();
            }
            else
            {
                var valueString = propertyValue.ToString();
                return await query
                    .Where(p => p.DynamicPropertiesJson != null && p.DynamicPropertiesJson.Contains($"\"{propertyKey}\":\"{valueString}\""))
                    .ToListAsync();
            }
        }

        public async Task<IEnumerable<ProductEntity>> CreateBulkAsync(IEnumerable<ProductEntity> products, string? createdBy = null)
        {
            return await BulkCreateAsync(products); // Reuse existing implementation
        }

        public async Task<IEnumerable<ProductEntity>> UpdateBulkAsync(IEnumerable<ProductEntity> products, string? modifiedBy = null)
        {
            if (products == null || !products.Any())
                return Enumerable.Empty<ProductEntity>();

            var productsToUpdate = products.ToList();
            foreach (var product in productsToUpdate)
            {
                product.UpdatedAt = DateTime.UtcNow;
                _context.Products.Update(product);
            }

            await _context.SaveChangesAsync();
            return productsToUpdate;
        }

        public async Task<int> DeleteBulkAsync(IEnumerable<int> ids)
        {
            if (ids == null || !ids.Any())
                return 0;

            var idsToDelete = ids.ToList();
            var productsToDelete = await _context.Products
                .Where(p => idsToDelete.Contains(p.Id))
                .ToListAsync();

            _context.Products.RemoveRange(productsToDelete);
            await _context.SaveChangesAsync();
            return productsToDelete.Count;
        }

        public async Task<int> GetCountAsync()
        {
            return await _context.Products.CountAsync();
        }

        public async Task<IEnumerable<ProductEntity>> GetByCreatedDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await _context.Products
                .Include(p => p.OfferProducts)
                .Where(p => p.CreatedAt >= startDate && p.CreatedAt <= endDate)
                .ToListAsync();
        }

        public async Task<IDbContextTransaction> BeginTransactionAsync()
        {
            return await _context.Database.BeginTransactionAsync();
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }
    }
}
