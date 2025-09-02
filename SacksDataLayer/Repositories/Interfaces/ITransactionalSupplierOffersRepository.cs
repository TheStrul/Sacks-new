using SacksDataLayer.Entities;

namespace SacksDataLayer.Repositories.Interfaces
{
    /// <summary>
    /// Transaction-aware repository interface for SupplierOffers
    /// Methods do not automatically save changes - use with IUnitOfWork for transaction coordination
    /// </summary>
    public interface ITransactionalSupplierOffersRepository
    {
        #region Query Operations

        /// <summary>
        /// Gets a supplier offer by its ID
        /// </summary>
        Task<SupplierOfferEntity?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets active offer for a product from a supplier
        /// </summary>
        Task<SupplierOfferEntity?> GetActiveOfferAsync(int productId, int supplierId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all offers for a specific product
        /// </summary>
        Task<IEnumerable<SupplierOfferEntity>> GetByProductIdAsync(int productId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all offers from a specific supplier
        /// </summary>
        Task<IEnumerable<SupplierOfferEntity>> GetBySupplierIdAsync(int supplierId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all supplier offers
        /// </summary>
        Task<IEnumerable<SupplierOfferEntity>> GetAllAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets offer count
        /// </summary>
        Task<int> GetCountAsync(CancellationToken cancellationToken = default);

        #endregion

        #region Transaction-Aware CRUD Operations

        /// <summary>
        /// Adds a supplier offer to the context (does not save)
        /// </summary>
        /// <param name="offer">Supplier offer to add</param>
        void Add(SupplierOfferEntity offer);

        /// <summary>
        /// Adds multiple supplier offers to the context (does not save)
        /// </summary>
        /// <param name="offers">Supplier offers to add</param>
        void AddRange(IEnumerable<SupplierOfferEntity> offers);

        /// <summary>
        /// Updates a supplier offer in the context (does not save)
        /// </summary>
        /// <param name="offer">Supplier offer to update</param>
        void Update(SupplierOfferEntity offer);

        /// <summary>
        /// Updates multiple supplier offers in the context (does not save)
        /// </summary>
        /// <param name="offers">Supplier offers to update</param>
        void UpdateRange(IEnumerable<SupplierOfferEntity> offers);

        /// <summary>
        /// Removes a supplier offer from the context (does not save)
        /// </summary>
        /// <param name="offer">Supplier offer to remove</param>
        void Remove(SupplierOfferEntity offer);

        /// <summary>
        /// Removes multiple supplier offers from the context (does not save)
        /// </summary>
        /// <param name="offers">Supplier offers to remove</param>
        void RemoveRange(IEnumerable<SupplierOfferEntity> offers);

        /// <summary>
        /// Removes a supplier offer by ID from the context (does not save)
        /// </summary>
        /// <param name="id">Supplier offer ID to remove</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if offer was found and marked for removal</returns>
        Task<bool> RemoveByIdAsync(int id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deactivates old offers for a specific product and supplier (does not save)
        /// </summary>
        /// <param name="productId">Product ID</param>
        /// <param name="supplierId">Supplier ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task DeactivateOldOffersAsync(int productId, int supplierId, CancellationToken cancellationToken = default);

        #endregion
    }
}
