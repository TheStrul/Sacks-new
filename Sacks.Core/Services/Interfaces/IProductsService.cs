namespace Sacks.Core.Services.Interfaces
{
    using Sacks.Core.Entities;

    /// <summary>
    /// Service interface for managing products with comprehensive business logic
    /// </summary>
    public interface IProductsService
    {
        #region Basic CRUD Operations

        /// <summary>
        /// Gets a product by its ID
        /// </summary>
        Task<Product?> GetProductAsync(int id);

        /// <summary>
        /// Gets a product by EAN
        /// </summary>
        Task<Product?> GetProductByEANAsync(string ean);

        /// <summary>
        /// 🚀 PERFORMANCE: Gets multiple products by EANs in a single database call
        /// </summary>
        Task<Dictionary<string, Product>> GetProductsByEANsBulkAsync(IEnumerable<string> eans);

        /// <summary>
        /// Gets products with pagination
        /// </summary>
        Task<(IEnumerable<Product> Products, int TotalCount)> GetProductsAsync(int pageNumber = 1, int pageSize = 50);

        /// <summary>
        /// Creates a new product
        /// </summary>
        Task<Product> CreateProductAsync(Product product, string? createdBy = null);

        /// <summary>
        /// Deletes a product
        /// </summary>
        Task<bool> DeleteProductAsync(int id);

        #endregion

        #region Advanced Operations

        /// <summary>
        /// Searches products by name
        /// </summary>
        Task<IEnumerable<Product>> SearchProductsByNameAsync(string searchTerm);

        /// <summary>
        /// Gets products by source file
        /// </summary>
        Task<IEnumerable<Product>> GetProductsBySourceFileAsync(string sourceFile);

        /// <summary>
        /// 🚀 PERFORMANCE OPTIMIZED: Bulk create/update with minimal database calls
        /// </summary>
        Task<ProductImportResult> BulkCreateOrUpdateProductsOptimizedAsync(Offer offer);

        #endregion

        #region Statistics

        /// <summary>
        /// Gets total product count
        /// </summary>
        Task<int> GetProductCountAsync();

        #endregion
    }

    public struct ProductImportResult
    {
        public int Created;
        public int Updated;
        public int NoChanges;
        public int Errors;
        public int Warnings;
    }
}
