using System.ComponentModel;
using ModernWinForms.Theming;

namespace ModernWinForms.Controls;

/// <summary>
/// Modern status strip control with theme support.
/// </summary>
[ToolboxItem(true)]
[DesignerCategory("Code")]
[Description("Modern status strip control with theme support.")]
public class ModernStatusStrip : StatusStrip
{
    private Font? _themeFont;
    /// <summary>
    /// Initializes a new instance of the ModernStatusStrip class.
    /// </summary>
    public ModernStatusStrip()
    {
        if (!DesignMode)
        {
            ThemeManager.ThemeChanged += OnThemeChanged;
            UpdateStyleFromTheme();
        }
    }

    private void OnThemeChanged(object? sender, EventArgs e)
    {
        UpdateStyleFromTheme();
        Invalidate();
    }

    private void UpdateStyleFromTheme()
    {
        var controlStyle = ThemeManager.GetControlStyle("ModernStatusStrip");

        if (controlStyle != null && controlStyle.States.TryGetValue("normal", out var normalState))
        {
            if (normalState.BackColor != null)
            {
                BackColor = ColorTranslator.FromHtml(normalState.BackColor);
            }

            if (normalState.ForeColor != null)
            {
                ForeColor = ColorTranslator.FromHtml(normalState.ForeColor);
            }
        }
        else
        {
            // Default modern styling
            BackColor = Color.FromArgb(246, 248, 250);
            ForeColor = Color.FromArgb(13, 17, 23);
            RenderMode = ToolStripRenderMode.Professional;
        }

        _themeFont?.Dispose();
        _themeFont = ThemeManager.CreateFont();
        if (_themeFont != null)
        {
            Font = _themeFont;
        }
    }

    /// <summary>
    /// Applies the specified skin definition to this status strip.
    /// </summary>
    /// <param name="skin">The skin definition to apply.</param>
    public void ApplySkin(SkinDefinition skin)
    {
        UpdateStyleFromTheme();
        Invalidate();
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            ThemeManager.ThemeChanged -= OnThemeChanged;
            _themeFont?.Dispose();
        }
        base.Dispose(disposing);
    }
}
