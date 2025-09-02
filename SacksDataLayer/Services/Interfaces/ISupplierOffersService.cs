using SacksDataLayer.FileProcessing.Models;
using SacksDataLayer.Entities;

namespace SacksDataLayer.Services.Interfaces
{
    /// <summary>
    /// Service interface for managing supplier offers
    /// </summary>
    public interface ISupplierOffersService
    {
        /// <summary>
        /// Gets a supplier offer by ID
        /// </summary>
        Task<SupplierOfferEntity?> GetOfferAsync(int id);

        /// <summary>
        /// Gets all offers for a specific supplier
        /// </summary>
        Task<IEnumerable<SupplierOfferEntity>> GetOffersBySupplierAsync(int supplierId);

        /// <summary>
        /// Gets all active offers for a specific supplier
        /// </summary>
        Task<IEnumerable<SupplierOfferEntity>> GetActiveOffersBySupplierAsync(int supplierId);

        /// <summary>
        /// Gets offers for a specific product
        /// </summary>
        Task<IEnumerable<SupplierOfferEntity>> GetOffersByProductAsync(int productId);

        /// <summary>
        /// Creates a new supplier offer
        /// </summary>
        Task<SupplierOfferEntity> CreateOfferAsync(SupplierOfferEntity offer, string? createdBy = null);

        /// <summary>
        /// Updates an existing supplier offer
        /// </summary>
        Task<SupplierOfferEntity> UpdateOfferAsync(SupplierOfferEntity offer, string? modifiedBy = null);

        /// <summary>
        /// Creates a new offer from file processing context
        /// </summary>
        Task<SupplierOfferEntity> CreateOfferFromFileAsync(int supplierId, string fileName, 
            DateTime processingDate, string? currency = null, string? offerType = "File Import", 
            string? createdBy = null);

        /// <summary>
        /// Checks if an offer with the specified name already exists for the supplier
        /// </summary>
        Task<bool> OfferExistsAsync(int supplierId, string offerName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deactivates an offer (sets IsActive = false)
        /// </summary>
        Task<bool> DeactivateOfferAsync(int offerId, string? modifiedBy = null);

        /// <summary>
        /// Deletes an offer
        /// </summary>
        Task<bool> DeleteOfferAsync(int id);

        /// <summary>
        /// Gets offers with pagination
        /// </summary>
        Task<(IEnumerable<SupplierOfferEntity> Offers, int TotalCount)> GetOffersAsync(
            int pageNumber = 1, int pageSize = 50);

        /// <summary>
        /// Searches offers by various criteria
        /// </summary>
        Task<IEnumerable<SupplierOfferEntity>> SearchOffersAsync(string searchTerm);
    }
}
