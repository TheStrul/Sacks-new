using Microsoft.EntityFrameworkCore;
using SacksDataLayer.Data;
using SacksDataLayer.Repositories.Interfaces;
using SacksDataLayer.Entities;

namespace SacksDataLayer.Repositories.Implementations
{
    /// <summary>
    /// Repository implementation for managing supplier offers
    /// </summary>
    public class SupplierOffersRepository : ISupplierOffersRepository
    {
        private readonly SacksDbContext _context;

        public SupplierOffersRepository(SacksDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<SupplierOfferEntity?> GetByIdAsync(int id, CancellationToken cancellationToken)
        {
            return await _context.SupplierOffers
                .Include(so => so.Supplier)
                .Include(so => so.OfferProducts)
                .FirstOrDefaultAsync(so => so.Id == id, cancellationToken);
        }

        public async Task<SupplierOfferEntity?> GetActiveOfferAsync(int productId, int supplierId, CancellationToken cancellationToken)
        {
            return await _context.SupplierOffers
                .Include(so => so.Supplier)
                .Include(so => so.OfferProducts)
                .FirstOrDefaultAsync(so => so.SupplierId == supplierId && so.IsActive && 
                                          so.OfferProducts.Any(op => op.ProductId == productId), cancellationToken);
        }

        public Task<SupplierOfferEntity> CreateAsync(SupplierOfferEntity offer, CancellationToken cancellationToken)
        {
            if (offer == null)
                throw new ArgumentNullException(nameof(offer));

            offer.UpdateModified();
            _context.SupplierOffers.Add(offer);
            return Task.FromResult(offer);
        }

        public Task<SupplierOfferEntity> UpdateAsync(SupplierOfferEntity offer, CancellationToken cancellationToken)
        {
            if (offer == null)
                throw new ArgumentNullException(nameof(offer));

            offer.UpdateModified();
            _context.SupplierOffers.Update(offer);
            return Task.FromResult(offer);
        }

        public async Task<IEnumerable<SupplierOfferEntity>> GetByProductIdAsync(int productId, CancellationToken cancellationToken)
        {
            return await _context.SupplierOffers
                .Include(so => so.Supplier)
                .Include(so => so.OfferProducts)
                .Where(so => so.OfferProducts.Any(op => op.ProductId == productId))
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<SupplierOfferEntity>> GetBySupplierIdAsync(int supplierId, CancellationToken cancellationToken)
        {
            return await _context.SupplierOffers
                .Include(so => so.Supplier)
                .Include(so => so.OfferProducts)
                .Where(so => so.SupplierId == supplierId)
                .ToListAsync(cancellationToken);
        }

        public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken)
        {
            var offer = await _context.SupplierOffers.FindAsync(new object[] { id }, cancellationToken);
            if (offer == null)
                return false;

            _context.SupplierOffers.Remove(offer);
            return true;
        }

        public async Task DeactivateOldOffersAsync(int productId, int supplierId, CancellationToken cancellationToken)
        {
            var oldOffers = await _context.SupplierOffers
                .Where(so => so.SupplierId == supplierId && so.IsActive &&
                            so.OfferProducts.Any(op => op.ProductId == productId))
                .ToListAsync(cancellationToken);

            foreach (var offer in oldOffers)
            {
                offer.IsActive = false;
                offer.UpdateModified();
            }
        }

        public async Task<SupplierOfferEntity?> GetBySupplierAndOfferNameAsync(int supplierId, string offerName, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(offerName))
                return null;

            return await _context.SupplierOffers
                .Include(so => so.Supplier)
                .FirstOrDefaultAsync(so => so.SupplierId == supplierId && 
                                          so.OfferName != null &&
                                          so.OfferName.Equals(offerName, StringComparison.OrdinalIgnoreCase), 
                                    cancellationToken);
        }
    }
}
