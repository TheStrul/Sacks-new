using SacksDataLayer.FileProcessing.Configuration;
using System.Threading;
using System.Threading.Tasks;

namespace SacksDataLayer.Services.Interfaces
{
    /// <summary>
    /// Service interface for supplier configuration management
    /// </summary>
    public interface ISupplierConfigurationService
    {
        /// <summary>
        /// Auto-detects supplier configuration from file path/name
        /// </summary>
        /// <param name="filePath">Path to the file to analyze</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Supplier configuration if found, null otherwise</returns>
        SupplierConfiguration? DetectSupplierFromFileAsync(string filePath);

        /// <summary>
        /// Gets all supplier configurations
        /// </summary>
        /// <returns>All supplier configurations</returns>
        Task<SuppliersConfiguration> GetAllConfigurationsAsync();
    }
}
