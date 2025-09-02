using Microsoft.EntityFrameworkCore;
using SacksDataLayer.Data;
using SacksDataLayer.Repositories.Interfaces;
using SacksDataLayer.Entities;
using System.Linq.Expressions;

namespace SacksDataLayer.Repositories.Implementations
{
    /// <summary>
    /// Repository implementation for managing products
    /// Transaction-only operations - all saves handled by UnitOfWork
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
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.EAN == ean, cancellationToken);
        }

        public async Task<Dictionary<string, ProductEntity>> GetByEANsBulkAsync(IEnumerable<string> eans, CancellationToken cancellationToken)
        {
            if (eans == null || !eans.Any())
                return new Dictionary<string, ProductEntity>();

            var eanList = eans.Where(e => !string.IsNullOrWhiteSpace(e)).Distinct().ToList();
            if (!eanList.Any())
                return new Dictionary<string, ProductEntity>();

            var products = await _context.Products
                .AsNoTracking()
                .Where(p => eanList.Contains(p.EAN))
                .ToDictionaryAsync(p => p.EAN, cancellationToken);

            return products;
        }

        public async Task<IEnumerable<ProductEntity>> GetAllAsync(CancellationToken cancellationToken)
        {
            return await _context.Products
                .Include(p => p.OfferProducts)
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }

        public Task<ProductEntity> CreateAsync(ProductEntity product, string? createdBy = null, CancellationToken cancellationToken = default)
        {
            if (product == null)
                throw new ArgumentNullException(nameof(product));

            product.CreatedAt = DateTime.UtcNow;
            _context.Products.Add(product);
            return Task.FromResult(product);
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

            return existingProduct;
        }

        public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken)
        {
            var product = await _context.Products.FindAsync(new object[] { id }, cancellationToken);
            if (product == null)
                return false;

            _context.Products.Remove(product);
            return true;
        }

        public async Task<IEnumerable<ProductEntity>> GetPagedAsync(int skip, int take, CancellationToken cancellationToken)
        {
            return await _context.Products
                .Include(p => p.OfferProducts)
                .AsNoTracking()
                .Skip(skip)
                .Take(take)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<ProductEntity>> SearchAsync(string searchTerm, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return Enumerable.Empty<ProductEntity>();

            var lowerSearchTerm = searchTerm.ToLower();
            return await _context.Products
                .Include(p => p.OfferProducts)
                .AsNoTracking()
                .Where(p => 
                    p.Name.ToLower().Contains(lowerSearchTerm) ||
                    (p.Description != null && p.Description.ToLower().Contains(lowerSearchTerm)) ||
                    (p.EAN != null && p.EAN.ToLower().Contains(lowerSearchTerm)))
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<ProductEntity>> SearchByNameAsync(string searchTerm, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return Enumerable.Empty<ProductEntity>();

            var lowerSearchTerm = searchTerm.ToLower();
            return await _context.Products
                .Include(p => p.OfferProducts)
                .AsNoTracking()
                .Where(p => p.Name.ToLower().Contains(lowerSearchTerm))
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<ProductEntity>> FindAsync(Expression<Func<ProductEntity, bool>> predicate, CancellationToken cancellationToken)
        {
            return await _context.Products
                .Include(p => p.OfferProducts)
                .AsNoTracking()
                .Where(predicate)
                .ToListAsync(cancellationToken);
        }

        public async Task<bool> ExistsAsync(int id, CancellationToken cancellationToken)
        {
            return await _context.Products.AnyAsync(p => p.Id == id, cancellationToken);
        }

        public async Task<bool> EANExistsAsync(string ean, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(ean))
                return false;

            return await _context.Products.AnyAsync(p => p.EAN == ean, cancellationToken);
        }

        public Task<IEnumerable<ProductEntity>> CreateBulkAsync(IEnumerable<ProductEntity> products, string? createdBy = null, CancellationToken cancellationToken = default)
        {
            if (products == null || !products.Any())
                return Task.FromResult(Enumerable.Empty<ProductEntity>());

            var productsToAdd = products.ToList();
            foreach (var product in productsToAdd)
            {
                product.CreatedAt = DateTime.UtcNow;
            }

            _context.Products.AddRange(productsToAdd);
            return Task.FromResult<IEnumerable<ProductEntity>>(productsToAdd);
        }

        public Task<IEnumerable<ProductEntity>> UpdateBulkAsync(IEnumerable<ProductEntity> products, string? modifiedBy = null, CancellationToken cancellationToken = default)
        {
            if (products == null || !products.Any())
                return Task.FromResult(Enumerable.Empty<ProductEntity>());

            var productsToUpdate = products.ToList();
            foreach (var product in productsToUpdate)
            {
                product.ModifiedAt = DateTime.UtcNow;
                _context.Products.Update(product);
            }

            return Task.FromResult<IEnumerable<ProductEntity>>(productsToUpdate);
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
                .AsNoTracking()
                .Where(p => p.CreatedAt >= startDate && p.CreatedAt <= endDate)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<ProductEntity>> GetBySourceFileAsync(string sourceFile, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(sourceFile))
                return Enumerable.Empty<ProductEntity>();

            return await _context.Products
                .Include(p => p.OfferProducts)
                .AsNoTracking()
                .Where(p => p.DynamicPropertiesJson != null && p.DynamicPropertiesJson.Contains($"SourceFile\":\"{sourceFile}\""))
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<ProductEntity>> SearchByDynamicPropertyAsync(string propertyKey, object? propertyValue = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(propertyKey))
                return Enumerable.Empty<ProductEntity>();

            var query = _context.Products
                .Include(p => p.OfferProducts)
                .AsNoTracking();

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
    }
}