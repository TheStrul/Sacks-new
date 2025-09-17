using System.Globalization;
using SacksDataLayer.Entities;
using SacksDataLayer.Configuration;

namespace SacksDataLayer.FileProcessing.Normalizers
{
    public partial class ConfigurationNormalizer
    {
        private Dictionary<PropertyDataType, Func<string, object?>> InitializeDataTypeConverters()
        {
            return new Dictionary<PropertyDataType, Func<string, object?>>
            {
                [PropertyDataType.String] = value => value,
                [PropertyDataType.Integer] = value => int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i) ? i as object : null,
                [PropertyDataType.Decimal] = value => decimal.TryParse(value, NumberStyles.Number | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var d) ? d as object : null,
                [PropertyDataType.Boolean] = value => ParseBooleanValue(value) as object,
                [PropertyDataType.DateTime] = value => DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt) ? dt as object : null,
                [PropertyDataType.Array] = value => (object?)value?.Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToArray()
            };
        }

        private bool ParseBooleanValue(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return false;
            var v = value.Trim().ToLowerInvariant();
            return v switch
            {
                "yes" or "y" or "true" or "1" or "active" or "enabled" or "on" => true,
                "no" or "n" or "false" or "0" or "inactive" or "disabled" or "off" => false,
                _ => false
            };
        }

        private bool IsPatternMatch(string input, string pattern)
        {
            if (pattern == "*") return true;
            if (pattern.StartsWith("*") && pattern.EndsWith("*")) return input.Contains(pattern.Trim('*'), StringComparison.OrdinalIgnoreCase);
            if (pattern.StartsWith("*")) return input.EndsWith(pattern.TrimStart('*'), StringComparison.OrdinalIgnoreCase);
            if (pattern.EndsWith("*")) return input.StartsWith(pattern.TrimEnd('*'), StringComparison.OrdinalIgnoreCase);
            return string.Equals(input, pattern, StringComparison.OrdinalIgnoreCase);
        }

        private int GetColumnIndex(string columnKey)
        {
            if (IsExcelColumnLetter(columnKey)) return ConvertExcelColumnToIndex(columnKey);
            return int.TryParse(columnKey, NumberStyles.Integer, CultureInfo.InvariantCulture, out var idx) ? idx : -1;
        }

        private bool IsExcelColumnLetter(string columnReference)
        {
            if (string.IsNullOrWhiteSpace(columnReference)) return false;
            return columnReference.All(c => char.IsLetter(c));
        }

        private int ConvertExcelColumnToIndex(string columnLetter)
        {
            if (string.IsNullOrWhiteSpace(columnLetter)) throw new ArgumentException("Column letter cannot be null or empty", nameof(columnLetter));
            columnLetter = columnLetter.ToUpperInvariant();
            var result = 0;
            for (int i = 0; i < columnLetter.Length; i++)
            {
                var c = columnLetter[i];
                if (c < 'A' || c > 'Z') throw new ArgumentException($"Invalid column letter: {columnLetter}", nameof(columnLetter));
                result = result * 26 + (c - 'A' + 1);
            }
            return result - 1;
        }

        private string ConstructProductNameFromProperties(ProductEntity product)
        {
            var components = new List<string>();
            List<string> preferredKeys;
            try { preferredKeys = _configuration?.GetCoreProductProperties() ?? new List<string>(); } catch { preferredKeys = new List<string>(); }
            if (!preferredKeys.Any()) preferredKeys = product.DynamicProperties.Keys.ToList();
            foreach (var key in preferredKeys)
            {
                if (product.DynamicProperties.TryGetValue(key, out var v) && v != null) components.Add(v.ToString()!);
            }
            if (!components.Any())
            {
                if (!string.IsNullOrWhiteSpace(product.EAN)) components.Add($"Product {product.EAN}");
            }
            return components.Any() ? string.Join(" ", components) : "Unknown Product";
        }

        private bool IsValidOfferProduct(ProductOfferAnnex offerProduct)
        {
            return offerProduct.Product != null && !string.IsNullOrWhiteSpace(offerProduct.Product.EAN) && offerProduct.Price > 0 && offerProduct.Quantity > 0;
        }
    }
}
