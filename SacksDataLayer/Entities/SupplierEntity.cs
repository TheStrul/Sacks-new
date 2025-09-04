using System.ComponentModel.DataAnnotations;

namespace SacksDataLayer.Entities;

public class SupplierEntity : Entity
{
    [MaxLength(100)]
    public required string Name { get; set; }
    
    [MaxLength(500)]
    public string? Description { get; set; }
        
    // Navigation property
    public virtual ICollection<SupplierOfferEntity> Offers { get; set; } = new List<SupplierOfferEntity>();
}
