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
        Task<SupplierConfiguration?> DetectSupplierFromFileAsync(string filePath, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets configuration for a specific supplier by name
        /// </summary>
        /// <param name="supplierName">Name of the supplier</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Supplier configuration if found, null otherwise</returns>
        Task<SupplierConfiguration?> GetSupplierConfigurationAsync(string supplierName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all supplier configurations
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>All supplier configurations</returns>
        Task<SuppliersConfiguration> GetAllConfigurationsAsync(CancellationToken cancellationToken = default);
    }
}
