using System.Globalization;
using System.Text.RegularExpressions;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using SacksDataLayer.Entities;
using SacksDataLayer.Parsing;

namespace SacksDataLayer.FileProcessing.Normalizers
{
    public partial class ConfigurationNormalizer
    {
        /// <summary>
        /// Applies transformations using the centralized Transformer class.
        /// </summary>
        /// <param name="value">The value to transform</param>
        /// <param name="transformations">List of transformation strings</param>
        /// <param name="currentKey">Current property key for value mapping context</param>
        /// <returns>The transformed value</returns>
        private string ApplyTransformations(string value, List<string> transformations, string currentKey)
        {
            return _transformer.ApplyTransformations(value, transformations, currentKey);
        }

        /// <summary>
        /// Validates a value using the centralized Transformer class.
        /// </summary>
        private async Task<ValidationResult> ValidateValueAsync(string? rawValue, SacksDataLayer.Configuration.ProductPropertyDefinition columnProperty)
        {
            return await _transformer.ValidateValueAsync(rawValue, columnProperty);
        }

        /// <summary>
        /// Normalizes decimal using the centralized Transformer class.
        /// </summary>
        private string NormalizeDecimal(string input)
        {
            return _transformer.NormalizeDecimal(input);
        }

        /// <summary>
        /// Normalizes unit using the centralized Transformer class.
        /// </summary>
        private string NormalizeUnit(string unit)
        {
            return _transformer.NormalizeUnit(unit);
        }
    }
}


