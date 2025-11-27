using System.ComponentModel;
using ModernWinForms.Theming;

namespace ModernWinForms.Controls;

/// <summary>
/// Modern table layout panel control with theme support.
/// </summary>
[ToolboxItem(true)]
[DesignerCategory("Code")]
[Description("Modern table layout panel control with theme support.")]
public class ModernTableLayoutPanel : TableLayoutPanel
{
    /// <summary>
    /// Initializes a new instance of the ModernTableLayoutPanel class.
    /// </summary>
    public ModernTableLayoutPanel()
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
        var controlStyle = ThemeManager.GetControlStyle("ModernTableLayoutPanel");

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
            // Default modern styling - transparent to inherit from parent
            BackColor = Color.Transparent;
        }
    }

    /// <summary>
    /// Applies the specified skin definition to this table layout panel.
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
        }
        base.Dispose(disposing);
    }
}
