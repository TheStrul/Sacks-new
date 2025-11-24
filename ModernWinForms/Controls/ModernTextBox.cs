using System.ComponentModel;
using System.Drawing.Drawing2D;
using ModernWinForms.Theming;

namespace ModernWinForms.Controls;

/// <summary>
/// Modern text box control with rounded corners and theme support.
/// </summary>
[ToolboxItem(true)]
[DesignerCategory("Code")]
public class ModernTextBox : Control
{
    private readonly TextBox _textBox;
    private ControlStyle _controlStyle = new();
    private int _padding = 10;

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
        
        _textBox.Enter += (s, e) => Invalidate();
        _textBox.Leave += (s, e) => Invalidate();
        _textBox.TextChanged += (s, e) => OnTextChanged(e);
        _textBox.KeyDown += (s, e) => OnKeyDown(e);

        Controls.Add(_textBox);
        
        Height = _textBox.Height + (_padding * 2);
        UpdateStyleFromSkin();
    }

    /// <summary>
    /// Gets or sets the text content of the text box.
    /// </summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public new string Text
    {
        get => _textBox.Text;
        set => _textBox.Text = value ?? "";
    }

    /// <summary>
    /// Gets or sets whether the text box is read-only.
    /// </summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public bool ReadOnly
    {
        get => _textBox.ReadOnly;
        set => _textBox.ReadOnly = value;
    }

    /// <summary>
    /// Gets or sets whether the text box is multiline.
    /// </summary>
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
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public string PlaceholderText
    {
        get => _textBox.PlaceholderText;
        set => _textBox.PlaceholderText = value;
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
        _controlStyle = new ControlStyle
        {
            BorderWidth = 1,
            CornerRadius = 4,
            States = new Dictionary<string, StateStyle>
            {
                ["normal"] = new() { BackColor = "#FFFFFF", ForeColor = "#0D1117", BorderColor = "#E0E6ED" },
                ["focused"] = new() { BorderColor = "#0969DA" }
            }
        };

        if (skin?.Controls != null && skin.Controls.TryGetValue("ModernTextBox", out var style))
        {
            _controlStyle = style;
        }
        else if (skin?.Palette != null)
        {
            _controlStyle.States["normal"].BackColor = skin.Palette.Surface;
            _controlStyle.States["normal"].ForeColor = skin.Palette.Text;
            _controlStyle.States["normal"].BorderColor = skin.Palette.Border;
            _controlStyle.States["focused"] = new StateStyle { BorderColor = skin.Palette.Primary };
        }

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

        var state = _textBox.Focused ? "focused" : "normal";
        var stateStyle = _controlStyle.States.GetValueOrDefault(state) ?? _controlStyle.States["normal"];
        var borderColor = ColorTranslator.FromHtml(stateStyle.BorderColor ?? "#CCCCCC");

        using (var pen = new Pen(borderColor, _controlStyle.BorderWidth))
        using (var path = GetRoundedPath(ClientRectangle, _controlStyle.CornerRadius))
        {
            using (var brush = new SolidBrush(BackColor))
            {
                g.FillPath(brush, path);
            }

            g.DrawPath(pen, path);
        }
    }

    private GraphicsPath GetRoundedPath(RectangleF rect, int radius)
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
}
