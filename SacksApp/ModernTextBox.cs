using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Drawing.Drawing2D;
using SacksApp.Theming;

namespace SacksApp;

public class ModernTextBox : Control
{
    private TextBox _textBox;
    private ControlStyle _controlStyle = new();
    private Color _borderColor = Color.Gray;
    private int _padding = 10;

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

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public new string Text
    {
        get => _textBox.Text;
        set => _textBox.Text = value ?? "";
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public bool ReadOnly
    {
        get => _textBox.ReadOnly;
        set => _textBox.ReadOnly = value;
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public bool Multiline
    {
        get => _textBox.Multiline;
        set
        {
            _textBox.Multiline = value;
            if (value)
            {
                Height = 100; // Default height for multiline
            }
            else
            {
                Height = _textBox.Height + (_padding * 2);
            }
        }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public new Font Font
    {
        get => _textBox.Font;
        set
        {
            if (_textBox != null) _textBox.Font = value;
        }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public new Color ForeColor
    {
        get => _textBox.ForeColor;
        set
        {
            if (_textBox != null) _textBox.ForeColor = value;
        }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public ScrollBars ScrollBars
    {
        get => _textBox.ScrollBars;
        set => _textBox.ScrollBars = value;
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public bool WordWrap
    {
        get => _textBox.WordWrap;
        set => _textBox.WordWrap = value;
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public string PlaceholderText
    {
        get => _textBox.PlaceholderText;
        set => _textBox.PlaceholderText = value;
    }

    public void ApplySkin(SkinDefinition skin)
    {
        UpdateStyleFromSkin(skin);
        Invalidate();
    }

    private void UpdateStyleFromSkin(SkinDefinition? skin = null)
    {
        // Default style
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
            // Fallback to palette
            _controlStyle.States["normal"].BackColor = skin.Palette.Surface;
            _controlStyle.States["normal"].ForeColor = skin.Palette.Text;
            _controlStyle.States["normal"].BorderColor = skin.Palette.Border;
            _controlStyle.States["focused"] = new StateStyle { BorderColor = skin.Palette.Primary };
        }

        // Apply colors to inner TextBox
        var normalState = _controlStyle.States["normal"];
        var backColor = ColorTranslator.FromHtml(normalState.BackColor ?? "#FFFFFF");
        var foreColor = ColorTranslator.FromHtml(normalState.ForeColor ?? "#000000");

        BackColor = backColor;
        _textBox.BackColor = backColor;
        _textBox.ForeColor = foreColor;
        
        // Update padding if specified
        if (_controlStyle.Padding != null)
        {
            _padding = _controlStyle.Padding.Left; // Simplified padding handling
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
            // Adjust path for border width
            var rect = new RectangleF(0.5f, 0.5f, Width - 1, Height - 1);
            
            // Draw background
            using (var brush = new SolidBrush(BackColor))
            {
                g.FillPath(brush, path);
            }

            // Draw border
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

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        if (_textBox != null)
        {
            _textBox.Width = Width - (_padding * 2);
            if (Multiline)
            {
                _textBox.Height = Height - (_padding * 2);
            }
        }
    }
}
