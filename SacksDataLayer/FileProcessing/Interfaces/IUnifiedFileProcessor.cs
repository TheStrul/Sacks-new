using SacksDataLayer.FileProcessing.Models;
using System.Threading.Tasks;

namespace SacksDataLayer.FileProcessing.Interfaces
{
    /// <summary>
    /// Interface for unified file processing operations
    /// </summary>
    public interface IUnifiedFileProcessor
    {
        /// <summary>
        /// Processes a supplier data file and imports it into the database
        /// </summary>
        /// <param name="filePath">Path to the file to process</param>
        /// <returns>Processing result with statistics</returns>
        Task<ProcessingResult> ProcessFileAsync(string filePath);
    }
}
