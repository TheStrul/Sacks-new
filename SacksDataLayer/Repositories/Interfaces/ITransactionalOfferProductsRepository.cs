using SacksDataLayer.Entities;

namespace SacksDataLayer.Repositories.Interfaces
{
    /// <summary>
    /// Transaction-aware repository interface for OfferProducts
    /// Methods do not automatically save changes - use with IUnitOfWork for transaction coordination
    /// </summary>
    public interface ITransactionalOfferProductsRepository
    {
        #region Query Operations

        /// <summary>
        /// Gets an offer-product relationship by its ID
        /// </summary>
        Task<ProductOffer?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all offer-product relationships for a specific offer
        /// </summary>
        Task<IEnumerable<ProductOffer>> GetByOfferIdAsync(int offerId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all offer-product relationships for a specific product
        /// </summary>
        Task<IEnumerable<ProductOffer>> GetByProductIdAsync(int productId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a specific offer-product relationship
        /// </summary>
        Task<ProductOffer?> GetByOfferAndProductAsync(int offerId, int productId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all offer-product relationships
        /// </summary>
        Task<IEnumerable<ProductOffer>> GetAllAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets offer-product count
        /// </summary>
        Task<int> GetCountAsync(CancellationToken cancellationToken = default);

        #endregion

        #region Transaction-Aware CRUD Operations

        /// <summary>
        /// Adds an offer-product relationship to the context (does not save)
        /// </summary>
        /// <param name="offerProduct">Offer-product relationship to add</param>
        void Add(ProductOffer offerProduct);

        /// <summary>
        /// Adds multiple offer-product relationships to the context (does not save)
        /// </summary>
        /// <param name="offerProducts">Offer-product relationships to add</param>
        void AddRange(IEnumerable<ProductOffer> offerProducts);

        /// <summary>
        /// Updates an offer-product relationship in the context (does not save)
        /// </summary>
        /// <param name="offerProduct">Offer-product relationship to update</param>
        void Update(ProductOffer offerProduct);

        /// <summary>
        /// Updates multiple offer-product relationships in the context (does not save)
        /// </summary>
        /// <param name="offerProducts">Offer-product relationships to update</param>
        void UpdateRange(IEnumerable<ProductOffer> offerProducts);

        /// <summary>
        /// Removes an offer-product relationship from the context (does not save)
        /// </summary>
        /// <param name="offerProduct">Offer-product relationship to remove</param>
        void Remove(ProductOffer offerProduct);

        /// <summary>
        /// Removes multiple offer-product relationships from the context (does not save)
        /// </summary>
        /// <param name="offerProducts">Offer-product relationships to remove</param>
        void RemoveRange(IEnumerable<ProductOffer> offerProducts);

        /// <summary>
        /// Removes an offer-product relationship by ID from the context (does not save)
        /// </summary>
        /// <param name="id">Offer-product ID to remove</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if offer-product was found and marked for removal</returns>
        Task<bool> RemoveByIdAsync(int id, CancellationToken cancellationToken = default);

        #endregion
    }
}