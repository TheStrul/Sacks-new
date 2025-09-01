using SacksDataLayer.FileProcessing.Models;

namespace SacksDataLayer.Services.Interfaces
{
    /// <summary>
    /// 🚀 ULTIMATE PERFORMANCE: In-memory file processing service that loads all data into memory,
    /// processes everything in-memory, and saves only at the end in a single transaction
    /// </summary>
    public interface IInMemoryFileProcessingService
    {
        /// <summary>
        /// Processes a file completely in memory and saves all changes in a single transaction at the end
        /// </summary>
        /// <param name="filePath">Path to the file to process</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Processing results with performance metrics</returns>
        Task<InMemoryProcessingResult> ProcessFileInMemoryAsync(string filePath, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Results from in-memory file processing
    /// </summary>
    public class InMemoryProcessingResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<string> Errors { get; set; } = new();
        
        // Performance metrics
        public long LoadDataDurationMs { get; set; }
        public long ProcessingDurationMs { get; set; }
        public long SaveDataDurationMs { get; set; }
        public long TotalDurationMs { get; set; }
        
        // Processing statistics
        public int ProductsCreated { get; set; }
        public int ProductsUpdated { get; set; }
        public int OfferProductsCreated { get; set; }
        public int OfferProductsUpdated { get; set; }
        public int SuppliersCreated { get; set; }
        public int OffersCreated { get; set; }
        
        // Data counts
        public int TotalProductsInMemory { get; set; }
        public int TotalSuppliersInMemory { get; set; }
        public int TotalOffersInMemory { get; set; }
        public int TotalOfferProductsInMemory { get; set; }
        public int ProcessedRecords { get; set; }
    }
}
