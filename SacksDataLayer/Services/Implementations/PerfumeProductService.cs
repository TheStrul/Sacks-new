using Microsoft.EntityFrameworkCore;
using SacksDataLayer.Data;
using SacksDataLayer.Entities;
using SacksDataLayer.Models;
using SacksDataLayer.Services.Interfaces;
using SacksDataLayer.Configuration;
using System.Linq.Expressions;
using System.Text.Json;

namespace SacksDataLayer.Services.Implementations
{
    /// <summary>
    /// Perfume-specific product service that maps dynamic properties to familiar filtering/sorting
    /// Includes property normalization for handling data variations
    /// </summary>
    public class PerfumeProductService : IPerfumeProductService
    {
        private readonly SacksDbContext _context;
        private readonly PropertyNormalizer _normalizer;

        public PerfumeProductService(SacksDbContext context, PropertyNormalizer normalizer)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _normalizer = normalizer ?? throw new ArgumentNullException(nameof(normalizer));
        }

        public async Task<PaginatedResult<PerfumeProductResult>> SearchPerfumeProductsAsync(
            PerfumeFilterModel filter,
            PerfumeSortModel sort,
            int pageNumber = 1,
            int pageSize = 50,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(filter);
            ArgumentNullException.ThrowIfNull(sort);

            // Validate pagination
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 50;
            if (pageSize > 1000) pageSize = 1000; // Limit max page size

            var query = _context.Products.AsNoTracking();

            // Apply filters
            query = ApplyFilters(query, filter);

            // Get total count before applying pagination
            var totalCount = await query.CountAsync(cancellationToken);

            // Apply sorting
            query = ApplySorting(query, sort);

            // Apply pagination
            var skip = (pageNumber - 1) * pageSize;
            var products = await query
                .Skip(skip)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            // Map to result objects
            var mappedResults = products.Select(MapToResult).ToList();

            return new PaginatedResult<PerfumeProductResult>
            {
                Items = mappedResults,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        public async Task<PerfumeFilterValues> GetAvailableFilterValuesAsync(CancellationToken cancellationToken = default)
        {
            // Get all products with dynamic properties
            var productsWithProperties = await _context.Products
                .AsNoTracking()
                .Where(p => p.DynamicPropertiesJson != null)
                .Select(p => p.DynamicPropertiesJson!)
                .ToListAsync(cancellationToken);

            // Parse and normalize all properties
            var allNormalizedProperties = new List<Dictionary<string, object?>>();
            
            foreach (var jsonProps in productsWithProperties)
            {
                try
                {
                    var props = JsonSerializer.Deserialize<Dictionary<string, object?>>(jsonProps);
                    if (props != null)
                    {
                        var normalized = _normalizer.NormalizeProperties(props);
                        allNormalizedProperties.Add(normalized);
                    }
                }
                catch (JsonException)
                {
                    // Skip invalid JSON
                    continue;
                }
            }

            // Extract unique normalized values for each property
            var genders = ExtractUniqueValues(allNormalizedProperties, "Gender");
            var sizes = ExtractUniqueValues(allNormalizedProperties, "Size");
            var concentrations = ExtractUniqueValues(allNormalizedProperties, "Concentration");
            var brands = ExtractUniqueValues(allNormalizedProperties, "Brand");
            var productLines = ExtractUniqueValues(allNormalizedProperties, "ProductLine");
            var fragranceFamilies = ExtractUniqueValues(allNormalizedProperties, "FragranceFamily");

            return new PerfumeFilterValues
            {
                Genders = genders.OrderBy(g => g).ToList(),
                Sizes = sizes.OrderBy(s => s).ToList(),
                Concentrations = concentrations.OrderBy(c => c).ToList(),
                Brands = brands.OrderBy(b => b).ToList(),
                ProductLines = productLines.OrderBy(pl => pl).ToList(),
                FragranceFamilies = fragranceFamilies.OrderBy(ff => ff).ToList()
            };
        }

        public async Task<PerfumeProductResult?> GetPerfumeProductAsync(int id, CancellationToken cancellationToken = default)
        {
            var product = await _context.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

            return product != null ? MapToResult(product) : null;
        }

        public async Task<PerfumeProductResult?> GetPerfumeProductByEANAsync(string ean, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(ean);

            var product = await _context.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.EAN == ean, cancellationToken);

            return product != null ? MapToResult(product) : null;
        }

        #region Private Helper Methods

        private IQueryable<ProductEntity> ApplyFilters(IQueryable<ProductEntity> query, PerfumeFilterModel filter)
        {
            // Text search
            if (!string.IsNullOrWhiteSpace(filter.SearchText))
            {
                var searchLower = filter.SearchText.ToLower();
                query = query.Where(p => 
                    p.Name.ToLower().Contains(searchLower) ||
                    (p.Description != null && p.Description.ToLower().Contains(searchLower)));
            }

            // For normalized property filtering, we need to search for all possible variations
            // Gender filter - search for all variations that normalize to the requested value
            if (!string.IsNullOrWhiteSpace(filter.Gender))
            {
                var genderVariations = GetAllVariationsForNormalizedValue("Gender", filter.Gender);
                if (genderVariations.Any())
                {
                    query = query.Where(p => 
                        p.DynamicPropertiesJson != null && 
                        genderVariations.Any(variation => p.DynamicPropertiesJson.Contains($"\"{variation}\"")));
                }
            }

            // Size filter
            if (!string.IsNullOrWhiteSpace(filter.Size))
            {
                var sizeVariations = GetAllVariationsForNormalizedValue("Size", filter.Size);
                if (sizeVariations.Any())
                {
                    query = query.Where(p => 
                        p.DynamicPropertiesJson != null && 
                        sizeVariations.Any(variation => p.DynamicPropertiesJson.Contains($"\"{variation}\"")));
                }
            }

            // Concentration filter
            if (!string.IsNullOrWhiteSpace(filter.Concentration))
            {
                var concentrationVariations = GetAllVariationsForNormalizedValue("Concentration", filter.Concentration);
                if (concentrationVariations.Any())
                {
                    query = query.Where(p => 
                        p.DynamicPropertiesJson != null && 
                        concentrationVariations.Any(variation => p.DynamicPropertiesJson.Contains($"\"{variation}\"")));
                }
            }

            // Brand filter
            if (!string.IsNullOrWhiteSpace(filter.Brand))
            {
                var brandVariations = GetAllVariationsForNormalizedValue("Brand", filter.Brand);
                if (brandVariations.Any())
                {
                    query = query.Where(p => 
                        p.DynamicPropertiesJson != null && 
                        brandVariations.Any(variation => p.DynamicPropertiesJson.Contains($"\"{variation}\"")));
                }
            }

            // Product Line filter
            if (!string.IsNullOrWhiteSpace(filter.ProductLine))
            {
                var lineVariations = GetAllVariationsForNormalizedValue("ProductLine", filter.ProductLine);
                if (lineVariations.Any())
                {
                    query = query.Where(p => 
                        p.DynamicPropertiesJson != null && 
                        lineVariations.Any(variation => p.DynamicPropertiesJson.Contains($"\"{variation}\"")));
                }
            }

            // Fragrance Family filter
            if (!string.IsNullOrWhiteSpace(filter.FragranceFamily))
            {
                var familyVariations = GetAllVariationsForNormalizedValue("FragranceFamily", filter.FragranceFamily);
                if (familyVariations.Any())
                {
                    query = query.Where(p => 
                        p.DynamicPropertiesJson != null && 
                        familyVariations.Any(variation => p.DynamicPropertiesJson.Contains($"\"{variation}\"")));
                }
            }

            return query;
        }

        private IQueryable<ProductEntity> ApplySorting(IQueryable<ProductEntity> query, PerfumeSortModel sort)
        {
            return sort.SortBy switch
            {
                PerfumeSortField.Name => sort.Direction == SortDirection.Ascending 
                    ? query.OrderBy(p => p.Name) 
                    : query.OrderByDescending(p => p.Name),
                
                PerfumeSortField.CreatedAt => sort.Direction == SortDirection.Ascending 
                    ? query.OrderBy(p => p.CreatedAt) 
                    : query.OrderByDescending(p => p.CreatedAt),
                
                PerfumeSortField.UpdatedAt => sort.Direction == SortDirection.Ascending 
                    ? query.OrderBy(p => p.ModifiedAt) 
                    : query.OrderByDescending(p => p.ModifiedAt),
                
                // For dynamic properties, we'll sort by the JSON contains - not ideal but functional
                PerfumeSortField.Brand => sort.Direction == SortDirection.Ascending 
                    ? query.OrderBy(p => p.DynamicPropertiesJson) 
                    : query.OrderByDescending(p => p.DynamicPropertiesJson),
                
                _ => query.OrderBy(p => p.Name) // Default sort
            };
        }

        private PerfumeProductResult MapToResult(ProductEntity product)
        {
            var result = new PerfumeProductResult
            {
                Id = product.Id,
                Name = product.Name,
                EAN = product.EAN,
                Description = product.Description,
                CreatedAt = product.CreatedAt,
                UpdatedAt = product.ModifiedAt ?? product.CreatedAt
            };

            // Map and normalize dynamic properties if they exist
            if (!string.IsNullOrWhiteSpace(product.DynamicPropertiesJson))
            {
                try
                {
                    var dynamicProps = JsonSerializer.Deserialize<Dictionary<string, object?>>(product.DynamicPropertiesJson);
                    if (dynamicProps != null)
                    {
                        // Normalize properties first
                        var normalizedProps = _normalizer.NormalizeProperties(dynamicProps);
                        
                        result.Gender = GetStringValue(normalizedProps, "Gender");
                        result.Size = GetStringValue(normalizedProps, "Size");
                        result.Concentration = GetStringValue(normalizedProps, "Concentration");
                        result.Brand = GetStringValue(normalizedProps, "Brand");
                        result.ProductLine = GetStringValue(normalizedProps, "ProductLine");
                        result.FragranceFamily = GetStringValue(normalizedProps, "FragranceFamily");
                    }
                }
                catch (JsonException)
                {
                    // Log error if needed, but don't fail the mapping
                }
            }

            return result;
        }

        private string? GetStringValue(Dictionary<string, object?> properties, string key)
        {
            if (properties.TryGetValue(key, out var value))
            {
                return value?.ToString();
            }
            return null;
        }

        private List<string> ExtractUniqueValues(List<Dictionary<string, object?>> allProperties, string normalizedKey)
        {
            var values = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            
            foreach (var props in allProperties)
            {
                if (props.TryGetValue(normalizedKey, out var value))
                {
                    var stringValue = value?.ToString();
                    if (!string.IsNullOrWhiteSpace(stringValue))
                    {
                        values.Add(stringValue);
                    }
                }
            }
            
            return values.ToList();
        }

        private List<string> GetAllVariationsForNormalizedValue(string propertyKey, string normalizedValue)
        {
            // This would ideally be cached for performance
            var variations = new List<string> { normalizedValue };
            
            // Add reverse lookup - find all original values that normalize to this value
            // For now, we'll include the normalized value itself
            // In a production system, you'd want to maintain a reverse mapping cache
            
            return variations;
        }

        private string? JsonExtractValue(string json, string propertyName)
        {
            try
            {
                var props = JsonSerializer.Deserialize<Dictionary<string, object?>>(json);
                if (props?.TryGetValue(propertyName, out var value) == true)
                {
                    return value?.ToString();
                }
            }
            catch (JsonException)
            {
                // Ignore JSON parsing errors
            }
            return null;
        }

        #endregion
    }
}
