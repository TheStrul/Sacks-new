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
        Task<SupplierOfferAnnex?> GetOfferAsync(int id);

        /// <summary>
        /// Gets all offers for a specific supplier
        /// </summary>
        Task<IEnumerable<SupplierOfferAnnex>> GetOffersBySupplierAsync(int supplierId);


        /// <summary>
        /// Gets offers for a specific product
        /// </summary>
        Task<IEnumerable<SupplierOfferAnnex>> GetOffersByProductAsync(int productId);

        /// <summary>
        /// Creates a new supplier offer
        /// </summary>
        Task<SupplierOfferAnnex> CreateOfferAsync(SupplierOfferAnnex offer, string? createdBy = null);

        /// <summary>
        /// Updates an existing supplier offer
        /// </summary>
        Task<SupplierOfferAnnex> UpdateOfferAsync(SupplierOfferAnnex offer, string? modifiedBy = null);

        /// <summary>
        /// Creates a new offer from file processing context
        /// </summary>
        Task<SupplierOfferAnnex> CreateOfferFromFileAsync(int supplierId, string fileName, 
            DateTime processingDate, string? currency = null, string? offerType = "File Import", 
            string? createdBy = null);

        /// <summary>
        /// Creates a new offer from file processing context using a supplier entity
        /// This overload eliminates the need for database validation of supplier existence
        /// </summary>
        SupplierOfferAnnex CreateOfferFromFileAsync(SupplierEntity supplier, string fileName, 
            DateTime processingDate, string? currency = null, string? offerType = "File Import", 
            string? createdBy = null);

        /// <summary>
        /// Checks if an offer with the specified name already exists for the supplier
        /// </summary>
        Task<bool> OfferExistsAsync(int supplierId, string offerName, CancellationToken cancellationToken = default);


        /// <summary>
        /// Deletes an offer
        /// </summary>
        Task<bool> DeleteOfferAsync(int id);

        /// <summary>
        /// Gets offers with pagination
        /// </summary>
        Task<(IEnumerable<SupplierOfferAnnex> Offers, int TotalCount)> GetOffersAsync(
            int pageNumber = 1, int pageSize = 50);

        /// <summary>
        /// Searches offers by various criteria
        /// </summary>
        Task<IEnumerable<SupplierOfferAnnex>> SearchOffersAsync(string searchTerm);
    }
}
