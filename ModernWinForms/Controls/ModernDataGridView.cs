using System.ComponentModel;
using ModernWinForms.Theming;

namespace ModernWinForms.Controls;

/// <summary>
/// Modern data grid view control with theme support.
/// </summary>
[ToolboxItem(true)]
[DesignerCategory("Code")]
[Description("Modern data grid view control with theme support.")]
public class ModernDataGridView : DataGridView
{
    /// <summary>
    /// Initializes a new instance of the ModernDataGridView class.
    /// </summary>
    public ModernDataGridView()
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
        var controlStyle = ThemeManager.GetControlStyle("ModernDataGridView");

        if (controlStyle != null && controlStyle.States.TryGetValue("normal", out var normalState))
        {
            if (normalState.BackColor != null)
            {
                BackgroundColor = ColorTranslator.FromHtml(normalState.BackColor);
            }

            if (normalState.ForeColor != null)
            {
                DefaultCellStyle.ForeColor = ColorTranslator.FromHtml(normalState.ForeColor);
            }

            if (normalState.BorderColor != null)
            {
                GridColor = ColorTranslator.FromHtml(normalState.BorderColor);
            }
        }
        else
        {
            // Default modern styling
            BackgroundColor = Color.White;
            GridColor = Color.FromArgb(224, 230, 237);
            BorderStyle = BorderStyle.None;

            EnableHeadersVisualStyles = false;
            ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
            ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(246, 248, 250);
            ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(13, 17, 23);
            ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(246, 248, 250);
            ColumnHeadersDefaultCellStyle.SelectionForeColor = Color.FromArgb(13, 17, 23);
            ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            ColumnHeadersDefaultCellStyle.Padding = new Padding(4, 8, 4, 8);

            DefaultCellStyle.BackColor = Color.White;
            DefaultCellStyle.ForeColor = Color.FromArgb(13, 17, 23);
            DefaultCellStyle.SelectionBackColor = Color.FromArgb(9, 105, 218);
            DefaultCellStyle.SelectionForeColor = Color.White;
            DefaultCellStyle.Font = new Font("Segoe UI", 9F);
            DefaultCellStyle.Padding = new Padding(4, 8, 4, 8);

            AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(246, 248, 250);

            RowHeadersVisible = false;
            AllowUserToAddRows = false;
            AllowUserToDeleteRows = false;
            AllowUserToResizeRows = false;
            SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            MultiSelect = false;
            CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            RowTemplate.Height = 35;
        }

        var themeFont = ThemeManager.CreateFont();
        if (themeFont != null)
        {
            Font = themeFont;
            DefaultCellStyle.Font = themeFont;
            themeFont.Dispose();
        }
    }

    /// <summary>
    /// Applies the specified skin definition to this data grid view.
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
