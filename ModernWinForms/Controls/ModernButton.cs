using System.ComponentModel;
using System.Drawing.Drawing2D;
using ModernWinForms.Accessibility;
using ModernWinForms.Animation;
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
    private GraphicsPath? _cachedPath;
    private Size _cachedSize;
    private int _cachedRadius;
    private Image? _image;
    private ContentAlignment _imageAlign = ContentAlignment.MiddleLeft;
    private Font? _themeFont;
    private AnimationEngine? _hoverAnimation;
    private AnimationEngine? _pressAnimation;
    private double _hoverProgress;
    private double _pressProgress;
    private bool _enableAnimations = true;
    private bool _enableRippleEffect = true;
    private Point _rippleCenter;
    private double _rippleProgress;
    private AnimationEngine? _rippleAnimation;
    private bool _showFocusIndicator = true;

    /// <summary>
    /// Gets or sets the image displayed on the button.
    /// </summary>
    [Category("Appearance")]
    [Description("The image/icon displayed on the button.")]
    [DefaultValue(null)]
    public new Image? Image
    {
        get => _image;
        set
        {
            if (_image != value)
            {
                _image = value;
                Invalidate();
            }
        }
    }

    /// <summary>
    /// Gets or sets the alignment of the image on the button.
    /// </summary>
    [Category("Appearance")]
    [Description("The alignment of the image on the button.")]
    [DefaultValue(ContentAlignment.MiddleLeft)]
    public new ContentAlignment ImageAlign
    {
        get => _imageAlign;
        set
        {
            if (_imageAlign != value)
            {
                _imageAlign = value;
                Invalidate();
            }
        }
    }

    /// <summary>
    /// Gets or sets whether smooth animations are enabled.
    /// </summary>
    [Category("Behavior")]
    [Description("Enables smooth state transition animations.")]
    [DefaultValue(true)]
    public bool EnableAnimations
    {
        get => _enableAnimations;
        set => _enableAnimations = value;
    }

    /// <summary>
    /// Gets or sets whether ripple effect is enabled on click.
    /// </summary>
    [Category("Behavior")]
    [Description("Enables ripple effect when the button is clicked.")]
    [DefaultValue(true)]
    public bool EnableRippleEffect
    {
        get => _enableRippleEffect;
        set => _enableRippleEffect = value;
    }

    /// <summary>
    /// Gets or sets whether to show focus indicator for keyboard navigation.
    /// </summary>
    [Category("Accessibility")]
    [Description("Shows a focus indicator when the button is focused via keyboard.")]
    [DefaultValue(true)]
    public bool ShowFocusIndicator
    {
        get => _showFocusIndicator;
        set
        {
            if (_showFocusIndicator != value)
            {
                _showFocusIndicator = value;
                Invalidate();
            }
        }
    }

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

        // Only initialize theme-dependent features at runtime, not in designer
        if (!DesignMode)
        {
            ThemeManager.ThemeChanged += OnThemeChanged;
            UpdateStyleFromSkin();
            UpdateFontFromTheme();
            InitializeAnimations();
        }
        else
        {
            // Use default style in designer
            _controlStyle = CreateDefaultStyle();
        }
    }

    private void InitializeAnimations()
    {
        _hoverAnimation = new AnimationEngine(this);
        _pressAnimation = new AnimationEngine(this);
        _rippleAnimation = new AnimationEngine(this);
    }

    private void OnThemeChanged(object? sender, EventArgs e)
    {
        UpdateStyleFromSkin();
        UpdateFontFromTheme();
        Invalidate();
    }

    private void UpdateFontFromTheme()
    {
        // Always update theme font cache; it will be used in painting if needed
        try
        {
            _themeFont?.Dispose();
            _themeFont = ThemeManager.CreateFont();
        }
        catch
        {
            // If theme manager fails, just use default font
            _themeFont = null;
        }
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
        try
        {
            _controlStyle = ThemeManager.GetControlStyle("ModernButton") ?? CreateDefaultStyle();
        }
        catch
        {
            // If theme manager fails, use default style
            _controlStyle = CreateDefaultStyle();
        }
    }

    private ControlStyle CreateDefaultStyle()
    {
        return new ControlStyle
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
        
        if (_enableAnimations)
        {
            _hoverAnimation?.Animate(1.0, 200, value => _hoverProgress = value);
        }
        else
        {
            _hoverProgress = 1.0;
            Invalidate();
        }
    }

    /// <inheritdoc/>
    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        
        if (_enableAnimations)
        {
            _hoverAnimation?.Animate(0.0, 200, value => _hoverProgress = value);
        }
        else
        {
            _hoverProgress = 0.0;
            Invalidate();
        }
    }

    /// <inheritdoc/>
    protected override void OnMouseDown(MouseEventArgs mevent)
    {
        base.OnMouseDown(mevent);
        
        if (mevent != null && _enableRippleEffect)
        {
            _rippleCenter = mevent.Location;
            _rippleAnimation?.Animate(1.0, 600, value => _rippleProgress = value, () => _rippleProgress = 0);
        }
        
        if (_enableAnimations)
        {
            _pressAnimation?.Animate(1.0, 100, value => _pressProgress = value);
        }
        else
        {
            _pressProgress = 1.0;
            Invalidate();
        }
    }

    /// <inheritdoc/>
    protected override void OnMouseUp(MouseEventArgs mevent)
    {
        base.OnMouseUp(mevent);
        
        if (_enableAnimations)
        {
            _pressAnimation?.Animate(0.0, 150, value => _pressProgress = value);
        }
        else
        {
            _pressProgress = 0.0;
            Invalidate();
        }
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

        // Determine state and colors with animation blending
        var normalStyle = _controlStyle.States.GetValueOrDefault("normal") 
            ?? new StateStyle { BackColor = "#FFFFFF", ForeColor = "#000000", BorderColor = "#CCCCCC" };
        
        Color backColor, foreColor, borderColor;
        
        if (!Enabled)
        {
            var disabledStyle = _controlStyle.States.GetValueOrDefault("disabled") ?? normalStyle;
            backColor = ColorCache.GetColor(disabledStyle.BackColor, Color.LightGray);
            foreColor = ColorCache.GetColor(disabledStyle.ForeColor, Color.Gray);
            borderColor = ColorCache.GetColor(disabledStyle.BorderColor, Color.LightGray);
        }
        else
        {
            var hoverStyle = _controlStyle.States.GetValueOrDefault("hover") ?? normalStyle;
            var pressedStyle = _controlStyle.States.GetValueOrDefault("pressed") ?? normalStyle;
            
            var normalBack = ColorCache.GetColor(normalStyle.BackColor, Color.White);
            var hoverBack = ColorCache.GetColor(hoverStyle.BackColor, Color.WhiteSmoke);
            var pressedBack = ColorCache.GetColor(pressedStyle.BackColor, Color.Gainsboro);
            
            // Blend colors based on animation progress
            backColor = BlendColors(normalBack, hoverBack, _hoverProgress);
            backColor = BlendColors(backColor, pressedBack, _pressProgress);
            
            var normalFore = ColorCache.GetColor(normalStyle.ForeColor, Color.Black);
            var hoverFore = ColorCache.GetColor(hoverStyle.ForeColor, Color.Black);
            foreColor = BlendColors(normalFore, hoverFore, _hoverProgress);
            
            var normalBorder = ColorCache.GetColor(normalStyle.BorderColor, Color.Gray);
            var hoverBorder = ColorCache.GetColor(hoverStyle.BorderColor, Color.DarkGray);
            borderColor = BlendColors(normalBorder, hoverBorder, _hoverProgress);
        }

        // Draw rounded button
        var path = GetOrCreateCachedPath(ClientRectangle, _controlStyle.CornerRadius);
        using var brush = new SolidBrush(backColor);
        using var pen = new Pen(borderColor, _controlStyle.BorderWidth);

        g.FillPath(brush, path);
        
        if (_controlStyle.BorderWidth > 0)
        {
            g.DrawPath(pen, path);
        }

        // Calculate layout for image and text
        var contentRect = ClientRectangle;
        var imageRect = Rectangle.Empty;
        var textRect = contentRect;

        // Draw image if present
        if (_image != null)
        {
            imageRect = CalculateImageRectangle(contentRect, _image.Size);
            g.DrawImage(_image, imageRect);

            // Adjust text rectangle to account for image
            textRect = CalculateTextRectangle(contentRect, imageRect);
        }

        // Draw text
        if (!string.IsNullOrEmpty(Text))
        {
            var textFlags = GetTextFormatFlags();
            var effectiveFont = GetEffectiveFont();
            
            // Add left padding for better alignment
            var paddedTextRect = textRect;
            paddedTextRect.X += 12; // Add 12px left padding
            paddedTextRect.Width -= 12;
            
            // Increase font size for emoji icons (they're at the start of text)
            if (Text.Length > 0 && char.IsHighSurrogate(Text[0]))
            {
                // This text contains emoji - use larger font
                using var emojiFont = new Font(effectiveFont.FontFamily, effectiveFont.Size * 1.3f, effectiveFont.Style);
                TextRenderer.DrawText(g, Text, emojiFont, paddedTextRect, foreColor, textFlags);
            }
            else
            {
                TextRenderer.DrawText(g, Text, effectiveFont, paddedTextRect, foreColor, textFlags);
            }
        }

        // Draw ripple effect
        if (_enableRippleEffect && _rippleProgress > 0)
        {
            DrawRippleEffect(g, path);
        }

        // Draw focus indicator for keyboard navigation
        if (_showFocusIndicator && Focused && ShowFocusCues)
        {
            AccessibilityHelper.DrawFocusRectangle(g, ClientRectangle, _controlStyle.CornerRadius);
        }
    }

    private void DrawRippleEffect(Graphics g, GraphicsPath clipPath)
    {
        var maxRadius = Math.Max(Width, Height) * 1.5;
        var currentRadius = (float)(maxRadius * _rippleProgress);
        var alpha = (int)((1.0 - _rippleProgress) * 60); // Fade out
        
        using var rippleBrush = new SolidBrush(Color.FromArgb(alpha, Color.White));
        
        // Clip to button shape
        var oldClip = g.Clip;
        g.SetClip(clipPath);
        
        g.FillEllipse(rippleBrush, 
            _rippleCenter.X - currentRadius, 
            _rippleCenter.Y - currentRadius, 
            currentRadius * 2, 
            currentRadius * 2);
        
        // Restore clip and dispose old clip if it exists
        g.Clip = oldClip;
        oldClip?.Dispose();
    }

    private static Color BlendColors(Color from, Color to, double progress)
    {
        progress = Math.Clamp(progress, 0, 1);
        var r = (int)(from.R + ((to.R - from.R) * progress));
        var g = (int)(from.G + ((to.G - from.G) * progress));
        var b = (int)(from.B + ((to.B - from.B) * progress));
        var a = (int)(from.A + ((to.A - from.A) * progress));
        return Color.FromArgb(a, r, g, b);
    }

    private Font GetEffectiveFont()
    {
        // Use theme font if available, otherwise use control's Font
        return _themeFont ?? Font ?? DefaultFont;
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
            _themeFont?.Dispose();
            _themeFont = null;
            _hoverAnimation?.Dispose();
            _hoverAnimation = null;
            _pressAnimation?.Dispose();
            _pressAnimation = null;
            _rippleAnimation?.Dispose();
            _rippleAnimation = null;
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

    private Rectangle CalculateImageRectangle(Rectangle contentRect, Size imageSize)
    {
        const int padding = 8;
        var x = contentRect.X + padding;
        var y = contentRect.Y + (contentRect.Height - imageSize.Height) / 2;
        var width = imageSize.Width;
        var height = imageSize.Height;

        return _imageAlign switch
        {
            ContentAlignment.TopLeft => new Rectangle(contentRect.X + padding, contentRect.Y + padding, width, height),
            ContentAlignment.TopCenter => new Rectangle(contentRect.X + (contentRect.Width - width) / 2, contentRect.Y + padding, width, height),
            ContentAlignment.TopRight => new Rectangle(contentRect.Right - width - padding, contentRect.Y + padding, width, height),
            ContentAlignment.MiddleLeft => new Rectangle(contentRect.X + padding, y, width, height),
            ContentAlignment.MiddleCenter => new Rectangle(contentRect.X + (contentRect.Width - width) / 2, y, width, height),
            ContentAlignment.MiddleRight => new Rectangle(contentRect.Right - width - padding, y, width, height),
            ContentAlignment.BottomLeft => new Rectangle(contentRect.X + padding, contentRect.Bottom - height - padding, width, height),
            ContentAlignment.BottomCenter => new Rectangle(contentRect.X + (contentRect.Width - width) / 2, contentRect.Bottom - height - padding, width, height),
            ContentAlignment.BottomRight => new Rectangle(contentRect.Right - width - padding, contentRect.Bottom - height - padding, width, height),
            _ => new Rectangle(x, y, width, height)
        };
    }

    private Rectangle CalculateTextRectangle(Rectangle contentRect, Rectangle imageRect)
    {
        const int padding = 8;
        const int spacing = 4;

        if (imageRect.IsEmpty)
        {
            return contentRect;
        }

        return _imageAlign switch
        {
            ContentAlignment.TopLeft or ContentAlignment.MiddleLeft or ContentAlignment.BottomLeft =>
                new Rectangle(
                    imageRect.Right + spacing,
                    contentRect.Y,
                    contentRect.Width - imageRect.Width - spacing - padding * 2,
                    contentRect.Height),

            ContentAlignment.TopRight or ContentAlignment.MiddleRight or ContentAlignment.BottomRight =>
                new Rectangle(
                    contentRect.X + padding,
                    contentRect.Y,
                    contentRect.Width - imageRect.Width - spacing - padding * 2,
                    contentRect.Height),

            ContentAlignment.TopCenter =>
                new Rectangle(
                    contentRect.X,
                    imageRect.Bottom + spacing,
                    contentRect.Width,
                    contentRect.Height - imageRect.Height - spacing - padding),

            ContentAlignment.BottomCenter =>
                new Rectangle(
                    contentRect.X,
                    contentRect.Y + padding,
                    contentRect.Width,
                    contentRect.Height - imageRect.Height - spacing - padding),

            _ => contentRect // MiddleCenter - overlap text over image area
        };
    }

    private TextFormatFlags GetTextFormatFlags()
    {
        var flags = TextFormatFlags.EndEllipsis | TextFormatFlags.WordBreak;

        // Horizontal alignment - default to left for better icon+text layout
        if (_image == null || _imageAlign is ContentAlignment.TopCenter or ContentAlignment.MiddleCenter or ContentAlignment.BottomCenter)
        {
            // If no image or centered image, use left alignment for text with emoji icons
            flags |= TextFormatFlags.Left;
        }
        else if (_imageAlign is ContentAlignment.TopRight or ContentAlignment.MiddleRight or ContentAlignment.BottomRight)
        {
            flags |= TextFormatFlags.Right;
        }
        else
        {
            flags |= TextFormatFlags.Left;
        }

        // Vertical alignment
        flags |= TextFormatFlags.VerticalCenter;

        return flags;
    }
}
