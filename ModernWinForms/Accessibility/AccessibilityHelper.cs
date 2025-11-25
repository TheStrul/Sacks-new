namespace ModernWinForms.Accessibility;

/// <summary>
/// Provides accessibility support utilities for modern controls.
/// </summary>
public static class AccessibilityHelper
{
    /// <summary>
    /// Checks if high contrast mode is enabled in Windows.
    /// </summary>
    /// <returns>True if high contrast mode is enabled.</returns>
    public static bool IsHighContrastMode()
    {
        return SystemInformation.HighContrast;
    }

    /// <summary>
    /// Gets the appropriate color for the current contrast mode.
    /// </summary>
    /// <param name="normalColor">Color to use in normal mode.</param>
    /// <param name="highContrastColor">Color to use in high contrast mode.</param>
    /// <returns>The appropriate color based on current system settings.</returns>
    public static Color GetContrastSafeColor(Color normalColor, Color? highContrastColor = null)
    {
        if (IsHighContrastMode())
        {
            return highContrastColor ?? SystemColors.WindowText;
        }
        return normalColor;
    }

    /// <summary>
    /// Gets the focus indicator color based on system settings.
    /// </summary>
    /// <returns>The focus indicator color.</returns>
    public static Color GetFocusIndicatorColor()
    {
        return IsHighContrastMode() 
            ? SystemColors.Highlight 
            : Color.FromArgb(0, 120, 215); // Blue
    }

    /// <summary>
    /// Draws a focus rectangle with rounded corners.
    /// </summary>
    /// <param name="g">The graphics object.</param>
    /// <param name="rect">The rectangle to draw.</param>
    /// <param name="radius">Corner radius.</param>
    /// <param name="color">Focus color.</param>
    public static void DrawFocusRectangle(Graphics g, Rectangle rect, int radius, Color? color = null)
    {
        ArgumentNullException.ThrowIfNull(g);
        
        var focusColor = color ?? GetFocusIndicatorColor();
        using var pen = new Pen(focusColor, 2) 
        { 
            DashStyle = System.Drawing.Drawing2D.DashStyle.Dash 
        };
        
        var focusRect = rect;
        focusRect.Inflate(-2, -2);
        
        if (radius > 0)
        {
            using var path = CreateRoundedPath(focusRect, radius);
            g.DrawPath(pen, path);
        }
        else
        {
            g.DrawRectangle(pen, focusRect);
        }
    }

    private static System.Drawing.Drawing2D.GraphicsPath CreateRoundedPath(Rectangle rect, int radius)
    {
        var path = new System.Drawing.Drawing2D.GraphicsPath();
        if (radius <= 0 || rect.Width < radius * 2 || rect.Height < radius * 2)
        {
            path.AddRectangle(rect);
            return path;
        }

        var d = radius * 2;
        path.AddArc(rect.X, rect.Y, d, d, 180, 90);
        path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
        path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
        path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }
}
