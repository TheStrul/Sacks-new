using SacksDataLayer.FileProcessing.Interfaces;
using SacksDataLayer.FileProcessing.Configuration;
using SacksDataLayer.FileProcessing.Models;
using SacksDataLayer.Entities;
using SacksAIPlatform.InfrastructuresLayer.FileProcessing;
using System.Globalization;
using System.Text.RegularExpressions;
using SacksDataLayer.Configuration;
using Microsoft.Extensions.Logging;

namespace SacksDataLayer.FileProcessing.Normalizers
{
    /// <summary>
    /// Result of column validation indicating whether validation passed and what action to take
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public bool SkipEntireRow { get; set; }
        public string? ErrorMessage { get; set; }

        public static ValidationResult Valid() => new() { IsValid = true };
        public static ValidationResult Invalid(bool skipEntireRow = false, string? errorMessage = null) =>
            new() { IsValid = false, SkipEntireRow = skipEntireRow, ErrorMessage = errorMessage };
    }

    /// <summary>
    /// Configuration-driven normalizer using unified ColumnProperties structure
    /// </summary>
    public class ConfigurationNormalizer : IOfferNormalizer
    {
        private readonly SupplierConfiguration _configuration;
        private readonly Dictionary<string, Func<string, object?>> _dataTypeConverters;
        private readonly ConfigurationDescriptionPropertyExtractor? _descriptionExtractor;
        private readonly ILogger<ConfigurationNormalizer>? _logger;

        public string SupplierName => _configuration.Name;

        public ConfigurationNormalizer(SupplierConfiguration configuration, ConfigurationPropertyNormalizer? propertyNormalizer = null, ILogger<ConfigurationNormalizer>? logger = null)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _dataTypeConverters = InitializeDataTypeConverters();
            _logger = logger;

            // Initialize description extractor if ConfigurationPropertyNormalizer is available
            _descriptionExtractor = propertyNormalizer != null ? new ConfigurationDescriptionPropertyExtractor(propertyNormalizer.Configuration) : null;
        }

        public bool CanHandle(string fileName, IEnumerable<RowData> firstFewRows)
        {
            ArgumentNullException.ThrowIfNull(fileName);

            var detection = _configuration.Detection;
            var lowerFileName = fileName.ToLowerInvariant();

            return detection.FileNamePatterns.Any(pattern =>
                IsPatternMatch(lowerFileName, pattern.ToLowerInvariant()));
        }

        public async Task<ProcessingResult> NormalizeAsync(ProcessingContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            var result = new ProcessingResult()
            {
                SupplierOffer = context.SupplierOffer,
                SourceFile = context.FileData.FilePath,
            };

            try
            {

                if (context.FileData.DataRows.Count == 0)
                {
                    return result;
                }

                // Require ColumnProperties configuration
                if (_configuration.ColumnProperties?.Count == 0)
                {
                    result.Errors.Add("No column properties configured for supplier");
                    return result;
                }

                // Use supplier offer from context if provided, otherwise create one
                result.Statistics.SupplierOffersCreated = context.SupplierOffer != null ? 0 : 1; // Only count if we created it


                // Process data rows using FileStructure configuration
                var dataStartIndex = _configuration.FileStructure.DataStartRowIndex-1; // Convert to zero-based index
                var allDataRows = context.FileData.DataRows.Skip(dataStartIndex).ToList();
                result.Statistics.TotalRowsProcessed = allDataRows.Count;

                var emptyRowsSkipped = allDataRows.Count(r => !r.HasData);
                var dataRows = allDataRows.Where(r => r.HasData);

                if (emptyRowsSkipped > 0)
                {
                    _logger?.LogDebug("Skipped {EmptyRowsSkipped} empty rows after row {DataStartIndex}", emptyRowsSkipped, dataStartIndex);
                }

                foreach (var row in dataRows)
                {
                    try
                    {
                        var offerProduct = await NormalizeRowAsync(row, context, context.FileData.FilePath, result.SupplierOffer);

                        if (offerProduct != null)
                        {
                            if (IsValidOfferProduct(offerProduct))
                            {
                                result.SupplierOffer.OfferProducts.Add(offerProduct);
                                result.Statistics.ProductsCreated++;
                                result.Statistics.PricingRecordsProcessed++;

                                // Check if this offer product has offer-specific data
                                var hasOfferData = offerProduct.Price > 0 ||
                                                 offerProduct.Quantity > 0 ||
                                                 offerProduct.OfferProperties.Count > 0; if (hasOfferData)
                                {
                                    result.Statistics.OfferProductsCreated++;
                                }
                                result.Statistics.StockRecordsProcessed++;
                            }
                            else
                            {
                                result.Statistics.ProductsSkipped++;
                                result.Statistics.OrphanedCommercialRecords++;
                                _logger?.LogDebug("Skipped invalid product in row {RowIndex}: Product='{ProductName}', EAN='{ProductEAN}' (missing required data)",
                                    row.Index, offerProduct.Product?.Name ?? "null", offerProduct.Product?.EAN ?? "null");
                            }
                        }
                        else
                        {
                            _logger?.LogTrace("Skipped entire row {RowIndex}: Row validation failed or marked for skipping", row.Index);
                        }
                    }
                    catch (Exception ex)
                    {
                        result.Statistics.ErrorCount++;
                        result.Errors.Add($"Row {row.Index}: {ex.Message}");
                    }
                }

                // Finalize statistics
                result.Statistics.ProcessingTime = DateTime.UtcNow - result.ProcessedAt;
                result.Statistics.WarningCount = result.Warnings.Count;


                return result;
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Processing failed: {ex.Message}");
                result.Statistics.ProcessingTime = DateTime.UtcNow - result.ProcessedAt;
                return result;
            }
        }

        /// <summary>
        /// Normalizes a single row using unified ColumnProperties configuration
        /// </summary>
        private async Task<OfferProductAnnex?> NormalizeRowAsync(
            RowData row,
            ProcessingContext context,
            string sourceFile,
            SupplierOfferAnnex supplierOffer)
        {
            try
            {
                var offerProduct = new OfferProductAnnex();
                var product = new ProductEntity();
                offerProduct.Product = product;
                offerProduct.Offer = supplierOffer;


                // Process each configured column property
                foreach (var columnConfig in _configuration.ColumnProperties)
                {
                    var columnKey = columnConfig.Key; // Excel column (A, B, C, etc.)
                    var columnProperty = columnConfig.Value;

                    // First ensure the column has a valid classification from market config
                    if (columnProperty.Classification == default)
                    {
                        _logger?.LogDebug("Skipped column {ColumnKey} in row {RowIndex}: No Classification resolved from market config", columnKey, row.Index);
                        continue;
                    }
                    
                    // Get column index
                    var columnIndex = GetColumnIndex(columnKey);
                    if (columnIndex < 0 || columnIndex >= row.Cells.Count)
                    {
                        _logger?.LogDebug("Skipped column {ColumnKey} in row {RowIndex}: Invalid column index {ColumnIndex} (row has {CellCount} cells)", 
                            columnKey, row.Index, columnIndex, row.Cells.Count);
                        continue;
                    }

                    // Extract and process cell value
                    string? stringValue = row.Cells[columnIndex].Value?.Trim();
                    
                    // Validate raw value first (before transformation)
                    var validationResult = await ValidateValueAsync(stringValue, columnProperty);
                    if (!validationResult.IsValid)
                    {
                        if (validationResult.SkipEntireRow)
                        {
                            // Skip the entire row - return null to indicate row should be skipped
                            _logger?.LogTrace("Skipping entire row {RowIndex}: Validation failed for column {ColumnKey} with value '{RawValue}' - {ErrorMessage}",
                                row.Index, columnKey, stringValue, validationResult.ErrorMessage);
                            return null;
                        }
                        // Skip just this column
                        _logger?.LogDebug("Skipped column {ColumnKey} in row {RowIndex}: Validation failed for value '{RawValue}' - {ErrorMessage}",
                            columnKey, row.Index, stringValue, validationResult.ErrorMessage);
                        continue;
                    }

                    // Apply transformations and type conversion after validation passes
                    var processedValue = await ProcessCellValueAsync(stringValue!, columnProperty);
                    if (processedValue == null && !columnProperty.AllowNull)
                    {
                        _logger?.LogDebug("Skipped column {ColumnKey} in row {RowIndex}: Processed value is null but AllowNull=false (raw='{R}'), Proccessd = {P}",
                            columnKey, row.Index, stringValue, processedValue);
                        continue;
                    }

                    switch (columnProperty.Classification)
                    {
                        case PropertyClassificationType.ProductName:
                            product.Name = processedValue?.ToString() ?? product.Name;
                            break;
                        case PropertyClassificationType.ProductEAN:
                            product.EAN = processedValue?.ToString() ?? product.EAN;
                            break;
                        case PropertyClassificationType.OfferPrice:
                            offerProduct.Price = processedValue is decimal d ? d : offerProduct.Price;
                            break;
                        case PropertyClassificationType.OfferQuantity:
                            offerProduct.Quantity = processedValue is int i ? i : offerProduct.Quantity;
                            break;
                        case PropertyClassificationType.OfferDescription:
                            offerProduct.Description = processedValue?.ToString() ?? offerProduct.Description;
                            break;
                        case PropertyClassificationType.ProductDynamic:
                            product.SetDynamicProperty(columnProperty.ProductPropertyKey, processedValue);
                            break;
                        case PropertyClassificationType.OfferDynamic:
                            offerProduct.SetOfferProperty(columnProperty.ProductPropertyKey, processedValue);
                            break;
                        default:
                            _logger?.LogDebug("Skipped column {ColumnKey} in row {RowIndex}: Unsupported Classification {Classification}",
                                columnKey, row.Index, columnProperty.Classification);
                            break;
                    }

                }

                // Extract additional properties from description if available and extractor is configured
                if (_descriptionExtractor is not null && !string.IsNullOrEmpty(offerProduct.Description))
                {
                    var extractedProperties = _descriptionExtractor.ExtractPropertiesFromDescription(offerProduct.Description);

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
                var transformedValue = ApplyTransformations(rawValue, columnProperty.Transformations);

                // Convert to target type
                if (_dataTypeConverters.TryGetValue(columnProperty.DataType.ToLowerInvariant(), out var converter))
                {
                    return converter(transformedValue);
                }

                return transformedValue;
            }
            catch (Exception)
            {
                // Return default value on conversion failure
                return columnProperty.DefaultValue;
            }
        }

        /// <summary>
        /// Validates raw string value against column validation rules (before transformation)
        /// </summary>
        private async Task<ValidationResult> ValidateValueAsync(string? rawValue, ColumnProperty columnProperty)
        {
            await Task.CompletedTask; // For async consistency

            // Check required field validation
            if (columnProperty.IsRequired == true && string.IsNullOrWhiteSpace(rawValue))
            {
                return ValidationResult.Invalid(
                    columnProperty.SkipEntireRow,
                    $"Required field '{columnProperty.ProductPropertyKey}' is empty");
            }

            // Check allowed values if configured (validate against raw values before transformation)
            if (columnProperty.AllowedValues.Count > 0)
            {
                if (!columnProperty.AllowedValues.Contains(rawValue, StringComparer.OrdinalIgnoreCase))
                {
                    return ValidationResult.Invalid(
                        columnProperty.SkipEntireRow,
                        $"Value '{rawValue}' is not in allowed values for '{columnProperty.ProductPropertyKey}'");
                }
            }

            // Check validation patterns if configured (validate against raw values)
            if (columnProperty.ValidationPatterns.Count > 0)
            {
                if (!string.IsNullOrEmpty(rawValue))
                {
                    var matchesPattern = columnProperty.ValidationPatterns
                        .Any(pattern => Regex.IsMatch(rawValue, pattern, RegexOptions.IgnoreCase));
                    if (!matchesPattern)
                    {
                        return ValidationResult.Invalid(
                            columnProperty.SkipEntireRow,
                            $"Value '{rawValue}' does not match validation patterns for '{columnProperty.ProductPropertyKey}'");
                    }
                }
            }

            return ValidationResult.Valid();
        }

        #endregion

        #region Utility Methods

        private string ApplyTransformations(string value, List<string> transformations)
        {
            if (transformations == null || !transformations.Any())
                return value;

            foreach (var transformation in transformations)
            {
                // Check if transformation has parameters (separated by :)
                var parts = transformation.Split(':', 2, StringSplitOptions.RemoveEmptyEntries);
                var transformType = parts[0].ToLowerInvariant();
                var parameters = parts.Length > 1 ? parts[1] : null;

                value = transformType switch
                {
                    "lowercase" => value.ToLowerInvariant(),
                    "uppercase" => value.ToUpperInvariant(),
                    "removesymbols" => Regex.Replace(value, @"[^\d.,]", ""),
                    "removecommas" => value.Replace(",", ""),
                    "removespaces" => value.Replace(" ", ""),
                    "extractafterpattern" => ExtractAfterPattern(value, parameters ?? "*:"),
                    "maptobool" => MapToBool(value, parameters ?? "SET:true,REG:false"),
                    _ => value
                };
            }

            return value;
        }

        private string ExtractAfterPattern(string value, string pattern)
        {
            if (string.IsNullOrWhiteSpace(value) || string.IsNullOrWhiteSpace(pattern))
                return value;

            // Handle wildcard patterns
            if (pattern.Contains("*"))
            {
                return ExtractAfterWildcardPattern(value, pattern);
            }

            // Split the pattern by pipe (|) to get individual prefixes
            var prefixes = pattern.Split('|', StringSplitOptions.RemoveEmptyEntries);

            foreach (var prefix in prefixes)
            {
                var trimmedPrefix = prefix.Trim();
                if (value.StartsWith(trimmedPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    // Extract everything after the prefix and trim whitespace
                    return value.Substring(trimmedPrefix.Length).Trim();
                }
            }

            // If no prefix is found, return the original value
            return value;
        }

        private string ExtractAfterWildcardPattern(string value, string pattern)
        {
            // Convert wildcard pattern to regex pattern
            var regexPattern = ConvertWildcardToRegex(pattern);
            
            try
            {
                var match = Regex.Match(value, regexPattern, RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    // If there are capture groups, return the first one, otherwise return the full match
                    if (match.Groups.Count > 1)
                    {
                        return match.Groups[1].Value.Trim();
                    }
                    return match.Value.Trim();
                }
            }
            catch (ArgumentException ex)
            {
                // Log regex error and return original value
                _logger?.LogWarning("Invalid regex pattern '{RegexPattern}' for wildcard '{WildcardPattern}': {ErrorMessage}",
                    regexPattern, pattern, ex.Message);
            }
            
            // If no match or error, return original value
            return value;
        }

        private string ConvertWildcardToRegex(string wildcardPattern)
        {
            // Handle common wildcard patterns and convert them to regex
            // Available patterns:
            // "*:" - Extract text between first and second colon (e.g., "REGULARs:D&G:P1DV1C02" → "D&G")
            // "*:*" - Same as above but more explicit
            // "after:" - Extract everything after first colon
            // "between:|" - Extract text between first and second pipe
            // "between:;" - Extract text between first and second semicolon
            // "between:X" - Extract text between first and second occurrence of character X
            // "prefix:*" - Extract everything after any uppercase prefix followed by colon
            return wildcardPattern switch
            {
                "*:" => @"^[^:]*:([^:]+):", // Extract text between first and second colon
                "*:*" => @"^[^:]*:([^:]+):.*", // Extract text between first and second colon (same as above but more explicit)
                "after:" => @"^[^:]*:(.+)$", // Extract everything after first colon
                "between:|" => @"^[^|]*\|([^|]+)\|", // Extract text between first and second pipe
                "between:;" => @"^[^;]*;([^;]+);", // Extract text between first and second semicolon
                "prefix:*" => @"^[A-Z]+:(.+)$", // Extract everything after any uppercase prefix followed by colon
                _ => HandleCustomWildcardPattern(wildcardPattern)
            };
        }

        private string HandleCustomWildcardPattern(string pattern)
        {
            // Handle more complex patterns
            if (pattern.StartsWith("between:") && pattern.Length > 8)
            {
                var delimiter = pattern.Substring(8, 1);
                var escapedDelimiter = Regex.Escape(delimiter);
                return $@"^[^{escapedDelimiter}]*{escapedDelimiter}([^{escapedDelimiter}]+){escapedDelimiter}";
            }
            
            // For simple wildcard patterns, convert * to .* and escape special chars
            var regexPattern = Regex.Escape(pattern).Replace(@"\*", "(.*)");
            return regexPattern;
        }

        private string MapToBool(string value, string mappingParameters)
        {
            if (string.IsNullOrWhiteSpace(value) || string.IsNullOrWhiteSpace(mappingParameters))
                return value;

            // Parse mapping parameters: "SET:true,REG:false"
            var mappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            
            var pairs = mappingParameters.Split(',', StringSplitOptions.RemoveEmptyEntries);
            foreach (var pair in pairs)
            {
                var keyValue = pair.Split(':', 2, StringSplitOptions.RemoveEmptyEntries);
                if (keyValue.Length == 2)
                {
                    mappings[keyValue[0].Trim()] = keyValue[1].Trim();
                }
            }

            // Look up the value in the mappings
            if (mappings.TryGetValue(value.Trim(), out var mappedValue))
            {
                return mappedValue;
            }

            // Return original value if no mapping found
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
                ["integer"] = value => int.TryParse(value, out int result) ? result : null, // Alias for int
                ["decimal"] = value => decimal.TryParse(value, NumberStyles.Currency, CultureInfo.InvariantCulture, out decimal result) ? result : null,
                ["bool"] = value => ParseBooleanValue(value),
                ["boolean"] = value => ParseBooleanValue(value), // Alias for bool
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

            // If no meaningful properties found, use EAN or fallback
            if (!nameComponents.Any())
            {
                if (!string.IsNullOrWhiteSpace(product.EAN))
                {
                    nameComponents.Add($"Product {product.EAN}");
                }
            }

            return nameComponents.Any() ? string.Join(" ", nameComponents) : "Unknown Product";
        }

        /// <summary>
        /// Validates an offer product for supplier offers
        /// </summary>
        private bool IsValidOfferProduct(OfferProductAnnex offerProduct)
        {
            // For supplier offers, require valid product with EAN
            return offerProduct.Product != null &&
                   !string.IsNullOrEmpty(offerProduct.Product.EAN) &&
                   offerProduct.Price > 0 &&
                   offerProduct.Quantity > 0;
        }

        #endregion
    }
}
