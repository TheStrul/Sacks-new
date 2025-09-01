using SacksDataLayer.Services.Interfaces;

namespace SacksDataLayer.Services.Implementations
{
    /// <summary>
    /// 🚀 PERFORMANCE OPTIMIZED: Thread-safe file processing service using in-memory data cache
    /// </summary>
    public class ThreadSafeFileProcessingService : IThreadSafeFileProcessingService
    {
        private readonly IInMemoryDataService _inMemoryDataService;
        private readonly IFileProcessingService _fileProcessingService;

        public ThreadSafeFileProcessingService(
            IInMemoryDataService inMemoryDataService,
            IFileProcessingService fileProcessingService)
        {
            _inMemoryDataService = inMemoryDataService ?? throw new ArgumentNullException(nameof(inMemoryDataService));
            _fileProcessingService = fileProcessingService ?? throw new ArgumentNullException(nameof(fileProcessingService));
        }

        /// <summary>
        /// 🚀 THREAD-SAFE: Processes a supplier file with maximum performance using in-memory data cache
        /// </summary>
        public async Task ProcessSupplierFileThreadSafeAsync(string filePath)
        {
            try
            {
                Console.WriteLine("=== 🚀 Thread-Safe File Processing ===\n");
                Console.WriteLine($"📁 Processing file: {Path.GetFileName(filePath)}");

                // Step 1: Load all data into memory for thread-safe access
                Console.WriteLine("🔄 Loading database into memory for thread-safe processing...");
                await _inMemoryDataService.LoadAllDataAsync();

                var cacheStats = _inMemoryDataService.GetCacheStats();
                Console.WriteLine($"✅ In-memory cache ready: {cacheStats.Products:N0} products, {cacheStats.Suppliers:N0} suppliers");

                // Step 2: Use the existing file processing service (which is already optimized)
                Console.WriteLine("🔄 Processing file using optimized service...");
                await _fileProcessingService.ProcessFileAsync(filePath);

                // Step 3: Show cache statistics after processing
                var finalStats = _inMemoryDataService.GetCacheStats();
                Console.WriteLine($"\n� Final cache stats: {finalStats.Products:N0} products, {finalStats.Suppliers:N0} suppliers");
                Console.WriteLine($"   Last loaded: {finalStats.LastLoaded:yyyy-MM-dd HH:mm:ss}");

                Console.WriteLine("\n✅ Thread-safe file processing completed successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error during thread-safe processing: {ex.Message}");
                Console.WriteLine($"   Stack trace: {ex.StackTrace}");
            }
        }
    }
}
