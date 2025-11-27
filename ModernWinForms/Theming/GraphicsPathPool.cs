using System.Collections.Concurrent;
using System.Drawing.Drawing2D;

namespace ModernWinForms.Theming;

/// <summary>
/// Object pool for GraphicsPath instances to reduce allocations.
/// </summary>
internal static class GraphicsPathPool
{
    private static readonly ConcurrentBag<GraphicsPath> _pool = new();
    private const int MaxPoolSize = 50;

    /// <summary>
    /// Rents a GraphicsPath from the pool or creates a new one.
    /// </summary>
    /// <returns>A GraphicsPath instance.</returns>
    public static GraphicsPath Rent()
    {
        if (_pool.TryTake(out var path))
        {
            path.Reset();
            return path;
        }
        return new GraphicsPath();
    }

    /// <summary>
    /// Returns a GraphicsPath to the pool for reuse.
    /// </summary>
    /// <param name="path">The path to return.</param>
    public static void Return(GraphicsPath? path)
    {
        if (path == null || _pool.Count >= MaxPoolSize)
        {
            path?.Dispose();
            return;
        }

        path.Reset();
        _pool.Add(path);
    }

    /// <summary>
    /// Clears the pool and disposes all cached GraphicsPath instances.
    /// Should be called during application shutdown to prevent resource leaks.
    /// </summary>
    public static void Clear()
    {
        while (_pool.TryTake(out var path))
        {
            path?.Dispose();
        }
    }
}
