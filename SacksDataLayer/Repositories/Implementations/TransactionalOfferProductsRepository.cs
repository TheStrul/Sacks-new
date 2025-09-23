using Microsoft.EntityFrameworkCore;
using SacksDataLayer.Data;
using SacksDataLayer.Repositories.Interfaces;
using SacksDataLayer.Entities;

namespace SacksDataLayer.Repositories.Implementations
{
    /// <summary>
    /// Transaction-aware repository implementation for OfferProducts
    /// All operations are tracked in context but not automatically saved - use with UnitOfWork
    /// </summary>
    public class TransactionalOfferProductsRepository : ITransactionalOfferProductsRepository
    {
        private readonly SacksDbContext _context;

        public TransactionalOfferProductsRepository(SacksDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        #region Query Operations

        public async Task<ProductOffer?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _context.OfferProducts
                .Include(op => op.Offer)
                .ThenInclude(o => o.Supplier)
                .Include(op => op.Product)
                .FirstOrDefaultAsync(op => op.Id == id, cancellationToken);
        }

        public async Task<IEnumerable<ProductOffer>> GetByOfferIdAsync(int offerId, CancellationToken cancellationToken = default)
        {
            return await _context.OfferProducts
                .Include(op => op.Product)
                .Where(op => op.OfferId == offerId)
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<ProductOffer>> GetByProductIdAsync(int productId, CancellationToken cancellationToken = default)
        {
            return await _context.OfferProducts
                .Include(op => op.Offer)
                .ThenInclude(o => o.Supplier)
                .Where(op => op.ProductId == productId)
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }

        public async Task<ProductOffer?> GetByOfferAndProductAsync(int offerId, int productId, CancellationToken cancellationToken = default)
        {
            return await _context.OfferProducts
                .Include(op => op.Offer)
                .Include(op => op.Product)
                .FirstOrDefaultAsync(op => op.OfferId == offerId && op.ProductId == productId, cancellationToken);
        }

        public async Task<IEnumerable<ProductOffer>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _context.OfferProducts
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }

        public async Task<int> GetCountAsync(CancellationToken cancellationToken = default)
        {
            return await _context.OfferProducts.CountAsync(cancellationToken);
        }

        #endregion

        #region Transaction-Aware CRUD Operations

        public void Add(ProductOffer offerProduct)
        {
            if (offerProduct == null)
                throw new ArgumentNullException(nameof(offerProduct));

            offerProduct.CreatedAt = DateTime.UtcNow;
            _context.OfferProducts.Add(offerProduct);
        }

        public void AddRange(IEnumerable<ProductOffer> offerProducts)
        {
            if (offerProducts == null)
                throw new ArgumentNullException(nameof(offerProducts));

            var offerProductsToAdd = offerProducts.ToList();
            foreach (var offerProduct in offerProductsToAdd)
            {
                offerProduct.CreatedAt = DateTime.UtcNow;
            }

            _context.OfferProducts.AddRange(offerProductsToAdd);
        }

        public void Update(ProductOffer offerProduct)
        {
            if (offerProduct == null)
                throw new ArgumentNullException(nameof(offerProduct));

            offerProduct.ModifiedAt = DateTime.UtcNow;
            _context.OfferProducts.Update(offerProduct);
        }

        public void UpdateRange(IEnumerable<ProductOffer> offerProducts)
        {
            if (offerProducts == null)
                throw new ArgumentNullException(nameof(offerProducts));

            var offerProductsToUpdate = offerProducts.ToList();
            foreach (var offerProduct in offerProductsToUpdate)
            {
                offerProduct.ModifiedAt = DateTime.UtcNow;
            }

            _context.OfferProducts.UpdateRange(offerProductsToUpdate);
        }

        public void Remove(ProductOffer offerProduct)
        {
            if (offerProduct == null)
                throw new ArgumentNullException(nameof(offerProduct));

            _context.OfferProducts.Remove(offerProduct);
        }

        public void RemoveRange(IEnumerable<ProductOffer> offerProducts)
        {
            if (offerProducts == null)
                throw new ArgumentNullException(nameof(offerProducts));

            _context.OfferProducts.RemoveRange(offerProducts);
        }

        public async Task<bool> RemoveByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var offerProduct = await _context.OfferProducts.FindAsync(new object[] { id }, cancellationToken);
            if (offerProduct == null)
                return false;

            _context.OfferProducts.Remove(offerProduct);
            return true;
        }

        #endregion
    }
}