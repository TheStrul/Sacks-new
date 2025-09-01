using System.Text.Json;

namespace SacksDataLayer.Configuration
{
    /// <summary>
    /// Normalizes dynamic property keys and values to standardized forms
    /// Handles multiple languages, abbreviations, and variations
    /// </summary>
    public class PropertyNormalizer
    {
        private readonly Dictionary<string, string> _keyMappings;
        private readonly Dictionary<string, Dictionary<string, string>> _valueMappings;

        public PropertyNormalizer()
        {
            _keyMappings = LoadKeyMappings();
            _valueMappings = LoadValueMappings();
        }

        /// <summary>
        /// Normalizes a property key to standard form
        /// </summary>
        /// <param name="key">Original property key</param>
        /// <returns>Normalized key</returns>
        public string NormalizeKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return key;

            var normalizedKey = key.Trim().ToLowerInvariant();
            
            return _keyMappings.TryGetValue(normalizedKey, out var standardKey) 
                ? standardKey 
                : key; // Return original if no mapping found
        }

        /// <summary>
        /// Normalizes a property value to standard form
        /// </summary>
        /// <param name="key">Normalized property key</param>
        /// <param name="value">Original property value</param>
        /// <returns>Normalized value</returns>
        public string NormalizeValue(string key, string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return value;

            var normalizedValue = value.Trim().ToLowerInvariant();

            if (_valueMappings.TryGetValue(key, out var valueMap) &&
                valueMap.TryGetValue(normalizedValue, out var standardValue))
            {
                return standardValue;
            }

            return value; // Return original if no mapping found
        }

        /// <summary>
        /// Normalizes all properties in a dictionary
        /// </summary>
        /// <param name="properties">Original properties</param>
        /// <returns>Normalized properties</returns>
        public Dictionary<string, object?> NormalizeProperties(Dictionary<string, object?> properties)
        {
            ArgumentNullException.ThrowIfNull(properties);
            
            var normalized = new Dictionary<string, object?>();

            foreach (var kvp in properties)
            {
                var normalizedKey = NormalizeKey(kvp.Key);
                var normalizedValue = kvp.Value?.ToString();

                if (!string.IsNullOrWhiteSpace(normalizedValue))
                {
                    normalizedValue = NormalizeValue(normalizedKey, normalizedValue);
                }

                normalized[normalizedKey] = normalizedValue;
            }

            return normalized;
        }

        #region Private Configuration Methods

        private Dictionary<string, string> LoadKeyMappings()
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                // Gender variations (prioritize gender over other meanings of "family")
                ["gender"] = "Gender",
                ["sex"] = "Gender",
                ["for"] = "Gender",
                ["genre"] = "Gender",
                ["sexe"] = "Gender", // French
                ["target"] = "Gender",
                ["destiné"] = "Gender", // French
                
                // Size/Volume variations
                ["size"] = "Size",
                ["volume"] = "Size",
                ["vol"] = "Size",
                ["ml"] = "Size",
                ["capacity"] = "Size",
                ["contenu"] = "Size", // French
                ["contenuto"] = "Size", // Italian
                ["contenido"] = "Size", // Spanish
                
                // Concentration variations
                ["concentration"] = "Concentration",
                ["con"] = "Concentration",
                ["type"] = "Concentration",
                ["%"] = "Concentration",
                ["conc"] = "Concentration",
                ["strength"] = "Concentration",
                ["force"] = "Concentration", // French
                
                // Brand variations
                ["brand"] = "Brand",
                ["marca"] = "Brand", // Spanish
                ["marque"] = "Brand", // French
                ["marchio"] = "Brand", // Italian
                ["manufacturer"] = "Brand",
                ["make"] = "Brand",
                
                // Product Line variations
                ["line"] = "ProductLine",
                ["collection"] = "ProductLine",
                ["serie"] = "ProductLine",
                ["série"] = "ProductLine", // French
                ["range"] = "ProductLine",
                // Note: "family" can mean gender or product line - context dependent
                // We'll handle this in business logic
                
                // Fragrance Family variations
                ["fragrancefamily"] = "FragranceFamily",
                ["fragrance_family"] = "FragranceFamily",
                ["fragrance family"] = "FragranceFamily",
                ["scent_family"] = "FragranceFamily",
                ["perfume_type"] = "FragranceFamily",
                ["olfactory"] = "FragranceFamily",
                ["olfactif"] = "FragranceFamily", // French
            };
        }

        private Dictionary<string, Dictionary<string, string>> LoadValueMappings()
        {
            return new Dictionary<string, Dictionary<string, string>>
            {
                ["Gender"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    // Women variations
                    ["w"] = "Women",
                    ["woman"] = "Women",
                    ["women"] = "Women",
                    ["female"] = "Women",
                    ["her"] = "Women",
                    ["mujer"] = "Women", // Spanish
                    ["mujeres"] = "Women", // Spanish
                    ["femme"] = "Women", // French
                    ["femmes"] = "Women", // French
                    ["donna"] = "Women", // Italian
                    ["donne"] = "Women", // Italian
                    ["f"] = "Women",
                    ["fem"] = "Women",
                    ["lady"] = "Women",
                    ["ladies"] = "Women",
                    
                    // Men variations
                    ["m"] = "Men",
                    ["man"] = "Men",
                    ["men"] = "Men",
                    ["male"] = "Men",
                    ["him"] = "Men",
                    ["hombre"] = "Men", // Spanish
                    ["hombres"] = "Men", // Spanish
                    ["homme"] = "Men", // French
                    ["hommes"] = "Men", // French
                    ["uomo"] = "Men", // Italian
                    ["uomini"] = "Men", // Italian
                    ["gentleman"] = "Men",
                    ["gentlemen"] = "Men",
                    
                    // Unisex variations
                    ["u"] = "Unisex",
                    ["unisex"] = "Unisex",
                    ["unisexe"] = "Unisex", // French
                    ["mixte"] = "Unisex", // French
                    ["all"] = "Unisex",
                    ["both"] = "Unisex",
                    ["everyone"] = "Unisex",
                },

                ["Concentration"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    // Eau de Toilette variations
                    ["edt"] = "EDT",
                    ["eau de toilette"] = "EDT",
                    ["e.d.t."] = "EDT",
                    ["e.d.t"] = "EDT",
                    ["toilet water"] = "EDT",
                    
                    // Eau de Parfum variations
                    ["edp"] = "EDP",
                    ["eau de parfum"] = "EDP",
                    ["e.d.p."] = "EDP",
                    ["e.d.p"] = "EDP",
                    
                    // Parfum variations
                    ["parfum"] = "Parfum",
                    ["perfume"] = "Parfum",
                    ["extrait"] = "Parfum",
                    ["pure perfume"] = "Parfum",
                    
                    // Eau de Cologne variations
                    ["edc"] = "EDC",
                    ["eau de cologne"] = "EDC",
                    ["e.d.c."] = "EDC",
                    ["cologne"] = "EDC",
                    
                    // Eau Fraiche variations
                    ["eau fraiche"] = "Eau Fraiche",
                    ["eau fraîche"] = "Eau Fraiche", // French accent
                    ["fresh water"] = "Eau Fraiche",
                },

                ["FragranceFamily"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    // Floral variations
                    ["floral"] = "Floral",
                    ["flower"] = "Floral",
                    ["flowers"] = "Floral",
                    ["fleuri"] = "Floral", // French
                    
                    // Oriental variations
                    ["oriental"] = "Oriental",
                    ["amber"] = "Oriental",
                    ["oriental spicy"] = "Oriental",
                    
                    // Fresh variations
                    ["fresh"] = "Fresh",
                    ["citrus"] = "Fresh",
                    ["aquatic"] = "Fresh",
                    ["marine"] = "Fresh",
                    ["frais"] = "Fresh", // French
                    
                    // Woody variations
                    ["woody"] = "Woody",
                    ["wood"] = "Woody",
                    ["woods"] = "Woody",
                    ["boisé"] = "Woody", // French
                    
                    // Chypre variations
                    ["chypre"] = "Chypre",
                    ["cyprus"] = "Chypre",
                    
                    // Fougère variations
                    ["fougere"] = "Fougère",
                    ["fougère"] = "Fougère", // French accent
                    ["fern"] = "Fougère",
                }
            };
        }

        #endregion

        /// <summary>
        /// Gets all possible normalized values for a given property
        /// Useful for building filter dropdowns
        /// </summary>
        /// <param name="propertyKey">Normalized property key</param>
        /// <returns>List of possible normalized values</returns>
        public List<string> GetPossibleValues(string propertyKey)
        {
            if (_valueMappings.TryGetValue(propertyKey, out var valueMap))
            {
                return valueMap.Values.Distinct().OrderBy(v => v).ToList();
            }
            return new List<string>();
        }

        /// <summary>
        /// Gets all normalized property keys
        /// </summary>
        /// <returns>List of normalized property keys</returns>
        public List<string> GetNormalizedKeys()
        {
            return _keyMappings.Values.Distinct().OrderBy(k => k).ToList();
        }
    }
}
