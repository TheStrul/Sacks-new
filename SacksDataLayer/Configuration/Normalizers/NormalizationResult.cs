namespace SacksDataLayer.Configuration.Normalizers;

/// <summary>
/// Result of processing a row that contains both product and supplier offer information
/// Enhanced for Phase 2 relational architecture
/// </summary>
public class NormalizationResult
{
    /// <summary>
    /// Core product data (with core properties only)
    /// </summary>
    public ProductEntity Product { get; set; } = null!;
    
    /// <summary>
    /// Supplier offer metadata (catalog/price list information)
    /// </summary>
    public SupplierOfferEntity? SupplierOffer { get; set; }
    
    /// <summary>
    /// Product-offer junction entity with pricing and offer-specific data
    /// </summary>
    public OfferProductEntity? OfferProduct { get; set; }
    
    /// <summary>
    /// Indicates if this row contains offer-specific properties
    /// </summary>
    public bool HasOfferProperties { get; set; }
    
    /// <summary>
    /// Raw offer properties extracted from the row (before OfferProduct creation)
    /// </summary>
    public Dictionary<string, object?> OfferProperties { get; set; } = new();
    
    /// <summary>
    /// Row processing metadata for debugging and validation
    /// </summary>
    public int RowIndex { get; set; }
    
    /// <summary>
    /// Indicates the processing mode used for this normalization
    /// </summary>
    public SacksDataLayer.FileProcessing.Models.ProcessingMode ProcessingMode { get; set; }
}
