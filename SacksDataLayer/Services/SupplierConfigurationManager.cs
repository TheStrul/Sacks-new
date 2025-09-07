using System.Text.Json;
using System.Text.Json.Serialization;
using SacksDataLayer.FileProcessing.Configuration;
using SacksDataLayer.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using SacksAIPlatform.InfrastructuresLayer.FileProcessing;

namespace SacksDataLayer.FileProcessing.Services
{
    /// <summary>
    /// Manages loading and updating supplier configurations from JSON files
    /// /// </summary>
    public class SupplierConfigurationManager
    {
        private SuppliersConfiguration? _configuration;
        private DateTime _lastLoadTime;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly ILogger<SupplierConfigurationManager>? _logger;

        /// <summary>
        /// Creates a new instance with IConfiguration support (recommended)
        /// /// </summary>
        public SupplierConfigurationManager(IConfiguration configuration, ILogger<SupplierConfigurationManager>? logger = null)
        {
            ArgumentNullException.ThrowIfNull(configuration);

            // Try to get from root level first (loaded from supplier-formats.json)
            _configuration = configuration.Get<SuppliersConfiguration>();
            
            // If that fails, try the "SupplierFormats" section
            _configuration ??= configuration.GetSection("SupplierFormats").Get<SuppliersConfiguration>();
            
            if (_configuration == null)
            {
                throw new InvalidOperationException("Failed to bind SupplierFormats configuration. Ensure supplier-formats.json is loaded correctly.");
            }

            _logger = logger;
            _jsonOptions = CreateJsonOptions();
        }

        private static JsonSerializerOptions CreateJsonOptions()
        {
            return new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true,
                AllowTrailingCommas = true,
                PropertyNameCaseInsensitive = true,
                Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
            };
        }

        /// <summary>
        /// Gets all supplier configurations
        /// /// </summary>
        public async Task<SuppliersConfiguration> GetConfigurationAsync()
        {
            await EnsureConfigurationLoadedAsync();
            return _configuration!;
        }


        /// <summary>
        /// Gets all supplier configurations ordered by name
        /// /// </summary>
        public async Task<List<SupplierConfiguration>> GetSupplierConfigurationsByPriorityAsync()
        {
            var config = await GetConfigurationAsync();
            var suppliers = config.Suppliers
                .OrderBy(s => s.Name)
                .ToList();
            
            // Ensure parent references are set
            foreach (var supplier in suppliers)
            {
                supplier.ParentConfiguration = config;
            }
            
            return suppliers;
        }

        /// <summary>
        /// Adds a new supplier configuration
        /// /// </summary>
        public async Task AddSupplierConfigurationAsync(SupplierConfiguration supplierConfig)
        {
            ArgumentNullException.ThrowIfNull(supplierConfig);
            
            var config = await GetConfigurationAsync();
            
            // Remove existing configuration with same name
            config.Suppliers.RemoveAll(s => 
                string.Equals(s.Name, supplierConfig.Name, StringComparison.OrdinalIgnoreCase));
            
            // Set parent reference and add new configuration
            supplierConfig.ParentConfiguration = config;
            config.Suppliers.Add(supplierConfig);
            
            await SaveConfigurationAsync(config);
        }



        /// <summary>
        /// Reloads configuration from file
        /// /// </summary>
        public async Task ReloadConfigurationAsync()
        {
            await EnsureConfigurationLoadedAsync();
        }

        /// <summary>
        /// Validates the configuration file
        /// /// </summary>
        public async Task<ConfigurationValidationResult> ValidateConfigurationAsync()
        {
            var result = new ConfigurationValidationResult();
            
            try
            {
                var config = await GetConfigurationAsync();
                
                // Validate basic structure
                if (config.Suppliers == null || !config.Suppliers.Any())
                {
                    result.AddError("No suppliers configured");
                    return result;
                }

                // Validate each supplier
                var supplierNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var supplier in config.Suppliers)
                {
                    ValidateSupplier(supplier, result, supplierNames);
                }

                // Remove priority-based validation since Priority no longer exists
                // var priorities = config.Suppliers
                //     .Where(s => s.Detection.Priority > 1)
                //     .GroupBy(s => s.Detection.Priority)
                //     .Where(g => g.Count() > 1);

                // foreach (var group in priorities)
                // {
                //     var suppliers = string.Join(", ", group.Select(s => s.Name));
                //     result.AddWarning($"Multiple suppliers have priority {group.Key}: {suppliers}");
                // }

                result.IsValid = !result.Errors.Any();
                result.AddInfo($"Validated {config.Suppliers.Count} supplier configurations");
            }
            catch (Exception ex)
            {
                result.AddError($"Configuration validation failed: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Auto-detects supplier configuration from file path/name
        /// /// </summary>
        public SupplierConfiguration? DetectSupplierFromFileAsync(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path cannot be null or empty", nameof(filePath));
                
            try
            {
                var fileName = Path.GetFileName(filePath);
                var fileNameLowerCase = fileName.ToLowerInvariant(); // Convert to lowercase for matching
                _logger?.LogDebug("Detecting supplier configuration for file: {FileName} (matching as: {FileNameLower})", fileName, fileNameLowerCase);
                
                // Try to match against each supplier's file name patterns using lowercase filename
                foreach (var supplier in _configuration!.Suppliers)
                {
                    if (supplier.Detection?.FileNamePatterns?.Any() == true)
                    {
                        foreach (var pattern in supplier.Detection.FileNamePatterns)
                        {
                            // Convert pattern to lowercase for case-insensitive matching
                            var patternLowerCase = pattern.ToLowerInvariant();
                            if (IsFileNameMatch(fileNameLowerCase, patternLowerCase))
                            {
                                _logger?.LogDebug("Matched supplier '{SupplierName}' with pattern '{Pattern}' (as '{PatternLower}') for file '{FileName}'", 
                                    supplier.Name, pattern, patternLowerCase, fileName);
                                
                                // Set parent reference before returning
                                supplier.ParentConfiguration = _configuration;
                                return supplier;
                            }
                        }
                    }
                }

                _logger?.LogWarning("No supplier configuration found for file: {FileName}", fileName);
                return null;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error detecting supplier configuration for file: {FilePath}", filePath);
                return null;
            }
        }

        /// <summary>
        /// Checks if a file name matches a pattern (supports wildcards)
        /// Note: Both fileName and pattern should already be normalized to the same case
        /// /// </summary>
        private bool IsFileNameMatch(string fileName, string pattern)
        {
            if (string.IsNullOrWhiteSpace(pattern) || string.IsNullOrWhiteSpace(fileName))
                return false;

            // Convert pattern to regex
            var regexPattern = pattern
                .Replace("*", ".*")
                .Replace("?", ".");

            return System.Text.RegularExpressions.Regex.IsMatch(fileName, regexPattern);
        }


        private async Task EnsureConfigurationLoadedAsync()
        {
            // Configuration is loaded in constructor, no need to reload from file
            await Task.CompletedTask;
        }

        private bool ShouldReload()
        {
            // Since we're loading from IConfiguration, we don't support file reloading
            return false;
        }

        private async Task LoadConfigurationAsync()
        {
            // Configuration is already loaded in constructor
            await Task.CompletedTask;
        }

        /// <summary>
        /// Loads ProductPropertyConfiguration from the standard location
        /// </summary>
        private async Task LoadProductPropertyConfigurationAsync()
        {
            // ProductPropertyConfiguration is already embedded in the loaded configuration
            await Task.CompletedTask;
        }

        private async Task SaveConfigurationAsync(SuppliersConfiguration config, string? filePath = null)
        {
            // Since we're loading from IConfiguration, we don't support saving back to file
            // This method is kept for interface compatibility
            await Task.CompletedTask;
            _configuration = config;
            _lastLoadTime = DateTime.UtcNow;
        }

        /// <summary>
        /// Creates a supplier configuration from Excel file analysis (non-interactive)
        /// All console interactions have been replaced with logging and sensible defaults
        /// /// </summary>
        /// <param name="excelFilePath">Path to the Excel file to analyze</param>
        /// <param name="supplierName">Name for the new supplier (if null, will be derived from filename)</param>
        /// <returns>The created supplier configuration</returns>
        public async Task<SupplierConfiguration> CreateSupplierConfigurationInteractivelyAsync(string excelFilePath, string? supplierName = null)
        {
            if (string.IsNullOrWhiteSpace(excelFilePath))
                throw new ArgumentException("Excel file path cannot be null or empty", nameof(excelFilePath));

            if (!File.Exists(excelFilePath))
                throw new FileNotFoundException($"Excel file not found: {excelFilePath}");

            _logger?.LogDebug("Starting supplier configuration creation for file: {FileName}", Path.GetFileName(excelFilePath));

            // Get the current configuration to access ProductPropertyConfiguration
            var currentConfig = await GetConfigurationAsync();
            var marketConfig = currentConfig.ProductPropertyConfiguration;

            // Step 1: Read the Excel file
            var fileReader = new FileDataReader();
            var fileData = await fileReader.ReadFileAsync(excelFilePath);

            _logger?.LogDebug("File analysis complete - Total rows: {RowCount}", fileData.RowCount);

            // Step 2: Determine supplier name
            if (string.IsNullOrWhiteSpace(supplierName))
            {
                var fileName = Path.GetFileNameWithoutExtension(excelFilePath);
                supplierName = ExtractSupplierNameFromFileName(fileName);
                _logger?.LogDebug("Auto-generated supplier name from filename: '{SupplierName}'", supplierName);
            }

            // Step 3: Analyze file structure and find header row
            var (headerRowIndex, dataStartRowIndex, expectedColumnCount) = AnalyzeFileStructure(fileData);
            
            _logger?.LogDebug("File structure analysis - Header row: {HeaderRow}, Data start: {DataStartRow}, Expected columns: {ExpectedColumns}", 
                headerRowIndex + 1, dataStartRowIndex + 1, expectedColumnCount);

            // Step 4: Log header row contents for review
            if (headerRowIndex >= 0 && headerRowIndex < fileData.RowCount)
            {
                var headerRow = fileData.GetRow(headerRowIndex);
                _logger?.LogDebug("Detected header row (Excel row {HeaderRowNumber}):", headerRowIndex + 1);
                for (int i = 0; i < headerRow?.Cells.Count; i++)
                {
                    var cellValue = headerRow.Cells[i]?.Value?.ToString()?.Trim() ?? "";
                    var columnLetter = GetExcelColumnLetter(i);
                    _logger?.LogDebug("   {ColumnLetter}: {CellValue}", columnLetter, cellValue);
                }
            }

            // Step 5: Create column mappings automatically (non-interactive)
            var columnProperties = CreateColumnMappingsAutomatically(fileData, headerRowIndex, expectedColumnCount, marketConfig);

            // Step 6: Generate file naming patterns automatically
            var currentFileName = Path.GetFileName(excelFilePath);
            var suggestedPatterns = GenerateFileNamePatterns(currentFileName, supplierName);
            
            _logger?.LogDebug("Generated file naming patterns based on '{CurrentFileName}':", currentFileName);
            for (int i = 0; i < suggestedPatterns.Count; i++)
            {
                _logger?.LogDebug("   {Index}. {Pattern}", i + 1, suggestedPatterns[i]);
            }

            // Step 7: Create the supplier configuration
            var supplierConfig = new SupplierConfiguration
            {
                Name = supplierName,
                Detection = new DetectionConfiguration
                {
                    FileNamePatterns = suggestedPatterns
                },
                ColumnProperties = columnProperties,
                FileStructure = new FileStructureConfiguration
                {
                    HeaderRowIndex = headerRowIndex + 1, // Convert back to 1-based
                    DataStartRowIndex = dataStartRowIndex + 1, // Convert back to 1-based
                    ExpectedColumnCount = expectedColumnCount
                },
            };

            // Set parent reference and resolve properties from market configuration
            supplierConfig.ParentConfiguration = currentConfig;
            if (currentConfig.ProductPropertyConfiguration != null)
            {
                supplierConfig.ResolveColumnProperties(currentConfig.ProductPropertyConfiguration);
            }

            _logger?.LogDebug("Supplier configuration created successfully!");
            _logger?.LogDebug("   • Supplier: {SupplierName}", supplierConfig.Name);
            _logger?.LogDebug("   • Columns mapped: {ColumnCount}", columnProperties.Count);
            _logger?.LogDebug("   • File patterns: {PatternCount}", suggestedPatterns.Count);

            // Log final configuration summary
            _logger?.LogDebug("Configuration Summary:");
            _logger?.LogDebug("Supplier Name: {SupplierName}", supplierConfig.Name);
            _logger?.LogDebug("Header Row: {HeaderRow}", supplierConfig.FileStructure.HeaderRowIndex);
            _logger?.LogDebug("Data Start Row: {DataStartRow}", supplierConfig.FileStructure.DataStartRowIndex);
            _logger?.LogDebug("Expected Columns: {ExpectedColumns}", supplierConfig.FileStructure.ExpectedColumnCount);
            
            _logger?.LogDebug("Column Mappings:");
            foreach (var (column, property) in columnProperties)
            {
                var req = property.IsRequired == true ? "Required" : "Optional";
                var unique = property.IsUnique == true ? ", Unique" : "";
                var skip = property.SkipEntireRow ? ", SkipRow" : "";
                
                // Show resolved properties from market config
                var displayName = !string.IsNullOrEmpty(property.DisplayName) ? property.DisplayName : property.ProductPropertyKey;
                var classification = !string.IsNullOrEmpty(property.Classification) ? property.Classification : "unknown";
                
                _logger?.LogDebug("   {Column}: {DisplayName} → {ProductPropertyKey} ({Classification}, {Requirements}{Unique}{Skip})", 
                    column, displayName, property.ProductPropertyKey, classification, req, unique, skip);
            }
            
            _logger?.LogDebug("File Patterns:");
            foreach (var pattern in suggestedPatterns)
            {
                _logger?.LogDebug("   • {Pattern}", pattern);
            }

            return supplierConfig;
        }

        /// <summary>
        /// Extracts a supplier name from a filename
        /// /// </summary>
        private string ExtractSupplierNameFromFileName(string fileName)
        {
            // Remove common suffixes and patterns
            var cleanName = fileName;
            
            // Remove date patterns (e.g., "31.8.25", "2025", etc.)
            cleanName = System.Text.RegularExpressions.Regex.Replace(cleanName, @"\d{1,2}\.\d{1,2}\.\d{2,4}", "");
            cleanName = System.Text.RegularExpressions.Regex.Replace(cleanName, @"\d{4}", "");
            cleanName = System.Text.RegularExpressions.Regex.Replace(cleanName, @"\d{1,2}\.\d{1,2}", "");
            
            // Remove common words
            cleanName = cleanName.Replace("_", " ").Replace("-", " ");
            cleanName = System.Text.RegularExpressions.Regex.Replace(cleanName, @"\s+", " ");
            
            return cleanName.Trim().ToUpperInvariant();
        }

        /// <summary>
        /// Analyzes file structure to determine header and data start rows
        /// /// </summary>
        private (int headerRowIndex, int dataStartRowIndex, int expectedColumnCount) AnalyzeFileStructure(FileData fileData)
        {
            int headerRowIndex = 0;
            int maxColumns = 0;
            int bestHeaderCandidate = 0;

            // Look at first 10 rows to find the one with most non-empty columns
            for (int i = 0; i < Math.Min(10, fileData.RowCount); i++)
            {
                var row = fileData.GetRow(i);
                if (row == null) continue;

                var nonEmptyColumns = row.Cells.Count(c => !string.IsNullOrWhiteSpace(c?.Value?.ToString()));
                
                // Prefer rows that look like headers (contain text, not just numbers)
                var textColumns = row.Cells.Count(c => 
                {
                    var value = c?.Value?.ToString()?.Trim();
                    return !string.IsNullOrEmpty(value) && !decimal.TryParse(value, out _);
                });

                // Score: favor rows with more text columns and reasonable total columns
                var score = textColumns * 2 + nonEmptyColumns;
                
                if (score > maxColumns && nonEmptyColumns >= 3) // At least 3 columns to be considered
                {
                    maxColumns = score;
                    bestHeaderCandidate = i;
                }
            }

            headerRowIndex = bestHeaderCandidate;
            var dataStartRowIndex = headerRowIndex + 1;
            
            // Determine expected column count from header row
            var headerRow = fileData.GetRow(headerRowIndex);
            var expectedColumnCount = headerRow?.Cells.Count(c => !string.IsNullOrWhiteSpace(c?.Value?.ToString())) ?? 10;

            return (headerRowIndex, dataStartRowIndex, expectedColumnCount);
        }

        /// <summary>
        /// Creates column mappings automatically using intelligent defaults (non-interactive)
        /// /// </summary>
        private Dictionary<string, ColumnProperty> CreateColumnMappingsAutomatically(
            FileData fileData, int headerRowIndex, int expectedColumnCount, ProductPropertyConfiguration? marketConfig)
        {
            var columnProperties = new Dictionary<string, ColumnProperty>();
            var headerRow = fileData.GetRow(headerRowIndex);
            
            if (headerRow == null)
            {
                throw new InvalidOperationException("Header row not found");
            }

            _logger?.LogDebug("Creating automatic column mappings...");

            for (int i = 0; i < Math.Min(headerRow.Cells.Count, expectedColumnCount); i++)
            {
                var cellValue = headerRow.Cells[i]?.Value?.ToString()?.Trim() ?? "";
                var columnLetter = GetExcelColumnLetter(i);
                
                if (string.IsNullOrWhiteSpace(cellValue))
                {
                    _logger?.LogDebug("Column {ColumnLetter}: (empty) - skipping", columnLetter);
                    continue;
                }

                _logger?.LogDebug("Processing column {ColumnLetter}: '{CellValue}'", columnLetter, cellValue);
                
                // Log sample data from this column
                var sampleValues = new List<string>();
                for (int sampleRow = headerRowIndex + 1; sampleRow < Math.Min(headerRowIndex + 6, fileData.RowCount); sampleRow++)
                {
                    var dataRow = fileData.GetRow(sampleRow);
                    var sampleValue = dataRow?.Cells.ElementAtOrDefault(i)?.Value?.ToString()?.Trim() ?? "";
                    if (!string.IsNullOrWhiteSpace(sampleValue))
                    {
                        sampleValues.Add(sampleValue);
                    }
                }

                if (sampleValues.Any())
                {
                    _logger?.LogDebug("   Sample data: {SampleData}", string.Join(", ", sampleValues.Take(3)));
                }

                // Auto-suggest mapping based on header name
                var suggestedMapping = SuggestPropertyMapping(cellValue);
                _logger?.LogDebug("   Auto-mapped: {TargetProperty} ({Classification})", suggestedMapping.targetProperty, suggestedMapping.classification);
                
                var targetProperty = suggestedMapping.targetProperty;

                // Use market config defaults if available
                bool isRequired = false;
                bool isUnique = false;
                
                if (marketConfig?.Properties.TryGetValue(targetProperty, out var marketProp) == true)
                {
                    isRequired = marketProp.IsRequired;
                    _logger?.LogDebug("   Market config applied: Required={IsRequired}", isRequired);
                }
                else
                {
                    // Fallback heuristics
                    isRequired = targetProperty.Contains("name", StringComparison.OrdinalIgnoreCase) || 
                                targetProperty.Contains("ean", StringComparison.OrdinalIgnoreCase);
                }
                
                isUnique = targetProperty.Contains("ean", StringComparison.OrdinalIgnoreCase) || 
                          targetProperty.Contains("id", StringComparison.OrdinalIgnoreCase);

                // Determine data type automatically
                var dataType = DetermineDataType(fileData, headerRowIndex, i);
                
                var columnProperty = new ColumnProperty
                {
                    ProductPropertyKey = targetProperty,
                    Format = dataType.format,
                    DefaultValue = dataType.defaultValue,
                    AllowNull = dataType.allowNull,
                    MaxLength = dataType.maxLength,
                    Transformations = dataType.transformations,
                    IsRequired = isRequired,
                    IsUnique = isUnique,
                    SkipEntireRow = false // Default to false for automatic mode
                };

                columnProperties[columnLetter] = columnProperty;
                _logger?.LogDebug("   Mapped {ColumnLetter} → {TargetProperty} (Required: {IsRequired}, Unique: {IsUnique})", 
                    columnLetter, targetProperty, isRequired, isUnique);
            }

            return columnProperties;
        }

        /// <summary>
        /// Suggests property mapping based on header text
        /// /// </summary>
        private (string targetProperty, string classification) SuggestPropertyMapping(string headerText)
        {
            var lower = headerText.ToLower();
            
            if (lower.Contains("name", StringComparison.OrdinalIgnoreCase) || lower.Contains("product", StringComparison.OrdinalIgnoreCase) || lower.Contains("title", StringComparison.OrdinalIgnoreCase))
                return ("Name", "coreProduct");
            if (lower.Contains("ean", StringComparison.OrdinalIgnoreCase) || lower.Contains("barcode", StringComparison.OrdinalIgnoreCase) || lower.Contains("code", StringComparison.OrdinalIgnoreCase))
                return ("EAN", "coreProduct");
            if (lower.Contains("price", StringComparison.OrdinalIgnoreCase) || lower.Contains("cost", StringComparison.OrdinalIgnoreCase) || lower.Contains("amount", StringComparison.OrdinalIgnoreCase))
                return ("Price", "offer");
            if (lower.Contains("brand", StringComparison.OrdinalIgnoreCase) || lower.Contains("manufacturer", StringComparison.OrdinalIgnoreCase))
                return ("Brand", "coreProduct");
            if (lower.Contains("category", StringComparison.OrdinalIgnoreCase) || lower.Contains("type", StringComparison.OrdinalIgnoreCase))
                return ("Category", "coreProduct");
            if (lower.Contains("size", StringComparison.OrdinalIgnoreCase) || lower.Contains("volume", StringComparison.OrdinalIgnoreCase) || lower.Contains("weight", StringComparison.OrdinalIgnoreCase))
                return ("Size", "coreProduct");
            if (lower.Contains("reference", StringComparison.OrdinalIgnoreCase) || lower.Contains("ref", StringComparison.OrdinalIgnoreCase) || lower.Contains("sku", StringComparison.OrdinalIgnoreCase))
                return ("Reference", "offer");
            if (lower.Contains("stock", StringComparison.OrdinalIgnoreCase) || lower.Contains("inventory", StringComparison.OrdinalIgnoreCase) || lower.Contains("qty", StringComparison.OrdinalIgnoreCase))
                return ("InStock", "offer");
            if (lower.Contains("description", StringComparison.OrdinalIgnoreCase) || lower.Contains("desc", StringComparison.OrdinalIgnoreCase))
                return ("Description", "coreProduct");
            if (lower.Contains("gender", StringComparison.OrdinalIgnoreCase) || lower.Contains("sex", StringComparison.OrdinalIgnoreCase))
                return ("Gender", "coreProduct");
            if (lower.Contains("line", StringComparison.OrdinalIgnoreCase) || lower.Contains("series", StringComparison.OrdinalIgnoreCase))
                return ("Line", "coreProduct");
            if (lower.Contains("capacity", StringComparison.OrdinalIgnoreCase) || lower.Contains("ml", StringComparison.OrdinalIgnoreCase) || lower.Contains("oz", StringComparison.OrdinalIgnoreCase))
                return ("Capacity", "offer");
            if (lower.Contains("unit", StringComparison.OrdinalIgnoreCase))
                return ("Unit", "coreProduct");
                
            return (headerText.Replace(" ", ""), "coreProduct");
        }

        /// <summary>
        /// Determines data type from sample data
        /// /// </summary>
        private (string type, string? format, object? defaultValue, bool allowNull, int? maxLength, List<string> transformations) DetermineDataType(FileData fileData, int headerRowIndex, int columnIndex)
        {
            var sampleValues = new List<string>();
            
            // Collect sample values
            for (int row = headerRowIndex + 1; row < Math.Min(headerRowIndex + 20, fileData.RowCount); row++)
            {
                var dataRow = fileData.GetRow(row);
                var value = dataRow?.Cells.ElementAtOrDefault(columnIndex)?.Value?.ToString()?.Trim();
                if (!string.IsNullOrWhiteSpace(value))
                {
                    sampleValues.Add(value);
                }
            }

            if (!sampleValues.Any())
            {
                return ("string", null, null, true, 255, new List<string>());
            }

            // Check if all values are numeric
            var numericValues = sampleValues.Where(v => decimal.TryParse(v.Replace("$", "").Replace(",", ""), out _)).Count();
            var totalValues = sampleValues.Count;
            
            if (numericValues > totalValues * 0.8) // 80% numeric
            {
                return ("decimal", "currency", 0.0, true, null, new List<string> { "removeSymbols", "parseDecimal" });
            }

            // Determine max length for strings
            var maxLength = sampleValues.Max(v => v.Length);
            var suggestedMaxLength = maxLength < 50 ? 100 : 
                                   maxLength < 200 ? 500 : 1000;

            return ("string", null, null, true, suggestedMaxLength, new List<string> { "trim" });
        }

        /// <summary>
        /// Generates suggested file name patterns
        /// /// </summary>
        private List<string> GenerateFileNamePatterns(string fileName, string supplierName)
        {
            var patterns = new List<string>();
            var nameWithoutExt = Path.GetFileNameWithoutExtension(fileName).ToLowerInvariant();
            var supplierNameLower = supplierName.ToLowerInvariant();
            
            // Pattern 1: Exact name with different extensions (lowercase)
            patterns.Add($"{nameWithoutExt}.xlsx");
            patterns.Add($"{nameWithoutExt}.xls");
            
            // Pattern 2: Supplier name with wildcards (lowercase only)
            patterns.Add($"{supplierNameLower}*.xlsx");
            
            // Pattern 3: Extract base name (remove dates/numbers) with wildcards
            var baseName = System.Text.RegularExpressions.Regex.Replace(nameWithoutExt, @"\d+", "*");
            if (baseName != nameWithoutExt)
            {
                patterns.Add($"{baseName}.xlsx");
            }

            return patterns.Distinct().ToList();
        }

        /// <summary>
        /// Converts column index to Excel column letter (A, B, C, etc.)
        /// /// </summary>
        private string GetExcelColumnLetter(int columnIndex)
        {
            string columnName = "";
            while (columnIndex >= 0)
            {
                columnName = (char)('A' + (columnIndex % 26)) + columnName;
                columnIndex = (columnIndex / 26) - 1;
            }
            return columnName;
        }

        /// <summary>
        /// Adds a new supplier configuration to the existing configuration file
        /// /// </summary>
        /// <param name="newSupplier">The supplier configuration to add</param>
        /// <param name="saveToFile">Whether to save immediately to file</param>
        /// <returns>True if added successfully, false if supplier already exists</returns>
        public async Task<bool> AddSupplierConfigurationAsync(SupplierConfiguration newSupplier, bool saveToFile = true)
        {
            if (newSupplier == null)
                throw new ArgumentNullException(nameof(newSupplier));

            var config = await GetConfigurationAsync();
            
            // Check if supplier already exists
            if (config.Suppliers.Any(s => s.Name.Equals(newSupplier.Name, StringComparison.OrdinalIgnoreCase)))
            {
                _logger?.LogWarning("Supplier '{SupplierName}' already exists!", newSupplier.Name);
                _logger?.LogDebug("Auto-replacing existing supplier in non-interactive mode");
                
                // Remove existing supplier automatically in non-interactive mode
                config.Suppliers.RemoveAll(s => s.Name.Equals(newSupplier.Name, StringComparison.OrdinalIgnoreCase));
                _logger?.LogDebug("Replaced existing supplier configuration for '{SupplierName}'", newSupplier.Name);
            }

            // Set parent reference and add the new supplier
            newSupplier.ParentConfiguration = config;
            config.Suppliers.Add(newSupplier);

            if (saveToFile)
            {
                await SaveConfigurationAsync(config);
                _logger?.LogDebug("Supplier '{SupplierName}' added to configuration (in-memory)", newSupplier.Name);
            }

            return true;
        }


        private void ValidateSupplier(SupplierConfiguration supplier, ConfigurationValidationResult result, HashSet<string> supplierNames)
        {
            // Check for required fields
            if (string.IsNullOrWhiteSpace(supplier.Name))
            {
                result.AddError("Supplier name is required");
                return;
            }

            // Check for duplicate names
            if (supplierNames.Contains(supplier.Name))
            {
                result.AddError($"Duplicate supplier name: {supplier.Name}");
            }
            else
            {
                supplierNames.Add(supplier.Name);
            }

            // Validate detection configuration
            if (supplier.Detection == null)
            {
                result.AddError($"Supplier '{supplier.Name}' missing detection configuration");
            }
            else if (!supplier.Detection.FileNamePatterns.Any())
            {
                result.AddWarning($"Supplier '{supplier.Name}' has no detection patterns");
            }
        }
        
        /// <summary>
        /// Gets all Excel files in the specified directory that don't match any known supplier configuration pattern
        /// /// </summary>
        public  List<string> GetUnmatchedFilesAsync(string inputDirectory)
        {
            if (string.IsNullOrWhiteSpace(inputDirectory) || !Directory.Exists(inputDirectory))
                return new List<string>();

            try
            {
                var allExcelFiles = Directory.GetFiles(inputDirectory, "*.xlsx")
                    .Where(f => !Path.GetFileName(f).StartsWith("~")) // Skip temp files
                    .ToList();

                var unmatchedFiles = new List<string>();

                foreach (var filePath in allExcelFiles)
                {
                    var supplierConfig = DetectSupplierFromFileAsync(filePath);
                    if (supplierConfig == null)
                    {
                        unmatchedFiles.Add(filePath);
                    }
                }

                _logger?.LogDebug("Found {UnmatchedCount} unmatched files out of {TotalFiles} Excel files", 
                    unmatchedFiles.Count, allExcelFiles.Count);

                return unmatchedFiles;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting unmatched files from directory: {InputDirectory}", inputDirectory);
                return new List<string>();
            }
        }
    }

    /// <summary>
    /// Result of configuration validation
    /// /// </summary>
    public class ConfigurationValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public List<string> Info { get; set; } = new();

        public void AddError(string error) => Errors.Add(error);
        public void AddWarning(string warning) => Warnings.Add(warning);
        public void AddInfo(string info) => Info.Add(info);
    }
}