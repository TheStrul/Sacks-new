using SacksDataLayer.FileProcessing.Models;

namespace SacksDataLayer.Services.Interfaces
{
    /// <summary>
    /// Service interface for managing offer-product relationships
    /// </summary>
    public interface IOfferProductsService
    {
        /// <summary>
        /// Gets an offer-product relationship by ID
        /// </summary>
        Task<OfferProductEntity?> GetOfferProductAsync(int id);

        /// <summary>
        /// Gets all offer-product relationships for a specific offer
        /// </summary>
        Task<IEnumerable<OfferProductEntity>> GetOfferProductsByOfferAsync(int offerId);

        /// <summary>
        /// Gets all offer-product relationships for a specific product
        /// </summary>
        Task<IEnumerable<OfferProductEntity>> GetOfferProductsByProductAsync(int productId);

        /// <summary>
        /// Gets a specific offer-product relationship
        /// </summary>
        Task<OfferProductEntity?> GetOfferProductAsync(int offerId, int productId);

        /// <summary>
        /// Creates a new offer-product relationship
        /// </summary>
        Task<OfferProductEntity> CreateOfferProductAsync(OfferProductEntity offerProduct, string? createdBy = null);

        /// <summary>
        /// Updates an existing offer-product relationship
        /// </summary>
        Task<OfferProductEntity> UpdateOfferProductAsync(OfferProductEntity offerProduct, string? modifiedBy = null);

        /// <summary>
        /// Creates or updates an offer-product relationship
        /// </summary>
        Task<OfferProductEntity> CreateOrUpdateOfferProductAsync(int offerId, int productId, 
            Dictionary<string, object?> offerProperties, string? createdBy = null);

        /// <summary>
        /// Bulk creates offer-product relationships
        /// </summary>
        Task<IEnumerable<OfferProductEntity>> BulkCreateOfferProductsAsync(
            IEnumerable<OfferProductEntity> offerProducts, string? createdBy = null);

        /// <summary>
        /// Deletes an offer-product relationship
        /// </summary>
        Task<bool> DeleteOfferProductAsync(int id);

        /// <summary>
        /// Gets offer-products with pagination
        /// </summary>
        Task<(IEnumerable<OfferProductEntity> OfferProducts, int TotalCount)> GetOfferProductsAsync(
            int pageNumber = 1, int pageSize = 50);

        /// <summary>
        /// Marks an offer-product as unavailable
        /// </summary>
        Task<bool> SetAvailabilityAsync(int id, bool isAvailable, string? modifiedBy = null);
    }
}
