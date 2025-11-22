namespace Sacks.Core.Services.Interfaces
{
    using Sacks.Core.Entities;

    /// <summary>
    /// Service interface for managing supplier offers
    /// </summary>
    public interface ISupplierOffersService
    {
        /// <summary>
        /// Gets a supplier offer by ID
        /// </summary>
        Task<Offer?> GetOfferAsync(int id);

        /// <summary>
        /// Gets all offers for a specific supplier
        /// </summary>
        Task<IEnumerable<Offer>> GetOffersBySupplierAsync(int supplierId);


        /// <summary>
        /// Gets offers for a specific product
        /// </summary>
        Task<IEnumerable<Offer>> GetOffersByProductAsync(int productId);

        /// <summary>
        /// Creates a new supplier offer
        /// </summary>
        Task<Offer> CreateOfferAsync(Offer offer, string? createdBy = null);

        /// <summary>
        /// Updates an existing supplier offer
        /// </summary>
        Task<Offer> UpdateOfferAsync(Offer offer, string? modifiedBy = null);



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
        Task<(IEnumerable<Offer> Offers, int TotalCount)> GetOffersAsync(
            int pageNumber = 1, int pageSize = 50);

        /// <summary>
        /// Searches offers by various criteria
        /// </summary>
        Task<IEnumerable<Offer>> SearchOffersAsync(string searchTerm);
    }
}
