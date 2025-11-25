using System.ComponentModel;
using ModernWinForms.Theming;

namespace ModernWinForms.Controls;

/// <summary>
/// Modern menu strip control with theme support.
/// </summary>
[ToolboxItem(true)]
[DesignerCategory("Code")]
[Description("Modern menu strip control with theme support.")]
public class ModernMenuStrip : MenuStrip
{
    /// <summary>
    /// Initializes a new instance of the ModernMenuStrip class.
    /// </summary>
    public ModernMenuStrip()
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
        var controlStyle = ThemeManager.GetControlStyle("ModernMenuStrip");

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

        var themeFont = ThemeManager.CreateFont();
        if (themeFont != null)
        {
            Font = themeFont;
            themeFont.Dispose();
        }

        // Apply custom renderer for modern look
        Renderer = new ModernMenuStripRenderer();
    }

    /// <summary>
    /// Applies the specified skin definition to this menu strip.
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

/// <summary>
/// Custom renderer for modern menu strip styling.
/// </summary>
internal sealed class ModernMenuStripRenderer : ToolStripProfessionalRenderer
{
    public ModernMenuStripRenderer() : base(new ModernMenuColorTable())
    {
        RoundedEdges = false;
    }
}

/// <summary>
/// Custom color table for modern menu strip.
/// </summary>
internal sealed class ModernMenuColorTable : ProfessionalColorTable
{
    public override Color MenuItemSelected => Color.FromArgb(221, 226, 235);
    public override Color MenuItemSelectedGradientBegin => Color.FromArgb(221, 226, 235);
    public override Color MenuItemSelectedGradientEnd => Color.FromArgb(221, 226, 235);
    public override Color MenuItemBorder => Color.FromArgb(208, 215, 222);
    public override Color MenuItemPressedGradientBegin => Color.FromArgb(208, 215, 222);
    public override Color MenuItemPressedGradientMiddle => Color.FromArgb(208, 215, 222);
    public override Color MenuItemPressedGradientEnd => Color.FromArgb(208, 215, 222);
    public override Color ToolStripDropDownBackground => Color.White;
    public override Color ImageMarginGradientBegin => Color.FromArgb(246, 248, 250);
    public override Color ImageMarginGradientMiddle => Color.FromArgb(246, 248, 250);
    public override Color ImageMarginGradientEnd => Color.FromArgb(246, 248, 250);
}
