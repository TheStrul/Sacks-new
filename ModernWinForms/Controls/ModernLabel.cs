using System.ComponentModel;
using ModernWinForms.Theming;

namespace ModernWinForms.Controls;

/// <summary>
/// Modern label control with theme support.
/// </summary>
[ToolboxItem(true)]
[DesignerCategory("Code")]
[Description("Modern label control with theme support.")]
public class ModernLabel : Label
{
    private Font? _themeFont;

    /// <summary>
    /// Initializes a new instance of the ModernLabel class.
    /// </summary>
    public ModernLabel()
    {
        ThemeManager.ThemeChanged += OnThemeChanged;
        UpdateStyleFromTheme();
    }

    private void OnThemeChanged(object? sender, EventArgs e)
    {
        UpdateStyleFromTheme();
        Invalidate();
    }

    private void UpdateStyleFromTheme()
    {
        var controlStyle = ThemeManager.GetControlStyle("ModernLabel");
        
        if (controlStyle != null && controlStyle.States.TryGetValue("normal", out var normalState))
        {
            if (normalState.ForeColor != null)
            {
                ForeColor = ColorTranslator.FromHtml(normalState.ForeColor);
            }
            
            if (normalState.BackColor != null)
            {
                BackColor = ColorTranslator.FromHtml(normalState.BackColor);
            }
        }

        _themeFont?.Dispose();
        _themeFont = ThemeManager.CreateFont();
        if (_themeFont != null)
        {
            Font = _themeFont;
        }
    }

    /// <summary>
    /// Applies the specified skin definition to this label.
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
            _themeFont = null;
        }
        base.Dispose(disposing);
    }
}
