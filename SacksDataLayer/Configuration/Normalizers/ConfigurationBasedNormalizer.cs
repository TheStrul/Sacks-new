using SacksDataLayer.FileProcessing.Interfaces;
using SacksDataLayer.FileProcessing.Configuration;
using SacksDataLayer.FileProcessing.Services;
using SacksDataLayer.FileProcessing.Models;
using SacksAIPlatform.InfrastructuresLayer.FileProcessing;
using System.Globalization;
using System.Text.RegularExpressions;

namespace SacksDataLayer.FileProcessing.Normalizers
{
    /// <summary>
    /// Configuration-driven normalizer that uses JSON configuration files instead of hardcoded logic
    /// </summary>
    public class ConfigurationBasedNormalizer : ISupplierProductNormalizer
    {
        private readonly SupplierConfiguration _configuration;
        private readonly Dictionary<string, Func<string, object?>> _dataTypeConverters;

        public string SupplierName => _configuration.Name;

        public IEnumerable<ProcessingMode> SupportedModes => _configuration.ProcessingModes.SupportedModes;

        public ConfigurationBasedNormalizer(SupplierConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _dataTypeConverters = InitializeDataTypeConverters();
        }

        public bool CanHandle(string fileName, IEnumerable<RowData> firstFewRows)
        {
            var detection = _configuration.Detection;

            // Check exclude patterns first
            if (detection.ExcludePatterns.Any(pattern => IsPatternMatch(fileName, pattern)))
            {
                return false;
            }

            // Check filename patterns
            if (detection.FileNamePatterns.Any(pattern => IsPatternMatch(fileName, pattern)))
            {
                return true;
            }

            // Check header keywords
            if (detection.HeaderKeywords.Any())
            {
                var headerRow = firstFewRows.FirstOrDefault(r => r.HasData);
                if (headerRow != null)
                {
                    var headers = headerRow.Cells.Select(c => c.Value?.Trim() ?? "").ToList();
                    var hasKeyword = detection.HeaderKeywords.Any(keyword =>
                        headers.Any(h => h.Contains(keyword, StringComparison.OrdinalIgnoreCase)));
                    
                    if (hasKeyword)
                        return true;
                }
            }

            // Check required columns
            if (detection.RequiredColumns.Any())
            {
                var headerRow = firstFewRows.FirstOrDefault(r => r.HasData);
                if (headerRow != null)
                {
                    var headers = headerRow.Cells.Select(c => c.Value?.Trim() ?? "").ToList();
                    var hasRequiredColumns = detection.RequiredColumns.All(required =>
                        headers.Any(h => string.Equals(h, required, StringComparison.OrdinalIgnoreCase)));
                    
                    if (hasRequiredColumns)
                        return true;
                }
            }

            return false;
        }

        public async Task<IEnumerable<ProductEntity>> NormalizeAsync(FileData fileData)
        {
            var products = new List<ProductEntity>();
            var errorCount = 0;
            var maxErrors = _configuration.Validation.MaxErrorsPerFile;

            if (fileData.dataRows.Count == 0)
                return products;

            // Find header row based on configuration
            var headerRowIndex = _configuration.Transformation.HeaderRowIndex;
            var headerRow = fileData.dataRows.Skip(headerRowIndex).FirstOrDefault(r => r.HasData);
            
            if (headerRow == null)
                return products;

            // Create column index mapping
            var columnIndexes = CreateColumnIndexMapping(headerRow);

            // Process data rows starting from configured index
            var dataStartIndex = Math.Max(_configuration.Transformation.DataStartRowIndex, headerRow.Index + 1);
            var dataRows = fileData.dataRows
                .Skip(dataStartIndex)
                .Where(r => !_configuration.Transformation.SkipEmptyRows || r.HasData);

            foreach (var row in dataRows)
            {
                try
                {
                    var product = await NormalizeRowAsync(row, columnIndexes, fileData.FilePath);
                    if (product != null)
                    {
                        // Validate the product
                        if (IsValidProduct(product))
                        {
                            products.Add(product);
                        }
                        else if (_configuration.Validation.SkipRowsWithoutName)
                        {
                            continue; // Skip invalid products silently
                        }
                    }
                }
                catch (Exception ex)
                {
                    errorCount++;
                    Console.WriteLine($"Error processing row {row.Index}: {ex.Message}");
                    
                    if (errorCount >= maxErrors)
                    {
                        Console.WriteLine($"Reached maximum error limit ({maxErrors}). Stopping processing.");
                        break;
                    }
                }
            }

            return products;
        }

        public Dictionary<string, string> GetColumnMapping()
        {
            return new Dictionary<string, string>(_configuration.ColumnMappings);
        }

        public Dictionary<string, string> GetColumnMapping(ProcessingMode mode = ProcessingMode.UnifiedProductCatalog)
        {
            var baseMapping = new Dictionary<string, string>(_configuration.ColumnMappings);
            
            // Apply mode-specific column filtering
            var modeConfig = mode == ProcessingMode.UnifiedProductCatalog 
                ? _configuration.ProcessingModes.CatalogMode 
                : _configuration.ProcessingModes.CommercialMode;

            // Remove ignored columns for this mode
            foreach (var ignoredColumn in modeConfig.IgnoredColumns)
            {
                baseMapping.Remove(ignoredColumn);
            }

            return baseMapping;
        }

        public ProcessingMode RecommendProcessingMode(string fileName, IEnumerable<RowData> firstFewRows)
        {
            // Check file name patterns for commercial data indicators
            var commercialIndicators = new[] { "price", "stock", "inventory", "offer", "quote", "commercial" };
            var catalogIndicators = new[] { "catalog", "product", "master", "reference" };

            var fileNameLower = fileName.ToLowerInvariant();
            
            if (commercialIndicators.Any(indicator => fileNameLower.Contains(indicator)))
            {
                return ProcessingMode.SupplierCommercialData;
            }

            if (catalogIndicators.Any(indicator => fileNameLower.Contains(indicator)))
            {
                return ProcessingMode.UnifiedProductCatalog;
            }

            // Check header presence for mode detection
            var headerRow = firstFewRows.FirstOrDefault(r => r.HasData);
            if (headerRow != null)
            {
                var headers = headerRow.Cells.Select(c => c.Value?.Trim()?.ToLowerInvariant() ?? "").ToList();
                
                // If file has strong commercial focus, recommend commercial mode
                var commercialHeaderCount = headers.Count(h => 
                    h.Contains("price") || h.Contains("stock") || h.Contains("inventory") || 
                    h.Contains("cost") || h.Contains("offer") || h.Contains("available"));

                var catalogHeaderCount = headers.Count(h => 
                    h.Contains("name") || h.Contains("description") || h.Contains("category") || 
                    h.Contains("family") || h.Contains("specification"));

                if (commercialHeaderCount > catalogHeaderCount && commercialHeaderCount >= 2)
                {
                    return ProcessingMode.SupplierCommercialData;
                }
            }

            // Default to catalog mode
            return _configuration.ProcessingModes.DefaultMode;
        }

        public async Task<ProcessingResult> NormalizeAsync(FileData fileData, ProcessingContext context)
        {
            var startTime = DateTime.UtcNow;
            var result = new ProcessingResult
            {
                Mode = context.Mode,
                SourceFile = context.SourceFileName,
                SupplierName = SupplierName,
                ProcessedAt = startTime
            };

            try
            {
                var products = new List<ProductEntity>();
                var statistics = new ProcessingStatistics();
                var warnings = new List<string>();
                var errors = new List<string>();

                if (fileData.dataRows.Count == 0)
                {
                    result.Statistics = statistics;
                    return result;
                }

                // Get mode-specific configuration
                var modeConfig = context.Mode == ProcessingMode.UnifiedProductCatalog 
                    ? _configuration.ProcessingModes.CatalogMode 
                    : _configuration.ProcessingModes.CommercialMode;

                // Find header row
                var headerRowIndex = _configuration.Transformation.HeaderRowIndex;
                var headerRow = fileData.dataRows.Skip(headerRowIndex).FirstOrDefault(r => r.HasData);
                
                if (headerRow == null)
                {
                    errors.Add("No valid header row found");
                    result.Errors = errors;
                    return result;
                }

                // Create mode-specific column mapping
                var columnIndexes = CreateModeSpecificColumnMapping(headerRow, context.Mode);
                statistics.TotalRowsProcessed = fileData.dataRows.Count;

                // Process data rows
                var dataStartIndex = Math.Max(_configuration.Transformation.DataStartRowIndex, headerRow.Index + 1);
                var dataRows = fileData.dataRows
                    .Skip(dataStartIndex)
                    .Where(r => !_configuration.Transformation.SkipEmptyRows || r.HasData);

                foreach (var row in dataRows)
                {
                    try
                    {
                        var product = await NormalizeModeSpecificRowAsync(row, columnIndexes, context, fileData.FilePath);
                        if (product != null)
                        {
                            if (IsValidProductForMode(product, context.Mode))
                            {
                                products.Add(product);
                                statistics.ProductsCreated++;
                                
                                // Update mode-specific statistics
                                if (context.Mode == ProcessingMode.UnifiedProductCatalog)
                                {
                                    statistics.UniqueProductsIdentified++;
                                }
                                else
                                {
                                    statistics.PricingRecordsProcessed++;
                                    if (product.DynamicProperties.ContainsKey("StockQuantity"))
                                    {
                                        statistics.StockRecordsProcessed++;
                                    }
                                }
                            }
                            else
                            {
                                statistics.ProductsSkipped++;
                                if (context.Mode == ProcessingMode.UnifiedProductCatalog)
                                {
                                    statistics.MissingCoreAttributes++;
                                }
                                else
                                {
                                    statistics.OrphanedCommercialRecords++;
                                }
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

                result.Products = products;
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

        private Dictionary<string, int> CreateColumnIndexMapping(RowData headerRow)
        {
            var mapping = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            
            for (int i = 0; i < headerRow.Cells.Count; i++)
            {
                var columnName = headerRow.Cells[i].Value?.Trim() ?? "";
                if (!string.IsNullOrEmpty(columnName))
                {
                    mapping[columnName] = i;
                }
            }

            return mapping;
        }

        private async Task<ProductEntity?> NormalizeRowAsync(RowData row, Dictionary<string, int> columnIndexes, string filePath)
        {
            var product = new ProductEntity();

            try
            {
                // Map core properties (Name, Description, SKU)
                product.Name = GetMappedValue(row, columnIndexes, "Name") ?? "Unknown Product";
                product.Description = GetMappedValue(row, columnIndexes, "Description");
                product.SKU = GetMappedValue(row, columnIndexes, "SKU");

                // Apply transformations to core properties
                if (_configuration.Transformation.TrimWhitespace)
                {
                    product.Name = product.Name?.Trim() ?? "";
                    product.Description = product.Description?.Trim();
                    product.SKU = product.SKU?.Trim();
                }

                // Process all mapped columns as dynamic properties
                foreach (var mapping in _configuration.ColumnMappings)
                {
                    var sourceColumn = mapping.Key;
                    var targetProperty = mapping.Value;
                    
                    // Skip core properties as they're already handled
                    if (targetProperty is "Name" or "Description" or "SKU")
                        continue;

                    var rawValue = GetCellValue(row, columnIndexes, sourceColumn);
                    if (!string.IsNullOrEmpty(rawValue))
                    {
                        var convertedValue = ConvertValue(targetProperty, rawValue);
                        if (convertedValue != null)
                        {
                            // Only set core product properties on the product entity
                            // Offer properties will be handled separately by the processor
                            if (!_configuration.PropertyClassification.OfferProperties.Contains(targetProperty))
                            {
                                product.SetDynamicProperty(targetProperty, convertedValue);
                            }
                        }
                    }
                    else
                    {
                        // Set default value if configured
                        SetDefaultValue(product, targetProperty);
                    }
                }

                // Add unmapped columns as dynamic properties
                await AddUnmappedPropertiesAsync(product, row, columnIndexes);

                // Note: No metadata is added to DynamicProperties - they should contain only product attributes
                // Metadata like SourceFile, ProcessingMode, etc. are handled separately by the processing layer

                return product;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error normalizing row {row.Index}: {ex.Message}");
                return null;
            }
        }

        private string? GetMappedValue(RowData row, Dictionary<string, int> columnIndexes, string targetProperty)
        {
            // Find all source columns that map to the target property
            var sourceColumns = _configuration.ColumnMappings
                .Where(kvp => kvp.Value == targetProperty)
                .Select(kvp => kvp.Key);

            foreach (var sourceColumn in sourceColumns)
            {
                var value = GetCellValue(row, columnIndexes, sourceColumn);
                if (!string.IsNullOrEmpty(value))
                {
                    return value;
                }
            }

            return null;
        }

        private string? GetCellValue(RowData row, Dictionary<string, int> columnIndexes, string columnName)
        {
            if (columnIndexes.TryGetValue(columnName, out int index) && index < row.Cells.Count)
            {
                var value = row.Cells[index].Value?.Trim();
                return string.IsNullOrEmpty(value) ? null : value;
            }
            return null;
        }

        private object? ConvertValue(string targetProperty, string rawValue)
        {
            if (string.IsNullOrWhiteSpace(rawValue))
                return null;

            // Get data type configuration for this property
            if (!_configuration.DataTypes.TryGetValue(targetProperty, out var dataTypeConfig))
            {
                return rawValue; // Return as string if no type configuration
            }

            try
            {
                // Apply transformations
                var transformedValue = ApplyTransformations(rawValue, dataTypeConfig.Transformations);

                // Convert to target type
                if (_dataTypeConverters.TryGetValue(dataTypeConfig.Type.ToLowerInvariant(), out var converter))
                {
                    return converter(transformedValue);
                }

                return transformedValue; // Return as string if no converter found
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error converting value '{rawValue}' for property '{targetProperty}': {ex.Message}");
                
                // Return default value if conversion fails
                if (dataTypeConfig.DefaultValue != null)
                {
                    return dataTypeConfig.DefaultValue;
                }

                return dataTypeConfig.AllowNull ? null : rawValue;
            }
        }

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

        private void SetDefaultValue(ProductEntity product, string targetProperty)
        {
            if (_configuration.DataTypes.TryGetValue(targetProperty, out var dataTypeConfig) &&
                dataTypeConfig.DefaultValue != null)
            {
                // Determine if this is a core product property or offer property
                // Only set core product properties default values
                // Offer properties will be handled separately
                if (!_configuration.PropertyClassification.OfferProperties.Contains(targetProperty))
                {
                    product.SetDynamicProperty(targetProperty, dataTypeConfig.DefaultValue);
                }
            }
        }

        private async Task AddUnmappedPropertiesAsync(ProductEntity product, RowData row, Dictionary<string, int> columnIndexes)
        {
            var mappedColumns = new HashSet<string>(_configuration.ColumnMappings.Keys, StringComparer.OrdinalIgnoreCase);

            foreach (var kvp in columnIndexes)
            {
                if (!mappedColumns.Contains(kvp.Key) && kvp.Value < row.Cells.Count)
                {
                    var value = row.Cells[kvp.Value].Value?.Trim();
                    if (!string.IsNullOrEmpty(value))
                    {
                        var cleanKey = CleanPropertyKey(kvp.Key);
                        product.SetDynamicProperty(cleanKey, value);
                    }
                }
            }

            await Task.CompletedTask;
        }

        private string CleanPropertyKey(string key)
        {
            return key.Replace(" ", "")
                      .Replace("-", "")
                      .Replace("_", "")
                      .Replace("(", "")
                      .Replace(")", "")
                      .Replace("[", "")
                      .Replace("]", "")
                      .Trim();
        }

        private bool IsValidProduct(ProductEntity product)
        {
            var validation = _configuration.Validation;

            // Skip rows that look like headers (e.g., "Item Name", "Item Code")
            if (IsHeaderRow(product))
                return false;

            // Check required fields
            foreach (var requiredField in validation.RequiredFields)
            {
                if (requiredField == "Name")
                {
                    if (string.IsNullOrWhiteSpace(product.Name))
                        return false;
                }
                else
                {
                    if (!product.HasDynamicProperty(requiredField))
                        return false;
                }
            }

            // Apply field validations
            foreach (var fieldValidation in validation.FieldValidations)
            {
                if (!ValidateField(product, fieldValidation.Key, fieldValidation.Value))
                    return false;
            }

            return true;
        }

        private bool IsHeaderRow(ProductEntity product)
        {
            // Check if product name/SKU matches known column headers
            var headerKeywords = new[] { "Item Name", "Item Code", "PRICE", "Category", "Family", "Commercial Line", "Pricing Item Name", "Size", "Unit", "EAN", "Capacity" };
            
            if (headerKeywords.Any(keyword => string.Equals(product.Name, keyword, StringComparison.OrdinalIgnoreCase) ||
                                               string.Equals(product.SKU, keyword, StringComparison.OrdinalIgnoreCase)))
                return true;

            // Check if multiple dynamic properties contain header-like values
            var headerMatches = 0;
            foreach (var prop in product.DynamicProperties)
            {
                if (headerKeywords.Any(keyword => string.Equals(prop.Value?.ToString(), keyword, StringComparison.OrdinalIgnoreCase)))
                {
                    headerMatches++;
                }
            }

            // If more than 50% of properties match header keywords, it's likely a header row
            return headerMatches > product.DynamicProperties.Count / 2 && headerMatches >= 3;
        }

        private bool ValidateField(ProductEntity product, string fieldName, FieldValidation validation)
        {
            object? value = fieldName == "Name" ? product.Name : product.GetDynamicProperty(fieldName);
            
            if (value == null)
                return true; // Allow null values (handled by required fields check)

            var stringValue = value.ToString() ?? "";

            // Length validations
            if (validation.MinLength.HasValue && stringValue.Length < validation.MinLength.Value)
                return false;
            
            if (validation.MaxLength.HasValue && stringValue.Length > validation.MaxLength.Value)
                return false;

            // Pattern validation
            if (!string.IsNullOrEmpty(validation.Pattern))
            {
                if (!Regex.IsMatch(stringValue, validation.Pattern))
                    return false;
            }

            // Allowed values validation
            if (validation.AllowedValues?.Any() == true)
            {
                if (!validation.AllowedValues.Contains(stringValue, StringComparer.OrdinalIgnoreCase))
                    return false;
            }

            // Numeric range validation
            if (validation.NumericRange != null && decimal.TryParse(stringValue, out decimal numValue))
            {
                if (validation.NumericRange.Min.HasValue && numValue < validation.NumericRange.Min.Value)
                    return false;
                
                if (validation.NumericRange.Max.HasValue && numValue > validation.NumericRange.Max.Value)
                    return false;
            }

            return true;
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

        private Dictionary<string, int> CreateModeSpecificColumnMapping(RowData headerRow, ProcessingMode mode)
        {
            var mapping = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var modeConfig = mode == ProcessingMode.UnifiedProductCatalog 
                ? _configuration.ProcessingModes.CatalogMode 
                : _configuration.ProcessingModes.CommercialMode;
            
            for (int i = 0; i < headerRow.Cells.Count; i++)
            {
                var columnName = headerRow.Cells[i].Value?.Trim() ?? "";
                if (!string.IsNullOrEmpty(columnName))
                {
                    // Skip ignored columns for this mode
                    if (modeConfig.IgnoredColumns.Contains(columnName, StringComparer.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    mapping[columnName] = i;
                }
            }

            return mapping;
        }

        private Task<ProductEntity?> NormalizeModeSpecificRowAsync(RowData row, Dictionary<string, int> columnIndexes, ProcessingContext context, string sourceFile)
        {
            try
            {
                var product = new ProductEntity();
                // Note: No metadata added to DynamicProperties - they should contain only product attributes

                var modeConfig = context.Mode == ProcessingMode.UnifiedProductCatalog 
                    ? _configuration.ProcessingModes.CatalogMode 
                    : _configuration.ProcessingModes.CommercialMode;

                // Process mapped columns based on mode priorities
                var columnMappings = GetColumnMapping(context.Mode);
                foreach (var mapping in columnMappings)
                {
                    var sourceColumn = mapping.Key;
                    var targetProperty = mapping.Value;

                    if (columnIndexes.TryGetValue(sourceColumn, out int columnIndex) && 
                        columnIndex < row.Cells.Count)
                    {
                        var cellValue = row.Cells[columnIndex].Value?.Trim();
                        
                        if (!string.IsNullOrEmpty(cellValue))
                        {
                            var convertedValue = ConvertValue(targetProperty, cellValue);
                            
                            // Set core properties or dynamic properties based on target
                            switch (targetProperty.ToLowerInvariant())
                            {
                                case "name":
                                    product.Name = convertedValue?.ToString() ?? "";
                                    break;
                                case "description":
                                    product.Description = convertedValue?.ToString();
                                    break;
                                case "sku":
                                    product.SKU = convertedValue?.ToString();
                                    break;
                                default:
                                    // Only set core product properties  
                                    // Offer properties will be handled separately
                                    if (!_configuration.PropertyClassification.OfferProperties.Contains(targetProperty))
                                    {
                                        product.SetDynamicProperty(targetProperty, convertedValue);
                                    }
                                    break;
                            }
                        }
                    }
                }

                // Apply mode-specific processing rules
                if (context.Mode == ProcessingMode.UnifiedProductCatalog)
                {
                    // For catalog mode, ensure we have minimum required product info
                    if (string.IsNullOrEmpty(product.Name))
                    {
                        // Try to construct name from available fields
                        var nameComponents = new List<string>();
                        
                        if (product.DynamicProperties.TryGetValue("Family", out var family) && family != null)
                            nameComponents.Add(family.ToString()!);
                        if (product.DynamicProperties.TryGetValue("Category", out var category) && category != null)
                            nameComponents.Add(category.ToString()!);
                        if (product.DynamicProperties.TryGetValue("PricingItemName", out var itemName) && itemName != null)
                            nameComponents.Add(itemName.ToString()!);

                        if (nameComponents.Any())
                        {
                            product.Name = string.Join(" - ", nameComponents);
                        }
                        else
                        {
                            product.Name = "Unknown Product";
                        }
                    }

                    // Remove pricing/commercial data in catalog mode
                    var commercialProps = new[] { "Price", "Stock", "StockQuantity", "Available", "Cost", "Offer" };
                    foreach (var prop in commercialProps)
                    {
                        product.DynamicProperties.Remove(prop);
                    }
                }
                else if (context.Mode == ProcessingMode.SupplierCommercialData)
                {
                    // For commercial mode, ensure we have pricing or stock info
                    var hasCommercialData = product.DynamicProperties.Keys.Any(k => 
                        k.Contains("Price", StringComparison.OrdinalIgnoreCase) ||
                        k.Contains("Stock", StringComparison.OrdinalIgnoreCase) ||
                        k.Contains("Available", StringComparison.OrdinalIgnoreCase));

                    if (!hasCommercialData)
                    {
                        return Task.FromResult<ProductEntity?>(null); // Skip rows without commercial data in commercial mode
                    }

                    // Note: No supplier metadata added to DynamicProperties
                }

                return Task.FromResult<ProductEntity?>(product);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to normalize row {row.Index}: {ex.Message}", ex);
            }
        }

        private bool IsValidProductForMode(ProductEntity product, ProcessingMode mode)
        {
            if (mode == ProcessingMode.UnifiedProductCatalog)
            {
                // For catalog mode, require minimum product identification
                return !string.IsNullOrEmpty(product.Name) && product.Name != "Unknown Product";
            }
            else
            {
                // For commercial mode, require some commercial data
                return product.DynamicProperties.Keys.Any(k => 
                    k.Contains("Price", StringComparison.OrdinalIgnoreCase) ||
                    k.Contains("Stock", StringComparison.OrdinalIgnoreCase) ||
                    k.Contains("Available", StringComparison.OrdinalIgnoreCase));
            }
        }

        /// <summary>
        /// Extracts offer properties from a row for creating SupplierOffer entities
        /// </summary>
        public Dictionary<string, object?> ExtractOfferProperties(RowData row, Dictionary<string, int> columnIndexes)
        {
            var offerProperties = new Dictionary<string, object?>();

            foreach (var mapping in _configuration.ColumnMappings)
            {
                var sourceColumn = mapping.Key;
                var targetProperty = mapping.Value;

                // Only process offer properties
                if (_configuration.PropertyClassification.OfferProperties.Contains(targetProperty))
                {
                    var rawValue = GetCellValue(row, columnIndexes, sourceColumn);
                    if (!string.IsNullOrEmpty(rawValue))
                    {
                        var convertedValue = ConvertValue(targetProperty, rawValue);
                        if (convertedValue != null)
                        {
                            offerProperties[targetProperty] = convertedValue;
                        }
                    }
                }
            }

            return offerProperties;
        }
    }
}