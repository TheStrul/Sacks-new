using SacksDataLayer.Configuration;

namespace SacksDataLayer.Models
{
    /// <summary>
    /// Dynamic product filter model that adapts to configured filterable properties
    /// Replaces hardcoded PerfumeFilterModel with configuration-driven approach
    /// </summary>
    public class ProductFilterModel
    {
        /// <summary>
        /// Dynamic property filters - key is the normalized property key
        /// </summary>
        public Dictionary<string, object?> PropertyFilters { get; set; } = new();

        /// <summary>
        /// Price range filter
        /// </summary>
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }

        /// <summary>
        /// Text search in name/description
        /// </summary>
        public string? SearchText { get; set; }

        /// <summary>
        /// Sets a property filter value
        /// </summary>
        public void SetPropertyFilter(string propertyKey, object? value)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(propertyKey);
            PropertyFilters[propertyKey] = value;
        }

        /// <summary>
        /// Gets a property filter value
        /// </summary>
        public T? GetPropertyFilter<T>(string propertyKey)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(propertyKey);
            
            if (PropertyFilters.TryGetValue(propertyKey, out var value) && value is T typedValue)
            {
                return typedValue;
            }
            return default;
        }

        /// <summary>
        /// Removes a property filter
        /// </summary>
        public void RemovePropertyFilter(string propertyKey)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(propertyKey);
            PropertyFilters.Remove(propertyKey);
        }

        /// <summary>
        /// Creates a filter model from configuration and filter values
        /// </summary>
        public static ProductFilterModel CreateFromConfiguration(
            ProductPropertyNormalizationConfiguration config,
            Dictionary<string, object?>? filterValues = null)
        {
            ArgumentNullException.ThrowIfNull(config);

            var model = new ProductFilterModel();

            if (filterValues != null)
            {
                foreach (var kvp in filterValues)
                {
                    model.SetPropertyFilter(kvp.Key, kvp.Value);
                }
            }

            return model;
        }
    }


    /// <summary>
    /// Dynamic sorting options that adapt to configured sortable properties
    /// </summary>
    public class ProductSortModel
    {
        public string SortBy { get; set; } = "Name";
        public SortDirection Direction { get; set; } = SortDirection.Ascending;

        /// <summary>
        /// Creates sort model from configuration
        /// </summary>
        public static ProductSortModel CreateFromConfiguration(
            ProductPropertyNormalizationConfiguration config,
            string? sortBy = null,
            SortDirection? direction = null)
        {
            ArgumentNullException.ThrowIfNull(config);

            var defaultSort = config.SortableProperties.FirstOrDefault() ?? 
                             new SortablePropertyDefinition { Key = "Name", DefaultDirection = SortDirection.Ascending };

            return new ProductSortModel
            {
                SortBy = sortBy ?? defaultSort.Key,
                Direction = direction ?? defaultSort.DefaultDirection
            };
        }
    }

    public enum SortDirection
    {
        Ascending,
        Descending
    }

    /// <summary>
    /// Dynamic result model that adapts to configured properties
    /// </summary>
    public class ProductResult
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string EAN { get; set; } = string.Empty;
        public string? Description { get; set; }
        
        /// <summary>
        /// Dynamic properties from ProductEntity.DynamicProperties
        /// </summary>
        public Dictionary<string, object?> Properties { get; set; } = new();
        
        /// <summary>
        /// Gets a typed property value
        /// </summary>
        public T? GetProperty<T>(string propertyKey)
        {
            if (Properties.TryGetValue(propertyKey, out var value) && value is T typedValue)
            {
                return typedValue;
            }
            return default;
        }

        /// <summary>
        /// Sets a property value
        /// </summary>
        public void SetProperty(string propertyKey, object? value)
        {
            Properties[propertyKey] = value;
        }
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
