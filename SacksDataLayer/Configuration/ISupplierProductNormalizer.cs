using SacksDataLayer.FileProcessing.Models;

namespace SacksDataLayer.FileProcessing.Interfaces
{
    /// <summary>
    /// Interface for normalizing supplier-specific data into ProductEntity with support for two-stage processing
    /// </summary>
    public interface ISupplierProductNormalizer
    {
        /// <summary>
        /// Gets the supplier name this normalizer handles
        /// </summary>
        string SupplierName { get; }

        /// <summary>
        /// Gets the supported processing modes for this supplier
        /// </summary>
        IEnumerable<ProcessingMode> SupportedModes { get; }

        /// <summary>
        /// Determines if this normalizer can handle the given file
        /// </summary>
        /// <param name="fileName">Name of the file</param>
        /// <param name="firstFewRows">First few rows of data to analyze</param>
        /// <returns>True if this normalizer can handle the file</returns>
        bool CanHandle(string fileName, IEnumerable<SacksAIPlatform.InfrastructuresLayer.FileProcessing.RowData> firstFewRows);

        /// <summary>
        /// Determines the recommended processing mode for the given file
        /// </summary>
        /// <param name="fileName">Name of the file</param>
        /// <param name="firstFewRows">First few rows of data to analyze</param>
        /// <returns>Recommended processing mode</returns>
        ProcessingMode RecommendProcessingMode(string fileName, IEnumerable<SacksAIPlatform.InfrastructuresLayer.FileProcessing.RowData> firstFewRows);

        /// <summary>
        /// Normalizes supplier data into ProductEntity objects using the specified processing mode
        /// </summary>
        /// <param name="fileData">Raw file data</param>
        /// <param name="context">Processing context including mode and additional parameters</param>
        /// <returns>Processing result with normalized ProductEntity objects and statistics</returns>
        Task<ProcessingResult> NormalizeAsync(SacksAIPlatform.InfrastructuresLayer.FileProcessing.FileData fileData, ProcessingContext context);

        /// <summary>
        /// Normalizes supplier data using default processing mode (for backward compatibility)
        /// </summary>
        /// <param name="fileData">Raw file data</param>
        /// <returns>Collection of normalized ProductEntity objects</returns>
        Task<IEnumerable<ProductEntity>> NormalizeAsync(SacksAIPlatform.InfrastructuresLayer.FileProcessing.FileData fileData);

        /// <summary>
        /// Gets the expected column mapping for this supplier based on processing mode
        /// </summary>
        /// <param name="mode">Processing mode to get mappings for</param>
        /// <returns>Dictionary mapping supplier column names to standard property names</returns>
        Dictionary<string, string> GetColumnMapping(ProcessingMode mode = ProcessingMode.UnifiedProductCatalog);
    }
}