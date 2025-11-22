namespace Sacks.Core.Services.Interfaces
{
    /// <summary>
    /// Service for managing offers (CRUD operations)
    /// </summary>
    public interface IOffersService
    {
        /// <summary>
        /// Gets all offers for a specific supplier
        /// </summary>
        Task<IEnumerable<OfferDto>> GetOffersBySupplierIdAsync(int supplierId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a single offer by ID
        /// </summary>
        Task<OfferDto?> GetOfferByIdAsync(int offerId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a new offer
        /// </summary>
        Task<OfferDto> CreateOfferAsync(CreateOfferRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates an existing offer
        /// </summary>
        Task<OfferDto> UpdateOfferAsync(int offerId, UpdateOfferRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes an offer
        /// </summary>
        Task<bool> DeleteOfferAsync(int offerId, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// DTO for offer data
    /// </summary>
    public class OfferDto
    {
        public int Id { get; set; }
        public int SupplierId { get; set; }
        public required string OfferName { get; set; }
        public required string Currency { get; set; }
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ModifiedAt { get; set; }
    }

    /// <summary>
    /// Request for creating an offer
    /// </summary>
    public class CreateOfferRequest
    {
        public required int SupplierId { get; set; }
        public required string OfferName { get; set; }
        public required string Currency { get; set; }
        public string? Description { get; set; }
    }

    /// <summary>
    /// Request for updating an offer
    /// </summary>
    public class UpdateOfferRequest
    {
        public string? Description { get; set; }
    }
}
