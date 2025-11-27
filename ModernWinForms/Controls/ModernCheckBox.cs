using System.ComponentModel;
using System.Drawing.Drawing2D;
using ModernWinForms.Animation;
using ModernWinForms.Theming;

namespace ModernWinForms.Controls;

/// <summary>
/// Modern checkbox control with smooth animations and theme support.
/// </summary>
[ToolboxItem(true)]
[DesignerCategory("Code")]
[Description("Modern checkbox control with smooth animations and theme support.")]
public class ModernCheckBox : CheckBox
{
    private ControlStyle _controlStyle = new();
    private AnimationEngine? _checkAnimation;
    private AnimationEngine? _hoverAnimation;
    private double _checkProgress;
    private double _hoverProgress;
    private bool _isHovered;
    private Font? _themeFont;

    /// <summary>
    /// Initializes a new instance of the ModernCheckBox class.
    /// </summary>
    public ModernCheckBox()
    {
        SetStyle(ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.SupportsTransparentBackColor, true);

        BackColor = Color.Transparent;
        FlatStyle = FlatStyle.Flat;
        
        _checkAnimation = new AnimationEngine(this);
        _hoverAnimation = new AnimationEngine(this);

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
        _themeFont = ThemeManager.CreateFont() 
            ?? throw new InvalidOperationException(
                "Theme font creation failed. Theme system not initialized.");
    }

    private void UpdateStyleFromSkin()
    {
        // ZERO TOLERANCE: Get style or create with REQUIRED states
        _controlStyle = ThemeManager.GetControlStyle("ModernCheckBox") ?? new ControlStyle
        {
            CornerRadius = 4,
            BorderWidth = 2,
            States = new Dictionary<string, StateStyle>
            {
                ["normal"] = new() { BackColor = "#FFFFFF", ForeColor = "#0D1117", BorderColor = "#D0D7DE" },
                ["hover"] = new() { BorderColor = "#0969DA" },
                ["checked"] = new() { BackColor = "#0969DA", BorderColor = "#0969DA" },
                ["disabled"] = new() { BackColor = "#F6F8FA", ForeColor = "#8C959F", BorderColor = "#D0D7DE" }
            }
        };

        // ZERO TOLERANCE: Validate 'normal' state exists
        if (!_controlStyle.States.TryGetValue("normal", out var normalState))
            throw new InvalidOperationException(
                "ModernCheckBox 'normal' state not defined in theme. Theme configuration is incomplete.");

        // ZERO TOLERANCE: All required colors MUST be defined
        if (string.IsNullOrWhiteSpace(normalState.BackColor))
            throw new InvalidOperationException(
                "ModernCheckBox BackColor not defined in 'normal' state. Theme configuration is incomplete.");

        if (string.IsNullOrWhiteSpace(normalState.ForeColor))
            throw new InvalidOperationException(
                "ModernCheckBox ForeColor not defined in 'normal' state. Theme configuration is incomplete.");

        if (string.IsNullOrWhiteSpace(normalState.BorderColor))
            throw new InvalidOperationException(
                "ModernCheckBox BorderColor not defined in 'normal' state. Theme configuration is incomplete.");
    }

    /// <inheritdoc/>
    protected override void OnCheckedChanged(EventArgs e)
    {
        base.OnCheckedChanged(e);
        _checkAnimation?.Animate(Checked ? 1.0 : 0.0, 200, value => _checkProgress = value);
    }

    /// <inheritdoc/>
    protected override void OnMouseEnter(EventArgs e)
    {
        base.OnMouseEnter(e);
        _isHovered = true;
        _hoverAnimation?.Animate(1.0, 150, value => _hoverProgress = value);
    }

    /// <inheritdoc/>
    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        _isHovered = false;
        _hoverAnimation?.Animate(0.0, 150, value => _hoverProgress = value);
    }

    /// <inheritdoc/>
    protected override void OnPaint(PaintEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        // Fill parent background
        using (var parentBrush = new SolidBrush(GetEffectiveBackColor()))
        {
            g.FillRectangle(parentBrush, ClientRectangle);
        }

        const int checkBoxSize = 18;
        const int spacing = 6;
        var checkBoxRect = new Rectangle(0, (Height - checkBoxSize) / 2, checkBoxSize, checkBoxSize);
        var textRect = new Rectangle(checkBoxSize + spacing, 0, Width - checkBoxSize - spacing, Height);

        // Determine colors
        var normalStyle = _controlStyle.States.GetValueOrDefault("normal") 
            ?? new StateStyle { BackColor = "#FFFFFF", BorderColor = "#CCCCCC" };
        var hoverStyle = _controlStyle.States.GetValueOrDefault("hover") ?? normalStyle;
        var checkedStyle = _controlStyle.States.GetValueOrDefault("checked") ?? normalStyle;

        var borderColor = ColorCache.GetColor(normalStyle.BorderColor, Color.Gray);
        if (_isHovered && Enabled)
        {
            var hoverBorderColor = ColorCache.GetColor(hoverStyle.BorderColor, borderColor);
            borderColor = BlendColors(borderColor, hoverBorderColor, _hoverProgress);
        }

        var backColor = ColorCache.GetColor(normalStyle.BackColor, Color.White);
        if (Checked)
        {
            var checkedBackColor = ColorCache.GetColor(checkedStyle.BackColor, Color.Blue);
            backColor = BlendColors(backColor, checkedBackColor, _checkProgress);
            var checkedBorderColor = ColorCache.GetColor(checkedStyle.BorderColor, Color.Blue);
            borderColor = BlendColors(borderColor, checkedBorderColor, _checkProgress);
        }

        // Draw checkbox
        using (var path = GraphicsHelper.CreateRoundedRectangle(checkBoxRect, _controlStyle.CornerRadius))
        using (var brush = new SolidBrush(backColor))
        using (var pen = new Pen(borderColor, _controlStyle.BorderWidth))
        {
            g.FillPath(brush, path);
            g.DrawPath(pen, path);
        }

        // Draw checkmark
        if (_checkProgress > 0)
        {
            DrawCheckmark(g, checkBoxRect, _checkProgress);
        }

        // Draw text
        if (!string.IsNullOrEmpty(Text))
        {
            var foreColor = Enabled 
                ? ColorCache.GetColor(normalStyle.ForeColor, Color.Black)
                : ColorCache.GetColor(_controlStyle.States.GetValueOrDefault("disabled")?.ForeColor, Color.Gray);
            
            var font = _themeFont ?? Font;
            TextRenderer.DrawText(g, Text, font, textRect, foreColor,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
        }
    }

    private void DrawCheckmark(Graphics g, Rectangle checkBoxRect, double progress)
    {
        using var pen = new Pen(Color.White, 2) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        
        // Scale and position checkmark
        var scale = (float)progress;
        var centerX = checkBoxRect.X + checkBoxRect.Width / 2f;
        var centerY = checkBoxRect.Y + checkBoxRect.Height / 2f;
        
        var points = new[]
        {
            new PointF(centerX - 4 * scale, centerY),
            new PointF(centerX - 1 * scale, centerY + 3 * scale),
            new PointF(centerX + 4 * scale, centerY - 3 * scale)
        };
        
        if (scale > 0.01f)
        {
            g.DrawLines(pen, points);
        }
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
            _checkAnimation?.Dispose();
            _checkAnimation = null;
            _hoverAnimation?.Dispose();
            _hoverAnimation = null;
            _themeFont?.Dispose();
            _themeFont = null;
        }
        base.Dispose(disposing);
    }
}
