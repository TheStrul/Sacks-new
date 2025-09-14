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
        Task<ProductOfferAnnex?> GetOfferProductAsync(int id);

        /// <summary>
        /// Gets all offer-product relationships for a specific offer
        /// </summary>
        Task<IEnumerable<ProductOfferAnnex>> GetOfferProductsByOfferAsync(int offerId);

        /// <summary>
        /// Gets all offer-product relationships for a specific product
        /// </summary>
        Task<IEnumerable<ProductOfferAnnex>> GetOfferProductsByProductAsync(int productId);

        /// <summary>
        /// Gets a specific offer-product relationship
        /// </summary>
        Task<ProductOfferAnnex?> GetOfferProductAsync(int offerId, int productId);

        /// <summary>
        /// Creates a new offer-product relationship
        /// </summary>
        Task<ProductOfferAnnex> CreateOfferProductAsync(ProductOfferAnnex offerProduct, string? createdBy = null);

        /// <summary>
        /// Updates an existing offer-product relationship
        /// </summary>
        Task<ProductOfferAnnex> UpdateOfferProductAsync(ProductOfferAnnex offerProduct, string? modifiedBy = null);

        /// <summary>
        /// Creates or updates an offer-product relationship
        /// </summary>
        Task<ProductOfferAnnex> CreateOrUpdateOfferProductAsync(int offerId, int productId, 
            Dictionary<string, object?> offerProperties, string? createdBy = null);

        /// <summary>
        /// Bulk creates offer-product relationships
        /// </summary>
        Task<IEnumerable<ProductOfferAnnex>> BulkCreateOfferProductsAsync(
            IEnumerable<ProductOfferAnnex> offerProducts, string? createdBy = null);

        /// <summary>
        /// Deletes an offer-product relationship
        /// </summary>
        Task<bool> DeleteOfferProductAsync(int id);

        /// <summary>
        /// Gets offer-products with pagination
        /// </summary>
        Task<(IEnumerable<ProductOfferAnnex> OfferProducts, int TotalCount)> GetOfferProductsAsync(
            int pageNumber = 1, int pageSize = 50);

    }
}
