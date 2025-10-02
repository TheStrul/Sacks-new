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
                        
                        // Apply configured transforms before persisting current subtitle state
                        ApplySubtitleTransforms(extractedData, subtitleConfig);

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
                        row.SubtitleData = new Dictionary<string, object?>(currentSubtitleData);
                        
                        _logger?.LogTrace("Applied subtitle data to row {RowIndex}: {SubtitleData}", 
                            row.Index, string.Join(", ", currentSubtitleData.Select(kv => $"{kv.Key}={kv.Value}")));
                    }
                }
            }
            return fileData;
        }

        private static void ApplySubtitleTransforms(Dictionary<string, object?> extractedData, SubtitleRowHandlingConfiguration subtitleConfig)
        {
            if (extractedData.Count == 0) return;
            var transforms = subtitleConfig.Transforms;
            if (transforms == null || transforms.Count == 0) return;

            foreach (var tr in transforms)
            {
                if (tr == null || string.IsNullOrWhiteSpace(tr.SourceKey) || string.IsNullOrWhiteSpace(tr.Mode)) continue;
                var key = tr.SourceKey;
                if (!extractedData.TryGetValue(key, out var valObj) || valObj is null) continue;
                var value = valObj.ToString() ?? string.Empty;
                var comp = tr.IgnoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

                switch (tr.Mode.ToLowerInvariant())
                {
                    case "ignoreequals":
                        if (!string.IsNullOrWhiteSpace(tr.Pattern) && string.Equals(value.Trim(), tr.Pattern.Trim(), comp))
                        {
                            // remove the key (effectively clears current Brand)
                            extractedData.Remove(key);
                        }
                        break;
                    case "removeprefix":
                        if (!string.IsNullOrEmpty(tr.Pattern))
                        {
                            var prefix = tr.Pattern;
                            if (value.StartsWith(prefix, comp))
                            {
                                var rest = value.Substring(prefix.Length).TrimStart();
                                // If replacement provided, prepend it; otherwise just use rest
                                var newVal = string.IsNullOrEmpty(tr.Replacement) ? rest : (tr.Replacement + rest);
                                extractedData[key] = newVal;
                            }
                        }
                        break;
                    case "regexreplace":
                        if (!string.IsNullOrWhiteSpace(tr.Pattern))
                        {
                            var opts = tr.IgnoreCase ? RegexOptions.IgnoreCase : RegexOptions.None;
                            var replaced = Regex.Replace(value, tr.Pattern, tr.Replacement ?? string.Empty, opts).Trim();
                            if (string.IsNullOrEmpty(replaced))
                            {
                                extractedData.Remove(key);
                            }
                            else
                            {
                                extractedData[key] = replaced;
                            }
                        }
                        break;
                }
            }
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

        private bool DetectByColumnCount(RowData row, SubtitleDetectionRule rule)
        {
            var nonEmptyColumns = row.Cells.Values.Count(v => !string.IsNullOrWhiteSpace(v));
            return nonEmptyColumns == rule.ExpectedColumnCount;
        }

        private bool DetectByPattern(RowData row, SubtitleDetectionRule rule)
        {
            if (!rule.ValidationPatterns.Any())
                return true;

            var rowContent = string.Join(" ", row.Cells.Values.Where(v => !string.IsNullOrWhiteSpace(v)));
            
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

        private Dictionary<string, object?> ExtractSubtitleDataAsync(
            RowData row, 
            SubtitleDetectionRule rule, 
            CancellationToken cancellationToken)
        {           
            var extractedData = new Dictionary<string, object?>();

            var extractedValue = row.Cells.Values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v))?.Trim();
            if (!string.IsNullOrEmpty(extractedValue))
            {
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
                return rows;
            }

            return rows.Where(row => !row.IsSubtitleRow);
        }
    }
}