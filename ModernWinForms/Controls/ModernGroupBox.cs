using System.ComponentModel;
using System.Drawing.Drawing2D;
using ModernWinForms.Theming;

namespace ModernWinForms.Controls;

/// <summary>
/// Modern group box control with rounded corners and theme support.
/// </summary>
[ToolboxItem(true)]
[DesignerCategory("Code")]
[Description("Modern group box control with rounded corners and theme support.")]
public class ModernGroupBox : GroupBox
{
    private ControlStyle _controlStyle = new();
    private GraphicsPath? _cachedPath;
    private Rectangle _cachedRect;
    private int _cachedRadius;
    private Font? _themeFont;

    /// <summary>
    /// Initializes a new instance of the ModernGroupBox class.
    /// </summary>
    public ModernGroupBox()
    {
        SetStyle(ControlStyles.UserPaint | 
                ControlStyles.ResizeRedraw | 
                ControlStyles.OptimizedDoubleBuffer | 
                ControlStyles.SupportsTransparentBackColor, true);
        
        BackColor = Color.Transparent;
        ThemeManager.ThemeChanged += OnThemeChanged;
        UpdateStyleFromSkin();
        UpdateFontFromTheme();
    }

    private void OnThemeChanged(object? sender, EventArgs e)
    {
        UpdateStyleFromSkin();
        UpdateFontFromTheme();
        Invalidate();
    }

    private void UpdateFontFromTheme()
    {
        _themeFont?.Dispose();
        _themeFont = ThemeManager.CreateFont();
    }

    /// <summary>
    /// Applies the specified skin definition to this group box.
    /// </summary>
    /// <param name="skin">The skin definition to apply.</param>
    public void ApplySkin(SkinDefinition skin)
    {
        UpdateStyleFromSkin(skin);
        Invalidate();
    }

    private void UpdateStyleFromSkin(SkinDefinition? skin = null)
    {
        // Get merged style from theme (structure) + skin (colors)
        _controlStyle = ThemeManager.GetControlStyle("ModernGroupBox") ?? new ControlStyle
        {
            BorderWidth = 1,
            CornerRadius = 4,
            States = new Dictionary<string, StateStyle>
            {
                ["normal"] = new() { ForeColor = "#0D1117", BorderColor = "#E0E6ED" }
            }
        };

        // Apply ForeColor from theme to control
        var stateStyle = _controlStyle.States.GetValueOrDefault("normal");
        if (stateStyle?.ForeColor != null)
        {
            try
            {
                ForeColor = ColorTranslator.FromHtml(stateStyle.ForeColor);
            }
            catch { /* Ignore invalid colors */ }
        }
    }

    /// <inheritdoc/>
    protected override void OnPaint(PaintEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        var stateStyle = _controlStyle.States.GetValueOrDefault("normal") 
            ?? new StateStyle { ForeColor = "#000000", BorderColor = "#CCCCCC" };
        var foreColor = ColorTranslator.FromHtml(stateStyle.ForeColor ?? "#000000");
        var borderColor = ColorTranslator.FromHtml(stateStyle.BorderColor ?? "#CCCCCC");
        
        var textSize = g.MeasureString(Text, Font);
        var textRect = new Rectangle(10, 0, (int)textSize.Width + 4, (int)textSize.Height);
        var borderRect = new Rectangle(0, (int)(textSize.Height / 2), ClientRectangle.Width - 1, ClientRectangle.Height - 1 - (int)(textSize.Height / 2));

        var path = GetOrCreateCachedPath(borderRect, _controlStyle.CornerRadius);
        using (var pen = new Pen(borderColor, _controlStyle.BorderWidth))
        {
            g.DrawPath(pen, path);
        }

        var parentColor = GetEffectiveBackColor();
        using (var brush = new SolidBrush(parentColor))
        {
            g.FillRectangle(brush, textRect);
        }

        var effectiveFont = _themeFont ?? Font ?? DefaultFont;
        using (var brush = new SolidBrush(foreColor))
        {
            g.DrawString(Text, effectiveFont, brush, textRect.X + 2, 0);
        }
    }

    private GraphicsPath GetOrCreateCachedPath(Rectangle rect, int radius)
    {
        // Check if we can reuse the cached path
        if (_cachedPath != null && _cachedRect == rect && _cachedRadius == radius)
        {
            return _cachedPath;
        }

        // Dispose old cached path
        _cachedPath?.Dispose();

        // Create new path and cache it
        _cachedPath = CreateRoundedPath(rect, radius);
        _cachedRect = rect;
        _cachedRadius = radius;

        return _cachedPath;
    }

    private static GraphicsPath CreateRoundedPath(Rectangle rect, int radius)
    {
        var path = new GraphicsPath();
        if (radius <= 0)
        {
            path.AddRectangle(rect);
            return path;
        }

        int d = radius * 2;
        path.AddArc(rect.X, rect.Y, d, d, 180, 90);
        path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
        path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
        path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }

    private Color GetEffectiveBackColor()
    {
        var current = Parent;
        while (current != null)
        {
            var color = current.BackColor;
            if (color.A > 0 && color != Color.Transparent)
            {
                return color;
            }
            current = current.Parent;
        }
        return SystemColors.Control;
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            ThemeManager.ThemeChanged -= OnThemeChanged;
            _cachedPath?.Dispose();
            _cachedPath = null;
            _themeFont?.Dispose();
            _themeFont = null;
        }
        base.Dispose(disposing);
    }
}
