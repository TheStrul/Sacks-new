using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SacksDataLayer.Entities;

/// <summary>
/// Represents a supplier's offer/catalog (like a price list or promotion)
/// </summary>
public class SupplierOfferAnnex : Annex
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
        
    // Navigation properties
    public virtual SupplierEntity Supplier { get; set; } = null!;
    public virtual ICollection<OfferProductAnnex> OfferProducts { get; set; } = new List<OfferProductAnnex>();
}
