using System.ComponentModel;
using System.Drawing.Drawing2D;
using SacksApp.Theming;

namespace SacksApp;

/// <summary>
/// A modern button control with skin support, custom rendering, and animations.
/// </summary>
public class ModernButton : Button
{
    // Skin configuration
    private SkinDefinition _currentSkin = new();
    private ControlStyle _controlStyle = new();
    
    // State management
    private bool _isHovered;
    private bool _isPressed;
    
    // Animation
    private readonly System.Windows.Forms.Timer _animationTimer;
    private float _currentHoverOpacity;
    private const int AnimationInterval = 16; // ~60fps
    private const float AnimationSpeed = 0.15f;

    public ModernButton()
    {
        SetStyle(ControlStyles.UserPaint | 
                ControlStyles.AllPaintingInWmPaint | 
                ControlStyles.OptimizedDoubleBuffer | 
                ControlStyles.ResizeRedraw | 
                ControlStyles.SupportsTransparentBackColor, true);

        // Set flat style to prevent default button rendering
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        BackColor = Color.Transparent;

        _animationTimer = new System.Windows.Forms.Timer { Interval = AnimationInterval };
        _animationTimer.Tick += AnimationTimer_Tick;

        // Initial default style
        UpdateStyleFromSkin();
    }

    /// <summary>
    /// Applies the specified skin to the button
    /// </summary>
    public void ApplySkin(SkinDefinition skin)
    {
        _currentSkin = skin;
        UpdateStyleFromSkin();
        Invalidate();
    }

    private void UpdateStyleFromSkin()
    {
        // Always start with fallback defaults to ensure States dictionary is never empty
        _controlStyle = new ControlStyle
        {
            CornerRadius = 8,
            BorderWidth = 1,
            Padding = new PaddingSpec { Left = 16, Top = 10, Right = 16, Bottom = 10 },
            States = new Dictionary<string, StateStyle>
            {
                ["normal"] = new() { BackColor = "#FAFBFC", ForeColor = "#0D1117", BorderColor = "#E0E6ED" },
                ["hover"] = new() { BackColor = "#F6F8FB", ForeColor = "#0D1117", BorderColor = "#D0D7E0" },
                ["pressed"] = new() { BackColor = "#EBEEF2", ForeColor = "#0D1117", BorderColor = "#B8C1CC" },
                ["disabled"] = new() { BackColor = "#F5F7FA", ForeColor = "#B0B8C3", BorderColor = "#E5EBF0" }
            }
        };

        // Override with skin-specific style if available
        if (_currentSkin?.Controls != null && _currentSkin.Controls.TryGetValue("ModernButton", out var style))
        {
            _controlStyle = style;
        }

        // Apply padding
        if (_controlStyle.Padding != null)
        {
            Padding = _controlStyle.Padding.ToPadding();
        }
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string Theme
    {
        get => AppThemeManager.CurrentTheme;
        set { /* No-op, handled by AppThemeManager.ApplyTheme */ }
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        base.OnMouseEnter(e);
        _isHovered = true;
        _animationTimer.Start();
        Invalidate();
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        _isHovered = false;
        _animationTimer.Start();
        Invalidate();
    }

    protected override void OnMouseDown(MouseEventArgs mevent)
    {
        base.OnMouseDown(mevent);
        _isPressed = true;
        Invalidate();
    }

    protected override void OnMouseUp(MouseEventArgs mevent)
    {
        base.OnMouseUp(mevent);
        _isPressed = false;
        Invalidate();
    }

    private void AnimationTimer_Tick(object? sender, EventArgs e)
    {
        bool animating = false;

        // Hover animation
        float targetOpacity = _isHovered ? 1.0f : 0.0f;
        if (Math.Abs(_currentHoverOpacity - targetOpacity) > 0.01f)
        {
            _currentHoverOpacity += (targetOpacity - _currentHoverOpacity) * AnimationSpeed;
            animating = true;
        }
        else
        {
            _currentHoverOpacity = targetOpacity;
        }

        if (animating)
        {
            Invalidate();
        }
        else
        {
            _animationTimer.Stop();
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        // Fill entire area with parent background to eliminate black corners
        var parentColor = Parent?.BackColor ?? SystemColors.Control;
        using (var parentBrush = new SolidBrush(parentColor))
        {
            g.FillRectangle(parentBrush, ClientRectangle);
        }

        // Determine current state style
        var stateName = !Enabled ? "disabled" : _isPressed ? "pressed" : _isHovered ? "hover" : "normal";
        var stateStyle = _controlStyle.States.TryGetValue(stateName, out var s) ? s : _controlStyle.States.GetValueOrDefault("normal");

        if (stateStyle == null) return;

        // Resolve colors
        var backColor = ColorTranslator.FromHtml(stateStyle.BackColor ?? "#FFFFFF");
        var foreColor = ColorTranslator.FromHtml(stateStyle.ForeColor ?? "#000000");
        var borderColor = ColorTranslator.FromHtml(stateStyle.BorderColor ?? "#CCCCCC");

        // Draw background
        using (var path = GetRoundedPath(ClientRectangle, _controlStyle.CornerRadius))
        using (var brush = new SolidBrush(backColor))
        using (var pen = new Pen(borderColor, _controlStyle.BorderWidth))
        {
            g.FillPath(brush, path);
            g.DrawPath(pen, path);
        }

        // Draw Text
        var flags = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis;
        TextRenderer.DrawText(g, Text, Font, ClientRectangle, foreColor, flags);
    }

    private GraphicsPath GetRoundedPath(Rectangle rect, int radius)
    {
        var path = new GraphicsPath();
        if (radius <= 0)
        {
            path.AddRectangle(rect);
            return path;
        }

        int d = radius * 2;
        var arc = new Rectangle(rect.X, rect.Y, d, d);

        // Top Left
        path.AddArc(arc, 180, 90);
        
        // Top Right
        arc.X = rect.Right - d;
        path.AddArc(arc, 270, 90);

        // Bottom Right
        arc.Y = rect.Bottom - d;
        path.AddArc(arc, 0, 90);

        // Bottom Left
        arc.X = rect.Left;
        path.AddArc(arc, 90, 90);

        path.CloseFigure();
        return path;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _animationTimer.Dispose();
        }
        base.Dispose(disposing);
    }
}
