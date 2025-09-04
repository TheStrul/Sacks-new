using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SacksDataLayer.Entities;

/// <summary>
/// Represents a supplier's offer/catalog (like a price list or promotion)
/// </summary>
public class SupplierOfferEntity : Entity
{
    // Foreign Key
    public int SupplierId { get; set; }
    
    // Offer metadata
    [MaxLength(255)]
    public string? OfferName { get; set; }
    
    [MaxLength(500)]
    public string? Description { get; set; }
    
    [MaxLength(20)]
    public string? Currency { get; set; }
    
    // Offer validity period
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
    
    // Offer status and type
    public bool IsActive { get; set; } = true;
    
    [MaxLength(50)]
    public string? OfferType { get; set; }
    
    [MaxLength(20)]
    public string? Version { get; set; }
        
    // Navigation properties
    public virtual SupplierEntity Supplier { get; set; } = null!;
    public virtual ICollection<OfferProductEntity> OfferProducts { get; set; } = new List<OfferProductEntity>();
}
