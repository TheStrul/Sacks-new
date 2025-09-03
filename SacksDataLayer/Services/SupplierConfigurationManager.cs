using System.Text.Json;
using System.Text.Json.Serialization;
using SacksDataLayer.FileProcessing.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using SacksAIPlatform.InfrastructuresLayer.FileProcessing;

namespace SacksDataLayer.FileProcessing.Services
{
    /// <summary>
    /// Manages loading and updating supplier configurations from JSON files
    /// </summary>
    public class SupplierConfigurationManager
    {
        private readonly string _configurationFilePath;
        private SuppliersConfiguration? _configuration;
        private DateTime _lastLoadTime;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly ILogger<SupplierConfigurationManager>? _logger;

        /// <summary>
        /// Creates a new instance with IConfiguration support (recommended)
        /// </summary>
        public SupplierConfigurationManager(IConfiguration configuration, ILogger<SupplierConfigurationManager>? logger = null)
        {
            ArgumentNullException.ThrowIfNull(configuration);
            
            var configPath = configuration["SupplierConfiguration:ConfigurationFilePath"] 
                ?? throw new InvalidOperationException("SupplierConfiguration:ConfigurationFilePath not found in configuration");
            
            _configurationFilePath = Path.Combine(AppContext.BaseDirectory, configPath);
            _logger = logger;
            _jsonOptions = CreateJsonOptions();
        }

        /// <summary>
        /// Creates a new instance with automatic configuration file discovery (legacy)
        /// </summary>
        [Obsolete("Use constructor with IConfiguration parameter instead")]
        public SupplierConfigurationManager(ILogger<SupplierConfigurationManager>? logger = null)
            : this(FindConfigurationFile(), logger)
        {
        }

        /// <summary>
        /// Creates a new instance with specified configuration file path (legacy)
        /// </summary>
        [Obsolete("Use constructor with IConfiguration parameter instead")]
        public SupplierConfigurationManager(string configurationFilePath, ILogger<SupplierConfigurationManager>? logger = null)
        {
            _configurationFilePath = configurationFilePath ?? throw new ArgumentNullException(nameof(configurationFilePath));
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
        /// Automatically finds the supplier configuration file in common locations
        /// </summary>
        /// <returns>Path to the configuration file</returns>
        /// <exception cref="FileNotFoundException">Thrown when configuration file is not found</exception>
        public static string FindConfigurationFile()
        {
            var possiblePaths = new[]
            {
                Path.Combine("..", "SacksDataLayer", "Configuration", "supplier-formats.json"),
                Path.Combine("SacksDataLayer", "Configuration", "supplier-formats.json"),
                Path.Combine("Configuration", "supplier-formats.json"),
                "supplier-formats.json"
            };

            foreach (var path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    return path;
                }
            }

            throw new FileNotFoundException("Configuration file 'supplier-formats.json' not found in any of the expected locations.");
        }

        /// <summary>
        /// Gets all supplier configurations
        /// </summary>
        public async Task<SuppliersConfiguration> GetConfigurationAsync()
        {
            await EnsureConfigurationLoadedAsync();
            return _configuration!;
        }

        /// <summary>
        /// Gets configuration for a specific supplier by name
        /// </summary>
        public async Task<SupplierConfiguration?> GetSupplierConfigurationAsync(string supplierName)
        {
            var config = await GetConfigurationAsync();
            return config.Suppliers.FirstOrDefault(s => 
                string.Equals(s.Name, supplierName, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Gets all supplier configurations ordered by name
        /// </summary>
        public async Task<List<SupplierConfiguration>> GetSupplierConfigurationsByPriorityAsync()
        {
            var config = await GetConfigurationAsync();
            return config.Suppliers
                .OrderBy(s => s.Name)
                .ToList();
        }

        /// <summary>
        /// Adds a new supplier configuration
        /// </summary>
        public async Task AddSupplierConfigurationAsync(SupplierConfiguration supplierConfig)
        {
            var config = await GetConfigurationAsync();
            
            // Remove existing configuration with same name
            config.Suppliers.RemoveAll(s => 
                string.Equals(s.Name, supplierConfig.Name, StringComparison.OrdinalIgnoreCase));
            
            // Add new configuration
            config.Suppliers.Add(supplierConfig);
            config.LastUpdated = DateTime.UtcNow;
            
            await SaveConfigurationAsync(config);
        }

        /// <summary>
        /// Updates an existing supplier configuration
        /// </summary>
        public async Task UpdateSupplierConfigurationAsync(SupplierConfiguration supplierConfig)
        {
            await AddSupplierConfigurationAsync(supplierConfig); // Same implementation
        }

        /// <summary>
        /// Removes a supplier configuration
        /// </summary>
        public async Task RemoveSupplierConfigurationAsync(string supplierName)
        {
            var config = await GetConfigurationAsync();
            var removed = config.Suppliers.RemoveAll(s => 
                string.Equals(s.Name, supplierName, StringComparison.OrdinalIgnoreCase));
            
            if (removed > 0)
            {
                config.LastUpdated = DateTime.UtcNow;
                await SaveConfigurationAsync(config);
            }
        }

        /// <summary>
        /// Reloads configuration from file
        /// </summary>
        public async Task ReloadConfigurationAsync()
        {
            _configuration = null;
            await EnsureConfigurationLoadedAsync();
        }

        /// <summary>
        /// Validates the configuration file
        /// </summary>
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
        /// </summary>
        public async Task<SupplierConfiguration?> DetectSupplierFromFileAsync(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path cannot be null or empty", nameof(filePath));
                
            try
            {
                var fileName = Path.GetFileName(filePath);
                _logger?.LogInformation("Detecting supplier configuration for file: {FileName}", fileName);
                
                var config = await GetConfigurationAsync();

                // Try to match against each supplier's file name patterns
                foreach (var supplier in config.Suppliers)
                {
                    if (supplier.Detection?.FileNamePatterns?.Any() == true)
                    {
                        foreach (var pattern in supplier.Detection.FileNamePatterns)
                        {
                            if (IsFileNameMatch(fileName, pattern))
                            {
                                _logger?.LogInformation("Matched supplier '{SupplierName}' with pattern '{Pattern}' for file '{FileName}'", 
                                    supplier.Name, pattern, fileName);
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
        /// </summary>
        private bool IsFileNameMatch(string fileName, string pattern)
        {
            if (string.IsNullOrWhiteSpace(pattern) || string.IsNullOrWhiteSpace(fileName))
                return false;

            // Convert pattern to regex
            var regexPattern = pattern
                .Replace("*", ".*")
                .Replace("?", ".");

            return System.Text.RegularExpressions.Regex.IsMatch(fileName, regexPattern, 
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }


        private async Task EnsureConfigurationLoadedAsync()
        {
            if (_configuration == null || ShouldReload())
            {
                await LoadConfigurationAsync();
            }
        }

        private bool ShouldReload()
        {
            try
            {
                var fileInfo = new FileInfo(_configurationFilePath);
                return fileInfo.Exists && fileInfo.LastWriteTime > _lastLoadTime;
            }
            catch
            {
                return false;
            }
        }

        private async Task LoadConfigurationAsync()
        {
            try
            {
                if (!File.Exists(_configurationFilePath))
                {
                    // Create default configuration if file doesn't exist
                    await CreateDefaultConfigurationAsync();
                }

                var json = await File.ReadAllTextAsync(_configurationFilePath);
                _configuration = JsonSerializer.Deserialize<SuppliersConfiguration>(json, _jsonOptions);
                _lastLoadTime = DateTime.UtcNow;

                if (_configuration == null)
                {
                    throw new InvalidOperationException("Failed to deserialize configuration file");
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to load configuration from '{_configurationFilePath}': {ex.Message}", ex);
            }
        }

        private async Task SaveConfigurationAsync(SuppliersConfiguration config, string? filePath = null)
        {
            var targetPath = filePath ?? _configurationFilePath;
            
            // Ensure directory exists
            var directory = Path.GetDirectoryName(targetPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(config, _jsonOptions);
            await File.WriteAllTextAsync(targetPath, json);
            
            if (filePath == null) // Only update cache if saving to main file
            {
                _configuration = config;
                _lastLoadTime = DateTime.UtcNow;
            }
        }

        private async Task CreateDefaultConfigurationAsync()
        {
            var defaultConfig = new SuppliersConfiguration
            {
                Version = "2.1",
                LastUpdated = DateTime.UtcNow,
                Suppliers = new List<SupplierConfiguration>
                {
                    CreateGenericSupplierConfiguration()
                }
            };

            await SaveConfigurationAsync(defaultConfig);
        }

        private SupplierConfiguration CreateGenericSupplierConfiguration()
        {
            return new SupplierConfiguration
            {
                Name = "Generic",
                Metadata = new SupplierMetadata 
                { 
                    Industry = "Default configuration for unknown suppliers" 
                },
                Detection = new DetectionConfiguration
                {
                    FileNamePatterns = new List<string> { "*" }
                },
                ColumnProperties = new Dictionary<string, ColumnProperty>
                {
                    { "A", new ColumnProperty { TargetProperty = "Name", DisplayName = "Name", DataType = new DataTypeConfiguration { Type = "string", AllowNull = false }, Classification = "coreProduct" } },
                    { "B", new ColumnProperty { TargetProperty = "EAN", DisplayName = "EAN", DataType = new DataTypeConfiguration { Type = "string", AllowNull = true }, Classification = "coreProduct" } },
                    { "C", new ColumnProperty { TargetProperty = "Price", DisplayName = "Price", DataType = new DataTypeConfiguration { Type = "decimal", DefaultValue = 0 }, Classification = "offer" } }
                },
                FileStructure = new FileStructureConfiguration
                {
                    DataStartRowIndex = 2,
                    ExpectedColumnCount = 10,
                },
                Transformation = new TransformationConfiguration
                {
                    SkipEmptyRows = true,
                    TrimWhitespace = true
                }
            };
        }

        /// <summary>
        /// Interactively analyzes an Excel file and creates a new supplier configuration
        /// </summary>
        /// <param name="excelFilePath">Path to the Excel file to analyze</param>
        /// <param name="supplierName">Name for the new supplier (if null, will be derived from filename)</param>
        /// <returns>The created supplier configuration</returns>
        public async Task<SupplierConfiguration> CreateSupplierConfigurationInteractivelyAsync(string excelFilePath, string? supplierName = null)
        {
            if (string.IsNullOrWhiteSpace(excelFilePath))
                throw new ArgumentException("Excel file path cannot be null or empty", nameof(excelFilePath));

            if (!File.Exists(excelFilePath))
                throw new FileNotFoundException($"Excel file not found: {excelFilePath}");

            Console.WriteLine("🔍 === INTERACTIVE SUPPLIER CONFIGURATION CREATOR ===");
            Console.WriteLine($"📁 Analyzing file: {Path.GetFileName(excelFilePath)}");
            Console.WriteLine();

            // Step 1: Read the Excel file
            var fileReader = new FileDataReader();
            var fileData = await fileReader.ReadFileAsync(excelFilePath);

            Console.WriteLine($"📊 File analysis complete:");
            Console.WriteLine($"   • Total rows: {fileData.RowCount}");
            Console.WriteLine($"   • Analyzing first 10 rows for structure...");
            Console.WriteLine();

            // Step 2: Determine supplier name
            if (string.IsNullOrWhiteSpace(supplierName))
            {
                var fileName = Path.GetFileNameWithoutExtension(excelFilePath);
                var suggestedName = ExtractSupplierNameFromFileName(fileName);
                
                Console.WriteLine($"💡 Suggested supplier name from filename: '{suggestedName}'");
                Console.Write("✏️  Enter supplier name (or press Enter to use suggested): ");
                var userInput = Console.ReadLine();
                supplierName = !string.IsNullOrWhiteSpace(userInput) ? userInput.Trim() : suggestedName;
            }

            Console.WriteLine($"✅ Supplier name: {supplierName}");
            Console.WriteLine();

            // Step 3: Analyze file structure and find header row
            var (headerRowIndex, dataStartRowIndex, expectedColumnCount) = AnalyzeFileStructure(fileData);
            
            Console.WriteLine($"📋 File structure analysis:");
            Console.WriteLine($"   • Detected header row: {headerRowIndex + 1} (Excel row number)");
            Console.WriteLine($"   • Detected data start row: {dataStartRowIndex + 1} (Excel row number)");
            Console.WriteLine($"   • Expected columns: {expectedColumnCount}");
            Console.WriteLine();

            // Step 4: Show header row and ask for confirmation
            if (headerRowIndex >= 0 && headerRowIndex < fileData.RowCount)
            {
                var headerRow = fileData.GetRow(headerRowIndex);
                Console.WriteLine("📋 Detected header row contents:");
                for (int i = 0; i < headerRow?.Cells.Count; i++)
                {
                    var cellValue = headerRow.Cells[i]?.Value?.ToString()?.Trim() ?? "";
                    var columnLetter = GetExcelColumnLetter(i);
                    Console.WriteLine($"   {columnLetter}: {cellValue}");
                }
                Console.WriteLine();

                Console.Write("❓ Is this the correct header row? (y/n): ");
                var confirmation = Console.ReadLine()?.Trim().ToLower();
                if (confirmation != "y" && confirmation != "yes")
                {
                    Console.Write("✏️  Enter the correct header row number (Excel numbering, 1-based): ");
                    if (int.TryParse(Console.ReadLine(), out int newHeaderRow) && newHeaderRow > 0)
                    {
                        headerRowIndex = newHeaderRow - 1; // Convert to 0-based
                        dataStartRowIndex = headerRowIndex + 1;
                        Console.WriteLine($"✅ Updated header row to: {headerRowIndex + 1}");
                    }
                }
            }

            // Step 5: Create column mappings interactively
            var columnProperties = CreateColumnMappingsInteractively(fileData, headerRowIndex, expectedColumnCount);

            // Step 6: Determine file naming patterns
            Console.WriteLine();
            Console.WriteLine("📁 File naming patterns:");
            var currentFileName = Path.GetFileName(excelFilePath);
            var suggestedPatterns = GenerateFileNamePatterns(currentFileName, supplierName);
            
            Console.WriteLine($"💡 Suggested patterns based on '{currentFileName}':");
            for (int i = 0; i < suggestedPatterns.Count; i++)
            {
                Console.WriteLine($"   {i + 1}. {suggestedPatterns[i]}");
            }
            Console.WriteLine();
            
            Console.Write("✏️  Enter additional patterns (comma-separated) or press Enter to use suggested: ");
            var additionalPatterns = Console.ReadLine()?.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim()).Where(p => !string.IsNullOrEmpty(p)).ToList() ?? new List<string>();
            
            var allPatterns = suggestedPatterns.Concat(additionalPatterns).Distinct().ToList();

            // Step 7: Create the supplier configuration
            var supplierConfig = new SupplierConfiguration
            {
                Name = supplierName,
                Detection = new DetectionConfiguration
                {
                    FileNamePatterns = allPatterns
                },
                ColumnProperties = columnProperties,
                FileStructure = new FileStructureConfiguration
                {
                    HeaderRowIndex = headerRowIndex + 1, // Convert back to 1-based
                    DataStartRowIndex = dataStartRowIndex + 1, // Convert back to 1-based
                    ExpectedColumnCount = expectedColumnCount
                },
                Transformation = new TransformationConfiguration
                {
                    SkipEmptyRows = true,
                    TrimWhitespace = true,
                    SkipRowsWithMergedCells = false
                },
                Metadata = new SupplierMetadata
                {
                    Industry = "Beauty & Cosmetics", // Default, user can modify later
                    Region = "Global", // Default, user can modify later
                    Contact = new ContactInfo
                    {
                        Name = $"{supplierName} Data Team",
                        Email = $"data@{supplierName.ToLower().Replace(" ", "")}.com",
                        Company = supplierName
                    },
                    FileFrequency = "monthly",
                    ExpectedFileSize = "Medium (1000-3000 products)",
                    Currency = "USD", // Default, user can modify later
                    Timezone = "UTC",
                    LastUpdated = DateTime.UtcNow,
                    Version = "1.0"
                }
            };

            Console.WriteLine();
            Console.WriteLine("✅ Supplier configuration created successfully!");
            Console.WriteLine($"   • Supplier: {supplierConfig.Name}");
            Console.WriteLine($"   • Columns mapped: {columnProperties.Count}");
            Console.WriteLine($"   • File patterns: {allPatterns.Count}");
            Console.WriteLine();

            return supplierConfig;
        }

        /// <summary>
        /// Extracts a supplier name from a filename
        /// </summary>
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
        /// </summary>
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
        /// Creates column mappings interactively with user input
        /// </summary>
        private Dictionary<string, ColumnProperty> CreateColumnMappingsInteractively(
            FileData fileData, int headerRowIndex, int expectedColumnCount)
        {
            var columnProperties = new Dictionary<string, ColumnProperty>();
            var headerRow = fileData.GetRow(headerRowIndex);
            
            if (headerRow == null)
            {
                throw new InvalidOperationException("Header row not found");
            }

            Console.WriteLine("🗂️  === COLUMN MAPPING CONFIGURATION ===");
            Console.WriteLine();

            for (int i = 0; i < Math.Min(headerRow.Cells.Count, expectedColumnCount); i++)
            {
                var cellValue = headerRow.Cells[i]?.Value?.ToString()?.Trim() ?? "";
                var columnLetter = GetExcelColumnLetter(i);
                
                if (string.IsNullOrWhiteSpace(cellValue))
                {
                    Console.WriteLine($"📋 Column {columnLetter}: (empty) - skipping");
                    continue;
                }

                Console.WriteLine($"📋 Column {columnLetter}: '{cellValue}'");
                
                // Show sample data from this column
                Console.WriteLine("   Sample data:");
                for (int sampleRow = headerRowIndex + 1; sampleRow < Math.Min(headerRowIndex + 6, fileData.RowCount); sampleRow++)
                {
                    var dataRow = fileData.GetRow(sampleRow);
                    var sampleValue = dataRow?.Cells.ElementAtOrDefault(i)?.Value?.ToString()?.Trim() ?? "";
                    if (!string.IsNullOrWhiteSpace(sampleValue))
                    {
                        Console.WriteLine($"      • {sampleValue}");
                    }
                }

                // Suggest mapping based on header name
                var suggestedMapping = SuggestPropertyMapping(cellValue);
                Console.WriteLine($"💡 Suggested mapping: {suggestedMapping.targetProperty} ({suggestedMapping.classification})");
                
                Console.Write($"✏️  Enter target property name (or 'skip' to ignore, press Enter to accept suggestion): ");
                var userInput = Console.ReadLine()?.Trim();
                
                if (userInput?.ToLower().Equals("skip", StringComparison.OrdinalIgnoreCase) == true)
                {
                    Console.WriteLine("   ⏭️  Skipping this column");
                    continue;
                }

                // If user pressed Enter (empty input), use the suggested mapping
                var targetProperty = string.IsNullOrWhiteSpace(userInput) ? suggestedMapping.targetProperty : userInput;
                
                // Show what was accepted
                if (string.IsNullOrWhiteSpace(userInput))
                {
                    Console.WriteLine($"   ✅ Accepted suggested mapping: {targetProperty}");
                }
                else
                {
                    Console.WriteLine($"   ✅ Using custom mapping: {targetProperty}");
                }
                
                // Determine classification - use suggested if user accepted the suggestion by pressing Enter
                string classification;
                if (string.IsNullOrWhiteSpace(userInput))
                {
                    // User accepted suggestion, use suggested classification
                    classification = suggestedMapping.classification;
                    Console.WriteLine($"   ✅ Using suggested classification: {classification}");
                }
                else
                {
                    // User provided custom mapping, ask for classification
                    Console.WriteLine("   📊 Property classification:");
                    Console.WriteLine("      1. coreProduct (product attributes like name, brand, category)");
                    Console.WriteLine("      2. offer (pricing, availability, supplier-specific data)");
                    Console.Write("   Enter classification (1 or 2): ");
                    var classificationInput = Console.ReadLine()?.Trim();
                    classification = classificationInput?.Equals("2", StringComparison.Ordinal) == true ? "offer" : "coreProduct";
                }
                
                // Determine data type based on sample data
                var dataType = DetermineDataType(fileData, headerRowIndex, i);
                
                Console.WriteLine($"   📊 Detected data type: {dataType.Type}");
                
                var columnProperty = new ColumnProperty
                {
                    TargetProperty = targetProperty,
                    DisplayName = cellValue,
                    DataType = dataType,
                    Classification = classification,
                    Validation = new ColumnValidationConfiguration
                    {
                        IsRequired = targetProperty.Contains("name", StringComparison.OrdinalIgnoreCase) || targetProperty.Contains("ean", StringComparison.OrdinalIgnoreCase),
                        IsUnique = targetProperty.Contains("ean", StringComparison.OrdinalIgnoreCase) || targetProperty.Contains("id", StringComparison.OrdinalIgnoreCase)
                    }
                };

                columnProperties[columnLetter] = columnProperty;
                Console.WriteLine($"   ✅ Mapped {columnLetter} → {targetProperty}");
                Console.WriteLine();
            }

            return columnProperties;
        }

        /// <summary>
        /// Suggests property mapping based on header text
        /// </summary>
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
        /// </summary>
        private DataTypeConfiguration DetermineDataType(FileData fileData, int headerRowIndex, int columnIndex)
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
                return new DataTypeConfiguration { Type = "string", AllowNull = true, MaxLength = 255 };
            }

            // Check if all values are numeric
            var numericValues = sampleValues.Where(v => decimal.TryParse(v.Replace("$", "").Replace(",", ""), out _)).Count();
            var totalValues = sampleValues.Count;
            
            if (numericValues > totalValues * 0.8) // 80% numeric
            {
                return new DataTypeConfiguration 
                { 
                    Type = "decimal", 
                    Format = "currency",
                    AllowNull = true,
                    DefaultValue = 0.0,
                    Transformations = new List<string> { "removeSymbols", "parseDecimal" }
                };
            }

            // Determine max length for strings
            var maxLength = sampleValues.Max(v => v.Length);
            var suggestedMaxLength = maxLength < 50 ? 100 : 
                                   maxLength < 200 ? 500 : 1000;

            return new DataTypeConfiguration 
            { 
                Type = "string", 
                AllowNull = true, 
                MaxLength = suggestedMaxLength,
                Transformations = new List<string> { "trim" }
            };
        }

        /// <summary>
        /// Generates suggested file name patterns
        /// </summary>
        private List<string> GenerateFileNamePatterns(string fileName, string supplierName)
        {
            var patterns = new List<string>();
            var nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
            
            // Pattern 1: Exact name with different extensions
            patterns.Add($"{nameWithoutExt}.xlsx");
            patterns.Add($"{nameWithoutExt}.xls");
            
            // Pattern 2: Supplier name with wildcards
            patterns.Add($"{supplierName}*.xlsx");
            patterns.Add($"{supplierName.ToLower()}*.xlsx");
            
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
        /// </summary>
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
        /// </summary>
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
                Console.WriteLine($"⚠️  Supplier '{newSupplier.Name}' already exists!");
                Console.Write("❓ Do you want to replace it? (y/n): ");
                var replace = Console.ReadLine()?.Trim().ToLower();
                
                if (replace == "y" || replace == "yes")
                {
                    // Remove existing supplier
                    config.Suppliers.RemoveAll(s => s.Name.Equals(newSupplier.Name, StringComparison.OrdinalIgnoreCase));
                    Console.WriteLine($"🔄 Replaced existing supplier configuration for '{newSupplier.Name}'");
                }
                else
                {
                    Console.WriteLine("❌ Operation cancelled - supplier not added");
                    return false;
                }
            }

            // Add the new supplier
            config.Suppliers.Add(newSupplier);
            config.LastUpdated = DateTime.UtcNow;

            if (saveToFile)
            {
                await SaveConfigurationAsync(config);
                Console.WriteLine($"✅ Supplier '{newSupplier.Name}' added to configuration file");
                Console.WriteLine($"📁 Configuration saved to: {_configurationFilePath}");
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

            // Validate column mappings
            if (supplier.ColumnIndexMappings == null || !supplier.ColumnIndexMappings.Any())
            {
                result.AddWarning($"Supplier '{supplier.Name}' has no column index mappings");
            }
        }
    }

    /// <summary>
    /// Result of configuration validation
    /// </summary>
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