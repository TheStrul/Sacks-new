﻿using SacksDataLayer.FileProcessing.Models;
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
        private readonly ISupplierOffersRepository _repository;
        private readonly ISuppliersRepository _suppliersRepository;

        public SupplierOffersService(ISupplierOffersRepository repository, ISuppliersRepository suppliersRepository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _suppliersRepository = suppliersRepository ?? throw new ArgumentNullException(nameof(suppliersRepository));
        }

        public async Task<SupplierOfferEntity?> GetOfferAsync(int id)
        {
            return await _repository.GetByIdAsync(id, CancellationToken.None);
        }

        public async Task<IEnumerable<SupplierOfferEntity>> GetOffersBySupplierAsync(int supplierId)
        {
            return await _repository.GetBySupplierIdAsync(supplierId, CancellationToken.None);
        }

        public async Task<IEnumerable<SupplierOfferEntity>> GetOffersByProductAsync(int productId)
        {
            return await _repository.GetByProductIdAsync(productId, CancellationToken.None);
        }

        public async Task<SupplierOfferEntity> CreateOfferAsync(SupplierOfferEntity offer, string? createdBy = null)
        {
            // Validate offer
            await ValidateOfferAsync(offer);

            // Verify supplier exists
            var supplier = await _suppliersRepository.GetByIdAsync(offer.SupplierId, CancellationToken.None);
            if (supplier == null)
                throw new InvalidOperationException($"Supplier with ID {offer.SupplierId} not found");

            return await _repository.CreateAsync(offer, CancellationToken.None);
        }

        public async Task<SupplierOfferEntity> UpdateOfferAsync(SupplierOfferEntity offer, string? modifiedBy = null)
        {
            // Validate offer
            await ValidateOfferAsync(offer);

            // Check if offer exists
            var existingOffer = await _repository.GetByIdAsync(offer.Id, CancellationToken.None);
            if (existingOffer == null)
                throw new InvalidOperationException($"Offer with ID {offer.Id} not found");

            return await _repository.UpdateAsync(offer, CancellationToken.None);
        }

        public async Task<SupplierOfferEntity> CreateOfferFromFileAsync(int supplierId, string fileName, 
            DateTime processingDate, string? currency = null, string? offerType = "File Import", 
            string? createdBy = null)
        {
            // Verify supplier exists
            var supplier = await _suppliersRepository.GetByIdAsync(supplierId, CancellationToken.None);
            if (supplier == null)
                throw new InvalidOperationException($"Supplier with ID {supplierId} not found");

            var offer = new SupplierOfferEntity
            {
                SupplierId = supplierId,
                OfferName = $"{supplier.Name} - {fileName}",
                Description = $"Automatic import from file: {fileName}",
                Currency = currency ?? "USD",
            };

            return await _repository.CreateAsync(offer, CancellationToken.None);
        }

        public async Task<bool> OfferExistsAsync(int supplierId, string offerName, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(offerName))
                return false;

            var existingOffer = await _repository.GetBySupplierAndOfferNameAsync(supplierId, offerName, cancellationToken);
            return existingOffer != null;
        }

        public async Task<bool> DeleteOfferAsync(int id)
        {
            try
            {
                await _repository.DeleteAsync(id, CancellationToken.None);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public Task<(IEnumerable<SupplierOfferEntity> Offers, int TotalCount)> GetOffersAsync(
            int pageNumber = 1, int pageSize = 50)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 50;
            if (pageSize > 1000) pageSize = 1000; // Limit max page size

            // Note: This is a simplified implementation
            // Since we don't have GetAllAsync, we can't implement proper pagination
            // In a real scenario, you'd add GetAllAsync to the repository interface
            return Task.FromResult<(IEnumerable<SupplierOfferEntity>, int)>((Enumerable.Empty<SupplierOfferEntity>(), 0));
        }

        public async Task<IEnumerable<SupplierOfferEntity>> SearchOffersAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return Enumerable.Empty<SupplierOfferEntity>();

            // Note: This is a simplified implementation
            // Since we don't have a comprehensive search method, this is a placeholder
            // In a real scenario, you'd implement search in the repository
            await Task.CompletedTask;
            return Enumerable.Empty<SupplierOfferEntity>();
        }

        private async Task ValidateOfferAsync(SupplierOfferEntity offer)
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
