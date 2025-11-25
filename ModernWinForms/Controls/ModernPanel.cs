using System.ComponentModel;
using System.Drawing.Drawing2D;
using ModernWinForms.Theming;

namespace ModernWinForms.Controls;

/// <summary>
/// Modern panel control with rounded corners, shadow, and theme support.
/// </summary>
[ToolboxItem(true)]
[DesignerCategory("Code")]
[Description("Modern panel control with rounded corners, shadow, and theme support.")]
public class ModernPanel : Panel
{
    private ControlStyle _controlStyle = new();
    private GraphicsPath? _cachedPath;
    private Rectangle _cachedRect;
    private int _cachedRadius;
    private bool _showShadow = true;
    private int _shadowDepth = 4;
    private Color _shadowColor = Color.FromArgb(50, 0, 0, 0);

    /// <summary>
    /// Initializes a new instance of the ModernPanel class.
    /// </summary>
    public ModernPanel()
    {
        SetStyle(ControlStyles.UserPaint |
                ControlStyles.ResizeRedraw |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.SupportsTransparentBackColor, true);

        BackColor = Color.White;
        ThemeManager.ThemeChanged += OnThemeChanged;
        UpdateStyleFromSkin();
    }

    /// <summary>
    /// Gets or sets whether to show shadow effect.
    /// </summary>
    [Category("Appearance")]
    [Description("Indicates whether to show shadow effect.")]
    [DefaultValue(true)]
    public bool ShowShadow
    {
        get => _showShadow;
        set
        {
            if (_showShadow != value)
            {
                _showShadow = value;
                Invalidate();
            }
        }
    }

    /// <summary>
    /// Gets or sets the shadow depth in pixels.
    /// </summary>
    [Category("Appearance")]
    [Description("The depth of the shadow effect in pixels.")]
    [DefaultValue(4)]
    public int ShadowDepth
    {
        get => _shadowDepth;
        set
        {
            if (_shadowDepth != value)
            {
                _shadowDepth = Math.Max(0, value);
                Invalidate();
            }
        }
    }

    /// <summary>
    /// Gets or sets the shadow color.
    /// </summary>
    [Category("Appearance")]
    [Description("The color of the shadow effect.")]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public Color ShadowColor
    {
        get => _shadowColor;
        set
        {
            if (_shadowColor != value)
            {
                _shadowColor = value;
                Invalidate();
            }
        }
    }

    private void OnThemeChanged(object? sender, EventArgs e)
    {
        UpdateStyleFromSkin();
        Invalidate();
    }

    private void UpdateStyleFromSkin()
    {
        _controlStyle = ThemeManager.GetControlStyle("ModernPanel") ?? new ControlStyle
        {
            CornerRadius = 8,
            BorderWidth = 0,
            States = new Dictionary<string, StateStyle>
            {
                ["normal"] = new() { BackColor = "#FFFFFF", BorderColor = "#E1E4E8" }
            }
        };

        var normalStyle = _controlStyle.States.GetValueOrDefault("normal");
        if (normalStyle?.BackColor != null)
        {
            BackColor = ColorCache.GetColor(normalStyle.BackColor, Color.White);
        }
    }

    /// <inheritdoc/>
    protected override void OnPaint(PaintEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        var panelRect = ClientRectangle;
        if (_showShadow && _shadowDepth > 0)
        {
            panelRect = new Rectangle(0, 0, Width - _shadowDepth, Height - _shadowDepth);
            DrawShadow(g, panelRect);
        }

        // Draw panel background
        var path = GetOrCreateCachedPath(panelRect, _controlStyle.CornerRadius);
        using (var brush = new SolidBrush(BackColor))
        {
            g.FillPath(brush, path);
        }

        // Draw border if needed
        if (_controlStyle.BorderWidth > 0)
        {
            var normalStyle = _controlStyle.States.GetValueOrDefault("normal");
            var borderColor = ColorCache.GetColor(normalStyle?.BorderColor, Color.LightGray);
            using var pen = new Pen(borderColor, _controlStyle.BorderWidth);
            g.DrawPath(pen, path);
        }
    }

    private void DrawShadow(Graphics g, Rectangle rect)
    {
        for (var i = 0; i < _shadowDepth; i++)
        {
            var alpha = (int)(_shadowColor.A * (1.0 - (i / (double)_shadowDepth)));
            var shadowRect = new Rectangle(
                rect.X + i + 2,
                rect.Y + i + 2,
                rect.Width,
                rect.Height);

            using var shadowPath = GraphicsHelper.CreateRoundedRectangle(shadowRect, _controlStyle.CornerRadius);
            using var shadowBrush = new SolidBrush(Color.FromArgb(alpha, _shadowColor));
            g.FillPath(shadowBrush, shadowPath);
        }
    }

    private GraphicsPath GetOrCreateCachedPath(Rectangle rect, int radius)
    {
        if (_cachedPath != null && _cachedRect == rect && _cachedRadius == radius)
        {
            return _cachedPath;
        }

        _cachedPath?.Dispose();
        _cachedPath = GraphicsHelper.CreateRoundedRectangle(rect, radius);
        _cachedRect = rect;
        _cachedRadius = radius;

        return _cachedPath;
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
}
