using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace SacksDataLayer.Entities;

/// <summary>
/// Junction table linking offers to products with specific pricing and terms
/// </summary>
public class OfferProductAnnex : Annex
{
    // Foreign Keys
    public int OfferId { get; set; } 
    public int ProductId { get; set; }
    
    // Product-specific pricing and terms within this offer
    [Column(TypeName = "decimal(18,2)")]
    public decimal? Price { get; set; }
    
    public int? Quantity { get; set; }
    
    /// <summary>
    /// Supplier's description of the product (supplier-specific)
    /// </summary>
    [StringLength(2000)]
    public string? Description { get; set; }
    
    // Additional product-specific properties as JSON
    [Column(TypeName = "nvarchar(max)")]
    public string? OfferPropertiesJson { get; set; }
    
    // In-memory dictionary for additional properties (not mapped to database)
    [NotMapped]
    public Dictionary<string, object?> OfferProperties { get; set; } = new();
    
    // Navigation properties
    public virtual SupplierOfferAnnex Offer { get; set; } = null!;
    public virtual ProductEntity Product { get; set; } = null!;
    
    // Methods to handle dynamic properties
    public void SetOfferProperty(string key, object? value)
    {
        OfferProperties[key] = value;
        SerializeOfferProperties();
    }
    
    public T? GetOfferProperty<T>(string key)
    {
        if (OfferProperties.TryGetValue(key, out var value) && value != null)
        {
            if (value is JsonElement element)
            {
                return JsonSerializer.Deserialize<T>(element.GetRawText());
            }
            return (T)Convert.ChangeType(value, typeof(T));
        }
        return default;
    }

    public void SerializeOfferProperties()
    {
        OfferPropertiesJson = OfferProperties.Count > 0 
            ? JsonSerializer.Serialize(OfferProperties) 
            : null;
    }

    public void DeserializeOfferProperties()
    {
        if (!string.IsNullOrEmpty(OfferPropertiesJson))
        {
            OfferProperties = JsonSerializer.Deserialize<Dictionary<string, object?>>(OfferPropertiesJson) 
                             ?? new Dictionary<string, object?>();
        }
    }
}
