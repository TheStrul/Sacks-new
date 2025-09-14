namespace SacksDataLayer.Services.Interfaces
{
    using SacksDataLayer.Entities;

    /// <summary>
    /// Service interface for managing offer-product relationships
    /// </summary>
    public interface IOfferProductsService
    {
        /// <summary>
        /// Gets an offer-product relationship by ID
        /// </summary>
        Task<OfferProductAnnex?> GetOfferProductAsync(int id);

        /// <summary>
        /// Gets all offer-product relationships for a specific offer
        /// </summary>
        Task<IEnumerable<OfferProductAnnex>> GetOfferProductsByOfferAsync(int offerId);

        /// <summary>
        /// Gets all offer-product relationships for a specific product
        /// </summary>
        Task<IEnumerable<OfferProductAnnex>> GetOfferProductsByProductAsync(int productId);

        /// <summary>
        /// Gets a specific offer-product relationship
        /// </summary>
        Task<OfferProductAnnex?> GetOfferProductAsync(int offerId, int productId);

        /// <summary>
        /// Creates a new offer-product relationship
        /// </summary>
        Task<OfferProductAnnex> CreateOfferProductAsync(OfferProductAnnex offerProduct, string? createdBy = null);

        /// <summary>
        /// Updates an existing offer-product relationship
        /// </summary>
        Task<OfferProductAnnex> UpdateOfferProductAsync(OfferProductAnnex offerProduct, string? modifiedBy = null);

        /// <summary>
        /// Creates or updates an offer-product relationship
        /// </summary>
        Task<OfferProductAnnex> CreateOrUpdateOfferProductAsync(int offerId, int productId, 
            Dictionary<string, object?> offerProperties, string? createdBy = null);

        /// <summary>
        /// Bulk creates offer-product relationships
        /// </summary>
        Task<IEnumerable<OfferProductAnnex>> BulkCreateOfferProductsAsync(
            IEnumerable<OfferProductAnnex> offerProducts, string? createdBy = null);

        /// <summary>
        /// Deletes an offer-product relationship
        /// </summary>
        Task<bool> DeleteOfferProductAsync(int id);

        /// <summary>
        /// Gets offer-products with pagination
        /// </summary>
        Task<(IEnumerable<OfferProductAnnex> OfferProducts, int TotalCount)> GetOfferProductsAsync(
            int pageNumber = 1, int pageSize = 50);

    }
}
