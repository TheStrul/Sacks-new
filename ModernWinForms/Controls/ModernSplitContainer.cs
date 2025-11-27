using System.ComponentModel;
using ModernWinForms.Theming;

namespace ModernWinForms.Controls;

/// <summary>
/// Modern split container control with theme support.
/// </summary>
[ToolboxItem(true)]
[DesignerCategory("Code")]
[Description("Modern split container control with theme support.")]
public class ModernSplitContainer : SplitContainer
{
    /// <summary>
    /// Initializes a new instance of the ModernSplitContainer class.
    /// </summary>
    public ModernSplitContainer()
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
        var controlStyle = ThemeManager.GetControlStyle("ModernSplitContainer");

        if (controlStyle != null && controlStyle.States.TryGetValue("normal", out var normalState))
        {
            if (normalState.BackColor != null)
            {
                BackColor = ColorTranslator.FromHtml(normalState.BackColor);
                Panel1.BackColor = BackColor;
                Panel2.BackColor = BackColor;
            }

            if (normalState.BorderColor != null)
            {
                var borderColor = ColorTranslator.FromHtml(normalState.BorderColor);
                // Set splitter appearance
                if (borderColor != Color.Empty)
                {
                    SplitterWidth = Math.Max(1, controlStyle.BorderWidth);
                }
            }
        }
        else
        {
            // Default modern styling
            BackColor = Color.FromArgb(246, 248, 250);
            Panel1.BackColor = Color.White;
            Panel2.BackColor = Color.White;
            BorderStyle = BorderStyle.None;
            SplitterWidth = 4;
        }
    }

    /// <summary>
    /// Applies the specified skin definition to this split container.
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
        base.OnPaint(e);
        
        // Custom splitter rendering if needed
        var controlStyle = ThemeManager.GetControlStyle("ModernSplitContainer");
        if (controlStyle?.States.TryGetValue("normal", out var normalState) == true && 
            normalState.BorderColor != null)
        {
            var splitterColor = ColorTranslator.FromHtml(normalState.BorderColor);
            using var brush = new SolidBrush(splitterColor);
            e.Graphics.FillRectangle(brush, SplitterRectangle);
        }
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
