using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace SacksDataLayer.Entities;

/// <summary>
/// Represents a supplier's offer/catalog (like a price list or promotion)
/// Uses navigation property pattern for consistent FK management
/// </summary>
public class SupplierOfferAnnex : Annex
{
    // Foreign Key - EF Core will manage this via navigation properties
    public int SupplierId { get; set; }
    
    // Offer metadata
    [MaxLength(255)]
    required public string OfferName { get; init; }
    
    [MaxLength(500)]
    public string? Description { get; set; }

    [MaxLength(20)]
    required public string Currency { get; init; } = "USD";

    // Navigation properties - EF Core manages relationships through these
    // Make Supplier optional so the offer can be created using SupplierId only
    public virtual SupplierEntity? Supplier { get; set; }
    public virtual ICollection<OfferProductAnnex> OfferProducts { get; set; } = new List<OfferProductAnnex>();

    // Offer-level dynamic properties
    [Column(TypeName = "nvarchar(max)")]
    public string? OfferPropertiesJson { get; set; }

    [NotMapped]
    public Dictionary<string, object?> OfferProperties { get; set; } = new();

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
            try
            {
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                return default;
            }
        }
        return default;
    }

    public void SerializeOfferProperties()
    {
        OfferPropertiesJson = OfferProperties.Count > 0 ? JsonSerializer.Serialize(OfferProperties) : null;
    }

    public void DeserializeOfferProperties()
    {
        if (!string.IsNullOrEmpty(OfferPropertiesJson))
        {
            OfferProperties = JsonSerializer.Deserialize<Dictionary<string, object?>>(OfferPropertiesJson) ?? new Dictionary<string, object?>();
        }
    }
}
