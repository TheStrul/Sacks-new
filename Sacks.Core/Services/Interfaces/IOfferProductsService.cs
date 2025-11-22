namespace Sacks.Core.Services.Interfaces
{
    using Sacks.Core.Entities;

    /// <summary>
    /// Service interface for managing offer-product relationships
    /// </summary>
    public interface IOfferProductsService
    {
        /// <summary>
        /// Gets an offer-product relationship by ID
        /// </summary>
        Task<ProductOffer?> GetOfferProductAsync(int id);

        /// <summary>
        /// Gets all offer-product relationships for a specific offer
        /// </summary>
        Task<IEnumerable<ProductOffer>> GetOfferProductsByOfferAsync(int offerId);

        /// <summary>
        /// Gets all offer-product relationships for a specific product
        /// </summary>
        Task<IEnumerable<ProductOffer>> GetOfferProductsByProductAsync(int productId);

        /// <summary>
        /// Gets a specific offer-product relationship
        /// </summary>
        Task<ProductOffer?> GetOfferProductAsync(int offerId, int productId);

        /// <summary>
        /// Creates a new offer-product relationship
        /// </summary>
        Task<ProductOffer> CreateOfferProductAsync(ProductOffer offerProduct, string? createdBy = null);

        /// <summary>
        /// Updates an existing offer-product relationship
        /// </summary>
        Task<ProductOffer> UpdateOfferProductAsync(ProductOffer offerProduct, string? modifiedBy = null);

        /// <summary>
        /// Creates or updates an offer-product relationship
        /// </summary>
        Task<ProductOffer> CreateOrUpdateOfferProductAsync(int offerId, int productId, 
            Dictionary<string, object?> offerProperties, string? createdBy = null);

        /// <summary>
        /// Bulk creates offer-product relationships
        /// </summary>
        Task<IEnumerable<ProductOffer>> BulkCreateOfferProductsAsync(
            IEnumerable<ProductOffer> offerProducts, string? createdBy = null);

        /// <summary>
        /// Deletes an offer-product relationship
        /// </summary>
        Task<bool> DeleteOfferProductAsync(int id);

        /// <summary>
        /// Gets offer-products with pagination
        /// </summary>
        Task<(IEnumerable<ProductOffer> OfferProducts, int TotalCount)> GetOfferProductsAsync(
            int pageNumber = 1, int pageSize = 50);

    }
}
