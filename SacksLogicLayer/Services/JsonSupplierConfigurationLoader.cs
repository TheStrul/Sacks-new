using System.Text.Json;
using Microsoft.Extensions.Logging;
using SacksDataLayer.FileProcessing.Configuration;

namespace SacksLogicLayer.Services
{
    /// <summary>
    /// Loads and validates supplier configuration JSON files (supplier-formats.json and perfume-property-normalization.json)
    /// and exposes a populated SuppliersConfiguration where Lookups are taken from perfume normalization's valueMappings.
    /// </summary>
    public class JsonSupplierConfigurationLoader
    {
        private static readonly JsonSerializerOptions s_jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };

        private readonly ILogger _logger;

        public JsonSupplierConfigurationLoader(ILogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Load supplier configuration and perfume normalization files from disk, validate and merge.
        /// </summary>
        /// <param name="supplierFormatsPath">Path to supplier-formats.json</param>
        /// <returns>Populated SuppliersConfiguration</returns>
        internal async Task<SuppliersConfiguration> LoadAsync(string supplierFormatsPath)
        {
            SuppliersConfiguration retval = new SuppliersConfiguration();
            try
            {
                _logger?.LogDebug("Loading supplier formats from {Path}", supplierFormatsPath);

                var supplierJson = await File.ReadAllTextAsync(supplierFormatsPath).ConfigureAwait(false);

                // In dev mode we expect canonical JSON shape; deserialize directly without legacy normalization
                retval = JsonSerializer.Deserialize<SuppliersConfiguration>(supplierJson, s_jsonOptions)!;
                retval!.FullPath = supplierFormatsPath;

                var errors = retval.ValidateConfiguration();

                // Validate in-memory configuration if it was loaded
                if (errors != null && errors.Count > 0)
                {
                    foreach (var item in errors)
                    {
                        _logger!.LogError("Supplier configuration validation error: {Error}", item);
                    }
                }


                _logger?.LogInformation("Loaded {Count} suppliers and {LookupCount} lookup tables", retval.Suppliers.Count, retval.Lookups?.Count ?? 0);
            }
            catch (Exception ex)
            {
                _logger!.LogError(ex, "Error reading supplier formats file at {Path}", supplierFormatsPath);
                throw;

            }
            return retval;
        }
    }
}
