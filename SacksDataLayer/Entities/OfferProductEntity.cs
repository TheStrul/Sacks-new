using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace SacksDataLayer.Entities;

/// <summary>
/// Junction table linking offers to products with specific pricing and terms
/// </summary>
public class OfferProductEntity : Entity
{
    // Foreign Keys
    public int OfferId { get; set; } 
    public int ProductId { get; set; }
    
    // Product-specific pricing and terms within this offer
    [Column(TypeName = "decimal(18,2)")]
    public decimal? Price { get; set; }
    
    [MaxLength(50)]
    public string? Capacity { get; set; }
    
    [Column(TypeName = "decimal(18,4)")]
    public decimal? Discount { get; set; }
    
    [MaxLength(50)]
    public string? UnitOfMeasure { get; set; }
    
    public int? MinimumOrderQuantity { get; set; }
    public int? MaximumOrderQuantity { get; set; }
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal? ListPrice { get; set; }
    
    public bool IsAvailable { get; set; } = true;
    
    [MaxLength(255)]
    public string? Notes { get; set; }
    
    // Additional product-specific properties as JSON
    [Column(TypeName = "nvarchar(max)")]
    public string? ProductPropertiesJson { get; set; }
    
    // In-memory dictionary for additional properties (not mapped to database)
    [NotMapped]
    public Dictionary<string, object?> ProductProperties { get; set; } = new();
    
    // Navigation properties
    public virtual SupplierOfferEntity Offer { get; set; } = null!;
    public virtual ProductEntity Product { get; set; } = null!;
    
    // Methods to handle dynamic properties
    public void SetProductProperty(string key, object? value)
    {
        ProductProperties[key] = value;
        SerializeProductProperties();
    }
    
    public T? GetProductProperty<T>(string key)
    {
        if (ProductProperties.TryGetValue(key, out var value) && value != null)
        {
            if (value is JsonElement element)
            {
                return JsonSerializer.Deserialize<T>(element.GetRawText());
            }
            return (T)Convert.ChangeType(value, typeof(T));
        }
        return default;
    }
    
    public void SerializeProductProperties()
    {
        ProductPropertiesJson = ProductProperties.Count > 0 
            ? JsonSerializer.Serialize(ProductProperties) 
            : null;
    }
    
    public void DeserializeProductProperties()
    {
        if (!string.IsNullOrEmpty(ProductPropertiesJson))
        {
            ProductProperties = JsonSerializer.Deserialize<Dictionary<string, object?>>(ProductPropertiesJson) 
                             ?? new Dictionary<string, object?>();
        }
    }
}
