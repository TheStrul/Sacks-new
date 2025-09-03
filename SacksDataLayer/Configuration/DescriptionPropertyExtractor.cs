using System.Text.RegularExpressions;
using SacksDataLayer.Configuration;

namespace SacksDataLayer.Configuration
{
    /// <summary>
    /// Extracts known product properties from unstructured description text
    /// Uses pattern matching and known product attribute patterns for cosmetics/perfumes
    /// </summary>
    public class DescriptionPropertyExtractor
    {
        private readonly PropertyNormalizer _normalizer;
        private readonly Dictionary<string, List<ExtractionPattern>> _extractionPatterns;

        public DescriptionPropertyExtractor(PropertyNormalizer normalizer)
        {
            _normalizer = normalizer ?? throw new ArgumentNullException(nameof(normalizer));
            _extractionPatterns = InitializeExtractionPatterns();
        }

        /// <summary>
        /// Extracts properties from description text and returns normalized property dictionary
        /// </summary>
        public Dictionary<string, object?> ExtractPropertiesFromDescription(string description)
        {
            if (string.IsNullOrWhiteSpace(description))
                return new Dictionary<string, object?>();

            var extractedProperties = new Dictionary<string, object?>();

            // Extract each property type
            foreach (var patternGroup in _extractionPatterns)
            {
                var propertyName = patternGroup.Key;
                var patterns = patternGroup.Value;

                var extractedValue = ExtractPropertyValue(description, patterns);
                if (!string.IsNullOrEmpty(extractedValue))
                {
                    extractedProperties[propertyName] = extractedValue;
                }
            }

            // Normalize the extracted properties using existing normalizer
            return _normalizer.NormalizeProperties(extractedProperties);
        }

        /// <summary>
        /// Test method to extract and display all matches for debugging purposes
        /// </summary>
        public Dictionary<string, List<string>> ExtractAllMatches(string description)
        {
            if (string.IsNullOrWhiteSpace(description))
                return new Dictionary<string, List<string>>();

            var allMatches = new Dictionary<string, List<string>>();

            foreach (var patternGroup in _extractionPatterns)
            {
                var propertyName = patternGroup.Key;
                var patterns = patternGroup.Value;
                var matches = new List<string>();

                foreach (var pattern in patterns)
                {
                    var regex = new Regex(pattern.Pattern, RegexOptions.IgnoreCase);
                    var regexMatches = regex.Matches(description);
                    
                    foreach (Match match in regexMatches)
                    {
                        var value = pattern.GroupIndex > 0 && match.Groups.Count > pattern.GroupIndex
                            ? match.Groups[pattern.GroupIndex].Value.Trim()
                            : match.Value.Trim();

                        if (pattern.Transformation != null)
                        {
                            try
                            {
                                value = pattern.Transformation(value);
                            }
                            catch
                            {
                                // Skip transformation errors
                                continue;
                            }
                        }

                        if (!string.IsNullOrWhiteSpace(value))
                        {
                            matches.Add($"{value} (pattern: {pattern.Pattern}, priority: {pattern.Priority})");
                        }
                    }
                }

                if (matches.Any())
                {
                    allMatches[propertyName] = matches;
                }
            }

            return allMatches;
        }

        /// <summary>
        /// Extracts a specific property value using multiple patterns with priority order
        /// </summary>
        private string? ExtractPropertyValue(string description, List<ExtractionPattern> patterns)
        {
            foreach (var pattern in patterns.OrderBy(p => p.Priority))
            {
                var match = Regex.Match(description, pattern.Pattern, RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    var value = pattern.GroupIndex > 0 && match.Groups.Count > pattern.GroupIndex
                        ? match.Groups[pattern.GroupIndex].Value.Trim()
                        : match.Value.Trim();

                    // Apply transformation if specified
                    if (pattern.Transformation != null)
                    {
                        value = pattern.Transformation(value);
                    }

                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        return value;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Initialize extraction patterns for common perfume/cosmetic properties
        /// </summary>
        private Dictionary<string, List<ExtractionPattern>> InitializeExtractionPatterns()
        {
            return new Dictionary<string, List<ExtractionPattern>>
            {
                ["Brand"] = new List<ExtractionPattern>
                {
                    // Known luxury brands (high priority)
                    new ExtractionPattern(@"\b(Dior|Chanel|Tom Ford|Yves Saint Laurent|YSL|Guerlain|Hermès|Creed|Armani|Versace|Dolce & Gabbana|Prada|Burberry|Calvin Klein|Hugo Boss|Issey Miyake|Kenzo|Lancôme|Estée Lauder|Clinique|Clarins|L'Oréal|Maybelline|Revlon|MAC|NARS|Urban Decay|Too Faced|Benefit|Sephora|Fenty Beauty|Rare Beauty|Glossier|Charlotte Tilotte|Pat McGrath|Huda Beauty|Kylie Cosmetics|Jeffree Star|Anastasia Beverly Hills|Tarte|Smashbox|Bobbi Brown|Laura Mercier|Hourglass|Drunk Elephant|The Ordinary|CeraVe|Neutrogena|Olay|Nivea|Dove|Garnier|Head & Shoulders|Pantene|TRESemmé|Schwarzkopf|Wella|Matrix|Redken|Paul Mitchell|Aveda|Bumble and bumble|Moroccanoil|Olaplex|Living Proof|Ouai|Verb|Briogeo|Pattern|SheaMoisture|Carol's Daughter|Cantu|As I Am|Mielle|Aunt Jackie's|Miss Jessie's|DevaCurl|Curls|Kinky-Curly|Shea Moisture|African Pride|Blue Magic|Pink|Luster's|Dark and Lovely|Optimum|Mizani|Affirm|Motions|TCB|Ultra Sheen|Isoplus|Lustrasilk|Soft & Beautiful|Just for Me|Kiddie|No-Lye|Gentle Treatment|Silk Elements|Profectiv|Dr. Miracle's|Africa's Best|Organic Root Stimulator|Palmer's|Jamaican Mango & Lime|Taliah Waajid|Camille Rose|Mielle Organics|Alikay Naturals|Curls Blueberry Bliss|TGIN|Aunt Jackie's|Miss Jessie's|DevaCurl|Ouidad|Moroccanoil|Macadamia|Argan Oil|Josie Maran|Acure|Alba Botanica|Andalou Naturals|Avalon Organics|Badger|Burt's Bees|California Baby|Desert Essence|Dr. Bronner's|Earth Mama|EO|Everyone|Jason|Kiss My Face|Mad Hippie|Nature's Gate|Pacifica|Shikai|Weleda|Yes To|Zion Health|Acure|Alba Botanica|Andalou Naturals|Avalon Organics|Badger|Burt's Bees|California Baby|Desert Essence|Dr. Bronner's|Earth Mama|EO|Everyone|Jason|Kiss My Face|Mad Hippie|Nature's Gate|Pacifica|Shikai|Weleda|Yes To|Zion Health)\b", 1, 1),
                    
                    // Generic brand patterns (lower priority)
                    new ExtractionPattern(@"^([A-Z][a-zA-Z\s&\.]{2,20})\s+(?:Eau de|EDT|EDP|Parfum|Cologne|Fragrance)", 1, 2),
                    new ExtractionPattern(@"by\s+([A-Z][a-zA-Z\s&\.]{2,20})", 1, 3),
                    new ExtractionPattern(@"([A-Z][a-zA-Z\s&\.]{2,20})\s+(?:for\s+(?:Men|Women|Him|Her))", 1, 4)
                },

                ["Size"] = new List<ExtractionPattern>
                {
                    // Volume patterns
                    new ExtractionPattern(@"\b(\d+(?:\.\d+)?)\s*ml\b", 1, 1, v => v + "ml"),
                    new ExtractionPattern(@"\b(\d+(?:\.\d+)?)\s*ML\b", 1, 1, v => v + "ml"),
                    new ExtractionPattern(@"\b(\d+(?:\.\d+)?)\s*mL\b", 1, 1, v => v + "ml"),
                    new ExtractionPattern(@"\b(\d+)\s*cc\b", 1, 2, v => v + "ml"), // cc = ml
                    new ExtractionPattern(@"\b(\d+(?:\.\d+)?)\s*oz\b", 1, 3, v => Math.Round(double.Parse(v) * 29.5735, 0) + "ml"), // Convert oz to ml
                    new ExtractionPattern(@"\b(\d+(?:\.\d+)?)\s*fl\.?\s*oz\b", 1, 3, v => Math.Round(double.Parse(v) * 29.5735, 0) + "ml"),
                    
                    // Weight patterns for cosmetics
                    new ExtractionPattern(@"\b(\d+(?:\.\d+)?)\s*g\b", 1, 4, v => v + "g"),
                    new ExtractionPattern(@"\b(\d+(?:\.\d+)?)\s*gr\b", 1, 4, v => v + "g"),
                    new ExtractionPattern(@"\b(\d+(?:\.\d+)?)\s*grams?\b", 1, 5, v => v + "g"),
                    
                    // Count patterns
                    new ExtractionPattern(@"\b(\d+)\s*(?:pieces?|pcs?|count|ct)\b", 1, 6, v => v + " pieces"),
                    new ExtractionPattern(@"\bpack\s+of\s+(\d+)", 1, 6, v => v + " pieces")
                },

                ["Gender"] = new List<ExtractionPattern>
                {
                    new ExtractionPattern(@"\b(?:for\s+)?(men|male|homme|masculin)\b", 1, 1, v => "Men"),
                    new ExtractionPattern(@"\b(?:for\s+)?(women|female|femme|feminine|lady|ladies)\b", 1, 1, v => "Women"),
                    new ExtractionPattern(@"\b(?:for\s+)?(unisex|both|everyone|all)\b", 1, 1, v => "Unisex"),
                    new ExtractionPattern(@"\b(?:for\s+)?(him)\b", 1, 2, v => "Men"),
                    new ExtractionPattern(@"\b(?:for\s+)?(her)\b", 1, 2, v => "Women"),
                    new ExtractionPattern(@"\b(masculine|masculino)\b", 1, 3, v => "Men"),
                    new ExtractionPattern(@"\b(feminine|feminino)\b", 1, 3, v => "Women")
                },

                ["Concentration"] = new List<ExtractionPattern>
                {
                    new ExtractionPattern(@"\b(Parfum|Pure Parfum|Extrait de Parfum)\b", 1, 1, v => "Parfum"),
                    new ExtractionPattern(@"\b(Eau de Parfum|EDP|EdP)\b", 1, 2, v => "Eau de Parfum"),
                    new ExtractionPattern(@"\b(Eau de Toilette|EDT|EdT)\b", 1, 3, v => "Eau de Toilette"),
                    new ExtractionPattern(@"\b(Eau de Cologne|EDC|EdC|Cologne)\b", 1, 4, v => "Eau de Cologne"),
                    new ExtractionPattern(@"\b(Eau Fraiche|Eau Fraîche)\b", 1, 5, v => "Eau Fraiche"),
                    new ExtractionPattern(@"\b(Aftershave|After Shave)\b", 1, 6, v => "Aftershave"),
                    new ExtractionPattern(@"\b(Body Spray|Deodorant Spray)\b", 1, 7, v => "Body Spray")
                },

                ["Category"] = new List<ExtractionPattern>
                {
                    // Fragrance categories
                    new ExtractionPattern(@"\b(Perfume|Fragrance|Scent|Cologne|Parfum)\b", 1, 1, v => "Fragrance"),
                    
                    // Makeup categories
                    new ExtractionPattern(@"\b(Foundation|Concealer|Primer|BB Cream|CC Cream|Tinted Moisturizer)\b", 1, 2, v => "Base Makeup"),
                    new ExtractionPattern(@"\b(Lipstick|Lip Gloss|Lip Balm|Lip Liner|Lip Stain|Lip Tint)\b", 1, 3, v => "Lip Makeup"),
                    new ExtractionPattern(@"\b(Eyeshadow|Eye Shadow|Mascara|Eyeliner|Eye Liner|Eyebrow|Brow)\b", 1, 4, v => "Eye Makeup"),
                    new ExtractionPattern(@"\b(Blush|Bronzer|Highlighter|Contour|Cheek)\b", 1, 5, v => "Face Makeup"),
                    new ExtractionPattern(@"\b(Nail Polish|Nail Lacquer|Nail Color|Manicure|Pedicure)\b", 1, 6, v => "Nail Care"),
                    
                    // Skincare categories
                    new ExtractionPattern(@"\b(Cleanser|Face Wash|Facial Cleanser|Cleansing)\b", 1, 7, v => "Cleanser"),
                    new ExtractionPattern(@"\b(Moisturizer|Cream|Lotion|Hydrating|Hydration)\b", 1, 8, v => "Moisturizer"),
                    new ExtractionPattern(@"\b(Serum|Treatment|Anti-aging|Anti-Aging|Firming)\b", 1, 9, v => "Treatment"),
                    new ExtractionPattern(@"\b(Sunscreen|SPF|Sun Protection|UV Protection)\b", 1, 10, v => "Sun Care"),
                    new ExtractionPattern(@"\b(Toner|Astringent|Clarifying|Balancing)\b", 1, 11, v => "Toner"),
                    new ExtractionPattern(@"\b(Exfoliant|Scrub|Peeling|Exfoliating)\b", 1, 12, v => "Exfoliant"),
                    
                    // Hair care categories
                    new ExtractionPattern(@"\b(Shampoo|Hair Wash|Cleansing Shampoo)\b", 1, 13, v => "Hair Care"),
                    new ExtractionPattern(@"\b(Conditioner|Hair Conditioner|Hair Treatment)\b", 1, 14, v => "Hair Care"),
                    new ExtractionPattern(@"\b(Hair Mask|Hair Treatment|Deep Conditioning)\b", 1, 15, v => "Hair Care"),
                    new ExtractionPattern(@"\b(Hair Oil|Hair Serum|Hair Treatment Oil)\b", 1, 16, v => "Hair Care"),
                    
                    // Body care categories
                    new ExtractionPattern(@"\b(Body Lotion|Body Cream|Body Moisturizer|Body Care)\b", 1, 17, v => "Body Care"),
                    new ExtractionPattern(@"\b(Body Wash|Shower Gel|Body Cleanser)\b", 1, 18, v => "Body Care"),
                    new ExtractionPattern(@"\b(Deodorant|Antiperspirant|Body Spray)\b", 1, 19, v => "Body Care")
                },

                ["ProductLine"] = new List<ExtractionPattern>
                {
                    // Common perfume lines
                    new ExtractionPattern(@"\b(Miss Dior|J'adore|Sauvage|Fahrenheit|Poison|Addict|Homme|Hypnotic|Pure|Intense|Noir|White|Black|Gold|Silver|Rose|Flower|Ocean|Summer|Winter|Night|Day|Classic|Modern|Vintage|New|Original|Special|Limited|Edition|Collection)\b", 1, 1),
                    
                    // Generic line patterns
                    new ExtractionPattern(@"([A-Z][a-zA-Z\s]{3,25})\s+(?:Eau de|EDT|EDP|Parfum|Collection|Line|Series)", 1, 2),
                    new ExtractionPattern(@"\b([A-Z][a-zA-Z\s]{3,25})\s+(?:for\s+(?:Men|Women|Him|Her))", 1, 3)
                },

                ["FragranceFamily"] = new List<ExtractionPattern>
                {
                    new ExtractionPattern(@"\b(Floral|Flower|Rose|Jasmine|Lily|Peony|Gardenia|Tuberose|Ylang|Magnolia)\b", 1, 1, v => "Floral"),
                    new ExtractionPattern(@"\b(Oriental|Amber|Vanilla|Sandalwood|Patchouli|Incense|Spicy|Warm)\b", 1, 2, v => "Oriental"),
                    new ExtractionPattern(@"\b(Fresh|Citrus|Lemon|Orange|Bergamot|Grapefruit|Lime|Green|Aquatic|Marine|Ocean)\b", 1, 3, v => "Fresh"),
                    new ExtractionPattern(@"\b(Woody|Wood|Cedar|Oak|Pine|Birch|Sandalwood|Rosewood|Teak)\b", 1, 4, v => "Woody"),
                    new ExtractionPattern(@"\b(Fruity|Fruit|Apple|Pear|Peach|Berry|Cherry|Grape|Tropical)\b", 1, 5, v => "Fruity"),
                    new ExtractionPattern(@"\b(Gourmand|Sweet|Chocolate|Caramel|Honey|Sugar|Dessert|Bakery)\b", 1, 6, v => "Gourmand"),
                    new ExtractionPattern(@"\b(Chypre|Mossy|Earthy|Herbal|Green)\b", 1, 7, v => "Chypre"),
                    new ExtractionPattern(@"\b(Fougère|Fougere|Lavender|Aromatic|Herbal)\b", 1, 8, v => "Fougère")
                }
            };
        }
    }

    /// <summary>
    /// Represents a regex pattern for extracting a specific property value
    /// </summary>
    public class ExtractionPattern
    {
        public string Pattern { get; }
        public int GroupIndex { get; }
        public int Priority { get; }
        public Func<string, string>? Transformation { get; }

        public ExtractionPattern(string pattern, int groupIndex = 0, int priority = 1, Func<string, string>? transformation = null)
        {
            Pattern = pattern;
            GroupIndex = groupIndex;
            Priority = priority;
            Transformation = transformation;
        }
    }
}
