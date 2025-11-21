using Sacks.Core.FileProcessing.Models;

namespace Sacks.Core.FileProcessing.Interfaces
{
    /// <summary>
    /// Interface for normalizing supplier-specific data into Product for supplier offers
    /// </summary>
    public interface IOfferNormalizer
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
        /// Normalizes supplier data into Product objects for supplier offers
        /// </summary>
        /// <param name="context">Processing context including additional parameters</param>
        /// <returns>Processing result with normalized Product objects and statistics</returns>
        Task NormalizeAsync(ProcessingContext context);

    }
}
