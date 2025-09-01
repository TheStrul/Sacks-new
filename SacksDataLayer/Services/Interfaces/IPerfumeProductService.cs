using SacksDataLayer.Models;

namespace SacksDataLayer.Services.Interfaces
{
    /// <summary>
    /// Service for perfume-specific product operations with familiar filtering/sorting
    /// </summary>
    public interface IPerfumeProductService
    {
        /// <summary>
        /// Search and filter perfume products with familiar properties
        /// </summary>
        /// <param name="filter">Filter criteria</param>
        /// <param name="sort">Sort options</param>
        /// <param name="pageNumber">Page number (1-based)</param>
        /// <param name="pageSize">Items per page</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Paginated results with mapped properties</returns>
        Task<PaginatedResult<PerfumeProductResult>> SearchPerfumeProductsAsync(
            PerfumeFilterModel filter,
            PerfumeSortModel sort,
            int pageNumber = 1,
            int pageSize = 50,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get available filter values for dropdowns/facets
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Available values for each filter property</returns>
        Task<PerfumeFilterValues> GetAvailableFilterValuesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Get a single perfume product with mapped properties
        /// </summary>
        /// <param name="id">Product ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Mapped perfume product or null</returns>
        Task<PerfumeProductResult?> GetPerfumeProductAsync(int id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get a single perfume product by EAN with mapped properties
        /// </summary>
        /// <param name="ean">Product EAN</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Mapped perfume product or null</returns>
        Task<PerfumeProductResult?> GetPerfumeProductByEANAsync(string ean, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Available filter values for building UI dropdowns/facets
    /// </summary>
    public class PerfumeFilterValues
    {
        public List<string> Genders { get; set; } = new();
        public List<string> Sizes { get; set; } = new();
        public List<string> Concentrations { get; set; } = new();
        public List<string> Brands { get; set; } = new();
        public List<string> ProductLines { get; set; } = new();
        public List<string> FragranceFamilies { get; set; } = new();
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
    }
}
