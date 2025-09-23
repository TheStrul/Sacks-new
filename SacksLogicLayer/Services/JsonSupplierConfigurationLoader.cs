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
        public async Task<SuppliersConfiguration> LoadAsync(string supplierFormatsPath)
        {
            if (string.IsNullOrWhiteSpace(supplierFormatsPath)) throw new ArgumentException("supplierFormatsPath is required", nameof(supplierFormatsPath));

            _logger?.LogDebug("Loading supplier formats from {Path}", supplierFormatsPath);
            if (!File.Exists(supplierFormatsPath)) throw new FileNotFoundException("Supplier formats file not found", supplierFormatsPath);
            var supplierJson = await File.ReadAllTextAsync(supplierFormatsPath);


            var suppliersConfig = JsonSerializer.Deserialize<SuppliersConfiguration>(supplierJson, s_jsonOptions)
                                  ?? throw new InvalidOperationException("Failed to deserialize supplier-formats.json");


            // Ensure suppliers ParentConfiguration references are set (defensive)
            foreach (var s in suppliersConfig.Suppliers)
            {
                s.ParentConfiguration = suppliersConfig;
                // merge lookups into each supplier's parser config
                s.ParserConfig!.DoMergeLoookUpTables(suppliersConfig.Lookups);
            }

            // Basic validation
            if (suppliersConfig.Suppliers == null || !suppliersConfig.Suppliers.Any())
            {
                throw new InvalidOperationException("No suppliers found in supplier-formats.json");
            }

            _logger?.LogInformation("Loaded {Count} suppliers and {LookupCount} lookup tables", suppliersConfig.Suppliers.Count, suppliersConfig.Lookups?.Count ?? 0);

            return suppliersConfig;
        }
    }
}
