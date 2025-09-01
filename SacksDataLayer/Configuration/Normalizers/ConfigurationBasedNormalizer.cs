using SacksDataLayer.FileProcessing.Interfaces;
using SacksDataLayer.FileProcessing.Configuration;
using SacksDataLayer.FileProcessing.Services;
using SacksDataLayer.FileProcessing.Models;
using SacksDataLayer.Configuration.Normalizers;
using SacksDataLayer.Entities;
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

        public ConfigurationBasedNormalizer(SupplierConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _dataTypeConverters = InitializeDataTypeConverters();
        }

        public bool CanHandle(string fileName, IEnumerable<RowData> firstFewRows)
        {
            var detection = _configuration.Detection;

            // Convert filename to lowercase for pattern matching
            var lowerFileName = fileName.ToLowerInvariant();

            // Check filename patterns against lowercase filename
            return detection.FileNamePatterns.Any(pattern => 
                IsPatternMatch(lowerFileName, pattern.ToLowerInvariant()));
        }

        public async Task<IEnumerable<ProductEntity>> NormalizeAsync(FileData fileData)
        {
            var products = new List<ProductEntity>();

            if (fileData.dataRows.Count == 0)
                return products;

            // Validate that we have the expected number of columns in the first row
            var firstRow = fileData.dataRows.FirstOrDefault(r => r.HasData);
            if (firstRow == null)
                return products;

            if (_configuration.Validation.ExpectedColumnCount > 0 && 
                firstRow.Cells.Count != _configuration.Validation.ExpectedColumnCount)
            {
                throw new InvalidOperationException(
                    $"Expected {_configuration.Validation.ExpectedColumnCount} columns but found {firstRow.Cells.Count}");
            }

            // Create column mapping using Excel column indexes
            var columnIndexes = CreateColumnIndexMapping();

            // Process data rows starting from configured index (convert from 1-based to 0-based)
            var dataStartIndex = _configuration.Validation.DataStartRowIndex - 1;
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
                        products.Add(product);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing row {row.Index}: {ex.Message}");
                    // Continue processing other rows instead of stopping
                }
            }

            return products;
        }

        public Dictionary<string, string> GetColumnMapping()
        {
            // Return the Excel column-based mappings
            return new Dictionary<string, string>(_configuration.ColumnIndexMappings);
        }

        public Dictionary<string, string> GetColumnMapping(ProcessingContext context)
        {
            // Return the Excel column-based mappings
            return new Dictionary<string, string>(_configuration.ColumnIndexMappings);
        }

        public async Task<ProcessingResult> NormalizeAsync(FileData fileData, ProcessingContext context)
        {
            var startTime = DateTime.UtcNow;
            var result = new ProcessingResult
            {
                SourceFile = context.SourceFileName,
                SupplierName = SupplierName,
                ProcessedAt = startTime
            };

            try
            {
                var normalizationResults = new List<NormalizationResult>();
                var statistics = new ProcessingStatistics();
                var warnings = new List<string>();
                var errors = new List<string>();

                if (fileData.dataRows.Count == 0)
                {
                    result.Statistics = statistics;
                    return result;
                }

                // Create supplier offer metadata for this processing session
                SupplierOfferEntity supplierOffer = CreateSupplierOfferFromContext(context);
                    result.SupplierOffer = supplierOffer;
                    statistics.SupplierOffersCreated = 1;

                // Find header row
                var headerRowIndex = _configuration.Transformation.HeaderRowIndex;
                var headerRow = fileData.dataRows.Skip(headerRowIndex).FirstOrDefault(r => r.HasData);
                
                if (headerRow == null)
                {
                    errors.Add("No valid header row found");
                    result.Errors = errors;
                    return result;
                }

                // Create column mapping for supplier offers
                var columnIndexes = CreateColumnMapping(headerRow);
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
                        var normalizationResult = await NormalizeRowToRelationalEntitiesAsync(
                            row, columnIndexes, context, fileData.FilePath, supplierOffer);
                        
                        if (normalizationResult != null)
                        {
                            if (IsValidNormalizationResult(normalizationResult))
                            {
                                normalizationResults.Add(normalizationResult);
                                statistics.ProductsCreated++;
                                
                                // Update supplier offer statistics
                                statistics.PricingRecordsProcessed++;
                                if (normalizationResult.HasOfferProperties)
                                {
                                    statistics.OfferProductsCreated++;
                                }
                                if (normalizationResult.OfferProperties.ContainsKey("StockQuantity"))
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

                result.NormalizationResults = normalizationResults;
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

        private Dictionary<string, int> CreateColumnMapping(RowData headerRow)
        {
            var mapping = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            
            // Create mapping using Excel column letters or numeric indices
            foreach (var indexMapping in _configuration.ColumnIndexMappings)
            {
                int columnIndex;
                
                // Try to parse as Excel column letter (A, B, C, etc.) or numeric index
                if (IsExcelColumnLetter(indexMapping.Key))
                {
                    columnIndex = ConvertExcelColumnToIndex(indexMapping.Key);
                }
                else if (int.TryParse(indexMapping.Key, out int numericIndex))
                {
                    columnIndex = numericIndex;
                }
                else
                {
                    continue; // Skip invalid column references
                }

                var targetProperty = indexMapping.Value;
                mapping[targetProperty] = columnIndex;
            }

            return mapping;
        }

        private Dictionary<string, int> CreateColumnIndexMapping()
        {
            var mapping = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            
            // Create mapping using Excel column letters to 0-based indices
            foreach (var indexMapping in _configuration.ColumnIndexMappings)
            {
                int columnIndex;
                
                // Convert Excel column letter (A, B, C, etc.) to 0-based index
                if (IsExcelColumnLetter(indexMapping.Key))
                {
                    columnIndex = ConvertExcelColumnToIndex(indexMapping.Key);
                }
                else if (int.TryParse(indexMapping.Key, out int numericIndex))
                {
                    columnIndex = numericIndex;
                }
                else
                {
                    continue; // Skip invalid column references
                }

                var targetProperty = indexMapping.Value;
                mapping[targetProperty] = columnIndex;
            }

            return mapping;
        }

        private async Task<ProductEntity?> NormalizeRowAsync(RowData row, Dictionary<string, int> columnIndexes, string filePath)
        {
            var product = new ProductEntity();

            try
            {
                // Map core properties (Name, Description, EAN)
                product.Name = GetMappedValue(row, columnIndexes, "Name") ?? "Unknown Product";
                product.Description = GetMappedValue(row, columnIndexes, "Description");
                product.EAN = GetMappedValue(row, columnIndexes, "EAN") ?? "";

                // Apply transformations to core properties
                if (_configuration.Transformation.TrimWhitespace)
                {
                    product.Name = product.Name?.Trim() ?? "";
                    product.Description = product.Description?.Trim();
                    product.EAN = product.EAN?.Trim() ?? "";
                }

                // Process all mapped columns as dynamic properties
                foreach (var mapping in _configuration.ColumnMappings)
                {
                    var sourceColumn = mapping.Key;
                    var targetProperty = mapping.Value;
                    
                    // Skip core properties as they're already handled
                    if (targetProperty is "Name" or "Description" or "EAN")
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
            // Simple validation - just check that the product has a name
            return !string.IsNullOrWhiteSpace(product.Name);
        }

        private bool IsHeaderRow(ProductEntity product)
        {
            // Check if product name/EAN matches known column headers
            var headerKeywords = new[] { "Item Name", "Item Code", "PRICE", "Category", "Family", "Commercial Line", "Pricing Item Name", "Size", "Unit", "EAN", "Capacity" };
            
            if (headerKeywords.Any(keyword => string.Equals(product.Name, keyword, StringComparison.OrdinalIgnoreCase) ||
                                               string.Equals(product.EAN, keyword, StringComparison.OrdinalIgnoreCase)))
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

        private Task<ProductEntity?> NormalizeModeSpecificRowAsync(RowData row, Dictionary<string, int> columnIndexes, ProcessingContext context, string sourceFile)
        {
            try
            {
                var product = new ProductEntity();
                // Note: No metadata added to DynamicProperties - they should contain only product attributes


                // Process mapped columns based on mode priorities
                var columnMappings = GetColumnMapping();
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
                                case "ean":
                                    product.EAN = convertedValue?.ToString() ?? "";
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

                // For supplier offers, ensure we have a valid product with EAN and name
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

                // Note: Keep all data including pricing - we'll separate core vs offer properties in the service layer

                return Task.FromResult<ProductEntity?>(product);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to normalize row {row.Index}: {ex.Message}", ex);
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

        /// <summary>
        /// Creates a SupplierOffer entity from processing context
        /// </summary>
        private SupplierOfferEntity CreateSupplierOfferFromContext(ProcessingContext context)
        {
            return new SupplierOfferEntity
            {
                OfferName = $"{SupplierName} - {context.SourceFileName}",
                Description = $"Offer created from file: {context.SourceFileName}",
                Currency = "USD", // Default currency, could be extracted from config or context
                ValidFrom = context.ProcessingDate,
                ValidTo = context.ProcessingDate.AddYears(1), // Default 1 year validity
                IsActive = true,
                OfferType = "File Import",
                Version = "1.0",
                CreatedAt = context.ProcessingDate
            };
        }

        /// <summary>
        /// Normalizes a single row into relational entities (ProductEntity + OfferProductEntity)
        /// </summary>
        private async Task<NormalizationResult?> NormalizeRowToRelationalEntitiesAsync(
            RowData row, 
            Dictionary<string, int> columnIndexes, 
            ProcessingContext context, 
            string sourceFile,
            SupplierOfferEntity? supplierOffer)
        {
            try
            {
                var product = new ProductEntity();
                var normalizationResult = new NormalizationResult
                {
                    RowIndex = row.Index,
                    SupplierOffer = supplierOffer
                };

                // Process mapped columns with property classification
                var columnMappings = GetColumnMapping(context);
                var offerProperties = new Dictionary<string, object?>();
                
                foreach (var mapping in columnMappings)
                {
                    var sourceColumn = mapping.Key;
                    var targetProperty = mapping.Value;

                    if (columnIndexes.TryGetValue(targetProperty, out int columnIndex) && 
                        columnIndex < row.Cells.Count)
                    {
                        var cellValue = row.Cells[columnIndex].Value?.Trim();
                        
                        if (!string.IsNullOrEmpty(cellValue))
                        {
                            var convertedValue = ConvertValue(targetProperty, cellValue);
                            
                            // Set core properties or collect offer properties based on classification
                            switch (targetProperty.ToLowerInvariant())
                            {
                                case "name":
                                    product.Name = convertedValue?.ToString() ?? "";
                                    break;
                                case "description":
                                    product.Description = convertedValue?.ToString();
                                    break;
                                case "ean":
                                    product.EAN = convertedValue?.ToString() ?? "";
                                    break;
                                default:
                                    // Classify property as core product or offer property
                                    if (_configuration.PropertyClassification.CoreProductProperties.Contains(targetProperty, StringComparer.OrdinalIgnoreCase))
                                    {
                                        // Core product property - goes to ProductEntity.DynamicProperties
                                        product.SetDynamicProperty(targetProperty, convertedValue);
                                    }
                                    else if (_configuration.PropertyClassification.OfferProperties.Contains(targetProperty, StringComparer.OrdinalIgnoreCase))
                                    {
                                        // Offer property - goes to OfferProductEntity
                                        offerProperties[targetProperty] = convertedValue;
                                    }
                                    else
                                    {
                                        // Unmapped property - default to core product property for backward compatibility
                                        product.SetDynamicProperty(targetProperty, convertedValue);
                                    }
                                    break;
                            }
                        }
                    }
                }

                // For supplier offers, create OfferProduct entity if we have offer properties
                if (offerProperties.Count > 0 && supplierOffer != null)
                {
                    var offerProduct = CreateOfferProductEntity(offerProperties, supplierOffer);
                    normalizationResult.OfferProduct = offerProduct;
                    normalizationResult.HasOfferProperties = true;
                    normalizationResult.OfferProperties = offerProperties;
                }

                // Ensure we have a valid product name
                if (string.IsNullOrEmpty(product.Name))
                {
                    product.Name = ConstructProductNameFromProperties(product);
                }

                normalizationResult.Product = product;
                return await Task.FromResult(normalizationResult);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to normalize row {row.Index}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Creates an OfferProduct entity from offer properties
        /// </summary>
        private OfferProductEntity CreateOfferProductEntity(Dictionary<string, object?> offerProperties, SupplierOfferEntity supplierOffer)
        {
            var offerProduct = new OfferProductEntity
            {
                IsAvailable = true,
                CreatedAt = DateTime.UtcNow
            };

            // Map standard offer properties to OfferProduct entity properties
            foreach (var prop in offerProperties)
            {
                switch (prop.Key.ToLowerInvariant())
                {
                    case "price":
                        if (decimal.TryParse(prop.Value?.ToString(), out decimal price))
                            offerProduct.Price = price;
                        break;
                    case "capacity":
                        offerProduct.Capacity = prop.Value?.ToString();
                        break;
                    case "discount":
                        if (decimal.TryParse(prop.Value?.ToString(), out decimal discount))
                            offerProduct.Discount = discount;
                        break;
                    case "listprice":
                        if (decimal.TryParse(prop.Value?.ToString(), out decimal listPrice))
                            offerProduct.ListPrice = listPrice;
                        break;
                    case "unitofmeasure":
                        offerProduct.UnitOfMeasure = prop.Value?.ToString();
                        break;
                    case "minimumorderquantity":
                        if (int.TryParse(prop.Value?.ToString(), out int minQty))
                            offerProduct.MinimumOrderQuantity = minQty;
                        break;
                    case "maximumorderquantity":
                        if (int.TryParse(prop.Value?.ToString(), out int maxQty))
                            offerProduct.MaximumOrderQuantity = maxQty;
                        break;
                    default:
                        // Store as dynamic property in JSON
                        offerProduct.SetProductProperty(prop.Key, prop.Value);
                        break;
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
            
            if (product.DynamicProperties.TryGetValue("Family", out var family) && family != null)
                nameComponents.Add(family.ToString()!);
            if (product.DynamicProperties.TryGetValue("Category", out var category) && category != null)
                nameComponents.Add(category.ToString()!);
            if (product.DynamicProperties.TryGetValue("PricingItemName", out var itemName) && itemName != null)
                nameComponents.Add(itemName.ToString()!);

            return nameComponents.Any() ? string.Join(" - ", nameComponents) : "Unknown Product";
        }

        /// <summary>
        /// Validates a normalization result for supplier offers
        /// </summary>
        private bool IsValidNormalizationResult(NormalizationResult normalizationResult)
        {
            // For supplier offers, require valid product with EAN
            return !string.IsNullOrEmpty(normalizationResult.Product.Name) && 
                   normalizationResult.Product.Name != "Unknown Product" && 
                   !string.IsNullOrEmpty(normalizationResult.Product.EAN);
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
    }
}