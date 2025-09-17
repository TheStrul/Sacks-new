using ParsingEngine;
using System.Text.Json;

// Load the supplier-formats.json and extract HAND supplier config
var supplierFormatsPath = "../SacksApp/Configuration/supplier-formats.json";
Console.WriteLine($"Loading supplier formats: {supplierFormatsPath}");

var jsonText = File.ReadAllText(supplierFormatsPath);
var supplierDoc = JsonDocument.Parse(jsonText);
var suppliersArray = supplierDoc.RootElement.GetProperty("suppliers");

// Find HAND supplier
var handSupplier = suppliersArray.EnumerateArray()
    .FirstOrDefault(s => s.GetProperty("name").GetString() == "HAND");

if (handSupplier.ValueKind == JsonValueKind.Undefined)
{
    Console.WriteLine("HAND supplier not found!");
    return;
}

var parserConfigElement = handSupplier.GetProperty("parserConfig");
var parserConfigJson = parserConfigElement.GetRawText();

// Write temp file and load it
var tempConfigFile = "temp-hand-config.json";
File.WriteAllText(tempConfigFile, parserConfigJson);
var cfg = ParserConfigLoader.FromJsonFile(tempConfigFile);

// Debug: list expected columns + rule IDs
Console.WriteLine("Rules expect columns (with rule IDs):");
foreach (var col in cfg.Columns)
{
    Console.WriteLine($"  - {col.Column}");
    foreach (var r in col.Rules.OrderBy(r => r.Priority))
        Console.WriteLine($"      • {r.Id}  (type={r.Type}, priority={r.Priority})");
}

var engine = new ParserEngine(cfg);

// Test with the exact example from your specification
var row = new RowData(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
{
    ["A"] = "REGULAR",
    ["B"] = "P1DV1L00", 
    ["C"] = "8057971188284",
    ["D"] = "REG",
    ["E"] = "VERSACE Yellow Diamond Intense Wom EDP (90ml)",
    ["F"] = "1304",
    ["G"] = "33.00"
});

// Debug: list keys actually in the row
Console.WriteLine("\nRow has columns:");
foreach (var k in row.Cells.Keys)
    Console.WriteLine($"  - {k}");

var bag = engine.Parse(row);

Console.WriteLine("\nParsed values:");
foreach (var kv in bag.Values.OrderBy(x => x.Key))
    Console.WriteLine($"  {kv.Key}: {kv.Value}");

Console.WriteLine("\nTrace:");
foreach (var t in bag.Trace)
    Console.WriteLine($"  {t}");

// Test additional examples
Console.WriteLine("\n" + new string('=', 50));
Console.WriteLine("Testing additional examples:");

var testCases = new[]
{
    "D&G Pour Homme EDT TST (125ml)",
    "MOSCHINO Pink Fresh Couture Wom EDT SMP (1ml)", 
    "MOSCHINO Toy2Pearl EDP (100+SG100+BL100+10)",
    "LOLITA LEMPICKA Mon Premier Wom EDP (100ml)"
};

foreach (var testCase in testCases)
{
    Console.WriteLine($"\nTesting: {testCase}");
    var testRow = new RowData(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["E"] = testCase
    });
    
    var testBag = engine.Parse(testRow);
    Console.WriteLine("Results:");
    foreach (var kv in testBag.Values.Where(x => x.Key.StartsWith("Product.") || x.Key.StartsWith("Offer.")).OrderBy(x => x.Key))
        Console.WriteLine($"  {kv.Key}: {kv.Value}");
}

// Clean up temp file
if (File.Exists(tempConfigFile))
    File.Delete(tempConfigFile);
