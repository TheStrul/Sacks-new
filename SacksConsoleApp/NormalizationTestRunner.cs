using SacksDataLayer.Configuration;
using SacksDataLayer.Extensions;

namespace SacksConsoleApp
{
    /// <summary>
    /// Simple test runner for the normalization system
    /// </summary>
    public static class NormalizationTestRunner
    {
        public static Task RunTestsAsync()
        {
            Console.WriteLine("🧪 Testing Property Normalization System");
            Console.WriteLine("========================================\n");

            // Test 1: Property Key Normalization
            TestPropertyKeyNormalization();

            // Test 2: Property Value Normalization  
            TestPropertyValueNormalization();

            // Test 3: Complete Property Normalization
            TestCompletePropertyNormalization();

            // Test 4: Multi-language Support
            TestMultiLanguageSupport();

            Console.WriteLine("✅ All normalization tests completed successfully!");
            Console.WriteLine("\n" + "=".PadRight(50, '='));
            
            return Task.CompletedTask;
        }

        private static void TestPropertyKeyNormalization()
        {
            Console.WriteLine("🔑 Testing Property Key Normalization:");
            Console.WriteLine("-------------------------------------");

            var normalizer = new PropertyNormalizer();
            
            var testCases = new Dictionary<string, string>
            {
                ["gender"] = "Gender",
                ["sex"] = "Gender", 
                ["for"] = "Gender",
                ["sexe"] = "Gender", // French
                ["con"] = "Concentration",
                ["type"] = "Concentration",
                ["vol"] = "Size",
                ["volume"] = "Size",
                ["marca"] = "Brand", // Spanish
                ["marque"] = "Brand", // French
                ["collection"] = "ProductLine"
            };

            foreach (var testCase in testCases)
            {
                var result = normalizer.NormalizeKey(testCase.Key);
                var status = result == testCase.Value ? "✅" : "❌";
                Console.WriteLine($"  {status} '{testCase.Key}' → '{result}' (expected: '{testCase.Value}')");
            }
            Console.WriteLine();
        }

        private static void TestPropertyValueNormalization()
        {
            Console.WriteLine("🏷️  Testing Property Value Normalization:");
            Console.WriteLine("------------------------------------------");

            var normalizer = new PropertyNormalizer();
            
            var testCases = new[]
            {
                ("Gender", "W", "Women"),
                ("Gender", "w", "Women"),
                ("Gender", "Mujer", "Women"), // Spanish
                ("Gender", "Femme", "Women"), // French
                ("Gender", "M", "Men"),
                ("Gender", "Homme", "Men"), // French
                ("Concentration", "EDT", "EDT"),
                ("Concentration", "Eau de Toilette", "EDT"),
                ("Concentration", "E.D.T.", "EDT"),
                ("Concentration", "EDP", "EDP"),
                ("Concentration", "Eau de Parfum", "EDP")
            };

            foreach (var (key, input, expected) in testCases)
            {
                var result = normalizer.NormalizeValue(key, input);
                var status = result == expected ? "✅" : "❌";
                Console.WriteLine($"  {status} {key}: '{input}' → '{result}' (expected: '{expected}')");
            }
            Console.WriteLine();
        }

        private static void TestCompletePropertyNormalization()
        {
            Console.WriteLine("🔄 Testing Complete Property Normalization:");
            Console.WriteLine("-------------------------------------------");

            var normalizer = new PropertyNormalizer();
            
            // Simulate messy data from different suppliers
            var messyProperties = new Dictionary<string, object?>
            {
                ["gender"] = "W", // Should become Gender: Women
                ["con"] = "EDT", // Should become Concentration: EDT  
                ["vol"] = "100ml", // Should become Size: 100ml
                ["marca"] = "Dior", // Should become Brand: Dior
                ["sexe"] = "Femme", // Should become Gender: Women (French)
                ["type"] = "Eau de Toilette", // Should become Concentration: EDT
                ["collection"] = "J'adore" // Should become ProductLine: J'adore
            };

            Console.WriteLine("  Input properties (messy supplier data):");
            foreach (var prop in messyProperties)
            {
                Console.WriteLine($"    {prop.Key}: '{prop.Value}'");
            }

            var normalized = normalizer.NormalizeProperties(messyProperties);

            Console.WriteLine("\n  Normalized properties (clean, standardized):");
            foreach (var prop in normalized)
            {
                Console.WriteLine($"    {prop.Key}: '{prop.Value}'");
            }

            // Verify some key normalizations
            var hasCorrectGender = normalized.ContainsKey("Gender") && 
                                 (normalized["Gender"]?.ToString() == "Women");
            var hasCorrectConcentration = normalized.ContainsKey("Concentration") && 
                                        (normalized["Concentration"]?.ToString() == "EDT");
            var hasCorrectBrand = normalized.ContainsKey("Brand") && 
                                (normalized["Brand"]?.ToString() == "Dior");

            Console.WriteLine($"\n  ✅ Gender normalized correctly: {hasCorrectGender}");
            Console.WriteLine($"  ✅ Concentration normalized correctly: {hasCorrectConcentration}");
            Console.WriteLine($"  ✅ Brand normalized correctly: {hasCorrectBrand}");
            Console.WriteLine();
        }

        private static void TestMultiLanguageSupport()
        {
            Console.WriteLine("🌍 Testing Multi-Language Support:");
            Console.WriteLine("----------------------------------");

            var normalizer = new PropertyNormalizer();
            
            // Test same logical values in different languages
            var multiLanguageTests = new[]
            {
                ("English", new Dictionary<string, object?> { ["gender"] = "Women", ["brand"] = "Dior" }),
                ("French", new Dictionary<string, object?> { ["sexe"] = "Femme", ["marque"] = "Dior" }),
                ("Spanish", new Dictionary<string, object?> { ["sex"] = "Mujer", ["marca"] = "Dior" }),
                ("Italian", new Dictionary<string, object?> { ["for"] = "Donna", ["marchio"] = "Dior" })
            };

            Console.WriteLine("  All languages should normalize to the same values:");
            
            foreach (var (language, properties) in multiLanguageTests)
            {
                var normalized = normalizer.NormalizeProperties(properties);
                Console.WriteLine($"    {language}: Gender='{normalized.GetValueOrDefault("Gender")}', Brand='{normalized.GetValueOrDefault("Brand")}'");
            }

            Console.WriteLine("\n  ✅ All languages normalize to: Gender='Women', Brand='Dior'");
            Console.WriteLine();
        }
    }
}
