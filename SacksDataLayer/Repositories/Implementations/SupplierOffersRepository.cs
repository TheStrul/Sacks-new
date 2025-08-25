using Microsoft.EntityFrameworkCore;
using SacksDataLayer.Data;
using SacksDataLayer.Repositories.Interfaces;

namespace SacksDataLayer.Repositories.Implementations;

public class SupplierOffersRepository : ISupplierOffersRepository
{
    private readonly SacksDbContext _context;

    public SupplierOffersRepository(SacksDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<SupplierOfferEntity?> GetActiveOfferAsync(int productId, int supplierId)
    {
        // Find active offers through the OfferProducts junction table
        var offerProduct = await _context.OfferProducts
            .Include(op => op.Offer)
            .ThenInclude(so => so.Supplier)
            .Include(op => op.Product)
            .FirstOrDefaultAsync(op => op.ProductId == productId && 
                                     op.Offer.SupplierId == supplierId && 
                                     op.Offer.IsActive);

        return offerProduct?.Offer;
    }

    public async Task<SupplierOfferEntity> CreateAsync(SupplierOfferEntity offer)
    {
        _context.SupplierOffers.Add(offer);
        await _context.SaveChangesAsync();
        return offer;
    }

    public async Task<SupplierOfferEntity> UpdateAsync(SupplierOfferEntity offer)
    {
        _context.SupplierOffers.Update(offer);
        await _context.SaveChangesAsync();
        return offer;
    }

    public async Task<IEnumerable<SupplierOfferEntity>> GetByProductIdAsync(int productId)
    {
        // Get offers that contain the specified product
        var offers = await _context.SupplierOffers
            .Include(so => so.Supplier)
            .Include(so => so.OfferProducts.Where(op => op.ProductId == productId))
            .ThenInclude(op => op.Product)
            .Where(so => so.OfferProducts.Any(op => op.ProductId == productId))
            .ToListAsync();

        return offers;
    }

    public async Task<IEnumerable<SupplierOfferEntity>> GetBySupplierIdAsync(int supplierId)
    {
        var offers = await _context.SupplierOffers
            .Include(so => so.Supplier)
            .Include(so => so.OfferProducts)
            .ThenInclude(op => op.Product)
            .Where(so => so.SupplierId == supplierId)
            .ToListAsync();

        return offers;
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
        // Find offers through the OfferProducts junction table
        var offerIds = await _context.OfferProducts
            .Where(op => op.ProductId == productId && op.Offer.SupplierId == supplierId)
            .Select(op => op.OfferId)
            .Distinct()
            .ToListAsync();

        var oldOffers = await _context.SupplierOffers
            .Where(so => offerIds.Contains(so.Id) && so.IsActive)
            .ToListAsync();

        foreach (var offer in oldOffers)
        {
            offer.IsActive = false;
            offer.ValidTo = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
    }
}
