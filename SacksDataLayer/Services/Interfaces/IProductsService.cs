namespace SacksDataLayer.Services.Interfaces
{
    /// <summary>
    /// Service interface for managing products with comprehensive business logic
    /// </summary>
    public interface IProductsService
    {
        #region Basic CRUD Operations

        /// <summary>
        /// Gets a product by its ID
        /// </summary>
        Task<ProductEntity?> GetProductAsync(int id);

        /// <summary>
        /// Gets a product by its EAN
        /// </summary>
        Task<ProductEntity?> GetProductByEANAsync(string ean);

        /// <summary>
        /// Gets products with pagination
        /// </summary>
        Task<(IEnumerable<ProductEntity> Products, int TotalCount)> GetProductsAsync(int pageNumber = 1, int pageSize = 50);

        /// <summary>
        /// Creates a new product
        /// </summary>
        Task<ProductEntity> CreateProductAsync(ProductEntity product, string? createdBy = null);

        /// <summary>
        /// Updates an existing product
        /// </summary>
        Task<ProductEntity> UpdateProductAsync(ProductEntity product, string? modifiedBy = null);

        /// <summary>
        /// Deletes a product
        /// </summary>
        Task<bool> DeleteProductAsync(int id);

        #endregion

        #region Advanced Operations

        /// <summary>
        /// Searches products by name
        /// </summary>
        Task<IEnumerable<ProductEntity>> SearchProductsByNameAsync(string searchTerm);

        /// <summary>
        /// Gets products by source file
        /// </summary>
        Task<IEnumerable<ProductEntity>> GetProductsBySourceFileAsync(string sourceFile);

        /// <summary>
        /// Creates or updates a product based on EAN
        /// </summary>
        Task<ProductEntity> CreateOrUpdateProductAsync(ProductEntity product, string? userContext = null);

        /// <summary>
        /// Bulk creates or updates products
        /// </summary>
        Task<(int Created, int Updated, int Errors)> BulkCreateOrUpdateProductsAsync(
            IEnumerable<ProductEntity> products, string? userContext = null);

        #endregion

        #region Statistics

        /// <summary>
        /// Gets total product count
        /// </summary>
        Task<int> GetProductCountAsync();

        #endregion
    }
}
