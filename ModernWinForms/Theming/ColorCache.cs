using System.Collections.Concurrent;

namespace ModernWinForms.Theming;

/// <summary>
/// Thread-safe cache for parsed colors to avoid repeated ColorTranslator.FromHtml calls.
/// </summary>
internal static class ColorCache
{
    private static readonly ConcurrentDictionary<string, Color> _cache = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a color from cache or parses and caches it.
    /// </summary>
    /// <param name="colorString">The color string (hex or named color).</param>
    /// <param name="fallback">Fallback color if parsing fails.</param>
    /// <returns>The parsed color or fallback.</returns>
    public static Color GetColor(string? colorString, Color fallback = default)
    {
        if (string.IsNullOrWhiteSpace(colorString))
        {
            return fallback;
        }

        return _cache.GetOrAdd(colorString, key =>
        {
            try
            {
                return ColorTranslator.FromHtml(key);
            }
            catch
            {
                return fallback;
            }
        });
    }

    /// <summary>
    /// Clears the color cache. Useful when themes change significantly.
    /// </summary>
    public static void Clear() => _cache.Clear();
}
