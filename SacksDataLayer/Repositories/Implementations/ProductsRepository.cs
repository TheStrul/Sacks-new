using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SacksDataLayer.Data;
using SacksDataLayer.FileProcessing.Models;
using SacksDataLayer.Repositories.Interfaces;
using SacksDataLayer.Entities;
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

        public async Task<ProductEntity?> GetByIdAsync(int id, CancellationToken cancellationToken)
        {
            return await _context.Products
                .Include(p => p.OfferProducts)
                .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        }

        public async Task<ProductEntity?> GetByEANAsync(string ean, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(ean))
                return null;

            return await _context.Products
                .Include(p => p.OfferProducts)
                .FirstOrDefaultAsync(p => p.EAN == ean, cancellationToken);
        }

        public async Task<IEnumerable<ProductEntity>> GetAllAsync(CancellationToken cancellationToken)
        {
            return await _context.Products
                .Include(p => p.OfferProducts)
                .ToListAsync(cancellationToken);
        }

        // The GetByNameAsync method is not part of the interface, but used internally
        public async Task<IEnumerable<ProductEntity>> GetByNameAsync(string name, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(name))
                return Enumerable.Empty<ProductEntity>();

            return await _context.Products
                .Include(p => p.OfferProducts)
                .Where(p => p.Name.Contains(name))
                .ToListAsync(cancellationToken);
        }


        public async Task<ProductEntity> CreateAsync(ProductEntity product, string? createdBy = null, CancellationToken cancellationToken = default)
        {
            if (product == null)
                throw new ArgumentNullException(nameof(product));

            product.CreatedAt = DateTime.UtcNow;
            _context.Products.Add(product);
            await _context.SaveChangesAsync(cancellationToken);
            return product;
        }

        public async Task<ProductEntity> UpdateAsync(ProductEntity product, string? modifiedBy = null, CancellationToken cancellationToken = default)
        {
            if (product == null)
                throw new ArgumentNullException(nameof(product));

            var existingProduct = await _context.Products.FindAsync(new object[] { product.Id }, cancellationToken);
            if (existingProduct == null)
                throw new InvalidOperationException($"Product with ID {product.Id} not found");

            existingProduct.Name = product.Name;
            existingProduct.Description = product.Description;
            existingProduct.EAN = product.EAN;
            existingProduct.ModifiedAt = DateTime.UtcNow;
            existingProduct.DynamicProperties = product.DynamicProperties;

            await _context.SaveChangesAsync(cancellationToken);
            return existingProduct;
        }

        public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken)
        {
            var product = await _context.Products.FindAsync(new object[] { id }, cancellationToken);
            if (product == null)
                return false;

            _context.Products.Remove(product);
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<IEnumerable<ProductEntity>> GetPagedAsync(int skip, int take, CancellationToken cancellationToken)
        {
            return await _context.Products
                .Include(p => p.OfferProducts)
                .Skip(skip)
                .Take(take)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<ProductEntity>> FindAsync(Expression<Func<ProductEntity, bool>> predicate, CancellationToken cancellationToken)
        {
            return await _context.Products
                .Include(p => p.OfferProducts)
                .Where(predicate)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<ProductEntity>> SearchByNameAsync(string searchTerm, CancellationToken cancellationToken)
        {
            return await GetByNameAsync(searchTerm, cancellationToken);
        }

        public async Task<bool> EANExistsAsync(string ean, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(ean))
                return false;

            return await _context.Products.AnyAsync(p => p.EAN == ean, cancellationToken);
        }

        public async Task<IEnumerable<ProductEntity>> GetBySourceFileAsync(string sourceFile, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(sourceFile))
                return Enumerable.Empty<ProductEntity>();

            return await _context.Products
                .Include(p => p.OfferProducts)
                .Where(p => p.DynamicPropertiesJson != null && p.DynamicPropertiesJson.Contains($"SourceFile\":\"{sourceFile}\""))
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<ProductEntity>> SearchByDynamicPropertyAsync(string propertyKey, object? propertyValue = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(propertyKey))
                return Enumerable.Empty<ProductEntity>();

            var query = _context.Products.Include(p => p.OfferProducts);

            if (propertyValue == null)
            {
                return await query
                    .Where(p => p.DynamicPropertiesJson != null && p.DynamicPropertiesJson.Contains($"\"{propertyKey}\""))
                    .ToListAsync(cancellationToken);
            }
            else
            {
                var valueString = propertyValue.ToString();
                return await query
                    .Where(p => p.DynamicPropertiesJson != null && p.DynamicPropertiesJson.Contains($"\"{propertyKey}\":\"{valueString}\""))
                    .ToListAsync(cancellationToken);
            }
        }

        public async Task<IEnumerable<ProductEntity>> CreateBulkAsync(IEnumerable<ProductEntity> products, string? createdBy = null, CancellationToken cancellationToken = default)
        {
            if (products == null || !products.Any())
                return Enumerable.Empty<ProductEntity>();

            var productsToAdd = products.ToList();
            foreach (var product in productsToAdd)
            {
                product.CreatedAt = DateTime.UtcNow;
            }

            _context.Products.AddRange(productsToAdd);
            await _context.SaveChangesAsync(cancellationToken);
            return productsToAdd;
        }

        public async Task<IEnumerable<ProductEntity>> UpdateBulkAsync(IEnumerable<ProductEntity> products, string? modifiedBy = null, CancellationToken cancellationToken = default)
        {
            if (products == null || !products.Any())
                return Enumerable.Empty<ProductEntity>();

            var productsToUpdate = products.ToList();
            foreach (var product in productsToUpdate)
            {
                product.ModifiedAt = DateTime.UtcNow;
                _context.Products.Update(product);
            }

            await _context.SaveChangesAsync(cancellationToken);
            return productsToUpdate;
        }

        public async Task<int> DeleteBulkAsync(IEnumerable<int> ids, CancellationToken cancellationToken)
        {
            if (ids == null || !ids.Any())
                return 0;

            var idsToDelete = ids.ToList();
            var productsToDelete = await _context.Products
                .Where(p => idsToDelete.Contains(p.Id))
                .ToListAsync(cancellationToken);

            _context.Products.RemoveRange(productsToDelete);
            await _context.SaveChangesAsync(cancellationToken);
            return productsToDelete.Count;
        }

        public async Task<int> GetCountAsync(CancellationToken cancellationToken)
        {
            return await _context.Products.CountAsync(cancellationToken);
        }

        public async Task<IEnumerable<ProductEntity>> GetByCreatedDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken)
        {
            return await _context.Products
                .Include(p => p.OfferProducts)
                .Where(p => p.CreatedAt >= startDate && p.CreatedAt <= endDate)
                .ToListAsync(cancellationToken);
        }

        public async Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Database.BeginTransactionAsync(cancellationToken);
        }

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return await _context.SaveChangesAsync(cancellationToken);
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
            existingProduct.EAN = product.EAN;
            existingProduct.ModifiedAt = DateTime.UtcNow;
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

        public async Task<bool> EANExistsAsync(string ean)
        {
            if (string.IsNullOrWhiteSpace(ean))
                return false;

            return await _context.Products.AnyAsync(p => p.EAN == ean);
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
                    (p.EAN != null && p.EAN.ToLower().Contains(lowerSearchTerm)))
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
                product.ModifiedAt = DateTime.UtcNow;
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
