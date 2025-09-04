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
            
            // Try to find the source Configuration folder instead of using output directory
            _configurationFilePath = FindSourceConfigurationPath(configPath);
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
        /// Finds the source configuration path (in source code) instead of output directory
        /// </summary>
        private string FindSourceConfigurationPath(string configPath)
        {
            var currentDirectory = Environment.CurrentDirectory;
            
            // Strategy 1: Check if we're running from project folder (dotnet run)
            var strategy1 = Path.Combine(currentDirectory, configPath);
            if (File.Exists(strategy1))
            {
                return Path.GetFullPath(strategy1);
            }

            // Strategy 2: Search upward for solution file, then go to SacksConsoleApp/Configuration
            var searchDir = new DirectoryInfo(currentDirectory);
            while (searchDir != null)
            {
                var solutionFile = searchDir.GetFiles("*.sln").FirstOrDefault();
                if (solutionFile != null)
                {
                    var solutionConfigPath = Path.Combine(searchDir.FullName, "SacksConsoleApp", configPath);
                    if (File.Exists(solutionConfigPath))
                    {
                        return solutionConfigPath;
                    }
                }
                searchDir = searchDir.Parent;
            }

            // Strategy 3: Check if we're running from bin folder (Visual Studio/output directory)
            var strategy3 = Path.Combine(currentDirectory, "..", "..", "..", configPath);
            if (File.Exists(strategy3))
            {
                return Path.GetFullPath(strategy3);
            }

            // Fallback: Use the original method if source file not found
            var fallbackPath = Path.Combine(AppContext.BaseDirectory, configPath);
            if (File.Exists(fallbackPath))
            {
                return fallbackPath;
            }

            // Last resort: return the expected path and let error handling deal with it
            return Path.Combine(currentDirectory, configPath);
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
            return config.GetResolvedSupplierConfiguration(supplierName);
        }

        /// <summary>
        /// Gets all supplier configurations ordered by name
        /// </summary>
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
        /// </summary>
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
                var fileNameLowerCase = fileName.ToLowerInvariant(); // Convert to lowercase for matching
                _logger?.LogInformation("Detecting supplier configuration for file: {FileName} (matching as: {FileNameLower})", fileName, fileNameLowerCase);
                
                var config = await GetConfigurationAsync();

                // Try to match against each supplier's file name patterns using lowercase filename
                foreach (var supplier in config.Suppliers)
                {
                    if (supplier.Detection?.FileNamePatterns?.Any() == true)
                    {
                        foreach (var pattern in supplier.Detection.FileNamePatterns)
                        {
                            // Convert pattern to lowercase for case-insensitive matching
                            var patternLowerCase = pattern.ToLowerInvariant();
                            if (IsFileNameMatch(fileNameLowerCase, patternLowerCase))
                            {
                                _logger?.LogInformation("Matched supplier '{SupplierName}' with pattern '{Pattern}' (as '{PatternLower}') for file '{FileName}'", 
                                    supplier.Name, pattern, patternLowerCase, fileName);
                                
                                // Set parent reference before returning
                                supplier.ParentConfiguration = config;
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
        /// </summary>
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

                var json = await File.ReadAllTextAsync(_configurationFilePath);
                _configuration = JsonSerializer.Deserialize<SuppliersConfiguration>(json, _jsonOptions);
                _lastLoadTime = DateTime.UtcNow;

                if (_configuration == null)
                {
                    throw new InvalidOperationException("Failed to deserialize configuration file");
                }

                // Load ProductPropertyConfiguration if not already embedded
                if (_configuration.ProductPropertyConfiguration == null)
                {
                    await LoadProductPropertyConfigurationAsync();
                }

                // Ensure parent references are set after loading
                _configuration.EnsureParentReferences();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to load configuration from '{_configurationFilePath}': {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Loads ProductPropertyConfiguration from the standard location
        /// </summary>
        private async Task LoadProductPropertyConfigurationAsync()
        {
            try
            {
                // Look for product-properties-perfume.json in the same directory as supplier-formats.json
                var configDir = Path.GetDirectoryName(_configurationFilePath);
                if (string.IsNullOrEmpty(configDir))
                    return;

                var productPropertiesPath = Path.Combine(configDir, "product-properties-perfume.json");
                
                if (!File.Exists(productPropertiesPath))
                {
                    _logger?.LogWarning("ProductPropertyConfiguration file not found at: {FilePath}", productPropertiesPath);
                    return;
                }

                var json = await File.ReadAllTextAsync(productPropertiesPath);
                var productConfig = JsonSerializer.Deserialize<ProductPropertyConfiguration>(json, _jsonOptions);
                
                if (productConfig != null && _configuration != null)
                {
                    _configuration.ProductPropertyConfiguration = productConfig;
                    _logger?.LogInformation("Loaded ProductPropertyConfiguration with {PropertyCount} properties", 
                        productConfig.Properties.Count);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to load ProductPropertyConfiguration");
                // Don't throw - this is optional for backward compatibility
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

        /// <summary>
        /// Interactively analyzes an Excel file and creates a new supplier configuration
        /// NOTE: Console interactions have been replaced with logging - method is no longer interactive
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

            _logger?.LogInformation("INTERACTIVE SUPPLIER CONFIGURATION CREATOR");
            _logger?.LogInformation("Analyzing file: {FileName}", Path.GetFileName(excelFilePath));

            // Get the current configuration to access ProductPropertyConfiguration
            var currentConfig = await GetConfigurationAsync();
            var marketConfig = currentConfig.ProductPropertyConfiguration;

            // Step 1: Read the Excel file
            var fileReader = new FileDataReader();
            var fileData = await fileReader.ReadFileAsync(excelFilePath);

            _logger?.LogInformation("File analysis complete:");
            _logger?.LogInformation("   • Total rows: {RowCount}", fileData.RowCount);
            _logger?.LogInformation("   • Analyzing first 10 rows for structure...");

            // Step 2: Determine supplier name
            if (string.IsNullOrWhiteSpace(supplierName))
            {
                var fileName = Path.GetFileNameWithoutExtension(excelFilePath);
                var suggestedName = ExtractSupplierNameFromFileName(fileName);
                
                _logger?.LogInformation("Suggested supplier name from filename: '{SuggestedName}'", suggestedName);
                Console.WriteLine($"📝 Suggested supplier name: '{suggestedName}'");
                Console.Write("✏️  Enter supplier name (press Enter to accept suggestion): ");
                var userInput = Console.ReadLine()?.Trim();
                
                supplierName = string.IsNullOrWhiteSpace(userInput) ? suggestedName : userInput;
            }

            _logger?.LogInformation("Supplier name: {SupplierName}", supplierName);

            // Step 3: Analyze file structure and find header row
            var (headerRowIndex, dataStartRowIndex, expectedColumnCount) = AnalyzeFileStructure(fileData);
            
            _logger?.LogInformation("File structure analysis:");
            _logger?.LogInformation("   • Detected header row: {HeaderRow} (Excel row number)", headerRowIndex + 1);
            _logger?.LogInformation("   • Detected data start row: {DataStartRow} (Excel row number)", dataStartRowIndex + 1);
            _logger?.LogInformation("   • Expected columns: {ExpectedColumns}", expectedColumnCount);

            // Step 4: Show header row and ask for confirmation
            if (headerRowIndex >= 0 && headerRowIndex < fileData.RowCount)
            {
                var headerRow = fileData.GetRow(headerRowIndex);
                _logger?.LogInformation("Detected header row contents:");
                Console.WriteLine();
                Console.WriteLine($"📋 Detected header row (Excel row {headerRowIndex + 1}):");
                for (int i = 0; i < headerRow?.Cells.Count; i++)
                {
                    var cellValue = headerRow.Cells[i]?.Value?.ToString()?.Trim() ?? "";
                    var columnLetter = GetExcelColumnLetter(i);
                    _logger?.LogInformation("   {ColumnLetter}: {CellValue}", columnLetter, cellValue);
                    Console.WriteLine($"   {columnLetter}: {cellValue}");
                }

                Console.WriteLine();
                Console.Write("❓ Is this the correct header row? (y/n): ");
                var confirmation = Console.ReadLine()?.Trim().ToLower();
                if (confirmation != "y" && confirmation != "yes")
                {
                    Console.WriteLine("❌ Header row confirmation cancelled. Please manually specify header row location or check the file structure.");
                    throw new InvalidOperationException("Header row not confirmed by user");
                }
            }

            // Step 5: Create column mappings interactively
            var columnProperties = CreateColumnMappingsInteractively(fileData, headerRowIndex, expectedColumnCount, marketConfig);

            // Step 6: Determine file naming patterns
            _logger?.LogInformation("File naming patterns:");
            var currentFileName = Path.GetFileName(excelFilePath);
            var suggestedPatterns = GenerateFileNamePatterns(currentFileName, supplierName);
            
            Console.WriteLine();
            Console.WriteLine("📁 FILE NAMING PATTERNS");
            Console.WriteLine("=======================");
            Console.WriteLine($"Current file: {currentFileName}");
            Console.WriteLine("Suggested patterns:");
            
            _logger?.LogInformation("Suggested patterns based on '{CurrentFileName}':", currentFileName);
            for (int i = 0; i < suggestedPatterns.Count; i++)
            {
                _logger?.LogInformation("   {Index}. {Pattern}", i + 1, suggestedPatterns[i]);
                Console.WriteLine($"   {i + 1}. {suggestedPatterns[i]}");
            }
            
            Console.WriteLine();
            Console.Write("❓ Add additional file patterns? (y/n, default: n): ");
            var addPatternsInput = Console.ReadLine()?.Trim().ToLower();
            var additionalPatterns = new List<string>();
            
            if (addPatternsInput == "y" || addPatternsInput == "yes")
            {
                Console.WriteLine("Enter additional patterns (one per line, empty line to finish):");
                string? pattern;
                while (!string.IsNullOrWhiteSpace(pattern = Console.ReadLine()?.Trim()))
                {
                    additionalPatterns.Add(pattern);
                    Console.WriteLine($"   ✅ Added: {pattern}");
                }
            }
            
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
            };

            // Set parent reference and resolve properties from market configuration
            supplierConfig.ParentConfiguration = currentConfig;
            if (currentConfig.ProductPropertyConfiguration != null)
            {
                supplierConfig.ResolveColumnProperties(currentConfig.ProductPropertyConfiguration);
            }

            _logger?.LogInformation("Supplier configuration created successfully!");
            _logger?.LogInformation("   • Supplier: {SupplierName}", supplierConfig.Name);
            _logger?.LogInformation("   • Columns mapped: {ColumnCount}", columnProperties.Count);
            _logger?.LogInformation("   • File patterns: {PatternCount}", allPatterns.Count);

            // Display final configuration summary
            Console.WriteLine();
            Console.WriteLine("📋 CONFIGURATION SUMMARY");
            Console.WriteLine("=========================");
            Console.WriteLine($"Supplier Name: {supplierConfig.Name}");
            Console.WriteLine($"Header Row: {supplierConfig.FileStructure.HeaderRowIndex}");
            Console.WriteLine($"Data Start Row: {supplierConfig.FileStructure.DataStartRowIndex}");
            Console.WriteLine($"Expected Columns: {supplierConfig.FileStructure.ExpectedColumnCount}");
            Console.WriteLine();
            Console.WriteLine("Column Mappings:");
            foreach (var (column, property) in columnProperties)
            {
                var req = property.IsRequired == true ? "Required" : "Optional";
                var unique = property.IsUnique == true ? ", Unique" : "";
                var skip = property.SkipEntireRow ? ", SkipRow" : "";
                
                // Show resolved properties from market config
                var displayName = !string.IsNullOrEmpty(property.DisplayName) ? property.DisplayName : property.ProductPropertyKey;
                var classification = !string.IsNullOrEmpty(property.Classification) ? property.Classification : "unknown";
                
                Console.WriteLine($"   {column}: {displayName} → {property.ProductPropertyKey} ({classification}, {req}{unique}{skip})");
            }
            Console.WriteLine();
            Console.WriteLine("File Patterns:");
            foreach (var pattern in allPatterns)
            {
                Console.WriteLine($"   • {pattern}");
            }
            Console.WriteLine();
            Console.Write("❓ Save this configuration? (y/n, default: y): ");
            var saveInput = Console.ReadLine()?.Trim().ToLower();
            if (saveInput == "n" || saveInput == "no")
            {
                Console.WriteLine("❌ Configuration cancelled by user.");
                throw new OperationCanceledException("Configuration creation cancelled by user");
            }
            Console.WriteLine("✅ Configuration approved for saving.");

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
            FileData fileData, int headerRowIndex, int expectedColumnCount, ProductPropertyConfiguration? marketConfig)
        {
            var columnProperties = new Dictionary<string, ColumnProperty>();
            var headerRow = fileData.GetRow(headerRowIndex);
            
            if (headerRow == null)
            {
                throw new InvalidOperationException("Header row not found");
            }

            _logger?.LogInformation("COLUMN MAPPING CONFIGURATION");
            Console.WriteLine();
            Console.WriteLine("🗂️  COLUMN MAPPING CONFIGURATION");
            Console.WriteLine("==================================");

            if (marketConfig != null)
            {
                Console.WriteLine($"📋 Using market configuration with {marketConfig.Properties.Count} available properties");
                Console.WriteLine();
            }
            else
            {
                Console.WriteLine("⚠️  No ProductPropertyConfiguration found - using basic property suggestions");
                Console.WriteLine();
            }

            for (int i = 0; i < Math.Min(headerRow.Cells.Count, expectedColumnCount); i++)
            {
                var cellValue = headerRow.Cells[i]?.Value?.ToString()?.Trim() ?? "";
                var columnLetter = GetExcelColumnLetter(i);
                
                if (string.IsNullOrWhiteSpace(cellValue))
                {
                    _logger?.LogInformation("Column {ColumnLetter}: (empty) - skipping", columnLetter);
                    Console.WriteLine($"📍 Column {columnLetter}: (empty) - skipping");
                    continue;
                }

                _logger?.LogInformation("Column {ColumnLetter}: '{CellValue}'", columnLetter, cellValue);
                Console.WriteLine();
                Console.WriteLine($"📍 Column {columnLetter}: '{cellValue}'");
                
                // Show sample data from this column
                _logger?.LogInformation("   Sample data:");
                Console.WriteLine("   📊 Sample data:");
                for (int sampleRow = headerRowIndex + 1; sampleRow < Math.Min(headerRowIndex + 6, fileData.RowCount); sampleRow++)
                {
                    var dataRow = fileData.GetRow(sampleRow);
                    var sampleValue = dataRow?.Cells.ElementAtOrDefault(i)?.Value?.ToString()?.Trim() ?? "";
                    if (!string.IsNullOrWhiteSpace(sampleValue))
                    {
                        _logger?.LogInformation("      • {SampleValue}", sampleValue);
                        Console.WriteLine($"      • {sampleValue}");
                    }
                }

                // Suggest mapping based on header name
                var suggestedMapping = SuggestPropertyMapping(cellValue);
                _logger?.LogInformation("Suggested mapping: {TargetProperty} ({Classification})", suggestedMapping.targetProperty, suggestedMapping.classification);
                Console.WriteLine($"   💡 Suggested mapping: {suggestedMapping.targetProperty} ({suggestedMapping.classification})");
                
                // Interactive input for target property
                string[] availableProperties;
                if (marketConfig != null)
                {
                    // Use properties from market configuration
                    availableProperties = marketConfig.Properties.Keys.OrderBy(k => k).ToArray();
                    Console.WriteLine($"   📋 Available properties from market configuration:");
                }
                else
                {
                    // Fallback to hardcoded list if no market config
                    availableProperties = new[] { 
                        "Name", "EAN", "Brand", "Category", "Size", "Description", "Gender", "Line", "Unit",
                        "Price", "Reference", "InStock", "Capacity"
                    };
                    Console.WriteLine($"   📋 Common target properties:");
                }
                for (int idx = 0; idx < availableProperties.Length; idx++)
                {
                    var propOpt = availableProperties[idx];
                    var defaultMarker = propOpt == suggestedMapping.targetProperty ? " (suggested)" : "";
                    
                    // Show additional info from market config if available
                    if (marketConfig?.Properties.TryGetValue(propOpt, out var propDefinition) == true)
                    {
                        Console.WriteLine($"      {idx + 1}. {propOpt} - {propDefinition.DisplayName} ({propDefinition.Classification}){defaultMarker}");
                    }
                    else
                    {
                        Console.WriteLine($"      {idx + 1}. {propOpt}{defaultMarker}");
                    }
                }
                Console.WriteLine($"      {availableProperties.Length + 1}. Custom (enter your own)");
                
                string prompt = marketConfig != null 
                    ? $"   ❓ Select target property (1-{availableProperties.Length + 1}, or press Enter for '{suggestedMapping.targetProperty}'): "
                    : $"   ❓ Select target property (1-{availableProperties.Length + 1}, or press Enter for '{suggestedMapping.targetProperty}'): ";
                Console.Write(prompt);
                var userInput = Console.ReadLine()?.Trim();
                
                string targetProperty;
                if (string.IsNullOrEmpty(userInput))
                {
                    targetProperty = suggestedMapping.targetProperty;
                }
                else if (int.TryParse(userInput, out var choice) && choice >= 1 && choice <= availableProperties.Length + 1)
                {
                    if (choice == availableProperties.Length + 1) // Custom option
                    {
                        Console.Write($"   ❓ Enter custom target property: ");
                        targetProperty = Console.ReadLine()?.Trim() ?? suggestedMapping.targetProperty;
                        
                        // Warn if not in market config
                        if (marketConfig != null && !marketConfig.Properties.ContainsKey(targetProperty))
                        {
                            Console.WriteLine($"   ⚠️  Warning: '{targetProperty}' is not defined in the market configuration");
                        }
                    }
                    else
                    {
                        targetProperty = availableProperties[choice - 1];
                    }
                }
                else
                {
                    // Allow direct entry if not a number
                    targetProperty = userInput;
                    
                    // Warn if not in market config
                    if (marketConfig != null && !marketConfig.Properties.ContainsKey(targetProperty))
                    {
                        Console.WriteLine($"   ⚠️  Warning: '{targetProperty}' is not defined in the market configuration");
                    }
                }
                
                // Show what was accepted
                _logger?.LogInformation("   ✅ Selected mapping: {TargetProperty}", targetProperty);
                Console.WriteLine($"   ✅ Mapping: {columnLetter} → {targetProperty}");
                
                // Show classification from market config if available
                if (marketConfig?.Properties.TryGetValue(targetProperty, out var marketProperty) == true)
                {
                    Console.WriteLine($"   📋 Market configuration: {marketProperty.DisplayName} ({marketProperty.Classification})");
                }                // Determine data type based on sample data
                var dataType = DetermineDataType(fileData, headerRowIndex, i);
                
                _logger?.LogInformation("   📊 Detected data type: {DataType}", dataType.type);
                Console.WriteLine($"   📊 Detected data type: {dataType.type}");
                
                // Interactive validation settings
                Console.WriteLine($"   ⚙️  Validation settings:");
                
                // Auto-suggest based on target property and market config
                bool suggestedIsRequired;
                bool suggestedIsUnique;
                
                if (marketConfig?.Properties.TryGetValue(targetProperty, out var marketProp) == true)
                {
                    // Use market configuration defaults
                    suggestedIsRequired = marketProp.IsRequired;
                    // ProductPropertyDefinition doesn't have IsUnique, so use heuristic
                    suggestedIsUnique = targetProperty.Contains("ean", StringComparison.OrdinalIgnoreCase) || 
                                      targetProperty.Contains("id", StringComparison.OrdinalIgnoreCase);
                    Console.WriteLine($"   💡 Market defaults: Required={marketProp.IsRequired} (Unique suggestion: {suggestedIsUnique})");
                }
                else
                {
                    // Fallback to heuristic suggestions
                    suggestedIsRequired = targetProperty.Contains("name", StringComparison.OrdinalIgnoreCase) || 
                                        targetProperty.Contains("ean", StringComparison.OrdinalIgnoreCase);
                    suggestedIsUnique = targetProperty.Contains("ean", StringComparison.OrdinalIgnoreCase) || 
                                      targetProperty.Contains("id", StringComparison.OrdinalIgnoreCase);
                }
                
                Console.Write($"      ❓ Is this field required? (y/n, default: {(suggestedIsRequired ? "y" : "n")}): ");
                var requiredInput = Console.ReadLine()?.Trim().ToLower();
                var isRequired = string.IsNullOrEmpty(requiredInput) ? suggestedIsRequired : 
                               (requiredInput == "y" || requiredInput == "yes");
                
                Console.Write($"      ❓ Should this field be unique? (y/n, default: {(suggestedIsUnique ? "y" : "n")}): ");
                var uniqueInput = Console.ReadLine()?.Trim().ToLower();
                var isUnique = string.IsNullOrEmpty(uniqueInput) ? suggestedIsUnique : 
                             (uniqueInput == "y" || uniqueInput == "yes");
                
                Console.Write($"      ❓ Skip entire row if validation fails? (y/n, default: n): ");
                var skipRowInput = Console.ReadLine()?.Trim().ToLower();
                var skipEntireRow = skipRowInput == "y" || skipRowInput == "yes";
                
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
                    SkipEntireRow = skipEntireRow
                };

                columnProperties[columnLetter] = columnProperty;
                _logger?.LogInformation("   ✅ Mapped {ColumnLetter} → {TargetProperty}", columnLetter, targetProperty);
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
        /// </summary>
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
                _logger?.LogWarning("Supplier '{SupplierName}' already exists!", newSupplier.Name);
                _logger?.LogInformation("Auto-replacing existing supplier in non-interactive mode");
                
                // Remove existing supplier automatically in non-interactive mode
                config.Suppliers.RemoveAll(s => s.Name.Equals(newSupplier.Name, StringComparison.OrdinalIgnoreCase));
                _logger?.LogInformation("Replaced existing supplier configuration for '{SupplierName}'", newSupplier.Name);
            }

            // Set parent reference and add the new supplier
            newSupplier.ParentConfiguration = config;
            config.Suppliers.Add(newSupplier);

            if (saveToFile)
            {
                await SaveConfigurationAsync(config);
                _logger?.LogInformation("Supplier '{SupplierName}' added to configuration file", newSupplier.Name);
                _logger?.LogInformation("Configuration saved to: {ConfigurationPath}", _configurationFilePath);
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
        /// </summary>
        public async Task<List<string>> GetUnmatchedFilesAsync(string inputDirectory)
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
                    var supplierConfig = await DetectSupplierFromFileAsync(filePath);
                    if (supplierConfig == null)
                    {
                        unmatchedFiles.Add(filePath);
                    }
                }

                _logger?.LogInformation("Found {UnmatchedCount} unmatched files out of {TotalFiles} Excel files", 
                    unmatchedFiles.Count, allExcelFiles.Count);

                return unmatchedFiles;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting unmatched files from directory: {Directory}", inputDirectory);
                return new List<string>();
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