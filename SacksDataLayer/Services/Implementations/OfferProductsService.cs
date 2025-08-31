using SacksDataLayer.FileProcessing.Models;
using SacksDataLayer.Repositories.Interfaces;
using SacksDataLayer.Services.Interfaces;
using System.Text.Json;

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

        public OfferProductsService(
            IOfferProductsRepository repository,
            ISupplierOffersRepository offersRepository,
            IProductsRepository productsRepository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _offersRepository = offersRepository ?? throw new ArgumentNullException(nameof(offersRepository));
            _productsRepository = productsRepository ?? throw new ArgumentNullException(nameof(productsRepository));
        }

        public async Task<OfferProductEntity?> GetOfferProductAsync(int id)
        {
            return await _repository.GetByIdAsync(id);
        }

        public async Task<IEnumerable<OfferProductEntity>> GetOfferProductsByOfferAsync(int offerId)
        {
            return await _repository.GetByOfferIdAsync(offerId);
        }

        public async Task<IEnumerable<OfferProductEntity>> GetOfferProductsByProductAsync(int productId)
        {
            return await _repository.GetByProductIdAsync(productId);
        }

        public async Task<OfferProductEntity?> GetOfferProductAsync(int offerId, int productId)
        {
            return await _repository.GetByOfferAndProductAsync(offerId, productId);
        }

        public async Task<OfferProductEntity> CreateOfferProductAsync(OfferProductEntity offerProduct, string? createdBy = null)
        {
            // Validate the offer-product relationship
            await ValidateOfferProductAsync(offerProduct);

            // Verify offer exists
            var offer = await _offersRepository.GetByIdAsync(offerProduct.OfferId);
            if (offer == null)
                throw new InvalidOperationException($"Offer with ID {offerProduct.OfferId} not found");

            // Verify product exists
            var product = await _productsRepository.GetByIdAsync(offerProduct.ProductId);
            if (product == null)
                throw new InvalidOperationException($"Product with ID {offerProduct.ProductId} not found");

            return await _repository.CreateAsync(offerProduct);
        }

        public async Task<OfferProductEntity> UpdateOfferProductAsync(OfferProductEntity offerProduct, string? modifiedBy = null)
        {
            // Validate the offer-product relationship
            await ValidateOfferProductAsync(offerProduct);

            // Check if offer-product exists
            var existingOfferProduct = await _repository.GetByIdAsync(offerProduct.Id);
            if (existingOfferProduct == null)
                throw new InvalidOperationException($"Offer-product with ID {offerProduct.Id} not found");

            return await _repository.UpdateAsync(offerProduct);
        }

        public async Task<OfferProductEntity> CreateOrUpdateOfferProductAsync(int offerId, int productId,
            Dictionary<string, object?> offerProperties, string? createdBy = null)
        {
            // Try to find existing offer-product relationship
            var existingOfferProduct = await _repository.GetByOfferAndProductAsync(offerId, productId);

            var offerPropertiesJson = JsonSerializer.Serialize(offerProperties);

            if (existingOfferProduct != null)
            {
                // Update existing relationship
                existingOfferProduct.ProductPropertiesJson = offerPropertiesJson;
                return await UpdateOfferProductAsync(existingOfferProduct, createdBy);
            }
            else
            {
                // Create new relationship
                var newOfferProduct = new OfferProductEntity
                {
                    OfferId = offerId,
                    ProductId = productId,
                    ProductPropertiesJson = offerPropertiesJson
                };

                return await CreateOfferProductAsync(newOfferProduct, createdBy);
            }
        }

        public async Task<IEnumerable<OfferProductEntity>> BulkCreateOfferProductsAsync(
            IEnumerable<OfferProductEntity> offerProducts, string? createdBy = null)
        {
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
                    Console.WriteLine($"Error processing offer-product {offerProduct.Id}: {ex.Message}");
                    throw; // Re-throw for now, but could be changed to continue processing
                }
            }

            return results;
        }

        public async Task<bool> DeleteOfferProductAsync(int id)
        {
            try
            {
                await _repository.DeleteAsync(id);
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

        public async Task<bool> SetAvailabilityAsync(int id, bool isAvailable, string? modifiedBy = null)
        {
            var offerProduct = await _repository.GetByIdAsync(id);
            if (offerProduct == null)
                return false;

            offerProduct.IsAvailable = isAvailable;
            await _repository.UpdateAsync(offerProduct);
            return true;
        }

        private async Task ValidateOfferProductAsync(OfferProductEntity offerProduct)
        {
            if (offerProduct == null)
                throw new ArgumentNullException(nameof(offerProduct));

            if (offerProduct.OfferId <= 0)
                throw new ArgumentException("Valid offer ID is required", nameof(offerProduct.OfferId));

            if (offerProduct.ProductId <= 0)
                throw new ArgumentException("Valid product ID is required", nameof(offerProduct.ProductId));

            // Additional business validation can be added here
            await Task.CompletedTask; // Placeholder for async validation
        }
    }
}
