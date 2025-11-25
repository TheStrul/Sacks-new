using System.ComponentModel;
using System.Drawing.Drawing2D;
using ModernWinForms.Animation;
using ModernWinForms.Theming;
using ModernWinForms.Validation;

namespace ModernWinForms.Controls;

/// <summary>
/// Modern text box control with rounded corners, validation, and theme support.
/// </summary>
[ToolboxItem(true)]
[DesignerCategory("Code")]
[Description("Modern text box control with rounded corners, validation, and theme support.")]
public class ModernTextBox : Control, IValidatable
{
    private readonly TextBox _textBox;
    private ControlStyle _controlStyle = new();
    private int _padding = 10;
    private GraphicsPath? _cachedPath;
    private Size _cachedSize;
    private int _cachedRadius;
    private Font? _themeFont;
    private AnimationEngine? _focusAnimation;
    private double _focusProgress;
    private ValidationState _validationState = ValidationState.None;
    private string _validationMessage = string.Empty;
    private bool _showValidationIcon = true;

    /// <summary>
    /// Initializes a new instance of the ModernTextBox class.
    /// </summary>
    public ModernTextBox()
    {
        SetStyle(ControlStyles.UserPaint | 
                ControlStyles.ResizeRedraw | 
                ControlStyles.OptimizedDoubleBuffer | 
                ControlStyles.SupportsTransparentBackColor, true);
        
        _textBox = new TextBox
        {
            BorderStyle = BorderStyle.None,
            Location = new Point(_padding, _padding),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom
        };
        
        _textBox.Enter += OnInnerTextBoxEnter;
        _textBox.Leave += OnInnerTextBoxLeave;
        _textBox.TextChanged += (s, e) => OnTextChanged(e);
        _textBox.KeyDown += (s, e) => OnKeyDown(e);

        Controls.Add(_textBox);
        
        _focusAnimation = new AnimationEngine(this);
        ThemeManager.ThemeChanged += OnThemeChanged;
        Height = _textBox.Height + (_padding * 2);
        UpdateStyleFromSkin();
        UpdateFontFromTheme();
    }

    private void OnInnerTextBoxEnter(object? sender, EventArgs e)
    {
        _focusAnimation?.Animate(1.0, 200, value => _focusProgress = value);
    }

    private void OnInnerTextBoxLeave(object? sender, EventArgs e)
    {
        _focusAnimation?.Animate(0.0, 200, value => _focusProgress = value);
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
            _textBox.Font = _themeFont;
        }
    }

    /// <summary>
    /// Gets or sets the text content of the text box.
    /// </summary>
    [Category("Appearance")]
    [Description("The text content of the text box.")]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public new string Text
    {
        get => _textBox.Text;
        set => _textBox.Text = value ?? "";
    }

    /// <summary>
    /// Gets or sets whether the text box is read-only.
    /// </summary>
    [Category("Behavior")]
    [Description("Indicates whether the text box is read-only.")]
    [DefaultValue(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public bool ReadOnly
    {
        get => _textBox.ReadOnly;
        set => _textBox.ReadOnly = value;
    }

    /// <summary>
    /// Gets or sets whether the text box is multiline.
    /// </summary>
    [Category("Behavior")]
    [Description("Indicates whether the text box supports multiple lines of text.")]
    [DefaultValue(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public bool Multiline
    {
        get => _textBox.Multiline;
        set
        {
            _textBox.Multiline = value;
            Height = value ? 100 : _textBox.Height + (_padding * 2);
        }
    }

    /// <summary>
    /// Gets or sets the font of the text box.
    /// </summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public new Font Font
    {
        get => _textBox.Font;
        set => _textBox.Font = value;
    }

    /// <summary>
    /// Gets or sets the foreground color of the text box.
    /// </summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public new Color ForeColor
    {
        get => _textBox.ForeColor;
        set => _textBox.ForeColor = value;
    }

    /// <summary>
    /// Gets or sets the scroll bars to display in the text box.
    /// </summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public ScrollBars ScrollBars
    {
        get => _textBox.ScrollBars;
        set => _textBox.ScrollBars = value;
    }

    /// <summary>
    /// Gets or sets whether word wrap is enabled.
    /// </summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public bool WordWrap
    {
        get => _textBox.WordWrap;
        set => _textBox.WordWrap = value;
    }

    /// <summary>
    /// Gets or sets the placeholder text displayed when the text box is empty.
    /// </summary>
    [Category("Appearance")]
    [Description("The placeholder text displayed when the text box is empty.")]
    [DefaultValue("")]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public string PlaceholderText
    {
        get => _textBox.PlaceholderText;
        set => _textBox.PlaceholderText = value;
    }

    /// <summary>
    /// Gets or sets the validation state.
    /// </summary>
    [Category("Validation")]
    [Description("The current validation state of the control.")]
    [DefaultValue(ValidationState.None)]
    public ValidationState ValidationState
    {
        get => _validationState;
        set
        {
            if (_validationState != value)
            {
                _validationState = value;
                Invalidate();
            }
        }
    }

    /// <summary>
    /// Gets or sets the validation message.
    /// </summary>
    [Category("Validation")]
    [Description("The validation message to display.")]
    [DefaultValue("")]
    public string ValidationMessage
    {
        get => _validationMessage;
        set
        {
            if (_validationMessage != value)
            {
                _validationMessage = value;
                Invalidate();
            }
        }
    }

    /// <summary>
    /// Gets or sets whether to show validation icon.
    /// </summary>
    [Category("Validation")]
    [Description("Indicates whether to show validation icon.")]
    [DefaultValue(true)]
    public bool ShowValidationIcon
    {
        get => _showValidationIcon;
        set
        {
            if (_showValidationIcon != value)
            {
                _showValidationIcon = value;
                Invalidate();
            }
        }
    }

    /// <summary>
    /// Applies the specified skin definition to this text box.
    /// </summary>
    /// <param name="skin">The skin definition to apply.</param>
    public void ApplySkin(SkinDefinition skin)
    {
        UpdateStyleFromSkin(skin);
        Invalidate();
    }

    private void UpdateStyleFromSkin(SkinDefinition? skin = null)
    {
        // Get merged style from theme (structure) + skin (colors)
        _controlStyle = ThemeManager.GetControlStyle("ModernTextBox") ?? new ControlStyle
        {
            BorderWidth = 1,
            CornerRadius = 4,
            States = new Dictionary<string, StateStyle>
            {
                ["normal"] = new() { BackColor = "#FFFFFF", ForeColor = "#0D1117", BorderColor = "#E0E6ED" },
                ["focused"] = new() { BorderColor = "#0969DA" },
                ["error"] = new() { BorderColor = "#DC3545" },
                ["warning"] = new() { BorderColor = "#FFC107" },
                ["success"] = new() { BorderColor = "#28A745" }
            }
        };

        var normalState = _controlStyle.States["normal"];
        var backColor = ColorTranslator.FromHtml(normalState.BackColor ?? "#FFFFFF");
        var foreColor = ColorTranslator.FromHtml(normalState.ForeColor ?? "#000000");

        BackColor = backColor;
        _textBox.BackColor = backColor;
        _textBox.ForeColor = foreColor;
        
        if (_controlStyle.Padding != null)
        {
            _padding = _controlStyle.Padding.Left;
            _textBox.Location = new Point(_padding, _padding);
            _textBox.Width = Width - (_padding * 2);
            if (!Multiline)
            {
                Height = _textBox.Height + (_padding * 2);
            }
            else
            {
                _textBox.Height = Height - (_padding * 2);
            }
        }
    }

    /// <inheritdoc/>
    protected override void OnPaint(PaintEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        // Determine border color based on validation state or focus
        var normalStyle = _controlStyle.States.GetValueOrDefault("normal") 
            ?? new StateStyle { BorderColor = "#CCCCCC" };
        var normalBorderColor = ColorCache.GetColor(normalStyle.BorderColor, Color.LightGray);
        
        var borderColor = normalBorderColor;
        
        if (_validationState != ValidationState.None)
        {
            var validationStateName = _validationState.ToString().ToLowerInvariant();
            var validationStyle = _controlStyle.States.GetValueOrDefault(validationStateName);
            if (validationStyle?.BorderColor != null)
            {
                borderColor = ColorCache.GetColor(validationStyle.BorderColor, normalBorderColor);
            }
        }
        else if (_textBox.Focused)
        {
            var focusedStyle = _controlStyle.States.GetValueOrDefault("focused");
            if (focusedStyle?.BorderColor != null)
            {
                var focusedBorderColor = ColorCache.GetColor(focusedStyle.BorderColor, normalBorderColor);
                borderColor = BlendColors(normalBorderColor, focusedBorderColor, _focusProgress);
            }
        }

        var path = GetOrCreateCachedPath(ClientRectangle, _controlStyle.CornerRadius);
        using (var pen = new Pen(borderColor, _controlStyle.BorderWidth))
        {
            using (var brush = new SolidBrush(BackColor))
            {
                g.FillPath(brush, path);
            }

            g.DrawPath(pen, path);
        }

        // Draw validation icon if needed
        if (_showValidationIcon && _validationState != ValidationState.None)
        {
            DrawValidationIcon(g);
        }
    }

    private void DrawValidationIcon(Graphics g)
    {
        const int iconSize = 16;
        const int margin = 8;
        var iconRect = new Rectangle(Width - iconSize - margin, (Height - iconSize) / 2, iconSize, iconSize);
        
        Color iconColor = _validationState switch
        {
            ValidationState.Success => Color.FromArgb(40, 167, 69),
            ValidationState.Warning => Color.FromArgb(255, 193, 7),
            ValidationState.Error => Color.FromArgb(220, 53, 69),
            _ => Color.Gray
        };
        
        using var pen = new Pen(iconColor, 2);
        
        switch (_validationState)
        {
            case ValidationState.Success:
                // Checkmark
                g.DrawLines(pen, [
                    new Point(iconRect.X + 3, iconRect.Y + 8),
                    new Point(iconRect.X + 6, iconRect.Y + 11),
                    new Point(iconRect.X + 13, iconRect.Y + 4)
                ]);
                break;
            case ValidationState.Warning:
                // Exclamation mark
                g.DrawLine(pen, iconRect.X + 8, iconRect.Y + 3, iconRect.X + 8, iconRect.Y + 10);
                g.DrawLine(pen, iconRect.X + 8, iconRect.Y + 12, iconRect.X + 8, iconRect.Y + 13);
                break;
            case ValidationState.Error:
                // X mark
                g.DrawLine(pen, iconRect.Left + 3, iconRect.Top + 3, iconRect.Right - 3, iconRect.Bottom - 3);
                g.DrawLine(pen, iconRect.Right - 3, iconRect.Top + 3, iconRect.Left + 3, iconRect.Bottom - 3);
                break;
        }
    }

    private static Color BlendColors(Color from, Color to, double progress)
    {
        progress = Math.Clamp(progress, 0, 1);
        var r = (int)(from.R + ((to.R - from.R) * progress));
        var g = (int)(from.G + ((to.G - from.G) * progress));
        var b = (int)(from.B + ((to.B - from.B) * progress));
        return Color.FromArgb(r, g, b);
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

    private static GraphicsPath CreateRoundedPath(RectangleF rect, int radius)
    {
        var path = new GraphicsPath();
        if (radius <= 0)
        {
            path.AddRectangle(rect);
            return path;
        }

        float d = radius * 2;
        path.AddArc(rect.X, rect.Y, d, d, 180, 90);
        path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
        path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
        path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }

    /// <inheritdoc/>
    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        _textBox.Width = Width - (_padding * 2);
        if (Multiline)
        {
            _textBox.Height = Height - (_padding * 2);
        }
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _textBox.Enter -= OnInnerTextBoxEnter;
            _textBox.Leave -= OnInnerTextBoxLeave;
            ThemeManager.ThemeChanged -= OnThemeChanged;
            _cachedPath?.Dispose();
            _cachedPath = null;
            _themeFont?.Dispose();
            _themeFont = null;
            _focusAnimation?.Dispose();
            _focusAnimation = null;
        }
        base.Dispose(disposing);
    }
}
