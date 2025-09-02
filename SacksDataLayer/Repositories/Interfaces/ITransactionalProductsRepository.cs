using SacksDataLayer.Entities;

namespace SacksDataLayer.Repositories.Interfaces
{
    /// <summary>
    /// Transaction-aware repository interface for Products
    /// Methods do not automatically save changes - use with IUnitOfWork for transaction coordination
    /// </summary>
    public interface ITransactionalProductsRepository
    {
        #region Transaction-Aware CRUD Operations

        /// <summary>
        /// Gets a product by its ID
        /// </summary>
        Task<ProductEntity?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a product by its EAN
        /// </summary>
        Task<ProductEntity?> GetByEANAsync(string ean, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets multiple products by their EANs in a single query
        /// </summary>
        Task<Dictionary<string, ProductEntity>> GetByEANsBulkAsync(IEnumerable<string> eans, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all products
        /// </summary>
        Task<IEnumerable<ProductEntity>> GetAllAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Adds a product to the context (does not save)
        /// </summary>
        /// <param name="product">Product to add</param>
        /// <param name="createdBy">User creating the product</param>
        void Add(ProductEntity product, string? createdBy = null);

        /// <summary>
        /// Adds multiple products to the context (does not save)
        /// </summary>
        /// <param name="products">Products to add</param>
        /// <param name="createdBy">User creating the products</param>
        void AddRange(IEnumerable<ProductEntity> products, string? createdBy = null);

        /// <summary>
        /// Updates a product in the context (does not save)
        /// </summary>
        /// <param name="product">Product to update</param>
        /// <param name="modifiedBy">User modifying the product</param>
        void Update(ProductEntity product, string? modifiedBy = null);

        /// <summary>
        /// Updates multiple products in the context (does not save)
        /// </summary>
        /// <param name="products">Products to update</param>
        /// <param name="modifiedBy">User modifying the products</param>
        void UpdateRange(IEnumerable<ProductEntity> products, string? modifiedBy = null);

        /// <summary>
        /// Removes a product from the context (does not save)
        /// </summary>
        /// <param name="product">Product to remove</param>
        void Remove(ProductEntity product);

        /// <summary>
        /// Removes multiple products from the context (does not save)
        /// </summary>
        /// <param name="products">Products to remove</param>
        void RemoveRange(IEnumerable<ProductEntity> products);

        /// <summary>
        /// Removes a product by ID from the context (does not save)
        /// </summary>
        /// <param name="id">Product ID to remove</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if product was found and marked for removal</returns>
        Task<bool> RemoveByIdAsync(int id, CancellationToken cancellationToken = default);

        #endregion

        #region Query Operations

        /// <summary>
        /// Gets products with pagination
        /// </summary>
        Task<IEnumerable<ProductEntity>> GetPagedAsync(int skip, int take, CancellationToken cancellationToken = default);

        /// <summary>
        /// Finds products matching a predicate
        /// </summary>
        Task<IEnumerable<ProductEntity>> FindAsync(System.Linq.Expressions.Expression<Func<ProductEntity, bool>> predicate, CancellationToken cancellationToken = default);

        /// <summary>
        /// Searches products by name
        /// </summary>
        Task<IEnumerable<ProductEntity>> SearchByNameAsync(string searchTerm, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if an EAN exists
        /// </summary>
        Task<bool> EANExistsAsync(string ean, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets products by source file
        /// </summary>
        Task<IEnumerable<ProductEntity>> GetBySourceFileAsync(string sourceFile, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets product count
        /// </summary>
        Task<int> GetCountAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets products by created date range
        /// </summary>
        Task<IEnumerable<ProductEntity>> GetByCreatedDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

        #endregion
    }
}
