#nullable enable
using SacksDataLayer.Entities;
using System.Threading;

namespace SacksDataLayer.Repositories.Interfaces
{
    /// <summary>
    /// Repository interface for managing offer-product relationships
    /// </summary>
    public interface IOfferProductsRepository
    {
        /// <summary>
        /// Gets an offer-product relationship by ID
        /// </summary>
        Task<OfferProductEntity?> GetByIdAsync(int id, CancellationToken cancellationToken);

        /// <summary>
        /// Gets all offer-product relationships for a specific offer
        /// </summary>
        Task<IEnumerable<OfferProductEntity>> GetByOfferIdAsync(int offerId, CancellationToken cancellationToken);

        /// <summary>
        /// Gets all offer-product relationships for a specific product
        /// </summary>
        Task<IEnumerable<OfferProductEntity>> GetByProductIdAsync(int productId, CancellationToken cancellationToken);

        /// <summary>
        /// Gets a specific offer-product relationship
        /// </summary>
        Task<OfferProductEntity?> GetByOfferAndProductAsync(int offerId, int productId, CancellationToken cancellationToken);

        /// <summary>
        /// Gets offer-products with pagination
        /// </summary>
        Task<IEnumerable<OfferProductEntity>> GetPagedAsync(int skip, int take, CancellationToken cancellationToken);

        /// <summary>
        /// Gets total count of offer-products
        /// </summary>
        Task<int> GetCountAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Creates a new offer-product relationship
        /// </summary>
        Task<OfferProductEntity> CreateAsync(OfferProductEntity offerProduct, CancellationToken cancellationToken);

        /// <summary>
        /// Updates an existing offer-product relationship
        /// </summary>
        Task<OfferProductEntity> UpdateAsync(OfferProductEntity offerProduct, CancellationToken cancellationToken);

        /// <summary>
        /// Bulk creates offer-product relationships
        /// </summary>
        Task<IEnumerable<OfferProductEntity>> BulkCreateAsync(IEnumerable<OfferProductEntity> offerProducts, CancellationToken cancellationToken);

        /// <summary>
        /// Deletes an offer-product relationship
        /// </summary>
        Task<bool> DeleteAsync(int id, CancellationToken cancellationToken);

        /// <summary>
        /// Checks if an offer-product relationship exists
        /// </summary>
        Task<bool> ExistsAsync(int offerId, int productId, CancellationToken cancellationToken);
    }
}
