using System.Threading;
using System.Threading.Tasks;

namespace SacksDataLayer.Services.Interfaces
{
    /// <summary>
    /// Service interface for unified file processing operations
    /// </summary>
    public interface IFileProcessingService
    {
        /// <summary>
        /// Processes a file (Excel, CSV, etc.) and imports data based on supplier configuration
        /// </summary>
        /// <param name="filePath">Path to the file to process</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task representing the async operation</returns>
        Task ProcessFileAsync(string filePath, CancellationToken cancellationToken = default);
    }
}
