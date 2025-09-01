namespace SacksDataLayer.Services.Interfaces
{
    /// <summary>
    /// 🚀 PERFORMANCE: Interface for thread-safe file processing using in-memory data cache
    /// </summary>
    public interface IThreadSafeFileProcessingService
    {
        /// <summary>
        /// Processes a supplier file with maximum performance using thread-safe in-memory data cache
        /// </summary>
        Task ProcessSupplierFileThreadSafeAsync(string filePath);
    }
}
