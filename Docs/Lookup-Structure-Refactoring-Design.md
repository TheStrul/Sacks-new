# Lookup Structure Refactoring Design

## Executive Summary

Change lookup tables from flat `Dictionary<string, string>` to array-based structure with explicit canonical values and aliases.

**Current Structure:**
```json
{
  "Lookups": {
    "COO": {
      "USA": "United States",
      "US": "United States",
      "United States": "United States"
    }
  }
}
```

**New Structure (Option B):**
```json
{
  "Lookups": {
    "COO": [
      {
        "Canonical": "United States",
        "Aliases": ["USA", "US", "United States", "America", "Made in USA"]
      },
      {
        "Canonical": "United Kingdom",
        "Aliases": ["UK", "GB", "Britain", "United Kingdom"]
      }
    ]
  }
}
```

---

## Benefits

? **Eliminates Redundancy**: No more `"USA": "United States"` repeated entries  
? **Clear Intent**: Explicit canonical value vs search aliases  
? **Easier Maintenance**: Add aliases without touching multiple entries  
? **Better Validation**: Can enforce "Canonical must exist in Aliases"  
? **Same Runtime Performance**: Flatten to `Dictionary<string, string>` at load time

---

## Implementation Plan

### Phase 1: Add New Models (No Breaking Changes)

```csharp
// SacksDataLayer/Models/LookupEntry.cs
namespace SacksDataLayer.FileProcessing.Configuration;

public sealed class LookupEntry
{
    public required string Canonical { get; set; }
    public List<string> Aliases { get; set; } = new();
}
```

### Phase 2: Add Deserialization Support

Modify `SuppliersConfiguration` to support **both formats** during deserialization:

```csharp
// Support both legacy flat dictionaries and new array format
[JsonPropertyName("Lookups")]
public JsonElement LookupsRaw { get; set; }

[JsonIgnore]
public Dictionary<string, Dictionary<string, string>> Lookups { get; private set; }

public void ProcessLookups()
{
    Lookups = new(StringComparer.OrdinalIgnoreCase);
    
    if (LookupsRaw.ValueKind == JsonValueKind.Object)
    {
        foreach (var prop in LookupsRaw.EnumerateObject())
        {
            var tableName = prop.Name;
            
            if (prop.Value.ValueKind == JsonValueKind.Array)
            {
                // New format: array of LookupEntry
                var entries = JsonSerializer.Deserialize<List<LookupEntry>>(prop.Value);
                Lookups[tableName] = FlattenEntries(entries);
            }
            else if (prop.Value.ValueKind == JsonValueKind.Object)
            {
                // Legacy format: flat dictionary
                var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(prop.Value);
                Lookups[tableName] = new(dict, StringComparer.OrdinalIgnoreCase);
            }
        }
    }
}

private static Dictionary<string, string> FlattenEntries(List<LookupEntry>? entries)
{
    var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    if (entries == null) return result;
    
    foreach (var entry in entries)
    {
        foreach (var alias in entry.Aliases)
        {
            if (!string.IsNullOrWhiteSpace(alias))
            {
                result[alias.Trim()] = entry.Canonical;
            }
        }
    }
    return result;
}
```

### Phase 3: Update Serialization (Save)

When saving, convert `Lookups` back to **new array format**:

```csharp
public async Task Save()
{
    // Convert flat dictionaries to new array format for cleaner JSON
    var newFormatLookups = new Dictionary<string, List<LookupEntry>>();
    
    foreach (var tbl in Lookups)
    {
        var entries = GroupByCanonical(tbl.Value);
        newFormatLookups[tbl.Key] = entries;
    }
    
    // Serialize with new format
    var json = JsonSerializer.Serialize(new
    {
        Version,
        Suppliers,
        Lookups = newFormatLookups
    }, s_jsonOptions);
    
    await File.WriteAllTextAsync(FullPath, json);
}

private static List<LookupEntry> GroupByCanonical(Dictionary<string, string> flatDict)
{
    // Group aliases by canonical value
    var groups = flatDict.GroupBy(kv => kv.Value, StringComparer.OrdinalIgnoreCase);
    
    return groups.Select(g => new LookupEntry
    {
        Canonical = g.Key,
        Aliases = g.Select(kv => kv.Key).Distinct(StringComparer.OrdinalIgnoreCase).ToList()
    }).ToList();
}
```

### Phase 4: Update LookupEditorForm

**Two Options:**

**Option A**: Keep flat editing (simpler)
- Continue editing as Key?Value pairs
- Save converts to new format automatically
- User doesn't see structure change

**Option B**: Edit canonicals+aliases (cleaner)
- Show Canonical column + Aliases (comma-separated)
- Requires new grid layout
- More user-friendly for maintaining aliases

**Recommendation: Start with Option A** (no UI changes needed)

### Phase 5: Validation Updates

Add validation for new structure:

```csharp
// In ValidateConfiguration()
if (entry.Aliases == null || entry.Aliases.Count == 0)
{
    errors.Add($"Lookup '{tableName}' entry with Canonical '{entry.Canonical}' has no aliases");
}

if (!entry.Aliases.Contains(entry.Canonical, StringComparer.OrdinalIgnoreCase))
{
    warnings.Add($"Lookup '{tableName}' Canonical '{entry.Canonical}' should be included in its own aliases");
}

var dupAliases = entry.Aliases
    .GroupBy(a => a, StringComparer.OrdinalIgnoreCase)
    .Where(g => g.Count() > 1)
    .Select(g => g.Key);
if (dupAliases.Any())
{
    errors.Add($"Lookup '{tableName}' has duplicate aliases: {string.Join(", ", dupAliases)}");
}
```

---

## Migration Strategy

### Backward Compatibility

? **Reading**: Support both old and new formats  
? **Writing**: Always save in new format  
? **Runtime**: Always use flattened `Dictionary<string, string>`

**Result**: Existing JSON files work as-is; next save converts to new format.

### Testing Strategy

```csharp
[Fact]
public async Task Load_LegacyFormat_WorksCorrectly()
{
    var json = """
    {
      "Lookups": {
        "COO": {
          "USA": "United States",
          "US": "United States"
        }
      }
    }
    """;
    
    var config = JsonSerializer.Deserialize<SuppliersConfiguration>(json);
    config.ProcessLookups();
    
    Assert.Equal("United States", config.Lookups["COO"]["USA"]);
    Assert.Equal("United States", config.Lookups["COO"]["US"]);
}

[Fact]
public async Task Load_NewFormat_WorksCorrectly()
{
    var json = """
    {
      "Lookups": {
        "COO": [
          {
            "Canonical": "United States",
            "Aliases": ["USA", "US", "United States"]
          }
        ]
      }
    }
    """;
    
    var config = JsonSerializer.Deserialize<SuppliersConfiguration>(json);
    config.ProcessLookups();
    
    Assert.Equal("United States", config.Lookups["COO"]["USA"]);
    Assert.Equal("United States", config.Lookups["COO"]["US"]);
}

[Fact]
public async Task Save_ConvertsFlatToNewFormat()
{
    var config = new SuppliersConfiguration();
    config.Lookups["COO"] = new(StringComparer.OrdinalIgnoreCase)
    {
        ["USA"] = "United States",
        ["US"] = "United States",
        ["United States"] = "United States"
    };
    
    await config.Save();
    
    var saved = await File.ReadAllTextAsync(config.FullPath);
    Assert.Contains("\"Canonical\"", saved);
    Assert.Contains("\"Aliases\"", saved);
}
```

---

## Files to Modify

### Core Changes
- ?? `SacksDataLayer/Models/SupplierConfigurationModels.cs` - Add deserialization/serialization
- ? `SacksDataLayer/Models/LookupEntry.cs` - New model
- ?? `SacksDataLayer/Models/ISuppliersConfiguration.cs` - Interface stays same (backward compat)

### Optional Changes (Phase 2)
- ?? `SacksApp/LookupEditorForm.cs` - Update UI for canonical+aliases editing (optional)

### Testing
- ? `Sacks.Tests/LookupStructureTests.cs` - New test file

---

## Rollout Plan

1. **Week 1**: Implement deserialization support (read both formats)
2. **Week 2**: Add tests; verify all existing JSON files load correctly
3. **Week 3**: Implement serialization (write new format)
4. **Week 4**: Test end-to-end; convert one supplier config as pilot
5. **Week 5**: Roll out to all supplier configs; update documentation

---

## Risk Mitigation

??? **Backward Compatibility**: Old files still work  
??? **Gradual Migration**: Files convert on next save  
??? **Runtime Performance**: Same (flattened dictionary)  
??? **No Code Changes**: `FindAction`, `MappingAction` unchanged

---

*Design Document for Struly-Dear*  
*Date: 2024*
