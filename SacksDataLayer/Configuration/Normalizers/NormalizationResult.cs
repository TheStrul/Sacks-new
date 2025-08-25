namespace SacksDataLayer.Configuration.Normalizers;

/// <summary>
/// Result of processing a row that contains both product and supplier offer information
/// </summary>
public class NormalizationResult
{
    public ProductEntity Product { get; set; } = null!;
    public SupplierOfferEntity? SupplierOffer { get; set; }
    public bool HasOfferProperties { get; set; }
    public Dictionary<string, object?> OfferProperties { get; set; } = new();
}
