using System.ComponentModel;
using System.Drawing.Drawing2D;
using ModernWinForms.Animation;
using ModernWinForms.Theming;

namespace ModernWinForms.Controls;

/// <summary>
/// Modern radio button control with custom rendering and theme support.
/// </summary>
[ToolboxItem(true)]
[DesignerCategory("Code")]
[Description("Modern radio button control with custom rendering and theme support.")]
public class ModernRadioButton : RadioButton
{
    private ControlStyle _controlStyle = new();
    private Font? _themeFont;
    private AnimationEngine? _checkAnimation;
    private double _checkProgress;

    /// <summary>
    /// Initializes a new instance of the ModernRadioButton class.
    /// </summary>
    public ModernRadioButton()
    {
        SetStyle(ControlStyles.UserPaint |
                ControlStyles.ResizeRedraw |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.SupportsTransparentBackColor, true);

        _checkAnimation = new AnimationEngine(this);
        
        if (!DesignMode)
        {
            ThemeManager.ThemeChanged += OnThemeChanged;
            UpdateStyleFromTheme();
        }
        
        _checkProgress = Checked ? 1.0 : 0.0;
    }

    /// <inheritdoc/>
    protected override void OnCheckedChanged(EventArgs e)
    {
        base.OnCheckedChanged(e);
        _checkAnimation?.Animate(Checked ? 1.0 : 0.0, 150, value => _checkProgress = value);
    }

    private void OnThemeChanged(object? sender, EventArgs e)
    {
        UpdateStyleFromTheme();
        Invalidate();
    }

    private void UpdateStyleFromTheme()
    {
        // ZERO TOLERANCE: Get style or create with REQUIRED states
        _controlStyle = ThemeManager.GetControlStyle("ModernRadioButton") ?? new ControlStyle
        {
            BorderWidth = 2,
            CornerRadius = 12,
            States = new Dictionary<string, StateStyle>
            {
                ["normal"] = new() { ForeColor = "#0D1117", BorderColor = "#D0D7DE" },
                ["checked"] = new() { BackColor = "#0969DA", BorderColor = "#0969DA" },
                ["hover"] = new() { BorderColor = "#0969DA" },
                ["disabled"] = new() { ForeColor = "#8C959F", BorderColor = "#D0D7DE" }
            }
        };

        // ZERO TOLERANCE: Validate 'normal' state exists
        if (!_controlStyle.States.TryGetValue("normal", out var normalState))
            throw new InvalidOperationException(
                "ModernRadioButton 'normal' state not defined in theme. Theme configuration is incomplete.");

        // ZERO TOLERANCE: ForeColor MUST be defined
        if (string.IsNullOrWhiteSpace(normalState.ForeColor))
            throw new InvalidOperationException(
                "ModernRadioButton ForeColor not defined in 'normal' state. Theme configuration is incomplete.");

        ForeColor = ColorTranslator.FromHtml(normalState.ForeColor);

        // ZERO TOLERANCE: Theme font MUST be available
        _themeFont?.Dispose();
        _themeFont = ThemeManager.CreateFont() 
            ?? throw new InvalidOperationException(
                "Theme font creation failed. Theme system not initialized.");
        
        Font = _themeFont;
    }

    /// <summary>
    /// Applies the specified skin definition to this radio button.
    /// </summary>
    /// <param name="skin">The skin definition to apply.</param>
    public void ApplySkin(SkinDefinition skin)
    {
        UpdateStyleFromTheme();
        Invalidate();
    }

    /// <inheritdoc/>
    protected override void OnPaint(PaintEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        // Clear background
        g.Clear(BackColor);

        // Calculate circle position and size
        const int circleSize = 16;
        const int circleMargin = 2;
        var circleRect = new Rectangle(circleMargin, (Height - circleSize) / 2, circleSize, circleSize);

        // Determine colors
        var normalStyle = _controlStyle.States.GetValueOrDefault("normal") ?? new StateStyle { BorderColor = "#CCCCCC" };
        var borderColor = ColorCache.GetColor(normalStyle.BorderColor, Color.LightGray);

        if (!Enabled)
        {
            var disabledStyle = _controlStyle.States.GetValueOrDefault("disabled");
            if (disabledStyle?.BorderColor != null)
            {
                borderColor = ColorCache.GetColor(disabledStyle.BorderColor, borderColor);
            }
        }
        else if (Checked)
        {
            var checkedStyle = _controlStyle.States.GetValueOrDefault("checked");
            if (checkedStyle?.BorderColor != null)
            {
                borderColor = ColorCache.GetColor(checkedStyle.BorderColor, borderColor);
            }
        }

        // Draw outer circle
        using (var pen = new Pen(borderColor, _controlStyle.BorderWidth))
        {
            g.DrawEllipse(pen, circleRect);
        }

        // Draw inner circle (when checked)
        if (_checkProgress > 0)
        {
            var checkedStyle = _controlStyle.States.GetValueOrDefault("checked");
            var fillColor = checkedStyle?.BackColor != null
                ? ColorCache.GetColor(checkedStyle.BackColor, Color.Blue)
                : borderColor;

            var innerSize = (int)(8 * _checkProgress);
            var innerRect = new Rectangle(
                circleRect.X + (circleRect.Width - innerSize) / 2,
                circleRect.Y + (circleRect.Height - innerSize) / 2,
                innerSize,
                innerSize
            );

            using var brush = new SolidBrush(fillColor);
            g.FillEllipse(brush, innerRect);
        }

        // Draw text
        var textRect = new Rectangle(
            circleRect.Right + 6,
            0,
            Width - circleRect.Right - 6,
            Height
        );

        var textColor = ForeColor;
        if (!Enabled)
        {
            var disabledStyle = _controlStyle.States.GetValueOrDefault("disabled");
            if (disabledStyle?.ForeColor != null)
            {
                textColor = ColorCache.GetColor(disabledStyle.ForeColor, textColor);
            }
        }

        TextRenderer.DrawText(
            g,
            Text,
            Font,
            textRect,
            textColor,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter
        );
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            ThemeManager.ThemeChanged -= OnThemeChanged;
            _themeFont?.Dispose();
            _themeFont = null;
            _checkAnimation?.Dispose();
            _checkAnimation = null;
        }
        base.Dispose(disposing);
    }
}
