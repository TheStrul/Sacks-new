using System.Text.RegularExpressions;

using Microsoft.Extensions.Logging;

using SacksDataLayer.FileProcessing.Configuration;

namespace SacksAIPlatform.InfrastructuresLayer.FileProcessing.Services
{
    /// <summary>
    /// Service for detecting and processing subtitle rows in Excel files
    /// </summary>
    public class SubtitleRowProcessor
    {
        private readonly ILogger<SubtitleRowProcessor>? _logger;

        public SubtitleRowProcessor(ILogger<SubtitleRowProcessor>? logger = null)
        {
            _logger = logger;
        }

        /// <summary>
        /// Processes file data to detect and handle subtitle rows according to supplier configuration
        /// </summary>
        /// <param name="fileData">The file data to process</param>
        /// <param name="supplierConfig">Supplier configuration containing subtitle handling rules</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Processed file data with subtitle information</returns>
        public FileData ProcessSubtitleRows(
            FileData fileData, 
            SupplierConfiguration supplierConfig, 
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(fileData);
            ArgumentNullException.ThrowIfNull(supplierConfig);

            // Check if subtitle handling is enabled
            if (supplierConfig.SubtitleHandling?.Enabled != true)
            {
                _logger?.LogDebug("Subtitle handling is disabled for supplier {SupplierName}", supplierConfig.Name);
                return fileData;
            }

            var subtitleConfig = supplierConfig.SubtitleHandling;
            var currentSubtitleData = new Dictionary<string, object?>();

            var applicableRules = subtitleConfig.DetectionRules
            .Where(r => r.ApplyToSubsequentRows)
            .ToList();

            if (!applicableRules.Any())
            {
                _logger?.LogDebug("No Subtitle handling rools where defined to supplier {SupplierName}", supplierConfig.Name);
                return fileData;
            }

            foreach (var row in fileData.DataRows)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Skip empty rows
                if (!row.HasData)
                    continue;

                // Try to detect if this row is a subtitle row
                var detectedRule = DetectSubtitleRowAsync(row, subtitleConfig, cancellationToken);
                
                if (detectedRule != null)
                {
                    _logger?.LogDebug("Detected subtitle row at index {RowIndex} using rule '{RuleName}'", 
                        row.Index, detectedRule.Name);

                    // Mark the row as a subtitle row
                    row.IsSubtitleRow = true;
                    row.SubtitleRuleName = detectedRule.Name;

                    // Extract subtitle data based on the action
                    if (subtitleConfig.Action.Equals("parse", StringComparison.OrdinalIgnoreCase))
                    {
                        var extractedData = ExtractSubtitleDataAsync(row, detectedRule, cancellationToken);
                        
                        // Update current subtitle data
                        foreach (var kvp in extractedData)
                        {
                            currentSubtitleData[kvp.Key] = kvp.Value;
                        }

                        row.SubtitleData = new Dictionary<string, object?>(extractedData);
                        
                        _logger?.LogDebug("Extracted subtitle data: {SubtitleData}", 
                            string.Join(", ", extractedData.Select(kv => $"{kv.Key}={kv.Value}")));
                    }
                }
                else
                {
                    // This is a regular data row - apply current subtitle data if configured

                    if (currentSubtitleData.Any())
                    {
                        // Apply subtitle data to this row
                        row.SubtitleData = new Dictionary<string, object?>(currentSubtitleData);
                        
                        _logger?.LogTrace("Applied subtitle data to row {RowIndex}: {SubtitleData}", 
                            row.Index, string.Join(", ", currentSubtitleData.Select(kv => $"{kv.Key}={kv.Value}")));
                    }
                }
            }
            return fileData;
        }

        /// <summary>
        /// Detects if a row matches any subtitle detection rules
        /// </summary>
        private SubtitleDetectionRule? DetectSubtitleRowAsync(
            RowData row, 
            SubtitleRowHandlingConfiguration subtitleConfig, 
            CancellationToken cancellationToken)
        {
            foreach (var rule in subtitleConfig.DetectionRules)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var matches = rule.DetectionMethod.ToLowerInvariant() switch
                {
                    "columncount" => DetectByColumnCount(row, rule),
                    "pattern" => DetectByPattern(row, rule),
                    "hybrid" => DetectByColumnCount(row, rule) && DetectByPattern(row, rule),
                    _ => false
                };

                if (matches)
                {
                    return rule;
                }
            }

            return null;
        }

        /// <summary>
        /// Detects subtitle row by column count
        /// </summary>
        private bool DetectByColumnCount(RowData row, SubtitleDetectionRule rule)
        {
            var nonEmptyColumns = row.Cells.Count(c => !string.IsNullOrWhiteSpace(c?.Value));
            return nonEmptyColumns == rule.ExpectedColumnCount;
        }

        /// <summary>
        /// Detects subtitle row by pattern matching
        /// </summary>
        private bool DetectByPattern(RowData row, SubtitleDetectionRule rule)
        {
            if (!rule.ValidationPatterns.Any())
                return true; // No patterns means any content is valid

            var rowContent = string.Join(" ", row.Cells.Select(c => c?.Value ?? "").Where(v => !string.IsNullOrWhiteSpace(v)));
            
            return rule.ValidationPatterns.Any(pattern =>
            {
                try
                {
                    return Regex.IsMatch(rowContent, pattern, RegexOptions.IgnoreCase);
                }
                catch (ArgumentException ex)
                {
                    _logger?.LogWarning("Invalid regex pattern '{Pattern}' in rule '{RuleName}': {Error}", 
                        pattern, rule.Name, ex.Message);
                    return false;
                }
            });
        }

        /// <summary>
        /// Extracts subtitle data from a detected subtitle row
        /// </summary>
        private Dictionary<string, object?> ExtractSubtitleDataAsync(
            RowData row, 
            SubtitleDetectionRule rule, 
            CancellationToken cancellationToken)
        {           
            var extractedData = new Dictionary<string, object?>();

            // For now, implement basic extraction based on rule name
            // This can be extended based on specific business requirements
                // Generic extraction - use rule name as key but normalize it to a consistent dynamic property key
                var extractedValue = row.Cells.FirstOrDefault(c => !string.IsNullOrWhiteSpace(c?.Value))?.Value?.Trim();
                if (!string.IsNullOrEmpty(extractedValue))
                {
                    // Normalize the rule name into TitleCase without separators
                    var normalizedKey = Regex.Replace(System.Globalization.CultureInfo.InvariantCulture.TextInfo.ToTitleCase(rule.Name.Trim().ToLowerInvariant()), @"[\s_\-]+", string.Empty);
                    extractedData[normalizedKey] = extractedValue;
                    _logger?.LogDebug("Extracted subtitle property '{Key}': {Value}", normalizedKey, extractedValue);
                }

            return extractedData;
        }

        /// <summary>
        /// Filters data rows to exclude subtitle rows based on configuration
        /// </summary>
        public static IEnumerable<RowData> FilterDataRows(
            IEnumerable<RowData> rows, 
            SubtitleRowHandlingConfiguration? subtitleConfig)
        {
            if (subtitleConfig?.Enabled != true)
            {
                return rows; // No filtering if subtitle handling is disabled
            }

            return rows.Where(row => !row.IsSubtitleRow);
        }
    }
}