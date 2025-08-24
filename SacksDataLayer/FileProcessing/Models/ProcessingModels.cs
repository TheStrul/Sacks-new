namespace SacksDataLayer.FileProcessing.Models
{
    /// <summary>
    /// Defines the two processing modes for file analysis
    /// </summary>
    public enum ProcessingMode
    {
        /// <summary>
        /// Stage 1: Focus on product identification and core attributes for unified catalog
        /// Ignores pricing, stock, and supplier-specific codes
        /// </summary>
        UnifiedProductCatalog,

        /// <summary>
        /// Stage 2: Focus on commercial data (pricing, stock) linked to unified products
        /// Prioritizes supplier-specific pricing and availability information
        /// </summary>
        SupplierCommercialData
    }

    /// <summary>
    /// Represents the result of processing with mode-specific insights
    /// </summary>
    public class ProcessingResult
    {
        public ProcessingMode Mode { get; set; }
        public IEnumerable<ProductEntity> Products { get; set; } = new List<ProductEntity>();
        public ProcessingStatistics Statistics { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public List<string> Errors { get; set; } = new();
        public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
        public string SourceFile { get; set; } = string.Empty;
        public string SupplierName { get; set; } = string.Empty;
    }

    /// <summary>
    /// Processing statistics specific to the chosen mode
    /// </summary>
    public class ProcessingStatistics
    {
        public int TotalRowsProcessed { get; set; }
        public int ProductsCreated { get; set; }
        public int ProductsSkipped { get; set; }
        public int ErrorCount { get; set; }
        public int WarningCount { get; set; }
        
        // Stage 1 specific metrics
        public int UniqueProductsIdentified { get; set; }
        public int DuplicateProductsDetected { get; set; }
        public int MissingCoreAttributes { get; set; }
        
        // Stage 2 specific metrics
        public int PricingRecordsProcessed { get; set; }
        public int StockRecordsProcessed { get; set; }
        public int ProductLinksEstablished { get; set; }
        public int OrphanedCommercialRecords { get; set; }
        
        public TimeSpan ProcessingTime { get; set; }
    }

    /// <summary>
    /// Context information for processing decisions
    /// </summary>
    public class ProcessingContext
    {
        public ProcessingMode Mode { get; set; }
        public string SourceFileName { get; set; } = string.Empty;
        public string SupplierName { get; set; } = string.Empty;
        public DateTime ProcessingDate { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> AdditionalContext { get; set; } = new();
        
        /// <summary>
        /// User-specified intent for this processing session
        /// </summary>
        public string ProcessingIntent { get; set; } = string.Empty;
    }
}
