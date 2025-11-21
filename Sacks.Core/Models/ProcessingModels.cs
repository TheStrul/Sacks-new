using SacksAIPlatform.InfrastructuresLayer.FileProcessing;

using Sacks.Core.Entities;
using Sacks.Core.FileProcessing.Configuration;

namespace Sacks.Core.FileProcessing.Models
{

    /// <summary>
    /// Represents the result of processing with supplier offer insights and relational entities
    /// </summary>
    public class ProcessingResult
    {
        /// <summary>
        /// Supplier offer metadata (one per file processing session)
        /// </summary>
        public Offer? SupplierOffer { get; set; }

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
        public int TotalRowsInFile { get; set; }

        public int TotalTitlesRows { get; set; }

        public int TotalDataRows { get; set; }

        public int TotalEmptyRows { get; set; }
       
        public int ProductsCreated { get; set; }
        public int ProductsUpdated { get; set; }
        public int ProductsSkipped { get; set; }
        public int ErrorCount { get; set; }
        public int WarningCount { get; set; }
                
        public int OfferProductsCreated { get; set; }        
    }

    /// <summary>
    /// Context information for processing decisions
    /// </summary>
    public class ProcessingContext
    {
        required public FileData FileData { get; init; }
        
        required public SupplierConfiguration SupplierConfiguration { get; init; }

        public string CorrelationId { get; init; } = Guid.NewGuid().ToString();

        required public ProcessingResult ProcessingResult { get; init; }
    }
}
