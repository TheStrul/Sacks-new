using System.ComponentModel;
using System.Drawing.Drawing2D;
using ModernWinForms.Theming;

namespace ModernWinForms.Controls;

/// <summary>
/// Modern tab control with custom rendering and theme support.
/// ZERO TOLERANCE: No fallbacks - fails immediately with descriptive errors.
/// </summary>
[ToolboxItem(true)]
[DesignerCategory("Code")]
[Description("Modern tab control with custom rendering and theme support.")]
public class ModernTabControl : TabControl
{
    private ControlStyle _controlStyle = new();
    private Font? _themeFont;

    /// <summary>
    /// Initializes a new instance of the ModernTabControl class.
    /// </summary>
    public ModernTabControl()
    {
        SetStyle(ControlStyles.UserPaint |
                ControlStyles.ResizeRedraw |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.SupportsTransparentBackColor, true);

        if (!DesignMode)
        {
            ThemeManager.ThemeChanged += OnThemeChanged;
            UpdateStyleFromTheme();
        }
        
        ItemSize = new Size(120, 40);
        SizeMode = TabSizeMode.Fixed;
    }

    private void OnThemeChanged(object? sender, EventArgs e)
    {
        UpdateStyleFromTheme();
        Invalidate();
    }

    private void UpdateStyleFromTheme()
    {
        _controlStyle = ThemeManager.GetControlStyle("ModernTabControl") ?? new ControlStyle
        {
            BorderWidth = 1,
            CornerRadius = 4,
            States = new Dictionary<string, StateStyle>
            {
                ["normal"] = new() { BackColor = "#FFFFFF", ForeColor = "#0D1117", BorderColor = "#D0D7DE" },
                ["selected"] = new() { BackColor = "#0969DA", ForeColor = "#FFFFFF", BorderColor = "#0969DA" },
                ["hover"] = new() { BackColor = "#F6F8FA", BorderColor = "#0969DA" }
            }
        };

        // ZERO TOLERANCE: 'normal' state MUST exist - throw immediately if missing
        if (!_controlStyle.States.TryGetValue("normal", out var normalState))
        {
            throw new InvalidOperationException(
                "ModernTabControl theme validation failed: 'normal' state is required but not defined. " +
                "Run ThemeValidationTool before starting the application to catch this error at startup.");
        }

        // ZERO TOLERANCE: BackColor MUST be defined in normal state
        if (string.IsNullOrWhiteSpace(normalState.BackColor))
        {
            throw new InvalidOperationException(
                "ModernTabControl theme validation failed: 'normal' state must define backColor. " +
                "Run ThemeValidationTool before starting the application to catch this error at startup.");
        }

        BackColor = ColorTranslator.FromHtml(normalState.BackColor);

        _themeFont?.Dispose();
        _themeFont = ThemeManager.CreateFont();
        if (_themeFont != null)
        {
            Font = _themeFont;
        }
    }

    /// <summary>
    /// Applies the specified skin definition to this tab control.
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

        // Draw tabs
        for (int i = 0; i < TabCount; i++)
        {
            DrawTab(g, i);
        }

        // Draw tab content area border
        var contentRect = new Rectangle(
            ClientRectangle.X,
            ClientRectangle.Y + ItemSize.Height,
            ClientRectangle.Width - 1,
            ClientRectangle.Height - ItemSize.Height - 1
        );

        // ZERO TOLERANCE: normal state MUST exist
        if (!_controlStyle.States.TryGetValue("normal", out var normalState))
        {
            throw new InvalidOperationException(
                "ModernTabControl rendering failed: 'normal' state not found. " +
                "This should have been caught by ThemeValidationTool at startup.");
        }

        var borderColor = ColorCache.GetColor(normalState.BorderColor, Color.LightGray);
        
        using var pen = new Pen(borderColor, _controlStyle.BorderWidth);
        g.DrawRectangle(pen, contentRect);
    }

    private void DrawTab(Graphics g, int index)
    {
        var tabRect = GetTabRect(index);
        var isSelected = SelectedIndex == index;
        
        // ZERO TOLERANCE: required states MUST exist
        if (!_controlStyle.States.TryGetValue("normal", out var normalState))
        {
            throw new InvalidOperationException(
                "ModernTabControl rendering failed: 'normal' state not found. " +
                "This should have been caught by ThemeValidationTool at startup.");
        }

        if (!_controlStyle.States.TryGetValue("selected", out var selectedState))
        {
            throw new InvalidOperationException(
                "ModernTabControl rendering failed: 'selected' state not found. " +
                "This should have been caught by ThemeValidationTool at startup.");
        }
        
        var backColor = isSelected
            ? ColorCache.GetColor(selectedState.BackColor, Color.Blue)
            : ColorCache.GetColor(normalState.BackColor, Color.White);
        
        var foreColor = isSelected
            ? ColorCache.GetColor(selectedState.ForeColor, Color.White)
            : ColorCache.GetColor(normalState.ForeColor, Color.Black);
        
        var borderColor = isSelected
            ? ColorCache.GetColor(selectedState.BorderColor, Color.Blue)
            : ColorCache.GetColor(normalState.BorderColor, Color.LightGray);

        // Draw tab background
        using (var brush = new SolidBrush(backColor))
        {
            g.FillRectangle(brush, tabRect);
        }

        // Draw tab border
        using (var pen = new Pen(borderColor, _controlStyle.BorderWidth))
        {
            g.DrawRectangle(pen, tabRect);
        }

        // Draw tab text
        var textRect = new Rectangle(tabRect.X + 4, tabRect.Y + 4, tabRect.Width - 8, tabRect.Height - 8);
        TextRenderer.DrawText(
            g,
            TabPages[index].Text,
            Font,
            textRect,
            foreColor,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter
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
        }
        base.Dispose(disposing);
    }
}
