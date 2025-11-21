using Microsoft.EntityFrameworkCore;
using Sacks.DataAccess.Data;
using Sacks.Core.Repositories.Interfaces;
using Sacks.Core.Entities;

namespace Sacks.DataAccess.Repositories.Implementations
{
    /// <summary>
    /// Transaction-aware repository implementation for SupplierOffers
    /// All operations are tracked in context but not automatically saved - use with UnitOfWork
    /// </summary>
    public class TransactionalSupplierOffersRepository : ITransactionalSupplierOffersRepository
    {
        private readonly SacksDbContext _context;

        public TransactionalSupplierOffersRepository(SacksDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        #region Query Operations

        public async Task<Offer?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _context.SupplierOffers
                .Include(so => so.Supplier)
                .Include(so => so.OfferProducts)
                .FirstOrDefaultAsync(so => so.Id == id, cancellationToken);
        }

        public async Task<Offer?> GetActiveOfferAsync(int productId, int supplierId, CancellationToken cancellationToken = default)
        {
            return await _context.SupplierOffers
                .Include(so => so.Supplier)
                .Include(so => so.OfferProducts)
                .FirstOrDefaultAsync(so => so.SupplierId == supplierId && 
                                          so.OfferProducts.Any(op => op.ProductId == productId), cancellationToken);
        }

        public async Task<IEnumerable<Offer>> GetByProductIdAsync(int productId, CancellationToken cancellationToken = default)
        {
            return await _context.SupplierOffers
                .Include(so => so.Supplier)
                .Where(so => so.OfferProducts.Any(op => op.ProductId == productId))
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Offer>> GetBySupplierIdAsync(int supplierId, CancellationToken cancellationToken = default)
        {
            return await _context.SupplierOffers
                .Include(so => so.OfferProducts)
                .Where(so => so.SupplierId == supplierId)
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Offer>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _context.SupplierOffers
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }

        public async Task<int> GetCountAsync(CancellationToken cancellationToken = default)
        {
            return await _context.SupplierOffers.CountAsync(cancellationToken);
        }

        #endregion

        #region Transaction-Aware CRUD Operations

        public void Add(Offer offer)
        {
            if (offer == null)
                throw new ArgumentNullException(nameof(offer));

            offer.CreatedAt = DateTime.UtcNow;
            _context.SupplierOffers.Add(offer);
        }

        public void AddRange(IEnumerable<Offer> offers)
        {
            if (offers == null)
                throw new ArgumentNullException(nameof(offers));

            var offersToAdd = offers.ToList();
            foreach (var offer in offersToAdd)
            {
                offer.CreatedAt = DateTime.UtcNow;
                _context.Entry(offer).State = EntityState.Added;
            }

            _context.SupplierOffers.AddRange(offersToAdd);
        }

        public void Update(Offer offer)
        {
            if (offer == null)
                throw new ArgumentNullException(nameof(offer));

            offer.ModifiedAt = DateTime.UtcNow;
            _context.SupplierOffers.Update(offer);
        }

        public void UpdateRange(IEnumerable<Offer> offers)
        {
            if (offers == null)
                throw new ArgumentNullException(nameof(offers));

            var offersToUpdate = offers.ToList();
            foreach (var offer in offersToUpdate)
            {
                offer.ModifiedAt = DateTime.UtcNow;
                _context.Entry(offer).State = EntityState.Modified;
            }

            _context.SupplierOffers.UpdateRange(offersToUpdate);
        }

        public void Remove(Offer offer)
        {
            if (offer == null)
                throw new ArgumentNullException(nameof(offer));

            _context.SupplierOffers.Remove(offer);
        }

        public void RemoveRange(IEnumerable<Offer> offers)
        {
            if (offers == null)
                throw new ArgumentNullException(nameof(offers));

            _context.SupplierOffers.RemoveRange(offers);
        }

        public async Task<bool> RemoveByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var offer = await _context.SupplierOffers.FindAsync(new object[] { id }, cancellationToken);
            if (offer == null)
                return false;

            _context.SupplierOffers.Remove(offer);
            return true;
        }

        public async Task DeactivateOldOffersAsync(int productId, int supplierId, CancellationToken cancellationToken = default)
        {
            var offersToDeactivate = await _context.SupplierOffers
                .Where(so => so.SupplierId == supplierId && 
                            so.OfferProducts.Any(op => op.ProductId == productId))
                .ToListAsync(cancellationToken);

            // Note: Since Offer doesn't have IsActive property,
            // this method currently doesn't perform any deactivation.
            // If needed, this could be extended to soft-delete or mark records differently.
            foreach (var offer in offersToDeactivate)
            {
                offer.ModifiedAt = DateTime.UtcNow;
                // Could add custom logic here if IsActive property is added later
            }
        }

        #endregion
    }
}
