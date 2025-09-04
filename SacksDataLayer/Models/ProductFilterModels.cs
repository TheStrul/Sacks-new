using System.ComponentModel.DataAnnotations;

namespace SacksDataLayer.Models
{
    /// <summary>
    /// Well-known product properties for perfume market filtering/sorting
    /// Maps to dynamic properties in ProductEntity
    /// </summary>
    public class PerfumeFilterModel
    {
        /// <summary>
        /// Product gender (Men, Women, Unisex)
        /// Maps to DynamicProperties["Gender"]
        /// </summary>
        public string? Gender { get; set; }

        /// <summary>
        /// Product size/volume (e.g., "50ml", "100ml")
        /// Maps to DynamicProperties["Size"] or DynamicProperties["Volume"]
        /// </summary>
        public string? Size { get; set; }

        /// <summary>
        /// Fragrance concentration (EDT, EDP, Parfum, etc.)
        /// Maps to DynamicProperties["Concentration"]
        /// </summary>
        public string? Concentration { get; set; }

        /// <summary>
        /// Brand name
        /// Maps to DynamicProperties["Brand"]
        /// </summary>
        public string? Brand { get; set; }

        /// <summary>
        /// Product line/collection
        /// Maps to DynamicProperties["Line"] or DynamicProperties["Collection"]
        /// </summary>
        public string? ProductLine { get; set; }

        /// <summary>
        /// Price range filter
        /// </summary>
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }

        /// <summary>
        /// Text search in name/description
        /// </summary>
        public string? SearchText { get; set; }
    }

    /// <summary>
    /// Sorting options for perfume products
    /// </summary>
    public class PerfumeSortModel
    {
        public PerfumeSortField SortBy { get; set; } = PerfumeSortField.Name;
        public SortDirection Direction { get; set; } = SortDirection.Ascending;
    }

    public enum PerfumeSortField
    {
        Name,
        Brand,
        Size,
        Concentration,
        Gender,
        Price,
        CreatedAt,
        UpdatedAt
    }

    public enum SortDirection
    {
        Ascending,
        Descending
    }

    /// <summary>
    /// Paginated result for perfume products
    /// </summary>
    public class PerfumeProductResult
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string EAN { get; set; } = string.Empty;
        public string? Description { get; set; }
        
        // Mapped properties from DynamicProperties
        public string? Gender { get; set; }
        public string? Size { get; set; }
        public string? Concentration { get; set; }
        public string? Brand { get; set; }
        public string? ProductLine { get; set; }
        
        // Price from offers (if needed)
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// Paginated response
    /// </summary>
    public class PaginatedResult<T>
    {
        public IEnumerable<T> Items { get; set; } = new List<T>();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
        public bool HasPrevious => PageNumber > 1;
        public bool HasNext => PageNumber < TotalPages;
    }
}
