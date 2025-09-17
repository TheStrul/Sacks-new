using System.Globalization;
using System.Text.RegularExpressions;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using SacksDataLayer.Entities;
using SacksDataLayer.Configuration;
using SacksDataLayer.Parsing;

namespace SacksDataLayer.FileProcessing.Normalizers
{
    public partial class ConfigurationNormalizer
    {
        /// <summary>
        /// Extracts text after a pattern using the centralized Transformer class.
        /// </summary>
        private string ExtractAfterPattern(string value, string pattern)
        {
            return _transformer.ExtractAfterPattern(value, pattern);
        }

        /// <summary>
        /// Extracts text after a wildcard pattern using the centralized Transformer class.
        /// </summary>
        private string ExtractAfterWildcardPattern(string value, string pattern)
        {
            return _transformer.ExtractAfterWildcardPattern(value, pattern);
        }

        /// <summary>
        /// Converts wildcard patterns to regex using the centralized Transformer class.
        /// </summary>
        private string ConvertWildcardToRegex(string wildcardPattern)
        {
            return _transformer.ConvertWildcardToRegex(wildcardPattern);
        }

        /// <summary>
        /// Extracts price and currency using the centralized Transformer class.
        /// </summary>
        private (string Price, string Currency) ExtractPriceAndCurrency(string value)
        {
            return _transformer.ExtractPriceAndCurrency(value);
        }

        /// <summary>
        /// Extracts size and units using the centralized Transformer class.
        /// </summary>
        private (string Size, string Unit) ExtractSizeAndUnits(string value)
        {
            return _transformer.ExtractSizeAndUnits(value);
        }

        /// <summary>
        /// Returns uppercase words using the centralized Transformer class.
        /// </summary>
        private static string UpperWords(string input)
        {
            return Transformer.ExtractUpperWordsFromStart(input);
        }

        /// <summary>
        /// Handles custom wildcard patterns using the centralized Transformer class.
        /// </summary>
        private string HandleCustomWildcardPattern(string pattern)
        {
            return _transformer.HandleCustomWildcardPattern(pattern);
        }

        /// <summary>
        /// Removes first occurrence using the centralized Transformer class.
        /// </summary>
        private static string RemoveFirstOccurrence(string source, string toRemove, bool ignoreCase = false)
        {
            return Transformer.RemoveFirstOccurrence(source, toRemove, ignoreCase);
        }

        /// <summary>
        /// Extracts all sizes using the centralized Transformer class.
        /// </summary>
        private SizeExtractionResult? ExtractAllSizes(string input)
        {
            return _transformer.ExtractAllSizes(input);
        }

        /// <summary>
        /// Applies transformations with extraction using the centralized Transformer class.
        /// </summary>
        private (string TransformedValue, Dictionary<string, string>? ExtraProperties) ApplyTransformationsWithExtraction(
            string value, List<string> transformations, TransformProps? transformProp = null)
        {
            return _transformer.ApplyTransformationsWithExtraction(value, transformations, transformProp?.Key);
        }
    }
}


