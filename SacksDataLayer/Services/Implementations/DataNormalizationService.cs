using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SacksDataLayer.Data;
using SacksDataLayer.Configuration;
using System.Text.Json;

namespace SacksDataLayer.Services.Implementations
{
    /// <summary>
    /// Service for normalizing existing product data to standardized property values
    /// </summary>
    public class DataNormalizationService
    {
        private readonly SacksDbContext _context;
        private readonly ConfigurationPropertyNormalizer _normalizer;
        private readonly ILogger<DataNormalizationService> _logger;

        public DataNormalizationService(
            SacksDbContext context, 
            ConfigurationPropertyNormalizer normalizer,
            ILogger<DataNormalizationService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _normalizer = normalizer ?? throw new ArgumentNullException(nameof(normalizer));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Normalizes all product dynamic properties in the database
        /// </summary>
        /// <param name="dryRun">If true, shows what would be changed without saving</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Summary of normalization results</returns>
        public async Task<NormalizationSummary> NormalizeAllProductsAsync(
            bool dryRun = false, 
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting data normalization. DryRun: {DryRun}", dryRun);

            var summary = new NormalizationSummary();
            var totalProducts = await _context.Products.CountAsync(cancellationToken);

            _logger.LogInformation("Total products to process: {TotalProducts}", totalProducts);

                var proc = await _context.Products
                    .Where(p => p.DynamicPropertiesJson != null)
                    .ToListAsync(cancellationToken);

                foreach (var product in proc)
                {
                    try
                    {
                        var originalJson = product.DynamicPropertiesJson!;
                        var originalProperties = JsonSerializer.Deserialize<Dictionary<string, object?>>(originalJson);
                        
                        if (originalProperties != null)
                        {
                            var normalizedProperties = _normalizer.NormalizeProperties(originalProperties);
                            var normalizedJson = JsonSerializer.Serialize(normalizedProperties);

                            if (originalJson != normalizedJson)
                            {
                                var changes = DetectChanges(originalProperties, normalizedProperties);
                                summary.ChangedProducts++;
                                summary.PropertyChanges.AddRange(changes);

                                _logger.LogDebug("Product {ProductId} ({ProductName}) - Changes: {Changes}", 
                                    product.Id, product.Name, string.Join(", ", changes.Select(c => $"{c.OriginalKey}→{c.NormalizedKey}")));

                                if (!dryRun)
                                {
                                    product.DynamicPropertiesJson = normalizedJson;
                                    product.ModifiedAt = DateTime.UtcNow;
                                }
                            }
                            else
                            {
                                summary.UnchangedProducts++;
                            }
                        }
                        
                        summary.ProcessedProducts++;
                    }
                    catch (JsonException ex)
                    {
                        summary.ErrorProducts++;
                        _logger.LogWarning(ex, "Failed to process product {ProductId} - Invalid JSON", product.Id);
                    }
                    catch (Exception ex)
                    {
                        summary.ErrorProducts++;
                        _logger.LogError(ex, "Unexpected error processing product {ProductId}", product.Id);
                    }


                if (!dryRun)
                {
                    await _context.SaveChangesAsync(cancellationToken);
                }

                // Log progress
                var percentage = totalProducts * 100 / totalProducts;
            }

            _logger.LogInformation("Data normalization completed. Summary: {Summary}", summary);
            return summary;
        }

        /// <summary>
        /// Analyzes what would change without making actual changes
        /// </summary>
        /// <param name="sampleSize">Number of products to analyze</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Analysis results</returns>
        public async Task<NormalizationAnalysis> AnalyzeNormalizationImpactAsync(
            int sampleSize = 1000, 
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Analyzing normalization impact on {SampleSize} products", sampleSize);

            var analysis = new NormalizationAnalysis();
            var sampleProducts = await _context.Products
                .Where(p => p.DynamicPropertiesJson != null)
                .Take(sampleSize)
                .Select(p => new { p.Id, p.Name, p.DynamicPropertiesJson })
                .ToListAsync(cancellationToken);

            foreach (var product in sampleProducts)
            {
                try
                {
                    var originalProperties = JsonSerializer.Deserialize<Dictionary<string, object?>>(product.DynamicPropertiesJson!);
                    if (originalProperties != null)
                    {
                        var normalizedProperties = _normalizer.NormalizeProperties(originalProperties);
                        var changes = DetectChanges(originalProperties, normalizedProperties);
                        
                        if (changes.Any())
                        {
                            analysis.ProductsWithChanges++;
                            analysis.AllChanges.AddRange(changes);
                            
                            foreach (var change in changes)
                            {
                                analysis.KeyMappings.TryAdd(change.OriginalKey, change.NormalizedKey);
                                analysis.ValueMappings.TryAdd($"{change.NormalizedKey}:{change.OriginalValue}", change.NormalizedValue);
                            }
                        }
                        else
                        {
                            analysis.ProductsWithoutChanges++;
                        }
                    }
                }
                catch (JsonException)
                {
                    analysis.ErrorProducts++;
                }
            }

            _logger.LogInformation("Analysis completed. {ProductsWithChanges} products would change out of {SampleSize}", 
                analysis.ProductsWithChanges, sampleSize);

            return analysis;
        }

        private List<PropertyChange> DetectChanges(
            Dictionary<string, object?> original, 
            Dictionary<string, object?> normalized)
        {
            var changes = new List<PropertyChange>();

            // Check for key changes and value changes
            foreach (var originalKvp in original)
            {
                var originalKey = originalKvp.Key;
                var originalValue = originalKvp.Value?.ToString() ?? string.Empty;
                var normalizedKey = _normalizer.NormalizeKey(originalKey);
                var normalizedValue = _normalizer.NormalizeValue(normalizedKey, originalValue);

                if (originalKey != normalizedKey || originalValue != normalizedValue)
                {
                    changes.Add(new PropertyChange
                    {
                        OriginalKey = originalKey,
                        NormalizedKey = normalizedKey,
                        OriginalValue = originalValue,
                        NormalizedValue = normalizedValue
                    });
                }
            }

            return changes;
        }
    }

    /// <summary>
    /// Summary of normalization results
    /// </summary>
    public class NormalizationSummary
    {
        public int ProcessedProducts { get; set; }
        public int ChangedProducts { get; set; }
        public int UnchangedProducts { get; set; }
        public int ErrorProducts { get; set; }
        public List<PropertyChange> PropertyChanges { get; set; } = new();

        public override string ToString()
        {
            return $"Processed: {ProcessedProducts}, Changed: {ChangedProducts}, Unchanged: {UnchangedProducts}, Errors: {ErrorProducts}";
        }
    }

    /// <summary>
    /// Analysis of normalization impact
    /// </summary>
    public class NormalizationAnalysis
    {
        public int ProductsWithChanges { get; set; }
        public int ProductsWithoutChanges { get; set; }
        public int ErrorProducts { get; set; }
        public Dictionary<string, string> KeyMappings { get; set; } = new();
        public Dictionary<string, string> ValueMappings { get; set; } = new();
        public List<PropertyChange> AllChanges { get; set; } = new();
    }

    /// <summary>
    /// Represents a property change during normalization
    /// </summary>
    public class PropertyChange
    {
        public string OriginalKey { get; set; } = string.Empty;
        public string NormalizedKey { get; set; } = string.Empty;
        public string OriginalValue { get; set; } = string.Empty;
        public string NormalizedValue { get; set; } = string.Empty;
    }
}
