using System.ComponentModel;
using System.Drawing.Drawing2D;
using ModernWinForms.Theming;

namespace ModernWinForms.Controls;

/// <summary>
/// Modern button control with fluent design, smooth state transitions, and theme support.
/// </summary>
[ToolboxItem(true)]
[DesignerCategory("Code")]
[Description("Modern button control with fluent design and smooth state transitions.")]
public class ModernButton : Button
{
    private ControlStyle _controlStyle = new();
    private bool _isHovered;
    private bool _isPressed;
    private GraphicsPath? _cachedPath;
    private Size _cachedSize;
    private int _cachedRadius;

    /// <summary>
    /// Initializes a new instance of the ModernButton class.
    /// </summary>
    public ModernButton()
    {
        // Enable double buffering and custom painting
        SetStyle(ControlStyles.UserPaint | 
                ControlStyles.AllPaintingInWmPaint | 
                ControlStyles.OptimizedDoubleBuffer | 
                ControlStyles.ResizeRedraw | 
                ControlStyles.SupportsTransparentBackColor, true);

        BackColor = Color.Transparent;
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        FlatAppearance.MouseDownBackColor = Color.Transparent;
        FlatAppearance.MouseOverBackColor = Color.Transparent;
        UseVisualStyleBackColor = false;

        ThemeManager.ThemeChanged += OnThemeChanged;
        UpdateStyleFromSkin();
    }

    private void OnThemeChanged(object? sender, EventArgs e)
    {
        UpdateStyleFromSkin();
        Invalidate();
    }

    /// <summary>
    /// Applies the specified skin definition to this button.
    /// </summary>
    /// <param name="skin">The skin definition to apply.</param>
    public void ApplySkin(SkinDefinition skin)
    {
        ArgumentNullException.ThrowIfNull(skin);
        UpdateStyleFromSkin(skin);
        Invalidate();
    }

    private void UpdateStyleFromSkin(SkinDefinition? skin = null)
    {
        // Get merged style from theme (structure) + skin (colors)
        _controlStyle = ThemeManager.GetControlStyle("ModernButton") ?? new ControlStyle
        {
            CornerRadius = 8,
            BorderWidth = 1,
            States = new Dictionary<string, StateStyle>
            {
                ["normal"] = new() { BackColor = "#FAFBFC", ForeColor = "#0D1117", BorderColor = "#E0E6ED" },
                ["hover"] = new() { BackColor = "#F6F8FB", ForeColor = "#0D1117", BorderColor = "#D0D7E0" },
                ["pressed"] = new() { BackColor = "#EBEEF2", ForeColor = "#0D1117", BorderColor = "#B8C1CC" },
                ["disabled"] = new() { BackColor = "#F5F7FA", ForeColor = "#B0B8C3", BorderColor = "#E5EBF0" }
            }
        };
    }

    /// <inheritdoc/>
    protected override void OnMouseEnter(EventArgs e)
    {
        base.OnMouseEnter(e);
        _isHovered = true;
        Invalidate();
    }

    /// <inheritdoc/>
    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        _isHovered = false;
        Invalidate();
    }

    /// <inheritdoc/>
    protected override void OnMouseDown(MouseEventArgs mevent)
    {
        base.OnMouseDown(mevent);
        _isPressed = true;
        Invalidate();
    }

    /// <inheritdoc/>
    protected override void OnMouseUp(MouseEventArgs mevent)
    {
        base.OnMouseUp(mevent);
        _isPressed = false;
        Invalidate();
    }

    /// <inheritdoc/>
    protected override void OnPaint(PaintEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        
        // Do NOT call base.OnPaint - we handle all painting
        
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        // Fill background with parent color to avoid transparency artifacts
        var parentColor = GetEffectiveBackColor();
        using (var parentBrush = new SolidBrush(parentColor))
        {
            g.FillRectangle(parentBrush, ClientRectangle);
        }

        // Determine state and colors
        var stateName = !Enabled ? "disabled" : _isPressed ? "pressed" : _isHovered ? "hover" : "normal";
        var stateStyle = _controlStyle.States.GetValueOrDefault(stateName) 
            ?? _controlStyle.States.GetValueOrDefault("normal") 
            ?? new StateStyle { BackColor = "#FFFFFF", ForeColor = "#000000", BorderColor = "#CCCCCC" };

        var backColor = ColorTranslator.FromHtml(stateStyle.BackColor ?? "#FFFFFF");
        var foreColor = ColorTranslator.FromHtml(stateStyle.ForeColor ?? "#000000");
        var borderColor = ColorTranslator.FromHtml(stateStyle.BorderColor ?? "#CCCCCC");

        // Draw rounded button
        var path = GetOrCreateCachedPath(ClientRectangle, _controlStyle.CornerRadius);
        using var brush = new SolidBrush(backColor);
        using var pen = new Pen(borderColor, _controlStyle.BorderWidth);

        g.FillPath(brush, path);
        
        if (_controlStyle.BorderWidth > 0)
        {
            g.DrawPath(pen, path);
        }

        // Draw text
        TextRenderer.DrawText(g, Text, Font, ClientRectangle, foreColor, 
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
    }

    private GraphicsPath GetOrCreateCachedPath(Rectangle rect, int radius)
    {
        // Check if we can reuse the cached path
        if (_cachedPath != null && _cachedSize == rect.Size && _cachedRadius == radius)
        {
            return _cachedPath;
        }

        // Dispose old cached path
        _cachedPath?.Dispose();

        // Create new path and cache it
        _cachedPath = CreateRoundedPath(rect, radius);
        _cachedSize = rect.Size;
        _cachedRadius = radius;

        return _cachedPath;
    }

    private GraphicsPath CreateRoundedPath(Rectangle rect, int radius)
    {
        var path = new GraphicsPath();
        
        float adjustment = _controlStyle.BorderWidth / 2f;
        var r = new RectangleF(rect.X + adjustment, rect.Y + adjustment, 
                             rect.Width - _controlStyle.BorderWidth, 
                             rect.Height - _controlStyle.BorderWidth);

        if (radius <= 0)
        {
            path.AddRectangle(r);
            return path;
        }

        float d = radius * 2;
        
        path.AddArc(r.X, r.Y, d, d, 180, 90);
        path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);

        path.CloseFigure();
        return path;
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            ThemeManager.ThemeChanged -= OnThemeChanged;
            _cachedPath?.Dispose();
            _cachedPath = null;
        }
        base.Dispose(disposing);
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
}
