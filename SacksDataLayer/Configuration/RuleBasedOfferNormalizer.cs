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
            throw new ArgumentException("SupplierConfiguration must have a ParserConfig", nameof(supplierConfiguration));
            
        _parserEngine = new ParserEngine(_supplierConfiguration.ParserConfig);
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Determines if this normalizer can handle the given file
    /// </summary>
    public bool CanHandle(string fileName, IEnumerable<SacksAIPlatform.InfrastructuresLayer.FileProcessing.RowData> firstFewRows)
    {
        // For now, use simple supplier name matching or file pattern detection
        // This can be enhanced with more sophisticated detection logic
        
        if (string.IsNullOrEmpty(fileName))
            return false;

        // Check if filename contains supplier identifier
        var fileNameLower = Path.GetFileNameWithoutExtension(fileName).ToLowerInvariant();
        var supplierNameLower = _supplierConfiguration.Name.ToLowerInvariant();
        
        return fileNameLower.Contains(supplierNameLower);
    }

    /// <summary>
    /// Normalizes supplier data into ProductOffer objects
    /// </summary>
    public async Task NormalizeAsync(ProcessingContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(context.FileData);
        ArgumentNullException.ThrowIfNull(context.ProcessingResult);

        _logger.LogInformation("Starting rule-based normalization for supplier: {SupplierName}", SupplierName);

        // Initialize supplier offer if not exists
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

        // Process each data row
        foreach (var row in context.FileData.DataRows)
        {
            try
            {
                processedRows++;

                // Skip empty rows
                if (!row.HasData)
                {
                    skippedRows++;
                    continue;
                }

                // Skip subtitle rows
                if (row.IsSubtitleRow)
                {
                    _logger.LogDebug("Skipping subtitle row {RowIndex}", row.Index);
                    skippedRows++;
                    continue;
                }

                // Convert SacksDataLayer.RowData to ParsingEngine.RowData
                var parsingEngineRow = new ParsingEngine.RowData(row.Cells);

                // Parse row using ParsingEngine
                var propertyBag = _parserEngine.Parse(parsingEngineRow);

                // Validate PropertyBag
                if (IsValidPropertyBag(propertyBag))
                {
                // Convert to ProductOffer
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

        // Update statistics
        context.ProcessingResult.Statistics.TotalDataRows = processedRows;
        context.ProcessingResult.Statistics.OfferProductsCreated = validOffers;
        context.ProcessingResult.Statistics.ProductsSkipped = skippedRows;

        _logger.LogInformation("Normalization completed. Processed: {ProcessedRows}, Valid offers: {ValidOffers}, Skipped: {SkippedRows}", 
            processedRows, validOffers, skippedRows);

        // Satisfy async interface requirement
        await Task.CompletedTask;
    }

    /// <summary>
    /// Validates if PropertyBag contains sufficient data for creating a ProductOffer
    /// </summary>
    private bool IsValidPropertyBag(PropertyBag propertyBag)
    {
        // Basic validation - ensure we have some extracted properties
        if (propertyBag.Values.Count == 0)
            return false;

        // EAN validation - must have non-empty EAN for product creation
        if (!propertyBag.Values.TryGetValue("Product.EAN", out var eanObj) || 
            eanObj is not string ean || 
            string.IsNullOrWhiteSpace(ean))
        {
            _logger.LogDebug("Skipping row - missing or empty EAN");
            return false;
        }

        // EAN format validation - must be numeric
        if (!System.Text.RegularExpressions.Regex.IsMatch(ean, @"^\d+$"))
        {
            _logger.LogDebug("Skipping row - invalid EAN format: {EAN}", ean);
            return false;
        }

        // TODO: Add more sophisticated validation rules based on business requirements
        // For example:
        // - Must have Price or ProductName
        // - Must have at least N non-empty properties
        // - Specific required fields based on supplier configuration

        return true;
    }

    /// <summary>
    /// Converts PropertyBag to ProductOffer entity with proper property categorization
    /// </summary>
    private ProductOffer? ConvertToProductOfferAnnex(PropertyBag propertyBag, ProcessingContext context)
    {
        try
        {
            var productOffer = new ProductOffer
            {
                Product = new Product()
            };

            // Categorize properties from PropertyBag using key patterns
            foreach (var kvp in propertyBag.Values)
            {
                var key = kvp.Key;
                var value = kvp.Value;

                if (string.IsNullOrWhiteSpace(value?.ToString()))
                    continue;

                // Parse key pattern: Category.PropertyName (e.g., "Offer.Price", "Product.EAN")
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
                                // Unknown offer built-in property, treat as dynamic
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
                                // Unknown product built-in property, treat as dynamic
                                productOffer.Product.SetDynamicProperty(propertyName, value);
                                break;
                        }
                    }
                    else
                    {
                        continue;
                    }

                }

                // Serialize the offer properties (Product.DynamicPropertiesJson is computed automatically)
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