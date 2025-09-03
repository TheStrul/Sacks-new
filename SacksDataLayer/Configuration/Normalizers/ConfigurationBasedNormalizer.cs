using SacksDataLayer.FileProcessing.Interfaces;
using SacksDataLayer.FileProcessing.Configuration;
using SacksDataLayer.FileProcessing.Models;
using SacksDataLayer.Entities;
using SacksAIPlatform.InfrastructuresLayer.FileProcessing;
using System.Globalization;
using System.Text.RegularExpressions;
using SacksDataLayer.Configuration;

namespace SacksDataLayer.FileProcessing.Normalizers
{
    /// <summary>
    /// Configuration-driven normalizer using unified ColumnProperties structure
    /// </summary>
    public class ConfigurationBasedNormalizer : ISupplierProductNormalizer
    {
        private readonly SupplierConfiguration _configuration;
        private readonly Dictionary<string, Func<string, object?>> _dataTypeConverters;
        private readonly DescriptionPropertyExtractor? _descriptionExtractor;

        public string SupplierName => _configuration.Name;

        public ConfigurationBasedNormalizer(SupplierConfiguration configuration, PropertyNormalizer? propertyNormalizer = null)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _dataTypeConverters = InitializeDataTypeConverters();
            
            // Initialize description extractor if PropertyNormalizer is available
            _descriptionExtractor = propertyNormalizer != null ? new DescriptionPropertyExtractor(propertyNormalizer) : null;
        }

        public bool CanHandle(string fileName, IEnumerable<RowData> firstFewRows)
        {
            ArgumentNullException.ThrowIfNull(fileName);
            
            var detection = _configuration.Detection;
            var lowerFileName = fileName.ToLowerInvariant();

            return detection.FileNamePatterns.Any(pattern => 
                IsPatternMatch(lowerFileName, pattern.ToLowerInvariant()));
        }

        public async Task<ProcessingResult> NormalizeAsync(FileData fileData, ProcessingContext context)
        {
            ArgumentNullException.ThrowIfNull(fileData);
            ArgumentNullException.ThrowIfNull(context);
            
            var startTime = DateTime.UtcNow;
            var result = new ProcessingResult
            {
                SourceFile = context.SourceFileName,
                SupplierName = SupplierName,
                ProcessedAt = startTime
            };

            try
            {
                var offerProducts = new List<OfferProductEntity>();
                var statistics = new ProcessingStatistics();
                var warnings = new List<string>();
                var errors = new List<string>();

                if (fileData.DataRows.Count == 0)
                {
                    result.Statistics = statistics;
                    return result;
                }

                // Require ColumnProperties configuration
                if (_configuration.ColumnProperties?.Count == 0)
                {
                    errors.Add("No column properties configured for supplier");
                    result.Errors = errors;
                    return result;
                }

                // Use supplier offer from context if provided, otherwise create one
                var supplierOffer = context.SupplierOffer ?? CreateSupplierOfferFromContext(context);
                result.SupplierOffer = supplierOffer;
                statistics.SupplierOffersCreated = context.SupplierOffer != null ? 0 : 1; // Only count if we created it

                statistics.TotalRowsProcessed = fileData.DataRows.Count;

                // Process data rows using FileStructure configuration
                var dataStartIndex = _configuration.FileStructure?.DataStartRowIndex ?? 1;
                var dataRows = fileData.DataRows
                    .Skip(dataStartIndex)
                    .Where(r => r.HasData); // Simplified: always skip empty rows for now

                foreach (var row in dataRows)
                {
                    try
                    {
                        var offerProduct = await NormalizeRowAsync(row, context, fileData.FilePath, supplierOffer);
                        
                        if (offerProduct != null)
                        {
                            if (IsValidOfferProduct(offerProduct))
                            {
                                offerProducts.Add(offerProduct);
                                statistics.ProductsCreated++;
                                statistics.PricingRecordsProcessed++;
                                
                                // Check if this offer product has offer-specific data
                                var hasOfferData = offerProduct.Price.HasValue ||
                                                 !string.IsNullOrEmpty(offerProduct.Capacity) ||
                                                 offerProduct.ProductProperties.Count > 0;
                                
                                if (hasOfferData)
                                {
                                    statistics.OfferProductsCreated++;
                                }
                                if (offerProduct.ProductProperties.ContainsKey("StockQuantity"))
                                {
                                    statistics.StockRecordsProcessed++;
                                }
                            }
                            else
                            {
                                statistics.ProductsSkipped++;
                                statistics.OrphanedCommercialRecords++;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        statistics.ErrorCount++;
                        errors.Add($"Row {row.Index}: {ex.Message}");
                    }
                }

                // Finalize statistics
                statistics.ProcessingTime = DateTime.UtcNow - startTime;
                statistics.WarningCount = warnings.Count;

                result.SupplierOffer.OfferProducts = offerProducts;
                result.Statistics = statistics;
                result.Warnings = warnings;
                result.Errors = errors;

                return result;
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Processing failed: {ex.Message}");
                result.Statistics.ProcessingTime = DateTime.UtcNow - startTime;
                return result;
            }
        }

        /// <summary>
        /// Normalizes a single row using unified ColumnProperties configuration
        /// </summary>
        private async Task<OfferProductEntity?> NormalizeRowAsync(
            RowData row, 
            ProcessingContext context, 
            string sourceFile,
            SupplierOfferEntity? supplierOffer)
        {
            try
            {
                var product = new ProductEntity();
                var offerProperties = new Dictionary<string, object?>();
                
                // Process each configured column property
                foreach (var columnConfig in _configuration.ColumnProperties)
                {
                    var columnKey = columnConfig.Key; // Excel column (A, B, C, etc.)
                    var columnProperty = columnConfig.Value;
                    var targetProperty = columnProperty.TargetProperty ?? columnProperty.DisplayName;
                    
                    if (string.IsNullOrEmpty(targetProperty))
                        continue;

                    // Get column index
                    var columnIndex = GetColumnIndex(columnKey);
                    if (columnIndex < 0 || columnIndex >= row.Cells.Count)
                        continue;

                    // Extract and process cell value
                    var rawValue = row.Cells[columnIndex].Value?.Trim();
                    if (string.IsNullOrEmpty(rawValue))
                    {
                        // Handle default values for empty cells
                        await SetDefaultValueIfConfiguredAsync(product, offerProperties, columnProperty, targetProperty);
                        continue;
                    }

                    // Apply transformations and type conversion
                    var processedValue = await ProcessCellValueAsync(rawValue, columnProperty);
                    if (processedValue == null && !columnProperty.DataType.AllowNull)
                        continue;

                    // Validate processed value
                    if (!await ValidateValueAsync(processedValue, columnProperty, targetProperty))
                        continue;

                    // Classify and assign value based on property classification
                    await AssignValueByClassificationAsync(
                        product, offerProperties, targetProperty, processedValue, columnProperty.Classification);
                }

                // Extract additional properties from description if available and extractor is configured
                if (_descriptionExtractor is not null && !string.IsNullOrEmpty(product.Description))
                {
                    var extractedProperties = _descriptionExtractor.ExtractPropertiesFromDescription(product.Description);
                    
                    // Add extracted properties to product's dynamic properties if they don't already exist
                    foreach (var extractedProp in extractedProperties)
                    {
                        if (!product.DynamicProperties.ContainsKey(extractedProp.Key) && extractedProp.Value != null)
                        {
                            product.SetDynamicProperty(extractedProp.Key, extractedProp.Value);
                        }
                    }
                }

                // Ensure product has valid name
                if (string.IsNullOrEmpty(product.Name))
                {
                    product.Name = ConstructProductNameFromProperties(product);
                }

                // Create unified OfferProduct entity containing everything
                var offerProduct = CreateOfferProductEntity(offerProperties, supplierOffer!);
                offerProduct.Product = product;
                if (supplierOffer != null)
                {
                    offerProduct.Offer = supplierOffer;
                    offerProduct.OfferId = supplierOffer.Id;
                }

                return offerProduct;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to normalize row {row.Index}: {ex.Message}", ex);
            }
        }

        #region Core Processing Methods

        /// <summary>
        /// Gets column index from Excel column reference (A=0, B=1, etc.)
        /// </summary>
        private int GetColumnIndex(string columnKey)
        {
            if (IsExcelColumnLetter(columnKey))
            {
                return ConvertExcelColumnToIndex(columnKey);
            }
            else if (int.TryParse(columnKey, out int numericIndex))
            {
                return numericIndex;
            }
            return -1;
        }

        /// <summary>
        /// Processes cell value through transformations and type conversion
        /// </summary>
        private async Task<object?> ProcessCellValueAsync(string rawValue, ColumnProperty columnProperty)
        {
            await Task.CompletedTask; // For async consistency
            
            try
            {
                // Apply configured transformations
                var transformedValue = ApplyTransformations(rawValue, columnProperty.DataType.Transformations);
                
                // Convert to target type
                if (_dataTypeConverters.TryGetValue(columnProperty.DataType.Type.ToLowerInvariant(), out var converter))
                {
                    return converter(transformedValue);
                }

                return transformedValue;
            }
            catch (Exception)
            {
                // Return default value on conversion failure
                return columnProperty.DataType.DefaultValue;
            }
        }

        /// <summary>
        /// Validates processed value against column validation rules
        /// </summary>
        private async Task<bool> ValidateValueAsync(object? value, ColumnProperty columnProperty, string targetProperty)
        {
            await Task.CompletedTask; // For async consistency
            
            // Check required field validation
            if (columnProperty.Validation.IsRequired && value == null)
            {
                return false;
            }

            // Check allowed values if configured
            if (columnProperty.Validation.AllowedValues.Count > 0)
            {
                var stringValue = value?.ToString();
                if (!columnProperty.Validation.AllowedValues.Contains(stringValue, StringComparer.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            // Check validation patterns if configured
            if (columnProperty.Validation.ValidationPatterns.Count > 0)
            {
                var stringValue = value?.ToString();
                if (!string.IsNullOrEmpty(stringValue))
                {
                    var matchesPattern = columnProperty.Validation.ValidationPatterns
                        .Any(pattern => Regex.IsMatch(stringValue, pattern, RegexOptions.IgnoreCase));
                    if (!matchesPattern)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Sets default value if configured for empty cells
        /// </summary>
        private async Task SetDefaultValueIfConfiguredAsync(
            ProductEntity product, 
            Dictionary<string, object?> offerProperties, 
            ColumnProperty columnProperty, 
            string targetProperty)
        {
            await Task.CompletedTask; // For async consistency
            
            if (columnProperty.DataType.DefaultValue != null)
            {
                await AssignValueByClassificationAsync(
                    product, offerProperties, targetProperty, 
                    columnProperty.DataType.DefaultValue, columnProperty.Classification);
            }
        }

        /// <summary>
        /// Assigns value to appropriate entity based on classification
        /// </summary>
        private async Task AssignValueByClassificationAsync(
            ProductEntity product, 
            Dictionary<string, object?> offerProperties, 
            string targetProperty, 
            object? value, 
            string classification)
        {
            await Task.CompletedTask; // For async consistency
            
            switch (targetProperty.ToLowerInvariant())
            {
                case "name":
                    product.Name = value?.ToString() ?? "";
                    break;
                case "description":
                    product.Description = value?.ToString();
                    break;
                case "ean":
                    product.EAN = value?.ToString() ?? "";
                    break;
                default:
                    // Classify based on configuration
                    if (classification == "coreProduct")
                    {
                        // Core product property - goes to ProductEntity.DynamicProperties
                        product.SetDynamicProperty(targetProperty, value);
                    }
                    else if (classification == "offer")
                    {
                        // Offer property - goes to OfferProductEntity
                        offerProperties[targetProperty] = value;
                    }
                    else
                    {
                        // Default to core product for unknown classification
                        product.SetDynamicProperty(targetProperty, value);
                    }
                    break;
            }
        }

        #endregion

        #region Utility Methods

        private string ApplyTransformations(string value, List<string> transformations)
        {
            if (transformations == null || !transformations.Any())
                return value;

            foreach (var transformation in transformations)
            {
                value = transformation.ToLowerInvariant() switch
                {
                    "trim" => value.Trim(),
                    "lowercase" => value.ToLowerInvariant(),
                    "uppercase" => value.ToUpperInvariant(),
                    "removesymbols" => Regex.Replace(value, @"[^\d.,]", ""),
                    "removecommas" => value.Replace(",", ""),
                    "removespaces" => value.Replace(" ", ""),
                    _ => value
                };
            }

            return value;
        }

        private bool IsPatternMatch(string input, string pattern)
        {
            // Simple wildcard pattern matching
            if (pattern == "*")
                return true;

            if (pattern.StartsWith("*") && pattern.EndsWith("*"))
            {
                var searchTerm = pattern.Trim('*');
                return input.Contains(searchTerm, StringComparison.OrdinalIgnoreCase);
            }

            if (pattern.StartsWith("*"))
            {
                var suffix = pattern.TrimStart('*');
                return input.EndsWith(suffix, StringComparison.OrdinalIgnoreCase);
            }

            if (pattern.EndsWith("*"))
            {
                var prefix = pattern.TrimEnd('*');
                return input.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
            }

            return string.Equals(input, pattern, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Checks if a string represents an Excel column letter format (A, B, C, AA, AB, etc.)
        /// </summary>
        private bool IsExcelColumnLetter(string columnReference)
        {
            if (string.IsNullOrWhiteSpace(columnReference))
                return false;

            return columnReference.All(c => char.IsLetter(c) && char.IsUpper(c));
        }

        /// <summary>
        /// Converts Excel column letters to zero-based column index
        /// A=0, B=1, C=2, ..., Z=25, AA=26, AB=27, etc.
        /// </summary>
        private int ConvertExcelColumnToIndex(string columnLetter)
        {
            if (string.IsNullOrWhiteSpace(columnLetter))
                throw new ArgumentException("Column letter cannot be null or empty", nameof(columnLetter));

            columnLetter = columnLetter.ToUpperInvariant();
            int result = 0;

            for (int i = 0; i < columnLetter.Length; i++)
            {
                char c = columnLetter[i];
                if (c < 'A' || c > 'Z')
                    throw new ArgumentException($"Invalid column letter: {columnLetter}", nameof(columnLetter));

                result = result * 26 + (c - 'A' + 1);
            }

            return result - 1; // Convert to zero-based index
        }

        private Dictionary<string, Func<string, object?>> InitializeDataTypeConverters()
        {
            return new Dictionary<string, Func<string, object?>>
            {
                ["string"] = value => value,
                ["int"] = value => int.TryParse(value, out int result) ? result : null,
                ["decimal"] = value => decimal.TryParse(value, NumberStyles.Currency, CultureInfo.InvariantCulture, out decimal result) ? result : null,
                ["bool"] = value => ParseBooleanValue(value),
                ["datetime"] = value => DateTime.TryParse(value, out DateTime result) ? result : null,
                ["date"] = value => DateOnly.TryParse(value, out DateOnly result) ? result : null,
                ["time"] = value => TimeOnly.TryParse(value, out TimeOnly result) ? result : null
            };
        }

        private bool ParseBooleanValue(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            value = value.Trim().ToLowerInvariant();
            
            return value switch
            {
                "yes" or "y" or "true" or "1" or "active" or "enabled" or "on" => true,
                "no" or "n" or "false" or "0" or "inactive" or "disabled" or "off" => false,
                _ => false
            };
        }

        /// <summary>
        /// Creates a SupplierOffer entity from processing context
        /// </summary>
        private SupplierOfferEntity CreateSupplierOfferFromContext(ProcessingContext context)
        {
            return new SupplierOfferEntity
            {
                OfferName = $"{SupplierName} - {context.SourceFileName}",
                Description = $"Offer created from file: {context.SourceFileName}",
                ValidFrom = context.ProcessingDate,
                ValidTo = context.ProcessingDate.AddYears(1), // Default 1 year validity
                IsActive = true,
                OfferType = "File Import",
                Version = "1.0",
                CreatedAt = context.ProcessingDate
            };
        }

        /// <summary>
        /// Creates an OfferProduct entity from offer properties
        /// </summary>
        private OfferProductEntity CreateOfferProductEntity(Dictionary<string, object?> offerProperties, SupplierOfferEntity? supplierOffer)
        {
            var offerProduct = new OfferProductEntity
            {
                IsAvailable = true,
                CreatedAt = DateTime.UtcNow
            };

            // Track which properties have been mapped to avoid duplication
            var mappedProperties = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Map standard offer properties to OfferProduct entity properties
            foreach (var prop in offerProperties)
            {
                switch (prop.Key.ToLowerInvariant())
                {
                    case "price":
                        if (decimal.TryParse(prop.Value?.ToString(), out decimal price))
                            offerProduct.Price = price;
                        mappedProperties.Add(prop.Key);
                        break;
                    case "capacity":
                        offerProduct.Capacity = prop.Value?.ToString();
                        mappedProperties.Add(prop.Key);
                        break;
                    case "discount":
                        if (decimal.TryParse(prop.Value?.ToString(), out decimal discount))
                            offerProduct.Discount = discount;
                        mappedProperties.Add(prop.Key);
                        break;
                    case "listprice":
                        if (decimal.TryParse(prop.Value?.ToString(), out decimal listPrice))
                            offerProduct.ListPrice = listPrice;
                        mappedProperties.Add(prop.Key);
                        break;
                    case "unitofmeasure":
                        offerProduct.UnitOfMeasure = prop.Value?.ToString();
                        mappedProperties.Add(prop.Key);
                        break;
                    case "minimumorderquantity":
                        if (int.TryParse(prop.Value?.ToString(), out int minQty))
                            offerProduct.MinimumOrderQuantity = minQty;
                        mappedProperties.Add(prop.Key);
                        break;
                    case "maximumorderquantity":
                        if (int.TryParse(prop.Value?.ToString(), out int maxQty))
                            offerProduct.MaximumOrderQuantity = maxQty;
                        mappedProperties.Add(prop.Key);
                        break;
                }
            }

            // Only store unmapped properties as dynamic properties to avoid duplication
            var unmappedProperties = offerProperties
                .Where(kvp => !mappedProperties.Contains(kvp.Key))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            if (unmappedProperties.Count > 0)
            {
                foreach (var prop in unmappedProperties)
                {
                    offerProduct.SetProductProperty(prop.Key, prop.Value);
                }
            }

            return offerProduct;
        }

        /// <summary>
        /// Constructs a product name from available properties when name is missing
        /// </summary>
        private string ConstructProductNameFromProperties(ProductEntity product)
        {
            var nameComponents = new List<string>();
            
            // Try to build a meaningful name from extracted/mapped properties
            if (product.DynamicProperties.TryGetValue("Brand", out var brand) && brand != null)
                nameComponents.Add(brand.ToString()!);
            
            if (product.DynamicProperties.TryGetValue("ProductLine", out var line) && line != null)
                nameComponents.Add(line.ToString()!);
            
            if (product.DynamicProperties.TryGetValue("Category", out var category) && category != null)
                nameComponents.Add(category.ToString()!);
            
            if (product.DynamicProperties.TryGetValue("Size", out var size) && size != null)
                nameComponents.Add(size.ToString()!);
            
            if (product.DynamicProperties.TryGetValue("Gender", out var gender) && gender != null)
                nameComponents.Add($"for {gender}");
            
            if (product.DynamicProperties.TryGetValue("Concentration", out var concentration) && concentration != null)
                nameComponents.Add(concentration.ToString()!);

            // Legacy properties for backward compatibility
            if (!nameComponents.Any())
            {
                if (product.DynamicProperties.TryGetValue("Family", out var family) && family != null)
                    nameComponents.Add(family.ToString()!);
                if (product.DynamicProperties.TryGetValue("PricingItemName", out var itemName) && itemName != null)
                    nameComponents.Add(itemName.ToString()!);
            }
            
            // If no meaningful properties found, use description
            if (!nameComponents.Any() && !string.IsNullOrWhiteSpace(product.Description))
            {
                // Truncate description if too long for a name
                var desc = product.Description.Length > 50 
                    ? product.Description.Substring(0, 50) + "..." 
                    : product.Description;
                nameComponents.Add(desc);
            }

            return nameComponents.Any() ? string.Join(" ", nameComponents) : "Unknown Product";
        }

        /// <summary>
        /// Validates an offer product for supplier offers
        /// </summary>
        private bool IsValidOfferProduct(OfferProductEntity offerProduct)
        {
            // For supplier offers, require valid product with EAN
            return offerProduct?.Product != null &&
                   !string.IsNullOrEmpty(offerProduct.Product.Name) && 
                   offerProduct.Product.Name != "Unknown Product" && 
                   !string.IsNullOrEmpty(offerProduct.Product.EAN);
        }

        #endregion
    }
}
