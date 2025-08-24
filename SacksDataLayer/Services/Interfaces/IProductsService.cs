using SacksDataLayer.FileProcessing.Models;
using SacksDataLayer.Repositories.Interfaces;

namespace SacksDataLayer.Services.Interfaces
{
    /// <summary>
    /// Service interface for managing products with business logic
    /// </summary>
    public interface IProductsService
    {
        #region Basic Operations

        /// <summary>
        /// Gets a product by its ID
        /// </summary>
        /// <param name="id">Product ID</param>
        /// <param name="includeDeleted">Whether to include soft deleted products</param>
        /// <returns>Product entity or null if not found</returns>
        Task<ProductEntity?> GetProductAsync(int id, bool includeDeleted = false);

        /// <summary>
        /// Gets a product by its SKU
        /// </summary>
        /// <param name="sku">Product SKU</param>
        /// <param name="includeDeleted">Whether to include soft deleted products</param>
        /// <returns>Product entity or null if not found</returns>
        Task<ProductEntity?> GetProductBySKUAsync(string sku, bool includeDeleted = false);

        /// <summary>
        /// Gets all products with optional pagination
        /// </summary>
        /// <param name="pageNumber">Page number (1-based)</param>
        /// <param name="pageSize">Number of items per page</param>
        /// <param name="includeDeleted">Whether to include soft deleted products</param>
        /// <returns>Paginated product collection</returns>
        Task<(IEnumerable<ProductEntity> Products, int TotalCount)> GetProductsAsync(int pageNumber = 1, int pageSize = 50, bool includeDeleted = false);

        /// <summary>
        /// Creates a new product with validation
        /// </summary>
        /// <param name="product">Product to create</param>
        /// <param name="createdBy">User creating the product</param>
        /// <returns>Created product</returns>
        Task<ProductEntity> CreateProductAsync(ProductEntity product, string? createdBy = null);

        /// <summary>
        /// Updates an existing product with validation
        /// </summary>
        /// <param name="product">Product to update</param>
        /// <param name="modifiedBy">User modifying the product</param>
        /// <returns>Updated product</returns>
        Task<ProductEntity> UpdateProductAsync(ProductEntity product, string? modifiedBy = null);

        /// <summary>
        /// Deletes a product (soft delete by default)
        /// </summary>
        /// <param name="id">Product ID to delete</param>
        /// <param name="deletedBy">User performing the deletion</param>
        /// <param name="hardDelete">Whether to permanently delete the product</param>
        /// <returns>True if deletion was successful</returns>
        Task<bool> DeleteProductAsync(int id, string? deletedBy = null, bool hardDelete = false);

        /// <summary>
        /// Restores a soft deleted product
        /// </summary>
        /// <param name="id">Product ID to restore</param>
        /// <returns>True if restoration was successful</returns>
        Task<bool> RestoreProductAsync(int id);

        #endregion

        #region Search and Filtering

        /// <summary>
        /// Searches products by multiple criteria
        /// </summary>
        /// <param name="searchTerm">Search term for name/description</param>
        /// <param name="sku">Optional SKU filter</param>
        /// <param name="processingMode">Optional processing mode filter</param>
        /// <param name="sourceFile">Optional source file filter</param>
        /// <param name="includeDeleted">Whether to include soft deleted products</param>
        /// <returns>Collection of matching products</returns>
        Task<IEnumerable<ProductEntity>> SearchProductsAsync(
            string? searchTerm = null,
            string? sku = null,
            ProcessingMode? processingMode = null,
            string? sourceFile = null,
            bool includeDeleted = false);

        /// <summary>
        /// Gets products by processing mode with detailed filtering
        /// </summary>
        /// <param name="mode">Processing mode</param>
        /// <param name="dateFrom">Optional start date filter</param>
        /// <param name="dateTo">Optional end date filter</param>
        /// <param name="includeDeleted">Whether to include soft deleted products</param>
        /// <returns>Collection of products</returns>
        Task<IEnumerable<ProductEntity>> GetProductsByModeAsync(
            ProcessingMode mode,
            DateTime? dateFrom = null,
            DateTime? dateTo = null,
            bool includeDeleted = false);

        /// <summary>
        /// Gets products by supplier source file
        /// </summary>
        /// <param name="sourceFile">Source file name or path</param>
        /// <param name="includeDeleted">Whether to include soft deleted products</param>
        /// <returns>Collection of products from the specified source</returns>
        Task<IEnumerable<ProductEntity>> GetProductsBySourceAsync(string sourceFile, bool includeDeleted = false);

        #endregion

        #region Bulk Operations

        /// <summary>
        /// Creates multiple products with validation and duplicate checking
        /// </summary>
        /// <param name="products">Products to create</param>
        /// <param name="createdBy">User creating the products</param>
        /// <param name="skipDuplicates">Whether to skip products with duplicate SKUs</param>
        /// <returns>Result containing created products and any issues</returns>
        Task<BulkOperationResult> CreateProductsBulkAsync(
            IEnumerable<ProductEntity> products,
            string? createdBy = null,
            bool skipDuplicates = true);

        /// <summary>
        /// Updates multiple products with validation
        /// </summary>
        /// <param name="products">Products to update</param>
        /// <param name="modifiedBy">User modifying the products</param>
        /// <returns>Result containing updated products and any issues</returns>
        Task<BulkOperationResult> UpdateProductsBulkAsync(
            IEnumerable<ProductEntity> products,
            string? modifiedBy = null);

        /// <summary>
        /// Deletes multiple products (soft delete by default)
        /// </summary>
        /// <param name="ids">Product IDs to delete</param>
        /// <param name="deletedBy">User performing the deletion</param>
        /// <param name="hardDelete">Whether to permanently delete the products</param>
        /// <returns>Number of products successfully deleted</returns>
        Task<int> DeleteProductsBulkAsync(IEnumerable<int> ids, string? deletedBy = null, bool hardDelete = false);

        #endregion

        #region Processing Integration

        /// <summary>
        /// Processes and saves products from a processing result
        /// </summary>
        /// <param name="processingResult">Result from product normalization</param>
        /// <param name="createdBy">User saving the products</param>
        /// <param name="skipDuplicates">Whether to skip products with duplicate SKUs</param>
        /// <returns>Result containing saved products and any issues</returns>
        Task<BulkOperationResult> SaveProcessingResultAsync(
            ProcessingResult processingResult,
            string? createdBy = null,
            bool skipDuplicates = true);

        /// <summary>
        /// Gets processing statistics with detailed breakdowns
        /// </summary>
        /// <param name="sourceFile">Optional source file filter</param>
        /// <param name="dateFrom">Optional start date filter</param>
        /// <param name="dateTo">Optional end date filter</param>
        /// <param name="includeDeleted">Whether to include soft deleted products</param>
        /// <returns>Comprehensive processing statistics</returns>
        Task<ProcessingStatisticsReport> GetProcessingStatisticsAsync(
            string? sourceFile = null,
            DateTime? dateFrom = null,
            DateTime? dateTo = null,
            bool includeDeleted = false);

        #endregion

        #region Validation and Business Rules

        /// <summary>
        /// Validates a product according to business rules
        /// </summary>
        /// <param name="product">Product to validate</param>
        /// <returns>Validation result with any errors or warnings</returns>
        Task<ValidationResult> ValidateProductAsync(ProductEntity product);

        /// <summary>
        /// Checks for duplicate products by SKU
        /// </summary>
        /// <param name="sku">SKU to check</param>
        /// <param name="excludeId">Optional ID to exclude from check (for updates)</param>
        /// <returns>True if duplicate exists</returns>
        Task<bool> HasDuplicateSKUAsync(string sku, int? excludeId = null);

        /// <summary>
        /// Gets suggested products based on name similarity
        /// </summary>
        /// <param name="productName">Product name to match against</param>
        /// <param name="maxResults">Maximum number of suggestions</param>
        /// <returns>Collection of similar products</returns>
        Task<IEnumerable<ProductEntity>> GetSimilarProductsAsync(string productName, int maxResults = 5);

        #endregion
    }

    /// <summary>
    /// Result of bulk operations
    /// </summary>
    public class BulkOperationResult
    {
        public IEnumerable<ProductEntity> SuccessfulProducts { get; set; } = new List<ProductEntity>();
        public IEnumerable<BulkOperationError> Errors { get; set; } = new List<BulkOperationError>();
        public int TotalProcessed { get; set; }
        public int SuccessCount { get; set; }
        public int ErrorCount { get; set; }
        public TimeSpan ProcessingTime { get; set; }
    }

    /// <summary>
    /// Error details for bulk operations
    /// </summary>
    public class BulkOperationError
    {
        public ProductEntity? Product { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public string ErrorType { get; set; } = string.Empty;
        public Exception? Exception { get; set; }
    }

    /// <summary>
    /// Validation result for products
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
    }

    /// <summary>
    /// Comprehensive processing statistics report
    /// </summary>
    public class ProcessingStatisticsReport
    {
        public Dictionary<ProcessingMode, int> ProductsByMode { get; set; } = new();
        public Dictionary<string, int> ProductsBySource { get; set; } = new();
        public Dictionary<DateTime, int> ProductsByDate { get; set; } = new();
        public int TotalProducts { get; set; }
        public int DeletedProducts { get; set; }
        public DateTime? OldestProduct { get; set; }
        public DateTime? NewestProduct { get; set; }
        public Dictionary<string, int> TopDynamicProperties { get; set; } = new();
    }
}
