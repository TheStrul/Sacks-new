#nullable enable
using SacksDataLayer.FileProcessing.Models;
using SacksDataLayer.Entities;
using System.Linq.Expressions;
using System.Threading;

namespace SacksDataLayer.Repositories.Interfaces
{
    /// <summary>
    /// Interface for Products repository with comprehensive CRUD operations
    /// </summary>
    public interface IProductsRepository
    {
        #region Basic CRUD Operations

        /// <summary>
        /// Gets a product by its ID
        /// </summary>
        /// <param name="id">Product ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Product entity or null if not found</returns>
        Task<ProductEntity?> GetByIdAsync(int id, CancellationToken cancellationToken);

        /// <summary>
        /// Gets a product by its EAN
        /// </summary>
        /// <param name="ean">Product EAN</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Product entity or null if not found</returns>
        Task<ProductEntity?> GetByEANAsync(string ean, CancellationToken cancellationToken);

        /// <summary>
        /// 🚀 PERFORMANCE: Gets multiple products by their EANs in a single query to avoid N+1 problem
        /// </summary>
        /// <param name="eans">Collection of EANs to lookup</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Dictionary mapping EAN to ProductEntity</returns>
        Task<Dictionary<string, ProductEntity>> GetByEANsBulkAsync(IEnumerable<string> eans, CancellationToken cancellationToken);

        /// <summary>
        /// Gets all products
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Collection of products</returns>
        Task<IEnumerable<ProductEntity>> GetAllAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Gets products with pagination
        /// </summary>
        /// <param name="skip">Number of records to skip</param>
        /// <param name="take">Number of records to take</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Paginated collection of products</returns>
        Task<IEnumerable<ProductEntity>> GetPagedAsync(int skip, int take, CancellationToken cancellationToken);

        /// <summary>
        /// Creates a new product
        /// </summary>
        /// <param name="product">Product to create</param>
        /// <param name="createdBy">User creating the product</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Created product with generated ID</returns>
        Task<ProductEntity> CreateAsync(ProductEntity product, string? createdBy = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates an existing product
        /// </summary>
        /// <param name="product">Product to update</param>
        /// <param name="modifiedBy">User modifying the product</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Updated product</returns>
        Task<ProductEntity> UpdateAsync(ProductEntity product, string? modifiedBy = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes a product (permanent removal)
        /// </summary>
        /// <param name="id">Product ID to delete</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if deletion was successful</returns>
        Task<bool> DeleteAsync(int id, CancellationToken cancellationToken);

        #endregion

        #region Advanced Query Operations

        /// <summary>
        /// Finds products matching the specified criteria
        /// </summary>
        /// <param name="predicate">Search criteria</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Collection of matching products</returns>
        Task<IEnumerable<ProductEntity>> FindAsync(Expression<Func<ProductEntity, bool>> predicate, CancellationToken cancellationToken);

        /// <summary>
        /// Searches products by name (case-insensitive partial match)
        /// </summary>
        /// <param name="searchTerm">Search term</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Collection of matching products</returns>
        Task<IEnumerable<ProductEntity>> SearchByNameAsync(string searchTerm, CancellationToken cancellationToken);

        /// <summary>
        /// Checks if a product with the specified EAN exists
        /// </summary>
        /// <param name="ean">Product EAN</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if a product with the EAN exists</returns>
        Task<bool> EANExistsAsync(string ean, CancellationToken cancellationToken);

        /// <summary>
        /// Gets products by supplier source file
        /// </summary>
        /// <param name="sourceFile">Source file name or path</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Collection of products from the specified source</returns>
        Task<IEnumerable<ProductEntity>> GetBySourceFileAsync(string sourceFile, CancellationToken cancellationToken);

        /// <summary>
        /// Searches products by dynamic property
        /// </summary>
        /// <param name="propertyKey">Dynamic property key</param>
        /// <param name="propertyValue">Dynamic property value (optional)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Collection of products with the specified dynamic property</returns>
        Task<IEnumerable<ProductEntity>> SearchByDynamicPropertyAsync(string propertyKey, object? propertyValue = null, CancellationToken cancellationToken = default);

        #endregion

        #region Bulk Operations

        /// <summary>
        /// Creates multiple products in a single transaction
        /// </summary>
        /// <param name="products">Products to create</param>
        /// <param name="createdBy">User creating the products</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Collection of created products with generated IDs</returns>
        Task<IEnumerable<ProductEntity>> CreateBulkAsync(IEnumerable<ProductEntity> products, string? createdBy = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates multiple products in a single transaction
        /// </summary>
        /// <param name="products">Products to update</param>
        /// <param name="modifiedBy">User modifying the products</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Collection of updated products</returns>
        Task<IEnumerable<ProductEntity>> UpdateBulkAsync(IEnumerable<ProductEntity> products, string? modifiedBy = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes multiple products in a single transaction
        /// </summary>
        /// <param name="ids">Product IDs to delete</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Number of products successfully deleted</returns>
        Task<int> DeleteBulkAsync(IEnumerable<int> ids, CancellationToken cancellationToken);

        #endregion

        #region Statistics and Analytics

        /// <summary>
        /// Gets total count of products
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Total product count</returns>
        Task<int> GetCountAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Gets products created within a date range
        /// </summary>
        /// <param name="startDate">Start date (inclusive)</param>
        /// <param name="endDate">End date (inclusive)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Collection of products created in the date range</returns>
        Task<IEnumerable<ProductEntity>> GetByCreatedDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken);

        #endregion

        #region Transaction Support

        /// <summary>
        /// Begins a database transaction
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Database transaction</returns>
        Task<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Saves all pending changes to the database
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Number of affected records</returns>
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

        #endregion
    }
}
