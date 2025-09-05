namespace SacksDataLayer.DTOs
{
    /// <summary>
    /// DTO for ProductPropertiesView data
    /// </summary>
    public class ProductViewDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? EAN { get; set; }
        
        // Dynamic properties as typed fields
        public string? Brand { get; set; }
        public string? Gender { get; set; }
        public string? Size { get; set; }
        public string? Concentration { get; set; }
        public string? FragranceFamily { get; set; }
        public string? ProductLine { get; set; }
        
        // Computed fields
        public int? SizeNumeric { get; set; }
        public string? GenderCode { get; set; }
        
        // Timestamps
        public DateTime CreatedAt { get; set; }
        public DateTime ModifiedAt { get; set; }
    }

    /// <summary>
    /// DTO for ProductOffersView data
    /// </summary>
    public class ProductOfferViewDto
    {
        // Product information
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? EAN { get; set; }
        
        // Supplier and offer information
        public int SupplierId { get; set; }
        public string SupplierName { get; set; } = string.Empty;
        public int OfferId { get; set; }
        public string? OfferName { get; set; }
        
        // Pricing information
        public decimal? Price { get; set; }
        public string? Currency { get; set; }
        public int? Quantity { get; set; }
        
        // Dynamic properties
        public string? Brand { get; set; }
        public string? Gender { get; set; }
        public string? Size { get; set; }
        public string? Concentration { get; set; }
        public string? FragranceFamily { get; set; }
        
        // Computed fields
        public int? SizeNumeric { get; set; }
        public decimal? PricePerMl { get; set; }
    }

    /// <summary>
    /// DTO for brand statistics
    /// </summary>
    public class BrandStatisticsDto
    {
        public string Brand { get; set; } = string.Empty;
        public int ProductCount { get; set; }
        public int GenderCount { get; set; }
        public int FragranceFamilyCount { get; set; }
        public decimal? AvgSize { get; set; }
        public int? MinSize { get; set; }
        public int? MaxSize { get; set; }
    }

    /// <summary>
    /// DTO for best price information
    /// </summary>
    public class BestPriceDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? Brand { get; set; }
        public string? Gender { get; set; }
        public string? Size { get; set; }
        public int SupplierId { get; set; }
        public string SupplierName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string? Currency { get; set; }
        public decimal? PricePerMl { get; set; }
    }

    /// <summary>
    /// DTO for search/filter parameters
    /// </summary>
    public class ProductSearchDto
    {
        public string? Brand { get; set; }
        public string? Gender { get; set; }
        public string? FragranceFamily { get; set; }
        public string? Concentration { get; set; }
        public int? MinSize { get; set; }
        public int? MaxSize { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public string? Currency { get; set; }
        public string? SupplierName { get; set; }
        public string? SearchText { get; set; }
        public string? OrderBy { get; set; }
        public int? Take { get; set; }
        public int Skip { get; set; } = 0;
    }

    /// <summary>
    /// DTO for product property summary
    /// </summary>
    public class ProductPropertySummaryDto
    {
        public string PropertyName { get; set; } = string.Empty;
        public List<PropertyValueCount> Values { get; set; } = new List<PropertyValueCount>();
    }

    /// <summary>
    /// Count of products for a specific property value
    /// </summary>
    public class PropertyValueCount
    {
        public string Value { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    /// <summary>
    /// DTO for products with multiple supplier offers
    /// </summary>
    public class ProductWithMultipleSuppliersDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? EAN { get; set; }
        public string? Brand { get; set; }
        public string? Gender { get; set; }
        public string? Size { get; set; }
        public string? Concentration { get; set; }
        public int SupplierCount { get; set; }
        public int OfferCount { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public decimal? PriceDifference { get; set; }
        public decimal? PriceDifferencePercentage { get; set; }
        public string? Currency { get; set; }
        public List<SupplierOfferSummaryDto> Offers { get; set; } = new List<SupplierOfferSummaryDto>();
    }

    /// <summary>
    /// DTO for supplier offer summary
    /// </summary>
    public class SupplierOfferSummaryDto
    {
        public int SupplierId { get; set; }
        public string SupplierName { get; set; } = string.Empty;
        public int OfferId { get; set; }
        public string? OfferName { get; set; }
        public decimal? Price { get; set; }
        public string? Currency { get; set; }
        public int? Quantity { get; set; }
        public decimal? PricePerMl { get; set; }
        public bool IsLowestPrice { get; set; }
        public bool IsHighestPrice { get; set; }
    }
}
