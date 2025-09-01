using Microsoft.EntityFrameworkCore;
using SacksDataLayer.Data;
using SacksDataLayer.Repositories.Interfaces;
using SacksDataLayer.Entities;
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

        public async Task<OfferProductEntity?> GetByIdAsync(int id, CancellationToken cancellationToken)
        {
            return await _context.OfferProducts
                .Include(op => op.Offer)
                .ThenInclude(o => o.Supplier)
                .Include(op => op.Product)
                .FirstOrDefaultAsync(op => op.Id == id, cancellationToken);
        }

        public async Task<IEnumerable<OfferProductEntity>> GetByOfferIdAsync(int offerId, CancellationToken cancellationToken)
        {
            return await _context.OfferProducts
                .Include(op => op.Product)
                .Where(op => op.OfferId == offerId)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<OfferProductEntity>> GetByProductIdAsync(int productId, CancellationToken cancellationToken)
        {
            return await _context.OfferProducts
                .Include(op => op.Offer)
                .ThenInclude(o => o.Supplier)
                .Where(op => op.ProductId == productId)
                .ToListAsync(cancellationToken);
        }

        public async Task<OfferProductEntity?> GetByOfferAndProductAsync(int offerId, int productId, CancellationToken cancellationToken)
        {
            return await _context.OfferProducts
                .Include(op => op.Offer)
                .Include(op => op.Product)
                .FirstOrDefaultAsync(op => op.OfferId == offerId && op.ProductId == productId, cancellationToken);
        }

        public async Task<IEnumerable<OfferProductEntity>> GetPagedAsync(int skip, int take, CancellationToken cancellationToken)
        {
            return await _context.OfferProducts
                .Include(op => op.Offer)
                .ThenInclude(o => o.Supplier)
                .Include(op => op.Product)
                .Skip(skip)
                .Take(take)
                .ToListAsync(cancellationToken);
        }

        public async Task<int> GetCountAsync(CancellationToken cancellationToken)
        {
            return await _context.OfferProducts.CountAsync(cancellationToken);
        }

        public async Task<OfferProductEntity> CreateAsync(OfferProductEntity offerProduct, CancellationToken cancellationToken)
        {
            if (offerProduct == null)
                throw new ArgumentNullException(nameof(offerProduct));

            offerProduct.CreatedAt = DateTime.UtcNow;
            _context.OfferProducts.Add(offerProduct);
            await _context.SaveChangesAsync(cancellationToken);
            return offerProduct;
        }

        public async Task<OfferProductEntity> UpdateAsync(OfferProductEntity offerProduct, CancellationToken cancellationToken)
        {
            if (offerProduct == null)
                throw new ArgumentNullException(nameof(offerProduct));

            var existingOfferProduct = await GetByIdAsync(offerProduct.Id, cancellationToken);
            if (existingOfferProduct == null)
                throw new InvalidOperationException($"OfferProduct with ID {offerProduct.Id} not found");

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
            existingOfferProduct.UpdateModified();

            await _context.SaveChangesAsync(cancellationToken);
            return existingOfferProduct;
        }

        public async Task<IEnumerable<OfferProductEntity>> BulkCreateAsync(IEnumerable<OfferProductEntity> offerProducts, CancellationToken cancellationToken)
        {
            var offerProductList = offerProducts.ToList();
            foreach (var offerProduct in offerProductList)
            {
                offerProduct.CreatedAt = DateTime.UtcNow;
            }

            _context.OfferProducts.AddRange(offerProductList);
            await _context.SaveChangesAsync(cancellationToken);
            return offerProductList;
        }

        public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken)
        {
            var offerProduct = await GetByIdAsync(id, cancellationToken);
            if (offerProduct == null)
                return false;

            _context.OfferProducts.Remove(offerProduct);
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<bool> ExistsAsync(int offerId, int productId, CancellationToken cancellationToken)
        {
            return await _context.OfferProducts
                .AnyAsync(op => op.OfferId == offerId && op.ProductId == productId, cancellationToken);
        }
    }
}
