using ParsingEngine;

var rulesPath = args.Length > 0 ? args[0] : "rules/parsing.rules.en-US.json";
Console.WriteLine($"Loading rules: {rulesPath}");

var cfg = ParserConfigLoader.FromJsonFile(rulesPath);

// Debug: list expected columns + rule IDs
Console.WriteLine("Rules expect columns (with rule IDs):");
foreach (var col in cfg.Columns)
{
    Console.WriteLine($"  - {col.Column}");
    foreach (var r in col.Rules.OrderBy(r => r.Priority))
        Console.WriteLine($"      · {r.Id}  (type={r.Type}, priority={r.Priority})");
}

var engine = new ParserEngine(cfg);

// Row with A..G columns
var row = new RowData(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
{
    ["A"] = "REGULARs:D&G:P1DV1L00",
    ["B"] = "P1DV1L00",
    ["C"] = "8057971188284",
    ["D"] = "REG",
    ["E"] = "D&G Devotion Intense Wom EDP (50ml)",
    ["F"] = "1304",
    ["G"] = "33.00"
});

// Debug: list keys actually in the row
Console.WriteLine("\nRow has columns:");
foreach (var k in row.Cells.Keys)
    Console.WriteLine($"  - {k}");

var bag = engine.Parse(row);

Console.WriteLine("\nParsed values:");
foreach (var kv in bag.Values)
    Console.WriteLine($"  {kv.Key}: {kv.Value}");

Console.WriteLine("\nTrace:");
foreach (var t in bag.Trace)
    Console.WriteLine($"  {t}");
