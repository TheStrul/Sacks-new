using SacksAIPlatform.InfrastructuresLayer.FileProcessing;

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
        required public SupplierOfferAnnex SupplierOffer { get; init; }

        public ProcessingStatistics Statistics { get; }  = new();
        public List<string> Warnings { get; } = new();
        public List<string> Errors { get; } = new();
        public DateTime ProcessedAt { get; } = DateTime.UtcNow;
        required public string SourceFile { get; init; }
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
        required public SupplierOfferAnnex SupplierOffer { get; init; }

        required public FileData FileData { get; init; }
    }
}
