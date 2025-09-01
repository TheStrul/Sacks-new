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

        public async Task<SupplierOfferEntity?> GetByIdAsync(int id)
        {
            return await _context.SupplierOffers
                .Include(so => so.Supplier)
                .Include(so => so.OfferProducts)
                .FirstOrDefaultAsync(so => so.Id == id);
        }

        public async Task<SupplierOfferEntity?> GetActiveOfferAsync(int productId, int supplierId)
        {
            return await _context.SupplierOffers
                .Include(so => so.Supplier)
                .Include(so => so.OfferProducts)
                .FirstOrDefaultAsync(so => so.SupplierId == supplierId && so.IsActive && 
                                          so.OfferProducts.Any(op => op.ProductId == productId));
        }

        public async Task<SupplierOfferEntity> CreateAsync(SupplierOfferEntity offer)
        {
            if (offer == null)
                throw new ArgumentNullException(nameof(offer));

            offer.UpdateModified();
            _context.SupplierOffers.Add(offer);
            await _context.SaveChangesAsync();
            return offer;
        }

        public async Task<SupplierOfferEntity> UpdateAsync(SupplierOfferEntity offer)
        {
            if (offer == null)
                throw new ArgumentNullException(nameof(offer));

            offer.UpdateModified();
            _context.SupplierOffers.Update(offer);
            await _context.SaveChangesAsync();
            return offer;
        }

        public async Task<IEnumerable<SupplierOfferEntity>> GetByProductIdAsync(int productId)
        {
            return await _context.SupplierOffers
                .Include(so => so.Supplier)
                .Include(so => so.OfferProducts)
                .Where(so => so.OfferProducts.Any(op => op.ProductId == productId))
                .ToListAsync();
        }

        public async Task<IEnumerable<SupplierOfferEntity>> GetBySupplierIdAsync(int supplierId)
        {
            return await _context.SupplierOffers
                .Include(so => so.Supplier)
                .Include(so => so.OfferProducts)
                .Where(so => so.SupplierId == supplierId)
                .ToListAsync();
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var offer = await _context.SupplierOffers.FindAsync(id);
            if (offer == null)
                return false;

            _context.SupplierOffers.Remove(offer);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task DeactivateOldOffersAsync(int productId, int supplierId)
        {
            var oldOffers = await _context.SupplierOffers
                .Where(so => so.SupplierId == supplierId && so.IsActive &&
                            so.OfferProducts.Any(op => op.ProductId == productId))
                .ToListAsync();

            foreach (var offer in oldOffers)
            {
                offer.IsActive = false;
                offer.UpdateModified();
            }

            await _context.SaveChangesAsync();
        }
    }
}
