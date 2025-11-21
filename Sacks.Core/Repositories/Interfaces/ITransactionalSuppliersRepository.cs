using Sacks.Core.Entities;

namespace Sacks.Core.Repositories.Interfaces
{
    /// <summary>
    /// Transaction-aware repository interface for Suppliers
    /// Methods do not automatically save changes - use with IUnitOfWork for transaction coordination
    /// </summary>
    public interface ITransactionalSuppliersRepository
    {
        #region Query Operations

        /// <summary>
        /// Gets a supplier by its ID
        /// </summary>
        Task<Supplier?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a supplier by name
        /// </summary>
        Task<Supplier?> GetByNameAsync(string name, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all suppliers
        /// </summary>
        Task<IEnumerable<Supplier>> GetAllAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets suppliers with pagination
        /// </summary>
        Task<IEnumerable<Supplier>> GetPagedAsync(int skip, int take, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets supplier count
        /// </summary>
        Task<int> GetCountAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if a supplier with the given name exists
        /// </summary>
        Task<bool> ExistsAsync(string name, CancellationToken cancellationToken = default);

        /// <summary>
        /// Searches suppliers by term
        /// </summary>
        Task<IEnumerable<Supplier>> SearchAsync(string searchTerm, CancellationToken cancellationToken = default);

        #endregion

        #region Transaction-Aware CRUD Operations

        /// <summary>
        /// Adds a supplier to the context (does not save)
        /// </summary>
        /// <param name="supplier">Supplier to add</param>
        void Add(Supplier supplier);

        /// <summary>
        /// Adds multiple suppliers to the context (does not save)
        /// </summary>
        /// <param name="suppliers">Suppliers to add</param>
        void AddRange(IEnumerable<Supplier> suppliers);

        /// <summary>
        /// Updates a supplier in the context (does not save)
        /// </summary>
        /// <param name="supplier">Supplier to update</param>
        void Update(Supplier supplier);

        /// <summary>
        /// Updates multiple suppliers in the context (does not save)
        /// </summary>
        /// <param name="suppliers">Suppliers to update</param>
        void UpdateRange(IEnumerable<Supplier> suppliers);

        /// <summary>
        /// Removes a supplier from the context (does not save)
        /// </summary>
        /// <param name="supplier">Supplier to remove</param>
        void Remove(Supplier supplier);

        /// <summary>
        /// Removes multiple suppliers from the context (does not save)
        /// </summary>
        /// <param name="suppliers">Suppliers to remove</param>
        void RemoveRange(IEnumerable<Supplier> suppliers);

        /// <summary>
        /// Removes a supplier by ID from the context (does not save)
        /// </summary>
        /// <param name="id">Supplier ID to remove</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if supplier was found and marked for removal</returns>
        Task<bool> RemoveByIdAsync(int id, CancellationToken cancellationToken = default);

        #endregion
    }
}
