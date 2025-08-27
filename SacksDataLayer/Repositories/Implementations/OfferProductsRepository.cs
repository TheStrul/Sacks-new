using Microsoft.EntityFrameworkCore;
using SacksDataLayer.Data;
using SacksDataLayer.Repositories.Interfaces;
using System.Text.Json;

namespace SacksDataLayer.Repositories.Implementations
{
    /// <summary>
    /// Repository implementation for OfferProduct relationships
    /// </summary>
    public class OfferProductsRepository : IOfferProductsRepository
    {
        private readonly SacksDbContext _context;

        public OfferProductsRepository(SacksDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<OfferProductEntity?> GetByIdAsync(int id)
        {
            return await _context.OfferProducts
                .Include(op => op.Offer)
                .ThenInclude(o => o.Supplier)
                .Include(op => op.Product)
                .FirstOrDefaultAsync(op => op.Id == id);
        }

        public async Task<IEnumerable<OfferProductEntity>> GetByOfferIdAsync(int offerId)
        {
            return await _context.OfferProducts
                .Include(op => op.Product)
                .Where(op => op.OfferId == offerId)
                .ToListAsync();
        }

        public async Task<IEnumerable<OfferProductEntity>> GetByProductIdAsync(int productId)
        {
            return await _context.OfferProducts
                .Include(op => op.Offer)
                .ThenInclude(o => o.Supplier)
                .Where(op => op.ProductId == productId)
                .ToListAsync();
        }

        public async Task<OfferProductEntity?> GetByOfferAndProductAsync(int offerId, int productId)
        {
            return await _context.OfferProducts
                .Include(op => op.Offer)
                .Include(op => op.Product)
                .FirstOrDefaultAsync(op => op.OfferId == offerId && op.ProductId == productId);
        }

        public async Task<IEnumerable<OfferProductEntity>> GetPagedAsync(int skip, int take)
        {
            return await _context.OfferProducts
                .Include(op => op.Offer)
                .ThenInclude(o => o.Supplier)
                .Include(op => op.Product)
                .Skip(skip)
                .Take(take)
                .ToListAsync();
        }

        public async Task<int> GetCountAsync()
        {
            return await _context.OfferProducts.CountAsync();
        }

        public async Task<OfferProductEntity> CreateAsync(OfferProductEntity offerProduct)
        {
            if (offerProduct == null)
                throw new ArgumentNullException(nameof(offerProduct));

            offerProduct.CreatedAt = DateTime.UtcNow;
            
            // Serialize ProductProperties if they exist
            if (offerProduct.ProductProperties != null && offerProduct.ProductProperties.Any())
            {
                // The ProductProperties will be automatically serialized by EF Core
            }

            _context.OfferProducts.Add(offerProduct);
            await _context.SaveChangesAsync();
            return offerProduct;
        }

        public async Task<OfferProductEntity> UpdateAsync(OfferProductEntity offerProduct)
        {
            if (offerProduct == null)
                throw new ArgumentNullException(nameof(offerProduct));

            var existingOfferProduct = await GetByIdAsync(offerProduct.Id);
            if (existingOfferProduct == null)
                throw new InvalidOperationException($"OfferProduct with ID {offerProduct.Id} not found");

            // Update properties
            existingOfferProduct.Price = offerProduct.Price;
            existingOfferProduct.Capacity = offerProduct.Capacity;
            existingOfferProduct.Discount = offerProduct.Discount;
            existingOfferProduct.UnitOfMeasure = offerProduct.UnitOfMeasure;
            existingOfferProduct.MinimumOrderQuantity = offerProduct.MinimumOrderQuantity;
            existingOfferProduct.MaximumOrderQuantity = offerProduct.MaximumOrderQuantity;
            existingOfferProduct.ListPrice = offerProduct.ListPrice;
            existingOfferProduct.Notes = offerProduct.Notes;
            existingOfferProduct.IsAvailable = offerProduct.IsAvailable;
            existingOfferProduct.ProductProperties = offerProduct.ProductProperties;
            existingOfferProduct.UpdateModified(offerProduct.ModifiedBy);

            await _context.SaveChangesAsync();
            return existingOfferProduct;
        }

        public async Task<IEnumerable<OfferProductEntity>> BulkCreateAsync(IEnumerable<OfferProductEntity> offerProducts)
        {
            var offerProductList = offerProducts.ToList();
            
            foreach (var offerProduct in offerProductList)
            {
                offerProduct.CreatedAt = DateTime.UtcNow;
            }

            _context.OfferProducts.AddRange(offerProductList);
            await _context.SaveChangesAsync();
            return offerProductList;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var offerProduct = await GetByIdAsync(id);
            if (offerProduct == null)
                return false;

            _context.OfferProducts.Remove(offerProduct);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsAsync(int offerId, int productId)
        {
            return await _context.OfferProducts
                .AnyAsync(op => op.OfferId == offerId && op.ProductId == productId);
        }
    }
}
