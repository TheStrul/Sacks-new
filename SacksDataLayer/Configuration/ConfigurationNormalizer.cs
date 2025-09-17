using SacksDataLayer.FileProcessing.Interfaces;
using SacksDataLayer.FileProcessing.Configuration;
using SacksDataLayer.FileProcessing.Models;
using SacksAIPlatform.InfrastructuresLayer.FileProcessing;
using SacksDataLayer.Configuration;
using SacksDataLayer.Parsing;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.IO;

namespace SacksDataLayer.FileProcessing.Normalizers
{
    /// <summary>
    /// Configuration-driven normalizer using unified ColumnProperties structure
    /// Minimal shell - heavy helpers implemented in partial files
    /// </summary>
    public partial class ConfigurationNormalizer : IOfferNormalizer
    {
        private readonly SupplierConfiguration _configuration;
        private readonly Dictionary<PropertyDataType, Func<string, object?>> _dataTypeConverters;
        private readonly Transformer _transformer;
        private ILogger? _logger;

        // Cached market-level properties for fast lookup during normalization
        private readonly ProductPropertyConfiguration? _marketConfig;
        private readonly Dictionary<string, ProductPropertyDefinition> _marketProperties;

        // Optional market normalization mappings (value mappings)
        private readonly Dictionary<string, Dictionary<string, string>> _valueMappings = new(StringComparer.OrdinalIgnoreCase);

        private static readonly JsonSerializerOptions s_jsonOptions = new() { PropertyNameCaseInsensitive = true };

        public string SupplierName => _configuration.Name;

        public ConfigurationNormalizer(SupplierConfiguration configuration, ILogger logger)
        {
            _configuration = configuration;
            _dataTypeConverters = InitializeDataTypeConverters();
            _logger = logger;

            // Cache market-level configuration and properties to avoid repeated lookups
            _marketConfig = _configuration.EffectiveMarketConfiguration;
            _marketProperties = _marketConfig?.Properties ?? new Dictionary<string, ProductPropertyDefinition>(StringComparer.OrdinalIgnoreCase);

  
            // Try to load optional normalization mappings (perfume-property-normalization.json) used by tests and market configs
            try
            {
                var mappingPath = Path.Combine(Directory.GetCurrentDirectory(), "SacksApp", "Configuration", "perfume-property-normalization.json");
                if (File.Exists(mappingPath))
                {
                    using var doc = JsonDocument.Parse(File.ReadAllText(mappingPath));
                    if (doc.RootElement.TryGetProperty("valueMappings", out var vm))
                    {
                        var dict = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(vm.GetRawText(), s_jsonOptions);
                        if (dict != null)
                        {
                            foreach (var kv in dict)
                            {
                                if (!_valueMappings.ContainsKey(kv.Key))
                                    _valueMappings[kv.Key] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                                foreach (var inner in kv.Value)
                                {
                                    // normalize key to expected lookup form (lowercase, punctuation removed and collapsed)
                                    var normKey = Transformer.NormalizeForMappingStatic(inner.Key);
                                    if (!_valueMappings[kv.Key].ContainsKey(normKey))
                                        _valueMappings[kv.Key][normKey] = inner.Value;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to load value mappings: {Message}", ex.Message);
            }

            // Initialize transformer with logger and value mappings
            _transformer = new Transformer(_logger, _valueMappings);
        }

        // Implement interface members
        public bool CanHandle(string fileName, IEnumerable<RowData> firstFewRows)
        {
            ArgumentNullException.ThrowIfNull(fileName);

            var detection = _configuration.Detection;
            if (detection == null || detection.FileNamePatterns == null || !detection.FileNamePatterns.Any())
            {
                return false;
            }

            var lowerFileName = fileName.ToLowerInvariant();
            foreach (var pattern in detection.FileNamePatterns)
            {
                if (IsPatternMatch(lowerFileName, pattern.ToLowerInvariant()))
                    return true;
            }

            return false;
        }

        public async Task NormalizeAsync(ProcessingContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            var result = context.ProcessingResult;

            try
            {
                if (context.FileData.DataRows.Count == 0)
                    return;

                if (_configuration.ColumnProperties?.Count == 0)
                {
                    result.Errors.Add("No column properties configured for supplier");
                    return;
                }

                foreach (var row in context.FileData.DataRows)
                {
                    try
                    {
                        var offerProduct = await NormalizeRowAsync(row, context);

                        if (offerProduct != null)
                        {
                            if (IsValidOfferProduct(offerProduct))
                            {
                                result.SupplierOffer!.OfferProducts.Add(offerProduct);
                                result.Statistics.OfferProductsCreated++;
                            }
                            else
                            {
                                var ean = offerProduct.Product?.EAN ?? "<empty>";
                                var price = offerProduct.Price;
                                var qty = offerProduct.Quantity;
                                var msg = $"Row {row.Index}: Skipped - invalid offer product (EAN={ean}, Price={price}, Quantity={qty})";
                                result.Warnings.Add(msg);
                                result.Statistics.ProductsSkipped++;
                                _logger?.LogWarning(msg);
                            }
                        }
                        else
                        {
                            var msg = $"Row {row.Index}: Normalization returned null (row skipped)";
                            result.Warnings.Add(msg);
                            _logger?.LogWarning(msg);
                        }
                    }
                    catch (Exception ex)
                    {
                        result.Statistics.ErrorCount++;
                        var err = $"Row {row.Index}: {ex.Message}";
                        result.Errors.Add(err);
                        _logger?.LogError(ex, "Error processing row {RowIndex}: {Message}", row.Index, ex.Message);
                    }
                }

                result.Statistics.WarningCount = result.Warnings.Count;
                return;
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Processing failed: {ex.Message}");
                return;
            }
        }
    }
}
