using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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
}
