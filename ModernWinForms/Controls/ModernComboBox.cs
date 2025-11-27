using System.ComponentModel;
using System.Drawing.Drawing2D;
using ModernWinForms.Animation;
using ModernWinForms.Theming;

namespace ModernWinForms.Controls;

/// <summary>
/// Modern combo box control with rounded corners and theme support.
/// </summary>
[ToolboxItem(true)]
[DesignerCategory("Code")]
[Description("Modern combo box control with rounded corners and theme support.")]
public class ModernComboBox : ComboBox
{
    private ControlStyle _controlStyle = new();
    private AnimationEngine? _focusAnimation;
    private double _focusProgress;
    private bool _isHovered;
    private Font? _themeFont;

    /// <summary>
    /// Initializes a new instance of the ModernComboBox class.
    /// </summary>
    public ModernComboBox()
    {
        SetStyle(ControlStyles.UserPaint |
                ControlStyles.ResizeRedraw |
                ControlStyles.OptimizedDoubleBuffer, true);

        DrawMode = DrawMode.OwnerDrawFixed;
        DropDownStyle = ComboBoxStyle.DropDownList;
        FlatStyle = FlatStyle.Flat;

        _focusAnimation = new AnimationEngine(this);
        
        if (!DesignMode)
        {
            ThemeManager.ThemeChanged += OnThemeChanged;
            UpdateStyleFromSkin();
            UpdateFontFromTheme();
        }
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
        if (_themeFont != null)
        {
            Font = _themeFont;
        }
    }

    private void UpdateStyleFromSkin()
    {
        _controlStyle = ThemeManager.GetControlStyle("ModernComboBox") ?? new ControlStyle
        {
            CornerRadius = 4,
            BorderWidth = 1,
            States = new Dictionary<string, StateStyle>
            {
                ["normal"] = new() { BackColor = "#FFFFFF", ForeColor = "#0D1117", BorderColor = "#D0D7DE" },
                ["hover"] = new() { BorderColor = "#0969DA" },
                ["focused"] = new() { BorderColor = "#0969DA" },
                ["disabled"] = new() { BackColor = "#F6F8FA", ForeColor = "#8C959F", BorderColor = "#D0D7DE" }
            }
        };

        var normalStyle = _controlStyle.States.GetValueOrDefault("normal");
        BackColor = ColorCache.GetColor(normalStyle?.BackColor, Color.White);
        ForeColor = ColorCache.GetColor(normalStyle?.ForeColor, Color.Black);
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
    protected override void OnEnter(EventArgs e)
    {
        base.OnEnter(e);
        _focusAnimation?.Animate(1.0, 200, value => _focusProgress = value);
    }

    /// <inheritdoc/>
    protected override void OnLeave(EventArgs e)
    {
        base.OnLeave(e);
        _focusAnimation?.Animate(0.0, 200, value => _focusProgress = value);
    }

    /// <inheritdoc/>
    protected override void OnPaint(PaintEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        // Determine colors
        var normalStyle = _controlStyle.States.GetValueOrDefault("normal") 
            ?? new StateStyle { BackColor = "#FFFFFF", BorderColor = "#CCCCCC" };
        
        var backColor = ColorCache.GetColor(normalStyle.BackColor, Color.White);
        var borderColor = ColorCache.GetColor(normalStyle.BorderColor, Color.Gray);

        if (!Enabled)
        {
            var disabledStyle = _controlStyle.States.GetValueOrDefault("disabled") ?? normalStyle;
            backColor = ColorCache.GetColor(disabledStyle.BackColor, Color.LightGray);
            borderColor = ColorCache.GetColor(disabledStyle.BorderColor, Color.Gray);
        }
        else if (Focused)
        {
            var focusedStyle = _controlStyle.States.GetValueOrDefault("focused") ?? normalStyle;
            var focusedBorderColor = ColorCache.GetColor(focusedStyle.BorderColor, borderColor);
            borderColor = BlendColors(borderColor, focusedBorderColor, _focusProgress);
        }
        else if (_isHovered)
        {
            var hoverStyle = _controlStyle.States.GetValueOrDefault("hover") ?? normalStyle;
            borderColor = ColorCache.GetColor(hoverStyle.BorderColor, borderColor);
        }

        // Draw background
        using (var path = GraphicsHelper.CreateRoundedRectangle(ClientRectangle, _controlStyle.CornerRadius))
        using (var brush = new SolidBrush(backColor))
        using (var pen = new Pen(borderColor, _controlStyle.BorderWidth))
        {
            g.FillPath(brush, path);
            g.DrawPath(pen, path);
        }

        // Draw selected text
        const int padding = 8;
        const int arrowWidth = 20;
        var textRect = new Rectangle(padding, 0, Width - padding - arrowWidth, Height);
        
        if (SelectedIndex >= 0)
        {
            var text = GetItemText(SelectedItem);
            TextRenderer.DrawText(g, text, Font, textRect, ForeColor,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
        }

        // Draw dropdown arrow
        DrawDropDownArrow(g, new Rectangle(Width - arrowWidth, 0, arrowWidth, Height), borderColor);
    }

    private void DrawDropDownArrow(Graphics g, Rectangle rect, Color color)
    {
        const int arrowSize = 6;
        var centerX = rect.X + rect.Width / 2;
        var centerY = rect.Y + rect.Height / 2;

        var points = new[]
        {
            new Point(centerX - arrowSize / 2, centerY - 2),
            new Point(centerX + arrowSize / 2, centerY - 2),
            new Point(centerX, centerY + arrowSize / 2 - 2)
        };

        using var brush = new SolidBrush(color);
        g.FillPolygon(brush, points);
    }

    /// <inheritdoc/>
    protected override void OnDrawItem(DrawItemEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        
        e.DrawBackground();
        
        if (e.Index >= 0)
        {
            var normalStyle = _controlStyle.States.GetValueOrDefault("normal");
            var foreColor = (e.State & DrawItemState.Selected) != 0
                ? e.ForeColor
                : ColorCache.GetColor(normalStyle?.ForeColor, Color.Black);

            var text = GetItemText(Items[e.Index]);
            TextRenderer.DrawText(e.Graphics, text, e.Font ?? Font, e.Bounds, foreColor,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
        }
        
        e.DrawFocusRectangle();
    }

    private static Color BlendColors(Color from, Color to, double progress)
    {
        progress = Math.Clamp(progress, 0, 1);
        var r = (int)(from.R + ((to.R - from.R) * progress));
        var g = (int)(from.G + ((to.G - from.G) * progress));
        var b = (int)(from.B + ((to.B - from.B) * progress));
        return Color.FromArgb(r, g, b);
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            ThemeManager.ThemeChanged -= OnThemeChanged;
            _focusAnimation?.Dispose();
            _focusAnimation = null;
            _themeFont?.Dispose();
            _themeFont = null;
        }
        base.Dispose(disposing);
    }
}
