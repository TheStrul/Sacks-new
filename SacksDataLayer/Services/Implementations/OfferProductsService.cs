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
        private readonly IOfferProductsRepository _repository;
        private readonly ISupplierOffersRepository _offersRepository;
        private readonly IProductsRepository _productsRepository;
        private readonly ILogger<OfferProductsService> _logger;

        public OfferProductsService(
            IOfferProductsRepository repository,
            ISupplierOffersRepository offersRepository,
            IProductsRepository productsRepository,
            ILogger<OfferProductsService> logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _offersRepository = offersRepository ?? throw new ArgumentNullException(nameof(offersRepository));
            _productsRepository = productsRepository ?? throw new ArgumentNullException(nameof(productsRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<OfferProductEntity?> GetOfferProductAsync(int id)
        {
            return await _repository.GetByIdAsync(id, CancellationToken.None);
        }

        public async Task<IEnumerable<OfferProductEntity>> GetOfferProductsByOfferAsync(int offerId)
        {
            return await _repository.GetByOfferIdAsync(offerId, CancellationToken.None);
        }

        public async Task<IEnumerable<OfferProductEntity>> GetOfferProductsByProductAsync(int productId)
        {
            return await _repository.GetByProductIdAsync(productId, CancellationToken.None);
        }

        public async Task<OfferProductEntity?> GetOfferProductAsync(int offerId, int productId)
        {
            return await _repository.GetByOfferAndProductAsync(offerId, productId, CancellationToken.None);
        }

        public async Task<OfferProductEntity> CreateOfferProductAsync(OfferProductEntity offerProduct, string? createdBy = null)
        {
            // Validate the offer-product relationship
            await ValidateOfferProductAsync(offerProduct);

            // Verify offer exists
            var offer = await _offersRepository.GetByIdAsync(offerProduct.OfferId, CancellationToken.None);
            if (offer == null)
                throw new InvalidOperationException($"Offer with ID {offerProduct.OfferId} not found");

            // Verify product exists
            var product = await _productsRepository.GetByIdAsync(offerProduct.ProductId, CancellationToken.None);
            if (product == null)
                throw new InvalidOperationException($"Product with ID {offerProduct.ProductId} not found");

            return await _repository.CreateAsync(offerProduct, CancellationToken.None);
        }

        public async Task<OfferProductEntity> UpdateOfferProductAsync(OfferProductEntity offerProduct, string? modifiedBy = null)
        {
            // Validate the offer-product relationship
            await ValidateOfferProductAsync(offerProduct);

            // Check if offer-product exists
            var existingOfferProduct = await _repository.GetByIdAsync(offerProduct.Id, CancellationToken.None);
            if (existingOfferProduct == null)
                throw new InvalidOperationException($"Offer-product with ID {offerProduct.Id} not found");

            return await _repository.UpdateAsync(offerProduct, CancellationToken.None);
        }

        public async Task<OfferProductEntity> CreateOrUpdateOfferProductAsync(int offerId, int productId,
            Dictionary<string, object?> offerProperties, string? createdBy = null)
        {
            // Try to find existing offer-product relationship
            var existingOfferProduct = await _repository.GetByOfferAndProductAsync(offerId, productId, CancellationToken.None);

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
                var newOfferProduct = new OfferProductEntity
                {
                    OfferId = offerId,
                    ProductId = productId,
                    OfferPropertiesJson = offerPropertiesJson
                };

                return await CreateOfferProductAsync(newOfferProduct, createdBy);
            }
        }

        public async Task<IEnumerable<OfferProductEntity>> BulkCreateOfferProductsAsync(
            IEnumerable<OfferProductEntity> offerProducts, string? createdBy = null)
        {
            ArgumentNullException.ThrowIfNull(offerProducts);
            
            var results = new List<OfferProductEntity>();

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
                await _repository.DeleteAsync(id, CancellationToken.None);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public Task<(IEnumerable<OfferProductEntity> OfferProducts, int TotalCount)> GetOfferProductsAsync(
            int pageNumber = 1, int pageSize = 50)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 50;
            if (pageSize > 1000) pageSize = 1000; // Limit max page size

            // Note: This is a simplified implementation
            // Since we don't have GetAllAsync, we can't implement proper pagination
            // In a real scenario, you'd add GetAllAsync to the repository interface
            return Task.FromResult<(IEnumerable<OfferProductEntity>, int)>((Enumerable.Empty<OfferProductEntity>(), 0));
        }

        public async Task<IEnumerable<OfferProductEntity>> SearchOfferProductsAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return Enumerable.Empty<OfferProductEntity>();

            // Note: This is a simplified implementation
            // Since we don't have a comprehensive search method, this is a placeholder
            // In a real scenario, you'd implement search in the repository
            await Task.CompletedTask;
            return Enumerable.Empty<OfferProductEntity>();
        }

        private async Task ValidateOfferProductAsync(OfferProductEntity offerProduct)
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
