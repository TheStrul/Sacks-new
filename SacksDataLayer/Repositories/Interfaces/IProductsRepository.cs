using SacksDataLayer.FileProcessing.Models;
using System.Linq.Expressions;

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
        /// <param name="includeDeleted">Whether to include soft deleted products</param>
        /// <returns>Product entity or null if not found</returns>
        Task<ProductEntity?> GetByIdAsync(int id, bool includeDeleted = false);

        /// <summary>
        /// Gets a product by its SKU
        /// </summary>
        /// <param name="sku">Product SKU</param>
        /// <param name="includeDeleted">Whether to include soft deleted products</param>
        /// <returns>Product entity or null if not found</returns>
        Task<ProductEntity?> GetBySKUAsync(string sku, bool includeDeleted = false);

        /// <summary>
        /// Gets all products with optional filtering
        /// </summary>
        /// <param name="includeDeleted">Whether to include soft deleted products</param>
        /// <returns>Collection of products</returns>
        Task<IEnumerable<ProductEntity>> GetAllAsync(bool includeDeleted = false);

        /// <summary>
        /// Gets products with pagination
        /// </summary>
        /// <param name="skip">Number of records to skip</param>
        /// <param name="take">Number of records to take</param>
        /// <param name="includeDeleted">Whether to include soft deleted products</param>
        /// <returns>Paginated collection of products</returns>
        Task<IEnumerable<ProductEntity>> GetPagedAsync(int skip, int take, bool includeDeleted = false);

        /// <summary>
        /// Creates a new product
        /// </summary>
        /// <param name="product">Product to create</param>
        /// <param name="createdBy">User creating the product</param>
        /// <returns>Created product with generated ID</returns>
        Task<ProductEntity> CreateAsync(ProductEntity product, string? createdBy = null);

        /// <summary>
        /// Updates an existing product
        /// </summary>
        /// <param name="product">Product to update</param>
        /// <param name="modifiedBy">User modifying the product</param>
        /// <returns>Updated product</returns>
        Task<ProductEntity> UpdateAsync(ProductEntity product, string? modifiedBy = null);

        /// <summary>
        /// Soft deletes a product
        /// </summary>
        /// <param name="id">Product ID to delete</param>
        /// <param name="deletedBy">User performing the deletion</param>
        /// <returns>True if deletion was successful</returns>
        Task<bool> SoftDeleteAsync(int id, string? deletedBy = null);

        /// <summary>
        /// Hard deletes a product (permanent removal)
        /// </summary>
        /// <param name="id">Product ID to delete</param>
        /// <returns>True if deletion was successful</returns>
        Task<bool> HardDeleteAsync(int id);

        /// <summary>
        /// Restores a soft deleted product
        /// </summary>
        /// <param name="id">Product ID to restore</param>
        /// <returns>True if restoration was successful</returns>
        Task<bool> RestoreAsync(int id);

        #endregion

        #region Advanced Query Operations

        /// <summary>
        /// Finds products matching the specified criteria
        /// </summary>
        /// <param name="predicate">Search criteria</param>
        /// <param name="includeDeleted">Whether to include soft deleted products</param>
        /// <returns>Collection of matching products</returns>
        Task<IEnumerable<ProductEntity>> FindAsync(Expression<Func<ProductEntity, bool>> predicate, bool includeDeleted = false);

        /// <summary>
        /// Searches products by name (case-insensitive partial match)
        /// </summary>
        /// <param name="searchTerm">Search term</param>
        /// <param name="includeDeleted">Whether to include soft deleted products</param>
        /// <returns>Collection of matching products</returns>
        Task<IEnumerable<ProductEntity>> SearchByNameAsync(string searchTerm, bool includeDeleted = false);

        /// <summary>
        /// Gets products by processing mode
        /// </summary>
        /// <param name="mode">Processing mode</param>
        /// <param name="includeDeleted">Whether to include soft deleted products</param>
        /// <returns>Collection of products processed in the specified mode</returns>
        Task<IEnumerable<ProductEntity>> GetByProcessingModeAsync(ProcessingMode mode, bool includeDeleted = false);

        /// <summary>
        /// Gets products by supplier source file
        /// </summary>
        /// <param name="sourceFile">Source file name or path</param>
        /// <param name="includeDeleted">Whether to include soft deleted products</param>
        /// <returns>Collection of products from the specified source</returns>
        Task<IEnumerable<ProductEntity>> GetBySourceFileAsync(string sourceFile, bool includeDeleted = false);

        /// <summary>
        /// Searches products by dynamic property
        /// </summary>
        /// <param name="propertyKey">Dynamic property key</param>
        /// <param name="propertyValue">Dynamic property value (optional)</param>
        /// <param name="includeDeleted">Whether to include soft deleted products</param>
        /// <returns>Collection of products with the specified dynamic property</returns>
        Task<IEnumerable<ProductEntity>> SearchByDynamicPropertyAsync(string propertyKey, object? propertyValue = null, bool includeDeleted = false);

        #endregion

        #region Bulk Operations

        /// <summary>
        /// Creates multiple products in a single transaction
        /// </summary>
        /// <param name="products">Products to create</param>
        /// <param name="createdBy">User creating the products</param>
        /// <returns>Collection of created products with generated IDs</returns>
        Task<IEnumerable<ProductEntity>> CreateBulkAsync(IEnumerable<ProductEntity> products, string? createdBy = null);

        /// <summary>
        /// Updates multiple products in a single transaction
        /// </summary>
        /// <param name="products">Products to update</param>
        /// <param name="modifiedBy">User modifying the products</param>
        /// <returns>Collection of updated products</returns>
        Task<IEnumerable<ProductEntity>> UpdateBulkAsync(IEnumerable<ProductEntity> products, string? modifiedBy = null);

        /// <summary>
        /// Soft deletes multiple products in a single transaction
        /// </summary>
        /// <param name="ids">Product IDs to delete</param>
        /// <param name="deletedBy">User performing the deletion</param>
        /// <returns>Number of products successfully deleted</returns>
        Task<int> SoftDeleteBulkAsync(IEnumerable<int> ids, string? deletedBy = null);

        #endregion

        #region Statistics and Analytics

        /// <summary>
        /// Gets total count of products
        /// </summary>
        /// <param name="includeDeleted">Whether to include soft deleted products</param>
        /// <returns>Total product count</returns>
        Task<int> GetCountAsync(bool includeDeleted = false);

        /// <summary>
        /// Gets count of products by processing mode
        /// </summary>
        /// <param name="mode">Processing mode</param>
        /// <param name="includeDeleted">Whether to include soft deleted products</param>
        /// <returns>Product count for the specified mode</returns>
        Task<int> GetCountByProcessingModeAsync(ProcessingMode mode, bool includeDeleted = false);

        /// <summary>
        /// Gets processing statistics for products
        /// </summary>
        /// <param name="sourceFile">Optional source file filter</param>
        /// <param name="includeDeleted">Whether to include soft deleted products</param>
        /// <returns>Processing statistics</returns>
        Task<Dictionary<ProcessingMode, int>> GetProcessingStatisticsAsync(string? sourceFile = null, bool includeDeleted = false);

        /// <summary>
        /// Gets products created within a date range
        /// </summary>
        /// <param name="startDate">Start date (inclusive)</param>
        /// <param name="endDate">End date (inclusive)</param>
        /// <param name="includeDeleted">Whether to include soft deleted products</param>
        /// <returns>Collection of products created in the date range</returns>
        Task<IEnumerable<ProductEntity>> GetByCreatedDateRangeAsync(DateTime startDate, DateTime endDate, bool includeDeleted = false);

        #endregion

        #region Transaction Support

        /// <summary>
        /// Begins a database transaction
        /// </summary>
        /// <returns>Database transaction</returns>
        Task<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction> BeginTransactionAsync();

        /// <summary>
        /// Saves all pending changes to the database
        /// </summary>
        /// <returns>Number of affected records</returns>
        Task<int> SaveChangesAsync();

        #endregion
    }
}
