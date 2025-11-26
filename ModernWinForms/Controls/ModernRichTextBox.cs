using System.ComponentModel;
using System.Reflection;
using ModernWinForms.Theming;

namespace ModernWinForms.Controls;

/// <summary>
/// Modern rich text box control with theme support and enhanced functionality.
/// Preserves all RichTextBox capabilities while adding modern styling.
/// ZERO TOLERANCE: Fails hard if theming is unavailable.
/// </summary>
[ToolboxItem(true)]
[DesignerCategory("Code")]
[Description("Modern rich text box control with theme support and enhanced functionality.")]
public class ModernRichTextBox : RichTextBox
{
    private ControlStyle _controlStyle = new();
    private Font? _themeFont;

    /// <summary>
    /// Initializes a new instance of the ModernRichTextBox class.
    /// </summary>
    public ModernRichTextBox()
    {
        // Enable double buffering via reflection to reduce flicker
        EnableDoubleBuffering();

        BorderStyle = BorderStyle.FixedSingle;
        
        ThemeManager.ThemeChanged += OnThemeChanged;
        UpdateStyleFromTheme();
    }

    /// <summary>
    /// Enables double buffering for this RichTextBox to reduce flicker.
    /// </summary>
    private void EnableDoubleBuffering()
    {
        typeof(RichTextBox).InvokeMember(
            "DoubleBuffered",
            BindingFlags.SetProperty | BindingFlags.Instance | BindingFlags.NonPublic,
            null,
            this,
            [true]);
    }

    private void OnThemeChanged(object? sender, EventArgs e)
    {
        UpdateStyleFromTheme();
        Invalidate();
    }

    private void UpdateStyleFromTheme()
    {
        // ZERO TOLERANCE: Get style or throw
        _controlStyle = ThemeManager.GetControlStyle("ModernRichTextBox") 
            ?? throw new InvalidOperationException(
                "ModernRichTextBox control style not found in theme. Theme configuration is incomplete.");

        // Get normal state or throw
        if (!_controlStyle.States.TryGetValue("normal", out var normalState))
            throw new InvalidOperationException(
                "ModernRichTextBox 'normal' state not defined in theme. Theme configuration is incomplete.");

        // Apply colors - REQUIRED, no fallbacks
        if (string.IsNullOrWhiteSpace(normalState.BackColor))
            throw new InvalidOperationException(
                "ModernRichTextBox BackColor not defined in 'normal' state. Theme configuration is incomplete.");

        if (string.IsNullOrWhiteSpace(normalState.ForeColor))
            throw new InvalidOperationException(
                "ModernRichTextBox ForeColor not defined in 'normal' state. Theme configuration is incomplete.");

        BackColor = ColorTranslator.FromHtml(normalState.BackColor);
        ForeColor = ColorTranslator.FromHtml(normalState.ForeColor);

        // Apply theme font - REQUIRED
        _themeFont?.Dispose();
        _themeFont = ThemeManager.CreateFont() 
            ?? throw new InvalidOperationException(
                "Theme font creation failed. Theme configuration is incomplete.");
        
        Font = _themeFont;
    }

    /// <summary>
    /// Applies the specified skin definition to this rich text box.
    /// </summary>
    /// <param name="skin">The skin definition to apply.</param>
    /// <exception cref="ArgumentNullException">Thrown when skin is null.</exception>
    public void ApplySkin(SkinDefinition skin)
    {
        ArgumentNullException.ThrowIfNull(skin);
        UpdateStyleFromTheme();
        Invalidate();
    }

    /// <summary>
    /// Clears all content from the rich text box.
    /// More efficient than setting Text = string.Empty for RTF content.
    /// </summary>
    public new void Clear()
    {
        base.Clear();
    }

    /// <summary>
    /// Appends text with the specified color.
    /// </summary>
    /// <param name="text">The text to append.</param>
    /// <param name="color">The color of the text.</param>
    /// <exception cref="ArgumentNullException">Thrown when text is null.</exception>
    public void AppendText(string text, Color color)
    {
        ArgumentNullException.ThrowIfNull(text);

        SelectionStart = TextLength;
        SelectionLength = 0;
        SelectionColor = color;
        AppendText(text);
        SelectionColor = ForeColor;
    }

    /// <summary>
    /// Appends a line of text with the specified color.
    /// </summary>
    /// <param name="line">The line of text to append.</param>
    /// <param name="color">The color of the text.</param>
    /// <exception cref="ArgumentNullException">Thrown when line is null.</exception>
    public void AppendLine(string line, Color color)
    {
        ArgumentNullException.ThrowIfNull(line);
        AppendText(line + Environment.NewLine, color);
    }

    /// <summary>
    /// Suspends visual updates to the control.
    /// Use with EndUpdate() for bulk updates to reduce flicker.
    /// </summary>
    public void BeginUpdate()
    {
        NativeMethods.SendMessage(Handle, NativeMethods.WM_SETREDRAW, IntPtr.Zero, IntPtr.Zero);
    }

    /// <summary>
    /// Resumes visual updates to the control.
    /// Must be called after BeginUpdate().
    /// </summary>
    public void EndUpdate()
    {
        NativeMethods.SendMessage(Handle, NativeMethods.WM_SETREDRAW, new IntPtr(1), IntPtr.Zero);
        Invalidate();
        Refresh();
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

    /// <summary>
    /// Native methods for control updates.
    /// </summary>
    private static class NativeMethods
    {
        internal const int WM_SETREDRAW = 0x000B;

        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        internal static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
    }
}
