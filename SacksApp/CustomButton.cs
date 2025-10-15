using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

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

    public CustomButton()
    {
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;  // Must be 0 when using UserPaint + Region
        TextImageRelation = TextImageRelation.ImageBeforeText;
        ImageAlign = ContentAlignment.MiddleLeft;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        UpdateColors();
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
        set
        {
            if (_badgeColor != value)
            {
                _badgeColor = value;
                RecreateBadge();
            }
        }
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
        set
        {
            if (_glyph != value)
            {
                _glyph = value ?? string.Empty;
                RecreateBadge();
            }
        }
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
        set
        {
            if (_badgeDiameter != value)
            {
                _badgeDiameter = Math.Max(12, value);
                RecreateBadge();
            }
        }
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
        set
        {
            if (_cornerRadius != value)
            {
                _cornerRadius = Math.Max(0, value);
                UpdateRegion();
                Invalidate();
            }
        }
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
        if (!_isHovered)
        {
            _isHovered = true;
            Invalidate();
        }
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        if (_isHovered || _isPressed)
        {
            _isHovered = false;
            _isPressed = false;
            Invalidate();
        }
    }

    protected override void OnMouseDown(MouseEventArgs mevent)
    {
        if (mevent is null)
        {
            throw new ArgumentNullException(nameof(mevent));
        }

        base.OnMouseDown(mevent);
        if (mevent.Button == MouseButtons.Left)
        {
            _isPressed = true;
            Invalidate();
        }
    }

    protected override void OnMouseUp(MouseEventArgs mevent)
    {
        base.OnMouseUp(mevent);
        if (_isPressed)
        {
            _isPressed = false;
            Invalidate();
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        if (e is null)
        {
            throw new ArgumentNullException(nameof(e));
        }

        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        // Determine background color based on state
        Color backColor = _isPressed ? _pressedBackColor : _isHovered ? _hoverBackColor : _normalBackColor;

        // Border width for focused state - changed to 1 pixel
        const int borderWidth = 1;
        const float halfBorder = 0.5f;

        // Always expand background slightly to cover anti-aliased edge pixels
        var bgRect = new RectangleF(-0.5f, -0.5f, Width + 1, Height + 1);
        var borderRect = new RectangleF(halfBorder, halfBorder, Width - borderWidth, Height - borderWidth);

        // Draw background with rounded corners (always full size)
        using (var backBrush = new SolidBrush(backColor))
        {
            if (_cornerRadius > 0)
            {
                using var bgPath = CreateRoundRectPath(bgRect, _cornerRadius);
                g.FillPath(backBrush, bgPath);
            }
            else
            {
                g.FillRectangle(backBrush, bgRect);
            }
        }

        // Draw border ONLY when focused - on top of the background
        if (Focused)
        {
            var borderColor = SystemColors.Highlight;
            using (var borderPen = new Pen(borderColor, borderWidth))
            {
                borderPen.Alignment = PenAlignment.Inset;
                if (_cornerRadius > 0)
                {
                    using var borderPath = CreateRoundRectPath(borderRect, _cornerRadius);
                    g.DrawPath(borderPen, borderPath);
                }
                else
                {
                    g.DrawRectangle(borderPen, halfBorder, halfBorder, Width - borderWidth, Height - borderWidth);
                }
            }
        }

        // Draw badge image - position it INSIDE the safe area (past the rounded corner)
        if (_badgeImage != null)
        {
            // Position badge further from edge to avoid corner clipping
            int imgX = _cornerRadius + 4; // Start after the corner radius + small margin
            int imgY = (ClientSize.Height - _badgeImage.Height) / 2;
            g.DrawImageUnscaled(_badgeImage, imgX, imgY);
        }

        // Draw text
        if (!string.IsNullOrEmpty(Text))
        {
            TextRenderer.DrawText(g, Text, Font, ClientRectangle, ForeColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _badgeImage?.Dispose();
            _badgeImage = null;
        }
        base.Dispose(disposing);
    }

    private void UpdateColors()
    {
        _normalBackColor = BackColor;
        
        // For white or very light backgrounds, darken slightly on hover instead of lightening
        // For darker backgrounds, lighten on hover
        if (_normalBackColor.GetBrightness() > 0.9f)
        {
            // Light background (like white) - darken slightly for hover
            _hoverBackColor = ControlPaint.Dark(_normalBackColor, 0.05f);
        }
        else
        {
            // Darker background - lighten for hover
            _hoverBackColor = ControlPaint.Light(_normalBackColor, 0.2f);
        }
        
        _pressedBackColor = ControlPaint.Dark(_normalBackColor, 0.1f);
    }

    private void RecreateBadge()
    {
        _badgeImage?.Dispose();
        _badgeImage = null;

        if (string.IsNullOrEmpty(_glyph))
        {
            // Reset padding if no badge
            Padding = new Padding(12, 12, 12, 12);
            Invalidate();
            return;
        }

        // Calculate badge size
        int target = (int)Math.Round(ClientSize.Height * 0.55);
        int diameter = Math.Clamp(target, Math.Max(20, _badgeDiameter), 44);

        // Update padding for text to not overlap badge
        // Add extra padding to account for border width and corner radius
        int leftPad = diameter + 22; // increased from 18 to account for rounded corner
        Padding = new Padding(leftPad, 12, 12, 12);

        // Create badge bitmap
        float scale = DeviceDpi / 96f;
        int d = Math.Max(12, (int)Math.Round(diameter * scale));
        var bmp = new Bitmap(d, d);
        bmp.SetResolution(DeviceDpi, DeviceDpi);

        using (var g = Graphics.FromImage(bmp))
        using (var brush = new SolidBrush(_badgeColor))
        using (var glyphBrush = new SolidBrush(Color.White))
        using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
            g.Clear(Color.Transparent);

            // Draw circle
            g.FillEllipse(brush, 0, 0, d, d);

            // Draw glyph
            float fontSize = d * 0.50f;
            using var font = GetMdl2Font(fontSize);
            g.DrawString(_glyph, font, glyphBrush, new RectangleF(0, 0, d, d), sf);
        }

        _badgeImage = bmp;
        Invalidate();
    }

    private void UpdateRegion()
    {
        // Use Region clipping to create rounded corners
        if (_cornerRadius <= 0)
        {
            Region = null;
            return;
        }
        var rect = new Rectangle(0, 0, Width, Height);
        using var path = CreateRoundRectPath(rect, _cornerRadius);
        Region = new Region(path);
    }

    private static GraphicsPath CreateRoundRectPath(Rectangle bounds, int radius)
    {
        return CreateRoundRectPath(new RectangleF(bounds.X, bounds.Y, bounds.Width, bounds.Height), radius);
    }

    private static GraphicsPath CreateRoundRectPath(RectangleF bounds, float radius)
    {
        float d = radius * 2f;
        var path = new GraphicsPath();
        
        if (radius < 0.1f)
        {
            path.AddRectangle(bounds);
            return path;
        }

        // Top-left arc
        path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
        // Top-right arc
        path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
        // Bottom-right arc
        path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
        // Bottom-left arc
        path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
        
        path.CloseFigure();
        return path;
    }

    private static Font GetMdl2Font(float size)
    {
        try
        {
            return new Font("Segoe MDL2 Assets", size, FontStyle.Regular, GraphicsUnit.Pixel);
        }
        catch
        {
            return new Font(SystemFonts.DefaultFont.FontFamily, size, FontStyle.Regular, GraphicsUnit.Pixel);
        }
    }
}
