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
    public class ConfigurationBasedNormalizer : ISupplierProductNormalizer
    {
        private readonly SupplierConfiguration _configuration;
        private readonly Dictionary<string, Func<string, object?>> _dataTypeConverters;
        private readonly ConfigurationBasedDescriptionPropertyExtractor? _descriptionExtractor;

        public string SupplierName => _configuration.Name;

        public ConfigurationBasedNormalizer(SupplierConfiguration configuration, ConfigurationBasedPropertyNormalizer? propertyNormalizer = null)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _dataTypeConverters = InitializeDataTypeConverters();

            // Initialize description extractor if ConfigurationBasedPropertyNormalizer is available
            _descriptionExtractor = propertyNormalizer != null ? new ConfigurationBasedDescriptionPropertyExtractor(propertyNormalizer.Configuration) : null;
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
                var allDataRows = fileData.DataRows.Skip(dataStartIndex).ToList();
                var emptyRowsSkipped = allDataRows.Count(r => !r.HasData);
                var dataRows = allDataRows.Where(r => r.HasData);

                if (emptyRowsSkipped > 0)
                {
                    Console.WriteLine($"🔄 DEBUG: Skipped {emptyRowsSkipped} empty rows after row {dataStartIndex}");
                }

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
                                                 offerProduct.Quantity.HasValue ||
                                                 offerProduct.OfferProperties.Count > 0; if (hasOfferData)
                                {
                                    statistics.OfferProductsCreated++;
                                }
                                if (offerProduct.OfferProperties.ContainsKey("StockQuantity"))
                                {
                                    statistics.StockRecordsProcessed++;
                                }
                            }
                            else
                            {
                                statistics.ProductsSkipped++;
                                statistics.OrphanedCommercialRecords++;
                                Console.WriteLine($"🔄 DEBUG: Skipped invalid product in row {row.Index}: Product='{offerProduct.Product?.Name ?? "null"}', EAN='{offerProduct.Product?.EAN ?? "null"}' (missing required data)");
                            }
                        }
                        else
                        {
                            //Console.WriteLine($"🔄 DEBUG: Skipped entire row {row.Index}: Row validation failed or marked for skipping");
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
                    {
                        Console.WriteLine($"🔄 DEBUG: Skipped column {columnKey} in row {row.Index}: No target property defined");
                        continue;
                    }

                    // Get column index
                    var columnIndex = GetColumnIndex(columnKey);
                    if (columnIndex < 0 || columnIndex >= row.Cells.Count)
                    {
                        Console.WriteLine($"🔄 DEBUG: Skipped column {columnKey} in row {row.Index}: Invalid column index {columnIndex} (row has {row.Cells.Count} cells)");
                        continue;
                    }

                    // Extract and process cell value
                    var rawValue = row.Cells[columnIndex].Value?.Trim();
                    if (string.IsNullOrEmpty(rawValue))
                    {
                        // Handle default values for empty cells
                        await SetDefaultValueIfConfiguredAsync(product, offerProperties, columnProperty, targetProperty);
                        continue;
                    }

                    // Validate raw value first (before transformation)
                    var validationResult = await ValidateValueAsync(rawValue, columnProperty, targetProperty);
                    if (!validationResult.IsValid)
                    {
                        if (validationResult.SkipEntireRow)
                        {
                            // Skip the entire row - return null to indicate row should be skipped
                            //Console.WriteLine($"🔄 DEBUG: Skipping entire row {row.Index}: Validation failed for column {columnKey} with value '{rawValue}' - {validationResult.ErrorMessage}");
                            return null;
                        }
                        // Skip just this column
                        Console.WriteLine($"🔄 DEBUG: Skipped column {columnKey} in row {row.Index}: Validation failed for value '{rawValue}' - {validationResult.ErrorMessage}");
                        continue;
                    }

                    // Apply transformations and type conversion after validation passes
                    var processedValue = await ProcessCellValueAsync(rawValue, columnProperty);
                    if (processedValue == null && !columnProperty.AllowNull)
                    {
                        Console.WriteLine($"🔄 DEBUG: Skipped column {columnKey} in row {row.Index}: Processed value is null but AllowNull=false (raw='{rawValue}')");
                        continue;
                    }

                    // Skip the second validation since we already validated the raw value
                    // The processedValue is now ready to be classified and assigned

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
        private async Task<ValidationResult> ValidateValueAsync(string rawValue, ColumnProperty columnProperty, string targetProperty)
        {
            await Task.CompletedTask; // For async consistency

            // Check required field validation
            if (columnProperty.IsRequired == true && string.IsNullOrWhiteSpace(rawValue))
            {
                return ValidationResult.Invalid(
                    columnProperty.SkipEntireRow,
                    $"Required field '{targetProperty}' is empty");
            }

            // Check allowed values if configured (validate against raw values before transformation)
            if (columnProperty.AllowedValues.Count > 0)
            {
                if (!columnProperty.AllowedValues.Contains(rawValue, StringComparer.OrdinalIgnoreCase))
                {
                    return ValidationResult.Invalid(
                        columnProperty.SkipEntireRow,
                        $"Value '{rawValue}' is not in allowed values for '{targetProperty}'");
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
                            $"Value '{rawValue}' does not match validation patterns for '{targetProperty}'");
                    }
                }
            }

            return ValidationResult.Valid();
        }

        /// <summary>
        /// Validates processed value against column validation rules (after transformation) - Legacy method
        /// </summary>
        private async Task<ValidationResult> ValidateValueAsync(object? value, ColumnProperty columnProperty, string targetProperty)
        {
            await Task.CompletedTask; // For async consistency

            // Check required field validation
            if (columnProperty.IsRequired == true && value == null)
            {
                return ValidationResult.Invalid(
                    columnProperty.SkipEntireRow,
                    $"Required field '{targetProperty}' is null");
            }

            // Check allowed values if configured
            if (columnProperty.AllowedValues.Count > 0)
            {
                var stringValue = value?.ToString();
                if (!columnProperty.AllowedValues.Contains(stringValue, StringComparer.OrdinalIgnoreCase))
                {
                    return ValidationResult.Invalid(
                        columnProperty.SkipEntireRow,
                        $"Value '{stringValue}' is not in allowed values for '{targetProperty}'");
                }
            }

            // Check validation patterns if configured
            if (columnProperty.ValidationPatterns.Count > 0)
            {
                var stringValue = value?.ToString();
                if (!string.IsNullOrEmpty(stringValue))
                {
                    var matchesPattern = columnProperty.ValidationPatterns
                        .Any(pattern => Regex.IsMatch(stringValue, pattern, RegexOptions.IgnoreCase));
                    if (!matchesPattern)
                    {
                        return ValidationResult.Invalid(
                            columnProperty.SkipEntireRow,
                            $"Value '{stringValue}' does not match validation patterns for '{targetProperty}'");
                    }
                }
            }

            return ValidationResult.Valid();
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

            if (columnProperty.DefaultValue != null)
            {
                await AssignValueByClassificationAsync(
                    product, offerProperties, targetProperty,
                    columnProperty.DefaultValue, columnProperty.Classification);
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
                    if (classification == "coreproduct")
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
                Console.WriteLine($"🔄 DEBUG: Invalid regex pattern '{regexPattern}' for wildcard '{pattern}': {ex.Message}");
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
        /// Creates a SupplierOffer entity from processing context
        /// </summary>
        private SupplierOfferEntity CreateSupplierOfferFromContext(ProcessingContext context)
        {
            return new SupplierOfferEntity
            {
                OfferName = $"{SupplierName} - {context.SourceFileName}",
                Description = $"Offer created from file: {context.SourceFileName}",
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
                    case "quantity":
                        if (int.TryParse(prop.Value?.ToString(), out int quantity))
                            offerProduct.Quantity = quantity;
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
                    offerProduct.SetOfferProperty(prop.Key, prop.Value);
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
