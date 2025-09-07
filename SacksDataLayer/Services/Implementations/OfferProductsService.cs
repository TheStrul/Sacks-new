using SacksDataLayer.FileProcessing.Models;
using SacksDataLayer.Repositories.Interfaces;
using SacksDataLayer.Services.Interfaces;
using SacksDataLayer.Entities;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace SacksDataLayer.Services.Implementations
{
    /// <summary>
    /// Service implementation for managing offer-product relationships
    /// </summary>
    public class OfferProductsService : IOfferProductsService
    {
        private readonly ITransactionalOfferProductsRepository _offerProductsRepository;
        private readonly ITransactionalSupplierOffersRepository _offersRepository;
        private readonly ITransactionalProductsRepository _productsRepository;
        private readonly ILogger<OfferProductsService> _logger;

        public OfferProductsService(
            ITransactionalOfferProductsRepository offerProductsRepository,
            ITransactionalSupplierOffersRepository offersRepository,
            ITransactionalProductsRepository productsRepository,
            ILogger<OfferProductsService> logger)
        {
            _offerProductsRepository = offerProductsRepository ?? throw new ArgumentNullException(nameof(offerProductsRepository));
            _offersRepository = offersRepository ?? throw new ArgumentNullException(nameof(offersRepository));
            _productsRepository = productsRepository ?? throw new ArgumentNullException(nameof(productsRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<OfferProductAnnex?> GetOfferProductAsync(int id)
        {
            return await _offerProductsRepository.GetByIdAsync(id, CancellationToken.None);
        }

        public async Task<IEnumerable<OfferProductAnnex>> GetOfferProductsByOfferAsync(int offerId)
        {
            return await _offerProductsRepository.GetByOfferIdAsync(offerId, CancellationToken.None);
        }

        public async Task<IEnumerable<OfferProductAnnex>> GetOfferProductsByProductAsync(int productId)
        {
            return await _offerProductsRepository.GetByProductIdAsync(productId, CancellationToken.None);
        }

        public async Task<OfferProductAnnex?> GetOfferProductAsync(int offerId, int productId)
        {
            return await _offerProductsRepository.GetByOfferAndProductAsync(offerId, productId, CancellationToken.None);
        }

        public async Task<OfferProductAnnex> CreateOfferProductAsync(OfferProductAnnex offerProduct, string? createdBy = null)
        {
            // Validate the offer-product relationship
            await ValidateOfferProductAsync(offerProduct);

            // Verify offer not allready exists
            var offer = await _offersRepository.GetByIdAsync(offerProduct.OfferId, CancellationToken.None);
            if (offer == null)
                throw new InvalidOperationException($"Offer with ID {offerProduct.OfferId} not found");

            // Verify product exists
            var product = await _productsRepository.GetByIdAsync(offerProduct.ProductId, CancellationToken.None);
            if (product == null)
                throw new InvalidOperationException($"Product with ID {offerProduct.ProductId} not found");

            _offerProductsRepository.Add(offerProduct);

            return offerProduct;
        }

        public async Task<OfferProductAnnex> UpdateOfferProductAsync(OfferProductAnnex offerProduct, string? modifiedBy = null)
        {
            // Validate the offer-product relationship
            await ValidateOfferProductAsync(offerProduct);

            // Check if offer-product exists
            var existingOfferProduct = await _offerProductsRepository.GetByIdAsync(offerProduct.Id, CancellationToken.None);
            if (existingOfferProduct == null)
                throw new InvalidOperationException($"Offer-product with ID {offerProduct.Id} not found");

            _offerProductsRepository.Update(offerProduct);

            return offerProduct;
        }

        public async Task<OfferProductAnnex> CreateOrUpdateOfferProductAsync(int offerId, int productId,
            Dictionary<string, object?> offerProperties, string? createdBy = null)
        {
            // Try to find existing offer-product relationship
            var existingOfferProduct = await _offerProductsRepository.GetByOfferAndProductAsync(offerId, productId, CancellationToken.None);

            var offerPropertiesJson = JsonSerializer.Serialize(offerProperties);

            if (existingOfferProduct != null)
            {
                // Update existing relationship
                existingOfferProduct.OfferPropertiesJson = offerPropertiesJson;
                return await UpdateOfferProductAsync(existingOfferProduct, createdBy);
            }
            else
            {
                // Create new relationship
                var newOfferProduct = new OfferProductAnnex
                {
                    OfferId = offerId,
                    ProductId = productId,
                    OfferPropertiesJson = offerPropertiesJson
                };

                return await CreateOfferProductAsync(newOfferProduct, createdBy);
            }
        }

        public async Task<IEnumerable<OfferProductAnnex>> BulkCreateOfferProductsAsync(
            IEnumerable<OfferProductAnnex> offerProducts, string? createdBy = null)
        {
            ArgumentNullException.ThrowIfNull(offerProducts);
            
            var results = new List<OfferProductAnnex>();

            foreach (var offerProduct in offerProducts)
            {
                try
                {
                    var result = await CreateOfferProductAsync(offerProduct, createdBy);
                    results.Add(result);
                }
                catch (Exception ex)
                {
                    // Log error but continue with other offer-products
                    _logger.LogError(ex, "Error processing offer-product {OfferProductId}: {ErrorMessage}", 
                        offerProduct.Id, ex.Message);
                    throw; // Re-throw for now, but could be changed to continue processing
                }
            }

            return results;
        }

        public async Task<bool> DeleteOfferProductAsync(int id)
        {
            try
            {
                await _offerProductsRepository.RemoveByIdAsync(id);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public Task<(IEnumerable<OfferProductAnnex> OfferProducts, int TotalCount)> GetOfferProductsAsync(
            int pageNumber = 1, int pageSize = 50)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 50;
            if (pageSize > 1000) pageSize = 1000; // Limit max page size

            // Note: This is a simplified implementation
            // Since we don't have GetAllAsync, we can't implement proper pagination
            // In a real scenario, you'd add GetAllAsync to the offerProductsRepository interface
            return Task.FromResult<(IEnumerable<OfferProductAnnex>, int)>((Enumerable.Empty<OfferProductAnnex>(), 0));
        }

        public async Task<IEnumerable<OfferProductAnnex>> SearchOfferProductsAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return Enumerable.Empty<OfferProductAnnex>();

            // Note: This is a simplified implementation
            // Since we don't have a comprehensive search method, this is a placeholder
            // In a real scenario, you'd implement search in the offerProductsRepository
            await Task.CompletedTask;
            return Enumerable.Empty<OfferProductAnnex>();
        }

        private async Task ValidateOfferProductAsync(OfferProductAnnex offerProduct)
        {
            if (offerProduct == null)
                throw new ArgumentNullException(nameof(offerProduct));

            if (offerProduct.OfferId <= 0)
                throw new ArgumentException("Valid offer ID is required", nameof(offerProduct));

            if (offerProduct.ProductId <= 0)
                throw new ArgumentException("Valid product ID is required", nameof(offerProduct));

            if (offerProduct.Price <= 0)
                throw new ArgumentException("Price must be greater than zero", nameof(offerProduct));

            if (offerProduct.Quantity <= 0)
                throw new ArgumentException("Quantity must be greater than zero", nameof(offerProduct));

            // Additional business validation can be added here
            await Task.CompletedTask; // Placeholder for async validation
        }
    }
}
