using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using SacksDataLayer.FileProcessing.Configuration;

namespace SacksLogicLayer.Services
{
    /// <summary>
    /// Loads and validates supplier configuration JSON files (supplier-formats.json and perfume-property-normalization.json)
    /// and exposes a populated SuppliersConfiguration where Lookups are taken from perfume normalization's valueMappings.
    /// </summary>
    public class JsonSupplierConfigurationLoader
    {
        private static readonly JsonSerializerOptions s_jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };

        private readonly ILogger _logger;

        public JsonSupplierConfigurationLoader(ILogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Load supplier configuration and perfume normalization files from disk, validate and merge.
        /// </summary>
        /// <param name="supplierFormatsPath">Path to supplier-formats.json</param>
        /// <returns>Populated SuppliersConfiguration</returns>
        internal async Task<SuppliersConfiguration> LoadAsync(string supplierFormatsPath)
        {
            SuppliersConfiguration retval = new SuppliersConfiguration();
            try
            {
                _logger?.LogDebug("Loading supplier formats from {Path}", supplierFormatsPath);
                string? supplierJson = null;

                supplierJson = await File.ReadAllTextAsync(supplierFormatsPath).ConfigureAwait(false);

                // Parse into mutable JsonNode so we can normalize legacy shapes before deserialization
                var root = JsonNode.Parse(supplierJson, new JsonNodeOptions { PropertyNameCaseInsensitive = true });
                NormalizeColumnRules(root);
                var normalizedJson = root!.ToJsonString();

                retval = JsonSerializer.Deserialize<SuppliersConfiguration>(normalizedJson, s_jsonOptions)!;
                retval!.FullPath = supplierFormatsPath;

                var errors = retval.ValidateConfiguration();

                // Validate in-memory configuration if it was loaded
                if (errors != null && errors.Count > 0)
                {
                    foreach (var item in errors)
                    {
                        _logger!.LogError("Supplier configuration validation error: {Error}", item);
                    }
                }


                _logger?.LogInformation("Loaded {Count} suppliers and {LookupCount} lookup tables", retval.Suppliers.Count, retval.Lookups?.Count ?? 0);
            }
            catch (Exception ex)
            {
                _logger!.LogError(ex, "Error reading supplier formats file at {Path}", supplierFormatsPath);
                throw;

            }
            return retval;
        }

        // Normalize legacy columnRules shapes so deserialization into ParsingEngine.RuleConfig
        // (which requires Actions) succeeds. Handles:
        // - columnRules as array: [ { "column": "C", "rule": { ... } }, ... ]
        // - columnRules entries wrapped with "rule": { "actions": [...] }
        private static void NormalizeColumnRules(JsonNode? root)
        {
            if (root is not JsonObject rootObj) return;
            if (!rootObj.TryGetPropertyValue("suppliers", out var suppliersNode) || suppliersNode is not JsonArray suppliers) return;

            foreach (var supplierNode in suppliers)
            {
                if (supplierNode is not JsonObject supplierObj) continue;
                if (!supplierObj.TryGetPropertyValue("parserConfig", out var parserNode) || parserNode is not JsonObject parserObj) continue;

                if (!parserObj.TryGetPropertyValue("columnRules", out var colRulesNode)) continue;

                // If columnRules is an array -> convert to object { columnName: ruleObject, ... }
                if (colRulesNode is JsonArray colRulesArray)
                {
                    var newObj = new JsonObject();
                    foreach (var item in colRulesArray)
                    {
                        if (item is not JsonObject itemObj) continue;
                        string? column = null;
                        if (itemObj.TryGetPropertyValue("column", out var col) && col is JsonValue colVal)
                            column = colVal.GetValue<string>();
                        if (string.IsNullOrWhiteSpace(column)) continue;

                        JsonNode? ruleNode = null;
                        if (itemObj.TryGetPropertyValue("rule", out var rn))
                        {
                            ruleNode = rn;
                        }
                        else
                        {
                            // treat remaining properties as the rule
                            var clone = new JsonObject(itemObj);
                            clone.Remove("column");
                            ruleNode = clone;
                        }

                        if (ruleNode != null)
                            newObj[column] = ruleNode;
                    }
                    parserObj["columnRules"] = newObj;
                    continue;
                }

                // If columnRules is an object, some entries might be wrapped as { "C": { "rule": { ... } } }
                if (colRulesNode is JsonObject colRulesObj)
                {
                    var keys = colRulesObj.Select(kvp => kvp.Key).ToList();
                    foreach (var key in keys)
                    {
                        if (colRulesObj[key] is JsonObject propObj && propObj.TryGetPropertyValue("rule", out var innerRule))
                        {
                            colRulesObj[key] = innerRule;
                        }
                    }
                }
            }
        }
    }
}
