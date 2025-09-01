using System.ComponentModel.DataAnnotations;

namespace SacksDataLayer.Entities;

public class SupplierEntity : Entity
{
    [MaxLength(100)]
    public required string Name { get; set; }
    
    [MaxLength(500)]
    public string? Description { get; set; }
    
    [MaxLength(100)]
    public string? Industry { get; set; }
    
    [MaxLength(100)]
    public string? Region { get; set; }
    
    [MaxLength(255)]
    public string? ContactName { get; set; }
    
    [MaxLength(255)]
    public string? ContactEmail { get; set; }
    
    [MaxLength(255)]
    public string? Company { get; set; }
    
    [MaxLength(50)]
    public string? FileFrequency { get; set; }
    
    public string? Notes { get; set; }
    
    // Navigation property
    public virtual ICollection<SupplierOfferEntity> Offers { get; set; } = new List<SupplierOfferEntity>();
}
