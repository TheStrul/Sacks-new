namespace Sacks.LogicLayer.Services.Interfaces
{
    using Sacks.Core.FileProcessing.Configuration;

    /// <summary>
    /// Service interface for supplier configuration management
    /// </summary>
    public interface ISupplierConfigurationService
    {
        /// <summary>
        /// Auto-detects supplier configuration from file path/name
        /// </summary>
        /// <param name="filePath">Path to the file to analyze</param>
        /// <returns>Supplier configuration if found, null otherwise</returns>
        SupplierConfiguration? DetectSupplierFromFileAsync(string filePath);

        /// <summary>
        /// Gets all supplier configurations
        /// </summary>
        /// <returns>All supplier configurations</returns>
        Task<ISuppliersConfiguration> GetAllConfigurationsAsync();
    }
}
