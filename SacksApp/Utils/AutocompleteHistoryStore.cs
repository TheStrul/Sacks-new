using System.Text.Json;

namespace SacksApp.Utils
{
    public sealed class AutocompleteHistoryStore
    {
        private const int MaxItemsPerKey = 100;
        public Dictionary<string, List<string>> Values { get; } = new(StringComparer.OrdinalIgnoreCase);

        private static string GetStorePath()
        {
            var root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var dir = Path.Combine(root, "SacksApp");
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, "TestPatternHistory.json");
        }

        public static AutocompleteHistoryStore Load()
        {
            try
            {
                var path = GetStorePath();
                if (!File.Exists(path)) return new AutocompleteHistoryStore();
                var json = File.ReadAllText(path);
                var data = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(json) ?? new();
                var store = new AutocompleteHistoryStore();
                foreach (var kv in data)
                {
                    if (kv.Key is null) continue;
                    store.Values[kv.Key] = kv.Value?.Where(v => !string.IsNullOrWhiteSpace(v)).Distinct(StringComparer.OrdinalIgnoreCase).Take(MaxItemsPerKey).ToList() ?? new List<string>();
                }
                return store;
            }
            catch
            {
                return new AutocompleteHistoryStore();
            }
        }

        JsonSerializerOptions jsonOptions = new JsonSerializerOptions { WriteIndented = true };

        public void Save()
        {
            try
            {
                var path = GetStorePath();
                var json = JsonSerializer.Serialize(Values, jsonOptions);
                File.WriteAllText(path, json);
            }
            catch
            {
                // ignore persistence errors
            }
        }

        public IReadOnlyList<string> Get(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return Array.Empty<string>();
            return Values.TryGetValue(key, out var list) ? list : Array.Empty<string>();
        }

        public void Add(string key, string value)
        {
            if (string.IsNullOrWhiteSpace(key)) return;
            var v = (value ?? string.Empty).Trim();
            if (v.Length == 0) return;
            if (!Values.TryGetValue(key, out var list))
            {
                list = new List<string>();
                Values[key] = list;
            }
            if (!list.Any(x => string.Equals(x, v, StringComparison.OrdinalIgnoreCase)))
            {
                list.Insert(0, v);
                if (list.Count > MaxItemsPerKey)
                    list.RemoveRange(MaxItemsPerKey, list.Count - MaxItemsPerKey);
            }
        }
    }
}
