using Microsoft.Extensions.Logging;

using SacksDataLayer.FileProcessing.Configuration;

using SacksLogicLayer.Services.Interfaces;

namespace SacksLogicLayer.Services.Implementations
{
    /// <summary>
    /// Service implementation for supplier configuration management
    /// </summary>
    public class SupplierConfigurationService : ISupplierConfigurationService
    {
        private readonly SupplierConfigurationManager _configurationManager;
        private readonly ILogger<SupplierConfigurationService> _logger;

        public SupplierConfigurationService(
            SupplierConfigurationManager configurationManager,
            ILogger<SupplierConfigurationService> logger)
        {
            _configurationManager = configurationManager ?? throw new ArgumentNullException(nameof(configurationManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Auto-detects supplier configuration from file path/name
        /// </summary>
        public async Task<SupplierConfiguration?> DetectSupplierFromFileAsync(string filePath)
        {
            try
            {
                _logger.LogDebug("Detecting supplier configuration for file: {FilePath}", filePath);
                var result = await _configurationManager.DetectSupplierFromFileAsync(filePath);
                
                if (result != null)
                {
                    _logger.LogDebug("Successfully detected supplier '{SupplierName}' for file: {FilePath}", 
                        result.Name, filePath);
                }
                else
                {
                    _logger.LogWarning("No supplier configuration detected for file: {FilePath}", filePath);
                }
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error detecting supplier configuration for file: {FilePath}", filePath);
                throw;
            }
        }

        /// <summary>
        /// Gets all supplier configurations
        /// </summary>
        public async Task<SuppliersConfiguration> GetAllConfigurationsAsync()
        {
            try
            {
                _logger.LogDebug("Getting all supplier configurations");
                return await _configurationManager.GetConfigurationAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all supplier configurations");
                throw;
            }
        }
    }
}
