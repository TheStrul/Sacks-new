using System.Drawing.Drawing2D;

namespace ModernWinForms.Theming;

/// <summary>
/// Helper methods for creating rounded rectangle graphics paths.
/// </summary>
internal static class GraphicsHelper
{
    /// <summary>
    /// Creates a rounded rectangle path.
    /// </summary>
    /// <param name="rect">The rectangle bounds.</param>
    /// <param name="radius">Corner radius.</param>
    /// <returns>A GraphicsPath representing a rounded rectangle.</returns>
    public static GraphicsPath CreateRoundedRectangle(Rectangle rect, int radius)
    {
        var path = GraphicsPathPool.Rent();
        
        if (radius <= 0 || rect.Width < radius * 2 || rect.Height < radius * 2)
        {
            path.AddRectangle(rect);
            return path;
        }

        var diameter = radius * 2;
        var arc = new Rectangle(rect.Location, new Size(diameter, diameter));

        // Top left
        path.AddArc(arc, 180, 90);
        
        // Top right
        arc.X = rect.Right - diameter;
        path.AddArc(arc, 270, 90);
        
        // Bottom right
        arc.Y = rect.Bottom - diameter;
        path.AddArc(arc, 0, 90);
        
        // Bottom left
        arc.X = rect.Left;
        path.AddArc(arc, 90, 90);
        
        path.CloseFigure();
        return path;
    }

    /// <summary>
    /// Creates a rounded rectangle path with variable corner radii.
    /// </summary>
    /// <param name="rect">The rectangle bounds.</param>
    /// <param name="topLeftRadius">Top left corner radius.</param>
    /// <param name="topRightRadius">Top right corner radius.</param>
    /// <param name="bottomRightRadius">Bottom right corner radius.</param>
    /// <param name="bottomLeftRadius">Bottom left corner radius.</param>
    /// <returns>A GraphicsPath representing a rounded rectangle.</returns>
    public static GraphicsPath CreateRoundedRectangle(
        Rectangle rect,
        int topLeftRadius,
        int topRightRadius,
        int bottomRightRadius,
        int bottomLeftRadius)
    {
        var path = GraphicsPathPool.Rent();

        // Top left
        if (topLeftRadius > 0)
        {
            path.AddArc(rect.X, rect.Y, topLeftRadius * 2, topLeftRadius * 2, 180, 90);
        }
        else
        {
            path.AddLine(rect.X, rect.Y, rect.X, rect.Y);
        }

        // Top edge
        path.AddLine(rect.X + topLeftRadius, rect.Y, rect.Right - topRightRadius, rect.Y);

        // Top right
        if (topRightRadius > 0)
        {
            path.AddArc(rect.Right - topRightRadius * 2, rect.Y, topRightRadius * 2, topRightRadius * 2, 270, 90);
        }

        // Right edge
        path.AddLine(rect.Right, rect.Y + topRightRadius, rect.Right, rect.Bottom - bottomRightRadius);

        // Bottom right
        if (bottomRightRadius > 0)
        {
            path.AddArc(rect.Right - bottomRightRadius * 2, rect.Bottom - bottomRightRadius * 2,
                bottomRightRadius * 2, bottomRightRadius * 2, 0, 90);
        }

        // Bottom edge
        path.AddLine(rect.Right - bottomRightRadius, rect.Bottom, rect.X + bottomLeftRadius, rect.Bottom);

        // Bottom left
        if (bottomLeftRadius > 0)
        {
            path.AddArc(rect.X, rect.Bottom - bottomLeftRadius * 2, bottomLeftRadius * 2, bottomLeftRadius * 2, 90, 90);
        }

        // Left edge
        path.AddLine(rect.X, rect.Bottom - bottomLeftRadius, rect.X, rect.Y + topLeftRadius);

        path.CloseFigure();
        return path;
    }
}
