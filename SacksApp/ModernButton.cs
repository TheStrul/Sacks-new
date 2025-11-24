using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Text.Json;
using SacksApp.Theming;

namespace SacksApp;

/// <summary>
/// Modern button control with configuration-driven theming and smooth rendering.
/// Supports themed states (normal, hover, pressed, disabled, focused) with explicit property overrides.
/// </summary>
public sealed class ModernButton : Button
{
    #region Fields

    private string _themeName = "Light";
    private ButtonState _currentState = ButtonState.Normal;
    private ThemeConfiguration? _themeConfig;
    
    // Explicit property overrides (nullable = use theme if null)
    private Color? _backColorOverride;
    private Color? _foreColorOverride;
    private Color? _borderColorOverride;
    private int? _borderWidthOverride;
    private int? _cornerRadiusOverride;

    private Image? _icon;
    private ContentAlignment _iconAlignment = ContentAlignment.MiddleLeft;

    #endregion

    #region Properties

    [Category("Appearance")]
    [Description("Theme preset name (Light, Dark, Primary, Secondary, Success, Danger)")]
    [DefaultValue("Light")]
    public string Theme
    {
        get => _themeName;
        set
        {
            if (_themeName != value)
            {
                _themeName = value;
                Invalidate();
            }
        }
    }

    [Category("Appearance")]
    [Description("Corner radius for rounded button")]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public int CornerRadius
    {
        get => _cornerRadiusOverride ?? GetThemeDefaults().CornerRadius;
        set
        {
            _cornerRadiusOverride = value;
            Invalidate();
        }
    }

    [Category("Appearance")]
    [Description("Icon image to display on button")]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public Image? Icon
    {
        get => _icon;
        set
        {
            _icon = value;
            Invalidate();
        }
    }

    [Category("Appearance")]
    [Description("Icon alignment (Left or Right)")]
    [DefaultValue(ContentAlignment.MiddleLeft)]
    public ContentAlignment IconAlignment
    {
        get => _iconAlignment;
        set
        {
            _iconAlignment = value;
            Invalidate();
        }
    }

    // Override base properties to support theme fallback
    public override Color BackColor
    {
        get => _backColorOverride ?? GetCurrentStyle().BackColorValue;
        set
        {
            _backColorOverride = value;
            Invalidate();
        }
    }

    public override Color ForeColor
    {
        get => _foreColorOverride ?? GetCurrentStyle().ForeColorValue;
        set
        {
            _foreColorOverride = value;
            Invalidate();
        }
    }

    [Category("Appearance")]
    [Description("Border color for button")]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public Color BorderColor
    {
        get => _borderColorOverride ?? GetCurrentStyle().BorderColorValue;
        set
        {
            _borderColorOverride = value;
            Invalidate();
        }
    }

    [Category("Appearance")]
    [Description("Border width in pixels")]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public int BorderWidth
    {
        get => _borderWidthOverride ?? GetCurrentStyle().BorderWidth ?? 1;
        set
        {
            _borderWidthOverride = value;
            Invalidate();
        }
    }

    #endregion

    #region Constructor

    public ModernButton()
    {
        // Load theme configuration
        LoadThemeConfiguration();

        // Configure for custom rendering
        SetStyle(ControlStyles.UserPaint |
                 ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.ResizeRedraw |
                 ControlStyles.SupportsTransparentBackColor, true);

        // Set defaults
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0; // We draw our own border
    }

    #endregion

    #region Theme Loading

    private void LoadThemeConfiguration()
    {
        try
        {
            var configPath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Configuration",
                "button-theme.json"
            );

            if (File.Exists(configPath))
            {
                var json = File.ReadAllText(configPath);
                _themeConfig = JsonSerializer.Deserialize<ThemeConfiguration>(json);
            }
        }
        catch
        {
            // Fallback to defaults if config load fails
            _themeConfig = null;
        }
    }

    private ThemeDefaults GetThemeDefaults()
    {
        return _themeConfig?.Defaults ?? new ThemeDefaults
        {
            CornerRadius = 8,
            IconSize = 16,
            IconSpacing = 8,
            Padding = new() { Left = 16, Top = 10, Right = 16, Bottom = 10 }
        };
    }

    private ButtonThemeStyle GetTheme()
    {
        if (_themeConfig?.Themes != null && _themeConfig.Themes.TryGetValue(_themeName, out var theme))
        {
            return theme;
        }

        // Fallback to Light theme
        return new ButtonThemeStyle
        {
            Normal = new() { BackColor = "#FFFFFF", ForeColor = "#212529", BorderColor = "#DEE2E6", BorderWidth = 1 },
            Hover = new() { BackColor = "#F8F9FA", ForeColor = "#212529", BorderColor = "#CED4DA", BorderWidth = 1 },
            Pressed = new() { BackColor = "#E9ECEF", ForeColor = "#212529", BorderColor = "#ADB5BD", BorderWidth = 1 },
            Disabled = new() { BackColor = "#F8F9FA", ForeColor = "#ADB5BD", BorderColor = "#E9ECEF", BorderWidth = 1 },
            Focused = new() { BorderColor = "#0D6EFD", BorderWidth = 2 }
        };
    }

    private ButtonStateStyle GetCurrentStyle()
    {
        var theme = GetTheme();
        var baseStyle = _currentState switch
        {
            ButtonState.Disabled => theme.Disabled,
            ButtonState.Pressed => theme.Pressed,
            ButtonState.Hover => theme.Hover,
            ButtonState.Focused => theme.Focused,
            _ => theme.Normal
        };

        // Merge focused style with current state if focused
        if (Focused && _currentState != ButtonState.Focused)
        {
            return new ButtonStateStyle
            {
                BackColor = baseStyle.BackColor,
                ForeColor = baseStyle.ForeColor,
                BorderColor = theme.Focused.BorderColor ?? baseStyle.BorderColor,
                BorderWidth = theme.Focused.BorderWidth ?? baseStyle.BorderWidth
            };
        }

        return baseStyle;
    }

    #endregion

    #region Event Handlers

    protected override void OnEnabledChanged(EventArgs e)
    {
        base.OnEnabledChanged(e);
        _currentState = Enabled ? ButtonState.Normal : ButtonState.Disabled;
        Invalidate();
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        base.OnMouseEnter(e);
        if (Enabled)
        {
            _currentState = ButtonState.Hover;
            Invalidate();
        }
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        if (Enabled)
        {
            _currentState = Focused ? ButtonState.Focused : ButtonState.Normal;
            Invalidate();
        }
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        base.OnMouseDown(e);
        if (Enabled && e.Button == MouseButtons.Left)
        {
            _currentState = ButtonState.Pressed;
            Invalidate();
        }
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        base.OnMouseUp(e);
        if (Enabled)
        {
            _currentState = ClientRectangle.Contains(e.Location) ? ButtonState.Hover : ButtonState.Normal;
            Invalidate();
        }
    }

    protected override void OnGotFocus(EventArgs e)
    {
        base.OnGotFocus(e);
        if (Enabled && _currentState == ButtonState.Normal)
        {
            _currentState = ButtonState.Focused;
        }
        Invalidate();
    }

    protected override void OnLostFocus(EventArgs e)
    {
        base.OnLostFocus(e);
        if (Enabled && _currentState == ButtonState.Focused)
        {
            _currentState = ButtonState.Normal;
        }
        Invalidate();
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        Invalidate();
    }

    #endregion

    #region Rendering

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        // Handled in OnPaint due to AllPaintingInWmPaint
    }



    protected override void OnPaint(PaintEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        var g = e.Graphics;
        
        // High-quality rendering
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        // 1. Manually paint the "corners" with the parent's background color
        // This eliminates black artifacts by ensuring the area behind the rounded corners
        // matches the parent container's background.
        var parentColor = GetEffectiveBackColor();
        using (var parentBrush = new SolidBrush(parentColor))
        {
            g.FillRectangle(parentBrush, ClientRectangle);
        }

        var rect = ClientRectangle;
        var radius = CornerRadius;

        // 2. Draw the rounded button background
        using (var path = CreateRoundedRectPath(rect, radius))
        using (var brush = new SolidBrush(BackColor))
        {
            g.FillPath(brush, path);
        }

        // 3. Draw border
        if (BorderWidth > 0)
        {
            // Inset rectangle for border to prevent clipping
            var borderRect = new Rectangle(
                rect.X + BorderWidth / 2,
                rect.Y + BorderWidth / 2,
                rect.Width - BorderWidth,
                rect.Height - BorderWidth
            );

            using var path = CreateRoundedRectPath(borderRect, radius - BorderWidth / 2);
            using var pen = new Pen(BorderColor, BorderWidth);
            g.DrawPath(pen, path);
        }

        // 4. Draw content (icon + text)
        DrawContent(g);
    }

    private Color GetEffectiveBackColor()
    {
        var current = Parent;
        while (current != null)
        {
            var color = current.BackColor;
            // Return the first opaque color found
            if (color.A == 255 && color != Color.Transparent)
            {
                return color;
            }
            current = current.Parent;
        }
        // Fallback to form background or control default
        return FindForm()?.BackColor ?? SystemColors.Control;
    }

    private void DrawContent(Graphics g)
    {
        var rect = ClientRectangle;
        var defaults = GetThemeDefaults();
        
        // Calculate content area
        var contentRect = new Rectangle(
            rect.X + Padding.Left,
            rect.Y + Padding.Top,
            rect.Width - Padding.Horizontal,
            rect.Height - Padding.Vertical
        );

        var iconSize = defaults.IconSize;
        var iconSpacing = defaults.IconSpacing;

        // Measure text
        var textSize = TextRenderer.MeasureText(g, Text ?? string.Empty, Font);

        // Calculate total content width (icon + spacing + text)
        var totalWidth = (Icon != null ? iconSize + iconSpacing : 0) + textSize.Width;
        
        // Starting X position (centered)
        var startX = contentRect.X + (contentRect.Width - totalWidth) / 2;
        var centerY = contentRect.Y + (contentRect.Height - Math.Max(iconSize, textSize.Height)) / 2;

        // Draw icon
        if (Icon != null)
        {
            var iconX = _iconAlignment == ContentAlignment.MiddleLeft ? startX : startX + textSize.Width + iconSpacing;
            var iconY = centerY + (textSize.Height - iconSize) / 2;
            var iconRect = new Rectangle(iconX, iconY, iconSize, iconSize);
            
            g.DrawImage(Icon, iconRect);
        }

        // Draw text
        var textX = Icon != null && _iconAlignment == ContentAlignment.MiddleLeft 
            ? startX + iconSize + iconSpacing 
            : startX;
        var textRect = new Rectangle(textX, centerY, textSize.Width, textSize.Height);
        
        TextRenderer.DrawText(g, Text, Font, textRect, ForeColor, TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
    }
    private static GraphicsPath CreateRoundedRectPath(Rectangle rect, int radius)
    {
        var path = new GraphicsPath();
        
        if (radius <= 0)
        {
            path.AddRectangle(rect);
            return path;
        }

        var diameter = radius * 2;
        var arc = new Rectangle(rect.Location, new Size(diameter, diameter));

        // Top left arc
        path.AddArc(arc, 180, 90);

        // Top right arc
        arc.X = rect.Right - diameter;
        path.AddArc(arc, 270, 90);

        // Bottom right arc
        arc.Y = rect.Bottom - diameter;
        path.AddArc(arc, 0, 90);

        // Bottom left arc
        arc.X = rect.Left;
        path.AddArc(arc, 90, 90);

        path.CloseFigure();
        return path;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Region?.Dispose();
            _icon?.Dispose();
        }
        base.Dispose(disposing);
    }

    #endregion

    #region Theme Configuration Classes

    private sealed class ThemeConfiguration
    {
        [System.Text.Json.Serialization.JsonPropertyName("version")]
        public string Version { get; set; } = "2.0";

        [System.Text.Json.Serialization.JsonPropertyName("defaults")]
        public ThemeDefaults Defaults { get; set; } = new();

        [System.Text.Json.Serialization.JsonPropertyName("rendering")]
        public RenderingOptions? Rendering { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("themes")]
        public Dictionary<string, ButtonThemeStyle>? Themes { get; set; }
    }

    private sealed class ThemeDefaults
    {
        [System.Text.Json.Serialization.JsonPropertyName("cornerRadius")]
        public int CornerRadius { get; set; } = 8;

        [System.Text.Json.Serialization.JsonPropertyName("iconSize")]
        public int IconSize { get; set; } = 16;

        [System.Text.Json.Serialization.JsonPropertyName("iconSpacing")]
        public int IconSpacing { get; set; } = 8;

        [System.Text.Json.Serialization.JsonPropertyName("padding")]
        public PaddingConfig Padding { get; set; } = new();
    }

    private sealed class PaddingConfig
    {
        [System.Text.Json.Serialization.JsonPropertyName("left")]
        public int Left { get; set; } = 16;

        [System.Text.Json.Serialization.JsonPropertyName("top")]
        public int Top { get; set; } = 10;

        [System.Text.Json.Serialization.JsonPropertyName("right")]
        public int Right { get; set; } = 16;

        [System.Text.Json.Serialization.JsonPropertyName("bottom")]
        public int Bottom { get; set; } = 10;
    }

    private sealed class RenderingOptions
    {
        [System.Text.Json.Serialization.JsonPropertyName("antiAlias")]
        public bool AntiAlias { get; set; } = true;

        [System.Text.Json.Serialization.JsonPropertyName("highQuality")]
        public bool HighQuality { get; set; } = true;

        [System.Text.Json.Serialization.JsonPropertyName("doubleBuffer")]
        public bool DoubleBuffer { get; set; } = true;
    }

    private sealed class ButtonThemeStyle
    {
        [System.Text.Json.Serialization.JsonPropertyName("normal")]
        public ButtonStateStyle Normal { get; set; } = new();

        [System.Text.Json.Serialization.JsonPropertyName("hover")]
        public ButtonStateStyle Hover { get; set; } = new();

        [System.Text.Json.Serialization.JsonPropertyName("pressed")]
        public ButtonStateStyle Pressed { get; set; } = new();

        [System.Text.Json.Serialization.JsonPropertyName("disabled")]
        public ButtonStateStyle Disabled { get; set; } = new();

        [System.Text.Json.Serialization.JsonPropertyName("focused")]
        public ButtonStateStyle Focused { get; set; } = new();
    }

    private sealed class ButtonStateStyle
    {
        [System.Text.Json.Serialization.JsonPropertyName("backColor")]
        public string? BackColor { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("foreColor")]
        public string? ForeColor { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("borderColor")]
        public string? BorderColor { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("borderWidth")]
        public int? BorderWidth { get; set; }

        public Color BackColorValue => ParseColor(BackColor, Color.White);
        public Color ForeColorValue => ParseColor(ForeColor, Color.Black);
        public Color BorderColorValue => ParseColor(BorderColor, Color.Gray);

        private static Color ParseColor(string? hex, Color fallback)
        {
            if (string.IsNullOrWhiteSpace(hex)) return fallback;
            
            try
            {
                hex = hex.TrimStart('#');
                if (hex.Length == 6)
                {
                    return Color.FromArgb(
                        Convert.ToInt32(hex.Substring(0, 2), 16),
                        Convert.ToInt32(hex.Substring(2, 2), 16),
                        Convert.ToInt32(hex.Substring(4, 2), 16)
                    );
                }
            }
            catch { }
            
            return fallback;
        }
    }

    private enum ButtonState
    {
        Normal,
        Hover,
        Pressed,
        Disabled,
        Focused
    }

    #endregion
}
