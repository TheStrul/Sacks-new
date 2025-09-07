using SacksDataLayer.FileProcessing.Models;
using SacksDataLayer.Repositories.Interfaces;
using SacksDataLayer.Services.Interfaces;
using SacksDataLayer.Entities;

namespace SacksDataLayer.Services.Implementations
{
    /// <summary>
    /// Service implementation for managing supplier offers
    /// </summary>
    public class SupplierOffersService : ISupplierOffersService
    {
        private readonly ITransactionalSupplierOffersRepository _offerRepository;
        private readonly ITransactionalSuppliersRepository _suppliersRepository;

        public SupplierOffersService(ITransactionalSupplierOffersRepository offerRepository, ITransactionalSuppliersRepository suppliersRepository)
        {
            _offerRepository = offerRepository ?? throw new ArgumentNullException(nameof(offerRepository));
            _suppliersRepository = suppliersRepository ?? throw new ArgumentNullException(nameof(suppliersRepository));
        }

        public async Task<SupplierOfferAnnex?> GetOfferAsync(int id)
        {
            return await _offerRepository.GetByIdAsync(id, CancellationToken.None);
        }

        public async Task<IEnumerable<SupplierOfferAnnex>> GetOffersBySupplierAsync(int supplierId)
        {
            return await _offerRepository.GetBySupplierIdAsync(supplierId, CancellationToken.None);
        }

        public async Task<IEnumerable<SupplierOfferAnnex>> GetOffersByProductAsync(int productId)
        {
            return await _offerRepository.GetByProductIdAsync(productId, CancellationToken.None);
        }

        public async Task<SupplierOfferAnnex> CreateOfferAsync(SupplierOfferAnnex offer, string? createdBy = null)
        {
            // Validate offer
            await ValidateOfferAsync(offer);

            // Verify supplier exists
            var supplier = await _suppliersRepository.GetByIdAsync(offer.SupplierId, CancellationToken.None);
            if (supplier == null)
                throw new InvalidOperationException($"Supplier with ID {offer.SupplierId} not found");

            offer.CreatedAt = DateTime.UtcNow;
            _offerRepository.Add(offer);
            return offer;
        }

        public async Task<SupplierOfferAnnex> UpdateOfferAsync(SupplierOfferAnnex offer, string? modifiedBy = null)
        {
            // Validate offer
            await ValidateOfferAsync(offer);

            // Check if offer exists
            var existingOffer = await _offerRepository.GetByIdAsync(offer.Id, CancellationToken.None);
            if (existingOffer == null)
                throw new InvalidOperationException($"Offer with ID {offer.Id} not found");

            offer.ModifiedAt = DateTime.UtcNow;
            _offerRepository.Update(offer);
            return offer;
        }

        public async Task<SupplierOfferAnnex> CreateOfferFromFileAsync(int supplierId, string fileName, 
            DateTime processingDate, string? currency = null, string? offerType = "File Import", 
            string? createdBy = null)
        {
            // Verify supplier exists
            var supplier = await _suppliersRepository.GetByIdAsync(supplierId, CancellationToken.None);
            if (supplier == null)
                throw new InvalidOperationException($"Supplier with ID {supplierId} not found");

            var offer = new SupplierOfferAnnex
            {
                SupplierId = supplierId,
                OfferName = $"{supplier.Name} - {fileName}",
                Description = $"Automatic import from file: {fileName}",
                Currency = currency ?? "USD",
            };

            _offerRepository.Add(offer);

            return offer;
        }

        public async Task<bool> OfferExistsAsync(int supplierId, string offerName, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(offerName))
                return false;
            var offers = await _offerRepository.GetBySupplierIdAsync(supplierId, cancellationToken);
            if (offers == null)
            {
                return false;
            }
            else
                            {
                return offers.Any(o => o.OfferName!.Equals(offerName, StringComparison.OrdinalIgnoreCase));
            }
        }

        public async Task<bool> DeleteOfferAsync(int id)
        {
            try
            {
                await _offerRepository.RemoveByIdAsync(id);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public Task<(IEnumerable<SupplierOfferAnnex> Offers, int TotalCount)> GetOffersAsync(
            int pageNumber = 1, int pageSize = 50)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 50;
            if (pageSize > 1000) pageSize = 1000; // Limit max page size

            // Note: This is a simplified implementation
            // Since we don't have GetAllAsync, we can't implement proper pagination
            // In a real scenario, you'd add GetAllAsync to the offerRepository interface
            return Task.FromResult<(IEnumerable<SupplierOfferAnnex>, int)>((Enumerable.Empty<SupplierOfferAnnex>(), 0));
        }

        public async Task<IEnumerable<SupplierOfferAnnex>> SearchOffersAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return Enumerable.Empty<SupplierOfferAnnex>();

            // Note: This is a simplified implementation
            // Since we don't have a comprehensive search method, this is a placeholder
            // In a real scenario, you'd implement search in the offerRepository
            await Task.CompletedTask;
            return Enumerable.Empty<SupplierOfferAnnex>();
        }

        private async Task ValidateOfferAsync(SupplierOfferAnnex offer)
        {
            if (offer == null)
                throw new ArgumentNullException(nameof(offer));

            if (offer.SupplierId <= 0)
                throw new ArgumentException("Valid supplier ID is required", nameof(offer));

            if (!string.IsNullOrEmpty(offer.OfferName) && offer.OfferName.Length > 255)
                throw new ArgumentException("Offer name cannot exceed 255 characters", nameof(offer));

            if (!string.IsNullOrEmpty(offer.Description) && offer.Description.Length > 500)
                throw new ArgumentException("Offer description cannot exceed 500 characters", nameof(offer));

            if (!string.IsNullOrEmpty(offer.Currency) && offer.Currency.Length > 20)
                throw new ArgumentException("Currency cannot exceed 20 characters", nameof(offer));


            await Task.CompletedTask; // For async consistency
        }
    }
}
