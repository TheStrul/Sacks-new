using System.ComponentModel;
using System.Drawing.Drawing2D;
using ModernWinForms.Theming;

namespace ModernWinForms.Controls;

/// <summary>
/// Modern panel control with rounded corners and theme support.
/// </summary>
[ToolboxItem(true)]
[DesignerCategory("Code")]
[Description("Modern panel control with rounded corners and theme support.")]
public class ModernPanel : Panel
{
    private ControlStyle _controlStyle = new();
    private GraphicsPath? _cachedPath;
    private Rectangle _cachedRect;
    private int _cachedRadius;

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
        
        if (!DesignMode)
        {
            ThemeManager.ThemeChanged += OnThemeChanged;
            UpdateStyleFromSkin();
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



    private GraphicsPath GetOrCreateCachedPath(Rectangle rect, int radius)
    {
        if (_cachedPath != null && _cachedRect == rect && _cachedRadius == radius)
        {
            return _cachedPath;
        }

        GraphicsPathPool.Return(_cachedPath);
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
            GraphicsPathPool.Return(_cachedPath);
            _cachedPath = null;
        }
        base.Dispose(disposing);
    }
}
