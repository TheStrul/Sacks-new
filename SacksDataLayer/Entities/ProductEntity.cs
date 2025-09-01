using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace SacksDataLayer.Entities
{
    /// <summary>
    /// Product entity with support for unlimited dynamic properties
    /// </summary>
    public class ProductEntity : Entity
    {
        /// <summary>
        /// Product name
        /// </summary>
        [Required]
        [StringLength(255)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Product description
        /// </summary>
        [StringLength(2000)]
        public string? Description { get; set; }

        /// <summary>
        /// Product EAN (European Article Number)
        /// </summary>
        [StringLength(100)]
        public string EAN { get; set; } = string.Empty;

        /// <summary>
        /// Dynamic properties stored as key-value pairs
        /// This allows unlimited custom product properties to be added to any product
        /// These are core product attributes that don't vary by supplier/offer
        /// </summary>
        public Dictionary<string, object?> DynamicProperties { get; set; } = new Dictionary<string, object?>();

        /// <summary>
        /// Navigation property to supplier offers for this product
        /// </summary>
        public virtual ICollection<OfferProductEntity> OfferProducts { get; set; } = new List<OfferProductEntity>();

        /// <summary>
        /// JSON representation of dynamic properties for database storage
        /// This property can be mapped to a JSON column in the database
        /// </summary>
        public string? DynamicPropertiesJson
        {
            get => DynamicProperties.Count > 0 ? JsonSerializer.Serialize(DynamicProperties) : null;
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    DynamicProperties = new Dictionary<string, object?>();
                }
                else
                {
                    try
                    {
                        DynamicProperties = JsonSerializer.Deserialize<Dictionary<string, object?>>(value) 
                                          ?? new Dictionary<string, object?>();
                    }
                    catch (JsonException)
                    {
                        DynamicProperties = new Dictionary<string, object?>();
                    }
                }
            }
        }

        /// <summary>
        /// Sets a dynamic property value (core product attribute)
        /// </summary>
        /// <param name="key">Property name</param>
        /// <param name="value">Property value</param>
        public void SetDynamicProperty(string key, object? value)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Property key cannot be null or empty", nameof(key));

            DynamicProperties[key] = value;
            ModifiedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Gets a dynamic property value
        /// </summary>
        /// <typeparam name="T">Expected type of the property value</typeparam>
        /// <param name="key">Property name</param>
        /// <returns>Property value or default(T) if not found</returns>
        public T? GetDynamicProperty<T>(string key)
        {
            if (string.IsNullOrWhiteSpace(key) || !DynamicProperties.ContainsKey(key))
                return default(T);

            var value = DynamicProperties[key];
            if (value is T directValue)
                return directValue;

            // Try to convert JSON element to target type
            if (value is JsonElement jsonElement)
            {
                try
                {
                    return JsonSerializer.Deserialize<T>(jsonElement.GetRawText());
                }
                catch (JsonException)
                {
                    return default(T);
                }
            }

            // Try direct conversion
            try
            {
                return (T?)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                return default(T);
            }
        }

        /// <summary>
        /// Gets a dynamic property value as object (core product attribute)
        /// </summary>
        /// <param name="key">Property name</param>
        /// <returns>Property value or null if not found</returns>
        public object? GetDynamicProperty(string key)
        {
            return string.IsNullOrWhiteSpace(key) || !DynamicProperties.ContainsKey(key) 
                ? null 
                : DynamicProperties[key];
        }

        /// <summary>
        /// Removes a dynamic property
        /// </summary>
        /// <param name="key">Property name</param>
        /// <returns>True if the property was removed, false if it didn't exist</returns>
        public bool RemoveDynamicProperty(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return false;

            var removed = DynamicProperties.Remove(key);
            if (removed)
                ModifiedAt = DateTime.UtcNow;
            
            return removed;
        }

        /// <summary>
        /// Checks if a dynamic property exists
        /// </summary>
        /// <param name="key">Property name</param>
        /// <returns>True if the property exists</returns>
        public bool HasDynamicProperty(string key)
        {
            return !string.IsNullOrWhiteSpace(key) && DynamicProperties.ContainsKey(key);
        }

        /// <summary>
        /// Gets all dynamic property keys
        /// </summary>
        /// <returns>Collection of all dynamic property keys</returns>
        public IEnumerable<string> GetDynamicPropertyKeys()
        {
            return DynamicProperties.Keys;
        }

        /// <summary>
        /// Clears all dynamic properties
        /// </summary>
        public void ClearDynamicProperties()
        {
            if (DynamicProperties.Count > 0)
            {
                DynamicProperties.Clear();
                ModifiedAt = DateTime.UtcNow;
            }
        }

        /// <summary>
        /// Sets multiple dynamic properties at once
        /// </summary>
        /// <param name="properties">Dictionary of properties to set</param>
        public void SetDynamicProperties(Dictionary<string, object?> properties)
        {
            if (properties == null)
                return;

            foreach (var kvp in properties)
            {
                if (!string.IsNullOrWhiteSpace(kvp.Key))
                {
                    DynamicProperties[kvp.Key] = kvp.Value;
                }
            }
            ModifiedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Gets a copy of all dynamic properties
        /// </summary>
        /// <returns>Dictionary containing all dynamic properties</returns>
        public Dictionary<string, object?> GetAllDynamicProperties()
        {
            return new Dictionary<string, object?>(DynamicProperties);
        }

        /// <summary>
        /// Enhanced string representation including dynamic properties count
        /// </summary>
        public override string ToString()
        {
            return $"{base.ToString()} - {Name} (Dynamic Props: {DynamicProperties.Count})";
        }
    }
}