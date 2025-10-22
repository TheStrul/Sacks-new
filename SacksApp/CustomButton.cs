using System.ComponentModel;
using System.Drawing.Drawing2D;

using SacksApp.Theming;

namespace SacksApp;

/// <summary>
/// Custom button with rounded corners, badge icon support, and proper hover/press states.
/// </summary>
[ToolboxItem(true)]
[DesignerCategory("Code")]
public class CustomButton : Button
{
    private Color _badgeColor = Color.FromArgb(0, 120, 215);
    private string _glyph = string.Empty;
    private int _badgeDiameter = 28;
    private int _cornerRadius = 12;
    private Image? _badgeImage;
    private Color _normalBackColor;
    private Color _hoverBackColor;
    private Color _pressedBackColor;
    private bool _isHovered;
    private bool _isPressed;

    private int _focusBorderWidth = 1;
    private Color _focusBorderColorOverride = Color.Empty;

    private bool _useTheme = true;

    public CustomButton()
    {
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        TextImageRelation = TextImageRelation.ImageBeforeText;
        ImageAlign = ContentAlignment.MiddleLeft;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);

        try
        {
            ButtonThemeProvider.Initialize();
            ButtonThemeProvider.ThemeChanged += (_, __) => ApplyThemeAndRefresh();
            ApplyThemeAndRefresh();
        }
        catch
        {
            UpdateColors();
        }
    }

    /// <summary>
    /// When true (default), applies values from button-theme.json; when false, uses local properties only.
    /// </summary>
    [Category("Appearance")]
    [DefaultValue(true)]
    public bool UseTheme
    {
        get => _useTheme;
        set { _useTheme = value; ApplyThemeAndRefresh(); }
    }

    /// <summary>
    /// Badge background color.
    /// </summary>
    [Category("Appearance")]
    [Description("Color of the badge circle.")]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public Color BadgeColor
    {
        get => _badgeColor;
        set { if (_badgeColor != value) { _badgeColor = value; RecreateBadge(); } }
    }

    /// <summary>
    /// Badge glyph (MDL2 icon).
    /// </summary>
    [Category("Appearance")]
    [Description("MDL2 icon glyph for the badge.")]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    [Editor("System.ComponentModel.Design.MultilineStringEditor, System.Design", "System.Drawing.Design.UITypeEditor, System.Drawing")]
    public string Glyph
    {
        get => _glyph;
        set { if (_glyph != value) { _glyph = value ?? string.Empty; RecreateBadge(); } }
    }

    /// <summary>
    /// Minimum badge diameter in pixels.
    /// </summary>
    [Category("Appearance")]
    [Description("Minimum diameter of the badge in pixels.")]
    [DefaultValue(28)]
    public int BadgeDiameter
    {
        get => _badgeDiameter;
        set { if (_badgeDiameter != value) { _badgeDiameter = Math.Max(12, value); RecreateBadge(); } }
    }

    /// <summary>
    /// Corner radius for rounded button.
    /// </summary>
    [Category("Appearance")]
    [Description("Radius of rounded corners in pixels.")]
    [DefaultValue(12)]
    public int CornerRadius
    {
        get => _cornerRadius;
        set { if (_cornerRadius != value) { _cornerRadius = Math.Max(0, value); UpdateRegion(); Invalidate(); } }
    }

    /// <summary>
    /// Focus border width in pixels (from theme or override).
    /// </summary>
    [Category("Appearance")]
    [DefaultValue(1)]
    public int FocusBorderWidth
    {
        get => _focusBorderWidth;
        set { _focusBorderWidth = Math.Max(0, value); Invalidate(); }
    }

    /// <summary>
    /// Optional custom focus border color; set to Color.Empty to use theme/system highlight.
    /// Hidden from designer serialization to satisfy WFO1000.
    /// </summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color FocusBorderColorOverride
    {
        get => _focusBorderColorOverride;
        set { _focusBorderColorOverride = value; Invalidate(); }
    }

    protected override void OnBackColorChanged(EventArgs e)
    {
        base.OnBackColorChanged(e);
        UpdateColors();
        Invalidate();
    }

    protected override void OnGotFocus(EventArgs e)
    {
        base.OnGotFocus(e);
        Invalidate(); // Redraw to show blue border
    }

    protected override void OnLostFocus(EventArgs e)
    {
        base.OnLostFocus(e);
        Invalidate(); // Redraw to hide border
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        RecreateBadge();
        UpdateRegion();
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        base.OnMouseEnter(e);
        if (!_isHovered) { _isHovered = true; Invalidate(); }
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        if (_isHovered || _isPressed) { _isHovered = false; _isPressed = false; Invalidate(); }
    }

    protected override void OnMouseDown(MouseEventArgs mevent)
    {
        if (mevent is null) throw new ArgumentNullException(nameof(mevent));
        base.OnMouseDown(mevent);
        if (mevent.Button == MouseButtons.Left) { _isPressed = true; Invalidate(); }
    }

    protected override void OnMouseUp(MouseEventArgs mevent)
    {
        base.OnMouseUp(mevent);
        if (_isPressed) { _isPressed = false; Invalidate(); }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        if (e is null) throw new ArgumentNullException(nameof(e));
        var g = e.Graphics;

        var theme = _useTheme ? ButtonThemeProvider.Current : null;
        if (theme?.Rendering.AntiAlias == true) g.SmoothingMode = SmoothingMode.AntiAlias; else g.SmoothingMode = SmoothingMode.None;
        if (theme?.Rendering.HighQualityPixelOffset == true) g.PixelOffsetMode = PixelOffsetMode.HighQuality; else g.PixelOffsetMode = PixelOffsetMode.Default;
        if (theme?.Rendering.ClearTypeText == true) g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit; else g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SystemDefault;

        Color backColor = GetStateBackColor(theme);

        int borderWidth = Math.Max(0, _focusBorderWidth);
        float expansion = (float)(theme?.Rendering.BackgroundExpansion ?? 0.5);
        float expand = Math.Max(0, expansion);
        var bgRect = new RectangleF(-expand, -expand, Width + expand * 2, Height + expand * 2);
        var borderRect = new RectangleF(borderWidth * 0.5f, borderWidth * 0.5f, Width - borderWidth, Height - borderWidth);

        using (var backBrush = new SolidBrush(backColor))
        {
            if (_cornerRadius > 0) { using var bgPath = CreateRoundRectPath(bgRect, _cornerRadius); g.FillPath(backBrush, bgPath); }
            else { g.FillRectangle(backBrush, bgRect); }
        }

        if (Focused && borderWidth > 0)
        {
            var borderColor = (_focusBorderColorOverride != Color.Empty)
                ? _focusBorderColorOverride
                : (theme?.Colors.FocusBorder.UseSystemHighlight != false
                    ? SystemColors.Highlight
                    : theme?.Colors.FocusBorder.CustomColor?.ToColor() ?? SystemColors.Highlight);

            using var borderPen = new Pen(borderColor, borderWidth) { Alignment = PenAlignment.Inset };
            if (_cornerRadius > 0) { using var borderPath = CreateRoundRectPath(borderRect, _cornerRadius); g.DrawPath(borderPen, borderPath); }
            else { g.DrawRectangle(borderPen, borderRect.X, borderRect.Y, borderRect.Width, borderRect.Height); }
        }

        if (_badgeImage != null)
        {
            int leftOffset = theme?.Spacing.BadgeLeftOffset ?? 4;
            int imgX = Math.Max(_cornerRadius + leftOffset, leftOffset);
            int imgY = (ClientSize.Height - _badgeImage.Height) / 2;
            g.DrawImageUnscaled(_badgeImage, imgX, imgY);
        }

        if (!string.IsNullOrEmpty(Text))
        {
            TextRenderer.DrawText(g, Text, Font, ClientRectangle, ForeColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) { _badgeImage?.Dispose(); _badgeImage = null; }
        base.Dispose(disposing);
    }

    private void ApplyThemeAndRefresh()
    {
        try
        {
            if (!_useTheme) { UpdateColors(); return; }
            var theme = ButtonThemeProvider.Current;

            _cornerRadius = theme.Defaults.CornerRadius;
            _badgeDiameter = theme.Defaults.BadgeDiameter;
            _focusBorderWidth = theme.Defaults.FocusBorderWidth;

            if (theme.Colors.BackColor is not null)
            {
                BackColor = theme.Colors.BackColor.ToColor();
            }

            // Typography
            var t = theme.Typography;
            if (t != null)
            {
                try
                {
                    var family = string.IsNullOrWhiteSpace(t.TextFontFamily) ? this.Font.FontFamily.Name : t.TextFontFamily;
                    var size = t.TextFontSize ?? this.Font.Size;
                    var style = ParseFontStyleOrDefault(t.TextFontStyle, this.Font.Style);
                    var newFont = new Font(family!, size, style, GraphicsUnit.Point);
                    this.Font = newFont;
                }
                catch { /* ignore font issues */ }

                if (t.TextColor is not null)
                {
                    this.ForeColor = t.TextColor.ToColor();
                }
            }

            var defaultPadding = theme.Spacing.DefaultPadding?.ToPadding() ?? new Padding(12);
            int leftExtra = theme.Spacing.BadgeLeftPaddingExtra;
            Padding = !string.IsNullOrEmpty(_glyph)
                ? new Padding(defaultPadding.Left + leftExtra, defaultPadding.Top, defaultPadding.Right, defaultPadding.Bottom)
                : defaultPadding;

            UpdateColors();
            RecreateBadge();
            UpdateRegion();
            Invalidate();
        }
        catch { UpdateColors(); }
    }

    private static FontStyle ParseFontStyleOrDefault(string? style, FontStyle fallback)
    {
        if (string.IsNullOrWhiteSpace(style)) return fallback;
        var s = style.Trim();
        return s.ToLowerInvariant() switch
        {
            "regular" => FontStyle.Regular,
            "bold" => FontStyle.Bold,
            "italic" => FontStyle.Italic,
            "bolditalic" => FontStyle.Bold | FontStyle.Italic,
            "underline" => FontStyle.Underline,
            "strikeout" => FontStyle.Strikeout,
            _ => fallback
        };
    }

    private static Color BlendOpacity(Color baseColor, double? opacity)
    {
        var a = (int)Math.Round((opacity ?? 1.0) * 255.0);
        a = Math.Clamp(a, 0, 255);
        return Color.FromArgb(a, baseColor);
    }

    private Color GetStateBackColor(ButtonTheme? theme)
    {
        // Explicit state colors win
        if (theme != null)
        {
            var colors = theme.Colors;
            if (_isPressed && colors.PressedColor?.Color is not null)
                return BlendOpacity(colors.PressedColor.Color.ToColor(), colors.PressedColor.Opacity);
            if (_isHovered && colors.HoverColor?.Color is not null)
                return BlendOpacity(colors.HoverColor.Color.ToColor(), colors.HoverColor.Opacity);

            // Active/inactive based on Enabled
            var state = Enabled ? colors.Active : colors.Inactive;
            if (state?.Color is not null)
                return BlendOpacity(state.Color.ToColor(), state.Opacity);
        }

        // Fallback to derived colors
        var normal = (theme != null && theme.Colors.BackColor is not null) ? theme.Colors.BackColor.ToColor() : _normalBackColor;
        if (_isPressed) return ControlPaint.Dark(normal, 0.10f);
        if (_isHovered) return ControlPaint.Light(normal, 0.20f);
        return normal;
    }

    private void UpdateColors()
    {
        _normalBackColor = BackColor;

        var theme = _useTheme ? ButtonThemeProvider.Current : null;
        if (theme == null)
        {
            _hoverBackColor = (_normalBackColor.GetBrightness() > 0.9f)
                ? ControlPaint.Dark(_normalBackColor, 0.05f)
                : ControlPaint.Light(_normalBackColor, 0.2f);
            _pressedBackColor = ControlPaint.Dark(_normalBackColor, 0.1f);
            return;
        }

        var baseColor = theme.Colors.BackColor?.ToColor() ?? _normalBackColor;
        _hoverBackColor = ControlPaint.Light(baseColor, 0.2f);
        _pressedBackColor = ControlPaint.Dark(baseColor, 0.1f);
    }

    private void RecreateBadge()
    {
        _badgeImage?.Dispose();
        _badgeImage = null;

        var theme = _useTheme ? ButtonThemeProvider.Current : null;

        if (string.IsNullOrEmpty(_glyph))
        {
            var fallback = theme?.Spacing.DefaultPadding?.ToPadding() ?? new Padding(12, 12, 12, 12);
            Padding = fallback;
            Invalidate();
            return;
        }

        double heightRatio = theme?.Defaults.BadgeHeightRatio ?? 0.55;
        int target = (int)Math.Round(ClientSize.Height * heightRatio);
        int diameter = Math.Clamp(target, Math.Max(20, _badgeDiameter), 64);

        int leftPadExtra = theme?.Spacing.BadgeLeftPaddingExtra ?? 22;
        int leftPad = diameter + leftPadExtra;
        var basePad = theme?.Spacing.DefaultPadding?.ToPadding() ?? new Padding(12);
        Padding = new Padding(basePad.Left + leftPad, basePad.Top, basePad.Right, basePad.Bottom);

        float scale = DeviceDpi / 96f;
        int d = Math.Max(12, (int)Math.Round(diameter * scale));
        var bmp = new Bitmap(d, d);
        bmp.SetResolution(DeviceDpi, DeviceDpi);

        using (var g = Graphics.FromImage(bmp))
        using (var brush = new SolidBrush(_badgeColor))
        using (var glyphBrush = new SolidBrush((theme?.Badge.GlyphColor ?? new ButtonTheme.RgbColor()).ToColor()))
        using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
            g.Clear(Color.Transparent);
            g.FillEllipse(brush, 0, 0, d, d);
            float fontSize = d * (float)(theme?.Defaults.BadgeIconSizeRatio ?? 0.50);
            using var font = GetMdl2Font(fontSize, theme?.Badge.FontFamily, theme?.Badge.FallbackToSystemFont ?? true);
            g.DrawString(_glyph, font, glyphBrush, new RectangleF(0, 0, d, d), sf);
        }

        _badgeImage = bmp;
        Invalidate();
    }

    private void UpdateRegion()
    {
        if (_cornerRadius <= 0) { Region = null; return; }
        var rect = new Rectangle(0, 0, Width, Height);
        using var path = CreateRoundRectPath(rect, _cornerRadius);
        Region = new Region(path);
    }

    private static GraphicsPath CreateRoundRectPath(Rectangle bounds, int radius) => CreateRoundRectPath(new RectangleF(bounds.X, bounds.Y, bounds.Width, bounds.Height), radius);

    private static GraphicsPath CreateRoundRectPath(RectangleF bounds, float radius)
    {
        float d = radius * 2f;
        var path = new GraphicsPath();
        if (radius < 0.1f) { path.AddRectangle(bounds); return path; }
        path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
        path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
        path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
        path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }

    private static Font GetMdl2Font(float size, string? familyOverride = null, bool fallbackToSystem = true)
    {
        try
        {
            return !string.IsNullOrWhiteSpace(familyOverride)
                ? new Font(familyOverride, size, FontStyle.Regular, GraphicsUnit.Pixel)
                : new Font("Segoe MDL2 Assets", size, FontStyle.Regular, GraphicsUnit.Pixel);
        }
        catch
        {
            if (fallbackToSystem)
            {
                return new Font(SystemFonts.DefaultFont.FontFamily, size, FontStyle.Regular, GraphicsUnit.Pixel);
            }
            throw;
        }
    }
}
