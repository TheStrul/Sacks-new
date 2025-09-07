using Microsoft.EntityFrameworkCore;
using SacksDataLayer.Data;
using SacksDataLayer.Repositories.Interfaces;
using SacksDataLayer.Entities;

namespace SacksDataLayer.Repositories.Implementations
{
    /// <summary>
    /// Transaction-aware repository implementation for Products
    /// All operations are tracked in context but not automatically saved - use with UnitOfWork
    /// </summary>
    public class TransactionalProductsRepository : ITransactionalProductsRepository
    {
        private readonly SacksDbContext _context;

        public TransactionalProductsRepository(SacksDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        #region Query Operations

        public async Task<ProductEntity?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _context.Products
                .Include(p => p.OfferProducts)
                .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        }

        public async Task<ProductEntity?> GetByEANAsync(string ean, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(ean))
                return null;

            return await _context.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.EAN == ean, cancellationToken);
        }

        public async Task<Dictionary<string, ProductEntity>> GetByEANsBulkAsync(IEnumerable<string> eans, CancellationToken cancellationToken = default)
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

        public async Task<IEnumerable<ProductEntity>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Products
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<ProductEntity>> GetPagedAsync(int skip, int take, CancellationToken cancellationToken = default)
        {
            return await _context.Products
                .AsNoTracking()
                .Skip(skip)
                .Take(take)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<ProductEntity>> FindAsync(System.Linq.Expressions.Expression<Func<ProductEntity, bool>> predicate, CancellationToken cancellationToken = default)
        {
            return await _context.Products
                .AsNoTracking()
                .Where(predicate)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<ProductEntity>> SearchByNameAsync(string searchTerm, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return Enumerable.Empty<ProductEntity>();

            var lowerSearchTerm = searchTerm.ToLower();
            return await _context.Products
                .AsNoTracking()
                .Where(p => p.Name.ToLower().Contains(lowerSearchTerm))
                .ToListAsync(cancellationToken);
        }

        public async Task<bool> EANExistsAsync(string ean, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(ean))
                return false;

            return await _context.Products.AnyAsync(p => p.EAN == ean, cancellationToken);
        }

        public async Task<IEnumerable<ProductEntity>> GetBySourceFileAsync(string sourceFile, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(sourceFile))
                return Enumerable.Empty<ProductEntity>();

            return await _context.Products
                .AsNoTracking()
                .Where(p => p.DynamicPropertiesJson != null && p.DynamicPropertiesJson.Contains($"SourceFile\":\"{sourceFile}\""))
                .ToListAsync(cancellationToken);
        }

        public async Task<int> GetCountAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Products.CountAsync(cancellationToken);
        }

        public async Task<IEnumerable<ProductEntity>> GetByCreatedDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
        {
            return await _context.Products
                .AsNoTracking()
                .Where(p => p.CreatedAt >= startDate && p.CreatedAt <= endDate)
                .ToListAsync(cancellationToken);
        }

        #endregion

        #region Transaction-Aware CRUD Operations

        public void Add(ProductEntity product, string? createdBy = null)
        {
            if (product == null)
                throw new ArgumentNullException(nameof(product));

            product.CreatedAt = DateTime.UtcNow;
            _context.Products.Add(product);
        }

        public void AddRange(IEnumerable<ProductEntity> products, string? createdBy = null)
        {
            if (products == null)
                throw new ArgumentNullException(nameof(products));

            var productsToAdd = products.ToList();
            foreach (var product in productsToAdd)
            {
                product.CreatedAt = DateTime.UtcNow;
            }

            _context.Products.AddRange(productsToAdd);
        }

        public void Update(ProductEntity product, string? modifiedBy = null)
        {
            if (product == null)
                throw new ArgumentNullException(nameof(product));

            product.ModifiedAt = DateTime.UtcNow;
            _context.Products.Update(product);
        }

        public void UpdateRange(IEnumerable<ProductEntity> products, string? modifiedBy = null)
        {
            if (products == null)
                throw new ArgumentNullException(nameof(products));

            var productsToUpdate = products.ToList();
            foreach (var product in productsToUpdate)
            {
                product.ModifiedAt = DateTime.UtcNow;
            }

            _context.Products.UpdateRange(productsToUpdate);
        }

        public void Remove(ProductEntity product)
        {
            if (product == null)
                throw new ArgumentNullException(nameof(product));

            _context.Products.Remove(product);
        }

        public void RemoveRange(IEnumerable<ProductEntity> products)
        {
            if (products == null)
                throw new ArgumentNullException(nameof(products));

            _context.Products.RemoveRange(products);
        }

        public async Task<bool> RemoveByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var product = await _context.Products.FindAsync(new object[] { id }, cancellationToken);
            if (product == null)
                return false;

            _context.Products.Remove(product);
            return true;
        }

        #endregion
    }
}