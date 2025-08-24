namespace SacksDataLayer.FileProcessing.Interfaces
{
    /// <summary>
    /// Interface for normalizing supplier-specific data into ProductEntity
    /// </summary>
    public interface ISupplierProductNormalizer
    {
        /// <summary>
        /// Gets the supplier name this normalizer handles
        /// </summary>
        string SupplierName { get; }

        /// <summary>
        /// Determines if this normalizer can handle the given file
        /// </summary>
        /// <param name="fileName">Name of the file</param>
        /// <param name="firstFewRows">First few rows of data to analyze</param>
        /// <returns>True if this normalizer can handle the file</returns>
        bool CanHandle(string fileName, IEnumerable<SacksAIPlatform.InfrastructuresLayer.FileProcessing.RowData> firstFewRows);

        /// <summary>
        /// Normalizes supplier data into ProductEntity objects
        /// </summary>
        /// <param name="fileData">Raw file data</param>
        /// <returns>Collection of normalized ProductEntity objects</returns>
        Task<IEnumerable<ProductEntity>> NormalizeAsync(SacksAIPlatform.InfrastructuresLayer.FileProcessing.FileData fileData);

        /// <summary>
        /// Gets the expected column mapping for this supplier
        /// </summary>
        /// <returns>Dictionary mapping supplier column names to standard property names</returns>
        Dictionary<string, string> GetColumnMapping();
    }
}