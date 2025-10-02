using Microsoft.Extensions.Logging;
using ParsingEngine;
using SacksDataLayer.Entities;
using SacksDataLayer.FileProcessing.Configuration;
using SacksDataLayer.FileProcessing.Interfaces;
using SacksDataLayer.FileProcessing.Models;

namespace SacksDataLayer.Configuration;

/// <summary>
/// Rule-based offer normalizer using ParsingEngine for data extraction
/// </summary>
public class RuleBasedOfferNormalizer : IOfferNormalizer
{
    private readonly SupplierConfiguration _supplierConfiguration;
    private readonly ParserEngine _parserEngine;
    private readonly ILogger<RuleBasedOfferNormalizer> _logger;

    public string SupplierName => _supplierConfiguration.Name;

    public RuleBasedOfferNormalizer(
        SupplierConfiguration supplierConfiguration, 
        ILogger<RuleBasedOfferNormalizer> logger)
    {
        _supplierConfiguration = supplierConfiguration ?? throw new ArgumentNullException(nameof(supplierConfiguration));
        
        if (_supplierConfiguration.ParserConfig == null)
            throw new ArgumentException("Suppl  ierConfiguration must have a ParserConfig", nameof(supplierConfiguration));
            
        _parserEngine = new ParserEngine(_supplierConfiguration.ParserConfig);
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public bool CanHandle(string fileName, IEnumerable<SacksAIPlatform.InfrastructuresLayer.FileProcessing.RowData> firstFewRows)
    {
        if (string.IsNullOrEmpty(fileName))
            return false;

        var fileNameLower = Path.GetFileNameWithoutExtension(fileName).ToLowerInvariant();
        var supplierNameLower = _supplierConfiguration.Name.ToLowerInvariant();
        
        return fileNameLower.Contains(supplierNameLower);
    }

    public async Task NormalizeAsync(ProcessingContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(context.FileData);
        ArgumentNullException.ThrowIfNull(context.ProcessingResult);

        _logger.LogInformation("Starting rule-based normalization for supplier: {SupplierName}", SupplierName);

        if (context.ProcessingResult.SupplierOffer == null)
        {
            context.ProcessingResult.SupplierOffer = new Offer
            {
                OfferName = $"{SupplierName} - {context.FileData.FileName}",
                Currency = _supplierConfiguration.Currency ?? "USD",
                Description = $"Processed from file: {context.FileData.FileName}",
                OfferProducts = new List<ProductOffer>()
            };
        }

        var processedRows = 0;
        var validOffers = 0;
        var skippedRows = 0;

        foreach (var row in context.FileData.DataRows)
        {
            try
            {
                processedRows++;

                if (!row.HasData)
                {
                    skippedRows++;
                    continue;
                }

                if (row.IsSubtitleRow)
                {
                    _logger.LogDebug("Skipping subtitle row {RowIndex}", row.Index);
                    skippedRows++;
                    continue;
                }

                var parsingEngineRow = new ParsingEngine.RowData(row.Cells);

                var propertyBag = _parserEngine.Parse(parsingEngineRow);

                // Apply subtitle-derived defaults using config-driven assignments
                ApplySubtitleAssignments(row, propertyBag);

                if (IsValidPropertyBag(propertyBag))
                {
                    var productOffer = ConvertToProductOfferAnnex(propertyBag, context);
                    if (productOffer != null)
                    {
                        context.ProcessingResult.SupplierOffer.OfferProducts.Add(productOffer);
                        validOffers++;

                        _logger.LogDebug("Created ProductOffer from row {RowIndex}: {PropertyCount} properties",
                            row.Index, propertyBag.Values.Count);
                    }
                    else
                    {
                        skippedRows++;
                        _logger.LogWarning("Failed to convert PropertyBag to ProductOffer for row {RowIndex}", row.Index);
                    }
                }
                else
                {
                    skippedRows++;
                    _logger.LogDebug("Skipping invalid PropertyBag for row {RowIndex}", row.Index);
                }
            }
            catch (Exception ex)
            {
                context.ProcessingResult.Errors.Add($"Error processing row {row.Index}: {ex.Message}");
                _logger.LogError(ex, "Error processing row {RowIndex}", row.Index);
                skippedRows++;
            }
        }

        context.ProcessingResult.Statistics.TotalDataRows = processedRows;
        context.ProcessingResult.Statistics.OfferProductsCreated = validOffers;
        context.ProcessingResult.Statistics.ProductsSkipped = skippedRows;

        _logger.LogInformation("Normalization completed. Processed: {ProcessedRows}, Valid offers: {ValidOffers}, Skipped: {SkippedRows}", 
            processedRows, validOffers, skippedRows);

        await Task.CompletedTask;
    }

    private void ApplySubtitleAssignments(SacksAIPlatform.InfrastructuresLayer.FileProcessing.RowData row, PropertyBag bag)
    {
        if (row?.SubtitleData == null || row.SubtitleData.Count == 0)
            return;

        var sh = _supplierConfiguration.SubtitleHandling;
        var assignments = sh?.Assignments ?? new List<SubtitleAssignmentMapping>();
        if (assignments.Count == 0)
        {
            return; // config has no assignments; nothing to apply
        }

        foreach (var map in assignments)
        {
            if (map == null) continue;
            if (!row.SubtitleData.TryGetValue(map.SourceKey, out var srcObj))
            {
                // Try case-insensitive lookup
                var kv = row.SubtitleData.FirstOrDefault(k => string.Equals(k.Key, map.SourceKey, StringComparison.OrdinalIgnoreCase));
                srcObj = kv.Value;
            }
            var src = srcObj?.ToString();
            if (string.IsNullOrWhiteSpace(src)) continue;

            string valueToAssign = src;

            // Optional lookup normalization
            if (!string.IsNullOrWhiteSpace(map.LookupTable))
            {
                var lookups = _supplierConfiguration.ParserConfig?.Lookups ?? new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
                if (lookups.TryGetValue(map.LookupTable, out var table) && table != null)
                {
                    // case-insensitive match on key
                    var match = table.FirstOrDefault(e => string.Equals(e.Key, src, StringComparison.OrdinalIgnoreCase));
                    if (!string.IsNullOrEmpty(match.Key))
                    {
                        valueToAssign = match.Value ?? string.Empty;
                    }
                    else
                    {
                        // if lookup provided but no match, skip this assignment to avoid mis-assignments
                        continue;
                    }
                }
            }

            // Apply to bag if allowed
            if (map.Overwrite || !bag.Values.TryGetValue(map.TargetProperty, out var existing) || string.IsNullOrWhiteSpace(existing?.ToString()))
            {
                bag.Set(map.TargetProperty, valueToAssign, "Subtitle");
            }
        }
    }

    private bool IsValidPropertyBag(PropertyBag propertyBag)
    {
        if (propertyBag.Values.Count == 0)
            return false;

        if (!propertyBag.Values.TryGetValue("Product.EAN", out var eanObj) || 
            eanObj is not string ean || 
            string.IsNullOrWhiteSpace(ean))
        {
            _logger.LogDebug("Skipping row - missing or empty EAN");
            return false;
        }

        if (!System.Text.RegularExpressions.Regex.IsMatch(ean, @"^\d+$"))
        {
            _logger.LogDebug("Skipping row - invalid EAN format: {EAN}", ean);
            return false;
        }

        return true;
    }

    private ProductOffer? ConvertToProductOfferAnnex(PropertyBag propertyBag, ProcessingContext context)
    {
        try
        {
            var productOffer = new ProductOffer
            {
                Product = new Product()
            };

            foreach (var kvp in propertyBag.Values)
            {
                var key = kvp.Key;
                var value = kvp.Value;

                if (string.IsNullOrWhiteSpace(value?.ToString()))
                    continue;

                if (key.Contains('.'))
                {
                    var parts = key.Split('.', 2);
                    if (parts.Length != 2)
                        continue;
                    var category = parts[0];
                    var propertyName = parts[1];
                    if (string.IsNullOrWhiteSpace(propertyName))
                        continue;
                    if (string.IsNullOrWhiteSpace(category))
                        continue;

                    if (category.Equals("offer", StringComparison.OrdinalIgnoreCase))
                    {
                        switch (propertyName.ToLowerInvariant())
                        {
                            case "price":
                                if (decimal.TryParse(value.ToString(), out var price))
                                    productOffer.Price = price;
                                break;
                            case "currency":
                                if (!string.IsNullOrWhiteSpace(value.ToString()))
                                    productOffer.Currency = value.ToString()!;
                                break;
                            case "quantity":
                                if (int.TryParse(value.ToString(), out var quantity))
                                    productOffer.Quantity = quantity;
                                break;
                            case "description":
                                productOffer.Description = value.ToString();
                                break;
                            default:
                                productOffer.SetOfferProperty(propertyName, value);
                                break;
                        }
                    }
                    else if (category.Equals("product", StringComparison.OrdinalIgnoreCase))
                    {
                        switch (propertyName.ToLowerInvariant())
                        {
                            case "ean":
                                productOffer.Product.EAN = value.ToString() ?? string.Empty;
                                break;
                            case "name":
                                productOffer.Product.Name = value.ToString() ?? string.Empty;
                                break;
                            default:
                                productOffer.Product.SetDynamicProperty(propertyName, value);
                                break;
                        }
                    }
                    else
                    {
                        continue;
                    }

                }

                productOffer.SerializeOfferProperties();
            }

            return productOffer;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error converting PropertyBag to ProductOffer");
            return null;
        }
    }
}