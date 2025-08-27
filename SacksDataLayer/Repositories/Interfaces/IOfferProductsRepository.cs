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
        Task<OfferProductEntity?> GetByIdAsync(int id);

        /// <summary>
        /// Gets all offer-product relationships for a specific offer
        /// </summary>
        Task<IEnumerable<OfferProductEntity>> GetByOfferIdAsync(int offerId);

        /// <summary>
        /// Gets all offer-product relationships for a specific product
        /// </summary>
        Task<IEnumerable<OfferProductEntity>> GetByProductIdAsync(int productId);

        /// <summary>
        /// Gets a specific offer-product relationship
        /// </summary>
        Task<OfferProductEntity?> GetByOfferAndProductAsync(int offerId, int productId);

        /// <summary>
        /// Gets offer-products with pagination
        /// </summary>
        Task<IEnumerable<OfferProductEntity>> GetPagedAsync(int skip, int take);

        /// <summary>
        /// Gets total count of offer-products
        /// </summary>
        Task<int> GetCountAsync();

        /// <summary>
        /// Creates a new offer-product relationship
        /// </summary>
        Task<OfferProductEntity> CreateAsync(OfferProductEntity offerProduct);

        /// <summary>
        /// Updates an existing offer-product relationship
        /// </summary>
        Task<OfferProductEntity> UpdateAsync(OfferProductEntity offerProduct);

        /// <summary>
        /// Bulk creates offer-product relationships
        /// </summary>
        Task<IEnumerable<OfferProductEntity>> BulkCreateAsync(IEnumerable<OfferProductEntity> offerProducts);

        /// <summary>
        /// Deletes an offer-product relationship
        /// </summary>
        Task<bool> DeleteAsync(int id);

        /// <summary>
        /// Checks if an offer-product relationship exists
        /// </summary>
        Task<bool> ExistsAsync(int offerId, int productId);
    }
}
