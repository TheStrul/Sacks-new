using SacksDataLayer.Entities;

namespace SacksDataLayer.FileProcessing.Models
{

    /// <summary>
    /// Represents the result of processing with supplier offer insights and relational entities
    /// </summary>
    public class ProcessingResult
    {
        /// <summary>
        /// Supplier offer metadata (one per file processing session)
        /// </summary>
        public SupplierOfferAnnex SupplierOffer { get; set; } = new();

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
        
        
        // Stage 2 specific metrics - Enhanced for relational architecture
        public int PricingRecordsProcessed { get; set; }
        public int StockRecordsProcessed { get; set; }
        public int OrphanedCommercialRecords { get; set; }
        public int OfferProductsCreated { get; set; }
        public int SupplierOffersCreated { get; set; }
        
        public TimeSpan ProcessingTime { get; set; }
    }

    /// <summary>
    /// Context information for processing decisions
    /// </summary>
    public class ProcessingContext
    {
        public string SourceFileName { get; set; } = string.Empty;
        public string SupplierName { get; set; } = string.Empty;
        public DateTime ProcessingDate { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> AdditionalContext { get; set; } = new();
        
        /// <summary>
        /// User-specified intent for this processing session
        /// </summary>
        public string ProcessingIntent { get; set; } = string.Empty;
        
        /// <summary>
        /// Optional offer metadata for commercial processing
        /// </summary>
        public SupplierOfferAnnex? SupplierOffer { get; set; }
    }
}
