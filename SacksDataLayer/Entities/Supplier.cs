using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace Sacks.Core.Entities;

public class Supplier : Entity
{
    [MaxLength(100)]
    public required string Name { get; set; }
    
    [MaxLength(500)]
    public string? Description { get; set; }
        
    // Navigation property
    public virtual ICollection<Offer> Offers { get; set; } = new List<Offer>();

    // Dynamic properties stored as key/value pairs and persisted as JSON
    public Dictionary<string, object?> DynamicProperties { get; set; } = new Dictionary<string, object?>();

    public string? DynamicPropertiesJson
    {
        get => DynamicProperties.Count > 0 ? JsonSerializer.Serialize(DynamicProperties) : null;
        set
        {
            if (string.IsNullOrEmpty(value))
                DynamicProperties = new Dictionary<string, object?>();
            else
            {
                try
                {
                    DynamicProperties = JsonSerializer.Deserialize<Dictionary<string, object?>>(value) ?? new Dictionary<string, object?>();
                }
                catch (JsonException)
                {
                    DynamicProperties = new Dictionary<string, object?>();
                }
            }
        }
    }

    public void SetDynamicProperty(string key, object? value)
    {
        if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Property key cannot be null or empty", nameof(key));
        DynamicProperties[key] = value;
        ModifiedAt = DateTime.UtcNow;
    }

    public T? GetDynamicProperty<T>(string key)
    {
        if (string.IsNullOrWhiteSpace(key) || !DynamicProperties.ContainsKey(key)) return default;
        var value = DynamicProperties[key];
        if (value is JsonElement je)
        {
            try { return JsonSerializer.Deserialize<T>(je.GetRawText()); } catch { return default; }
        }
        try { return (T?)Convert.ChangeType(value, typeof(T)); } catch { return default; }
    }

    public void MergeDynamicProperties(Dictionary<string, object?> newProperties)
    {
        if (newProperties == null) return;
        foreach (var kvp in newProperties)
        {
            if (string.IsNullOrWhiteSpace(kvp.Key)) continue;
            DynamicProperties[kvp.Key] = kvp.Value;
        }
        ModifiedAt = DateTime.UtcNow;
    }
}
