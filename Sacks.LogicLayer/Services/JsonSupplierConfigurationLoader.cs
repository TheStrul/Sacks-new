using System.Text.Json;
using Microsoft.Extensions.Logging;
using Sacks.Core.FileProcessing.Configuration;

namespace Sacks.LogicLayer.Services
{
    /// <summary>
    /// Loads and validates supplier configuration JSON files.
    /// Mandatory main file: supplier-formats.json containing SuppliersConfiguration (Version, Lookups, optional Suppliers).
    /// Additional .json files in same folder may contain a single SupplierConfiguration only.
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


        private async Task<SuppliersConfiguration> LoadSuppliersConfigurationAsync(string filePath)
        {
            SuppliersConfiguration retval = new SuppliersConfiguration();
            try
            {
                var fileName = Path.GetFileName(filePath);
                _logger?.LogInformation("Loading main suppliers configuration from {Path}", filePath);

                var supplierJson = await File.ReadAllTextAsync(filePath).ConfigureAwait(false);

                retval = JsonSerializer.Deserialize<SuppliersConfiguration>(supplierJson, s_jsonOptions)
                         ?? throw new InvalidOperationException($"Failed to deserialize SuppliersConfiguration from {fileName}");
                retval.FullPath = filePath;

            }
            catch (Exception ex)
            {
                _logger!.LogError(ex, "Error reading supplier formats file at {Path}", filePath);
                throw;
            }
            return retval;
        }

        internal async Task<SuppliersConfiguration> LoadAllFromFolderAsync(string mainFileFullPath)
        {
            _logger?.LogInformation("Loading supplier configurations: {path}", mainFileFullPath);

            var result = await LoadSuppliersConfigurationAsync(mainFileFullPath).ConfigureAwait(false);

            string folder = Path.GetDirectoryName(mainFileFullPath)!;
            // Merge per-supplier files
            await MergeSuppliersFromFolderAsync(result, folder, Path.GetFileName(mainFileFullPath)).ConfigureAwait(false);

            return result;
        }

        private async Task MergeSuppliersFromFolderAsync(SuppliersConfiguration aggregate, string dirPath, string mainFileName)
        {
            try
            {
                var jsonFiles = Directory.GetFiles(dirPath, "*.json", SearchOption.TopDirectoryOnly);
                foreach (var file in jsonFiles)
                {
                    var name = Path.GetFileName(file);
                    if (string.Equals(name, mainFileName, StringComparison.OrdinalIgnoreCase))
                        continue; // skip the main file
                    if (string.Equals(name, "appsettings.json", StringComparison.OrdinalIgnoreCase))
                        continue; // skip the main file

                    try
                    {
                        var json = await File.ReadAllTextAsync(file).ConfigureAwait(false);

                        // Only accept a single SupplierConfiguration in extra files
                        var supplier = JsonSerializer.Deserialize<SupplierConfiguration>(json, s_jsonOptions);
                        if (supplier != null && !string.IsNullOrWhiteSpace(supplier.Name))
                        {
                            // Insert or replace by Name (case-insensitive)
                            var existingIndex = aggregate.Suppliers.FindIndex(
                                s => string.Equals(s?.Name, supplier.Name, StringComparison.OrdinalIgnoreCase));
                            if (existingIndex >= 0)
                            {
                                aggregate.Suppliers[existingIndex] = supplier;
                                _logger?.LogInformation("Replaced supplier '{SupplierName}' from file: {File}", supplier.Name, name);
                            }
                            else
                            {
                                aggregate.Suppliers.Add(supplier);
                                _logger?.LogInformation("Added supplier '{SupplierName}' from file: {File}", supplier.Name, name);
                            }
                            continue;
                        }

                        _logger?.LogWarning("Ignoring non-supplier JSON file (expected single SupplierConfiguration): {File}", name);
                    }
                    catch (Exception exFile)
                    {
                        _logger?.LogError(exFile, "Failed to process supplier file: {File}", name);
                    }
                }

                // Validate after merging
                var errors = aggregate.ValidateConfiguration();
                if (errors != null && errors.Count > 0)
                {
                    foreach (var item in errors)
                    {
                        _logger!.LogError("Supplier configuration validation error: {Error}", item);
                    }
                }

                _logger?.LogInformation("Total suppliers after merge: {Count}", aggregate.Suppliers.Count);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error merging supplier files from directory: {Dir}", dirPath);
                throw;
            }
        }
    }
}
