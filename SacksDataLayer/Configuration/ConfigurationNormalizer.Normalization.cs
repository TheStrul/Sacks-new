using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using SacksAIPlatform.InfrastructuresLayer.FileProcessing;
using SacksDataLayer.Entities;
using SacksDataLayer.FileProcessing.Models;
using SacksDataLayer.Configuration;

namespace SacksDataLayer.FileProcessing.Normalizers
{
    public partial class ConfigurationNormalizer
    {
        private async Task<ProductOfferAnnex?> NormalizeRowAsync(RowData row, ProcessingContext context)
        {
            try
            {
                var offerProduct = new ProductOfferAnnex();
                var product = new ProductEntity();
                offerProduct.Product = product;
                offerProduct.Offer = context.ProcessingResult.SupplierOffer!;

                if (row.SubtitleData.Any())
                {
                    await ApplySubtitleDataAsync(row.SubtitleData, product, offerProduct);
                }

                // Process regular column properties
                foreach (var columnConfig in _configuration.ColumnProperties)
                {
                    var columnKey = columnConfig.Key;
                    var columnProperty = columnConfig.Value;

                    if (columnProperty.Classification == default)
                        continue;

                    var columnIndex = GetColumnIndex(columnKey);
                    if (columnIndex < 0 || columnIndex >= row.Cells.Count)
                        continue;

                    string? stringValue = row.Cells[columnIndex].Value?.Trim();
                    var validationResult = await ValidateValueAsync(stringValue, columnProperty);
                    if (!validationResult.IsValid)
                    {
                        if (validationResult.SkipEntireRow)
                            return null;
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(stringValue))
                    {
                        if (columnProperty.IsRequired)
                            return null;
                        continue;
                    }

                    // Use simple transformation for regular column properties
                    string processedValue = ApplyTransformations(stringValue!, columnProperty.Transformations, columnProperty.Key);

                    if (string.IsNullOrEmpty(processedValue) && columnProperty.IsRequired)
                        return null;

                    ApplyColumnValue(columnProperty.Classification, columnProperty.Key, processedValue, product, offerProduct);
                }

                // Process ExtendedProperties separately for advanced extraction
                if (_configuration.ExtendedProperties != null)
                {
                    foreach (var extendedProperty in _configuration.ExtendedProperties)
                    {
                        var sourceColumnKey = extendedProperty.Key;
                        var extractConfig = extendedProperty.Value;

                        var columnIndex = GetColumnIndex(sourceColumnKey);
                        if (columnIndex < 0 || columnIndex >= row.Cells.Count)
                            continue;

                        string? stringValue = row.Cells[columnIndex].Value?.Trim();
                        if (string.IsNullOrWhiteSpace(stringValue))
                            continue;

                        // Process each transformation property defined for this extended property
                        foreach (var transformProp in extractConfig.TransformProperties)
                        {
                            if (string.IsNullOrWhiteSpace(transformProp.Key) || string.IsNullOrWhiteSpace(transformProp.Transformation))
                                continue;

                            // Apply the specific transformation for this property
                            var transformationsList = new List<string> { transformProp.Transformation };
                            if (!string.IsNullOrEmpty(transformProp.Parameters))
                            {
                                // If parameters are provided, append them to the transformation
                                transformationsList[0] = $"{transformProp.Transformation}:{transformProp.Parameters}";
                            }

                            var transformationResult = ApplyTransformationsWithExtraction(stringValue!, transformationsList, transformProp);
                            string processedValue = transformationResult.TransformedValue;
                            var extractedProperties = transformationResult.ExtraProperties;

                            // Store the main transformed value
                            if (!string.IsNullOrEmpty(processedValue))
                            {
                                // Check if this property has a specific classification in market config
                                if (_marketProperties.TryGetValue(transformProp.Key, out var marketPropertyDef))
                                {
                                    ApplyExtractedProperty(marketPropertyDef.Classification, transformProp.Key, processedValue, product, offerProduct);
                                }
                                else
                                {
                                    // Default to product dynamic property if no market configuration found
                                    product.SetDynamicProperty(transformProp.Key, processedValue);
                                }
                            }

                            // Handle any additional extracted properties from advanced transformations
                            if (extractedProperties != null && extractedProperties.Any())
                            {
                                foreach (var extractedProperty in extractedProperties)
                                {
                                    if (string.IsNullOrWhiteSpace(extractedProperty.Key) || string.IsNullOrWhiteSpace(extractedProperty.Value))
                                        continue;

                                    // Check if the extracted property has a specific classification in market config
                                    if (_marketProperties.TryGetValue(extractedProperty.Key, out var marketPropertyDef))
                                    {
                                        ApplyExtractedProperty(marketPropertyDef.Classification, extractedProperty.Key, extractedProperty.Value, product, offerProduct);
                                    }
                                    else
                                    {
                                        // Default to product dynamic property if no market configuration found
                                        product.SetDynamicProperty(extractedProperty.Key, extractedProperty.Value);
                                    }
                                }
                            }
                        }
                    }
                }

                if (string.IsNullOrEmpty(product.Name))
                    product.Name = ConstructProductNameFromProperties(product);

                // Ensure product-level currency defaults to the parent offer currency when missing
                if (string.IsNullOrWhiteSpace(offerProduct.Currency) && offerProduct.Offer != null && !string.IsNullOrWhiteSpace(offerProduct.Offer.Currency))
                {
                    offerProduct.Currency = offerProduct.Offer.Currency;
                }

                return offerProduct;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to normalize row {row.Index}: {ex.Message}", ex);
            }
        }

        private async Task ApplySubtitleDataAsync(Dictionary<string, object?> subtitleData, ProductEntity product, ProductOfferAnnex offerProduct)
        {
            await Task.CompletedTask;
            foreach (var kvp in subtitleData)
            {
                var key = kvp.Key;
                var value = kvp.Value;
                if (value == null) continue;
                var normalizedKey = NormalizeSubtitleKey(key);
                var stringVal = value.ToString() ?? string.Empty;
                // Description-based normalization removed; rely on supplier transforms and value mappings
                if (!product.DynamicProperties.ContainsKey(normalizedKey))
                    product.SetDynamicProperty(normalizedKey, stringVal);
            }
        }

        private string NormalizeSubtitleKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return key ?? string.Empty;
            var title = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(key.Trim().ToLowerInvariant());
            return Regex.Replace(title, @"[\s_\-]+", string.Empty);
        }

        private void ApplyExtractedProperty(PropertyClassificationType classification, string propertyKey, string value, ProductEntity product, ProductOfferAnnex offer)
        {
            switch (classification)
            {
                case PropertyClassificationType.ProductName:
                    product.Name = value;
                    break;
                case PropertyClassificationType.ProductEAN:
                    product.EAN = value;
                    break;
                case PropertyClassificationType.OfferPrice:
                    if (decimal.TryParse(value, System.Globalization.NumberStyles.Number | System.Globalization.NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var d))
                        offer.Price = d;
                    break;
                case PropertyClassificationType.OfferCurrency:
                    offer.Currency = value;
                    break;
                case PropertyClassificationType.OfferQuantity:
                    if (int.TryParse(value, System.Globalization.NumberStyles.Number, CultureInfo.InvariantCulture, out var i))
                        offer.Quantity = i;
                    break;
                case PropertyClassificationType.OfferDescription:
                    offer.Description = value;
                    break;
                case PropertyClassificationType.OfferDynamic:
                    offer.SetOfferProperty(propertyKey, value);
                    break;
                case PropertyClassificationType.ProductDynamic:
                default:
                    product.SetDynamicProperty(propertyKey, value);
                    break;
            }
        }

        /// <summary>
        /// Applies a column value to the appropriate product or offer property based on its classification
        /// </summary>
        private void ApplyColumnValue(PropertyClassificationType classification, string propertyKey, string value, ProductEntity product, ProductOfferAnnex offer)
        {
            switch (classification)
            {
                case PropertyClassificationType.ProductName:
                    product.Name = value ?? product.Name;
                    break;
                case PropertyClassificationType.ProductEAN:
                    product.EAN = value ?? product.EAN;
                    break;
                case PropertyClassificationType.OfferPrice:
                    if (decimal.TryParse(value, NumberStyles.Number | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var d))
                    {
                        offer.Price = d;
                    }
                    break;
                case PropertyClassificationType.OfferCurrency:
                    offer.Currency = value ?? offer.Currency;
                    break;
                case PropertyClassificationType.OfferQuantity:
                    if (int.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var i))
                    {
                        offer.Quantity = i;
                    }
                    break;
                case PropertyClassificationType.OfferDescription:
                    offer.Description = value ?? offer.Description;
                    break;
                case PropertyClassificationType.ProductDynamic:
                    product.SetDynamicProperty(propertyKey, value);
                    break;
                case PropertyClassificationType.OfferDynamic:
                    offer.SetOfferProperty(propertyKey, value);
                    break;
            }
        }
    }
}
