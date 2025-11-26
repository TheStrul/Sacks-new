using System.ComponentModel;
using ModernWinForms.Controls;
using ModernWinForms.Theming;

namespace SacksApp;

/// <summary>
/// Custom message box with modern styling from ThemeManager.
/// Uses theme colors with intelligent fallbacks when palette entries are missing.
/// </summary>
public partial class CustomMessageBox : Form
{
    private readonly Font _themeFont;
    private DialogResult _result = DialogResult.None;

    // Icon colors from theme palette with fallbacks
    private static Color ErrorColor => GetSemanticColor("error");
    private static Color WarningColor => GetSemanticColor("warning");
    private static Color InfoColor => GetSemanticColor("info");
    private static Color SuccessColor => GetSemanticColor("success");
    private static Color QuestionColor => GetSemanticColor("question");
    private static Color BackgroundColor => GetPaletteColor("background");
    private static Color SurfaceColor => GetPaletteColor("surface");
    private static Color TextColor => GetPaletteColor("text");
    private static Color BorderColor => GetPaletteColor("border");

    /// <summary>
    /// Gets a semantic color from the current theme palette with intelligent fallbacks.
    /// </summary>
    private static Color GetSemanticColor(string name)
    {
        var skin = ThemeManager.CurrentSkinDefinition;
        if (skin == null)
        {
            // Fallback to standard colors when theme not initialized
            return name.ToLowerInvariant() switch
            {
                "error" => Color.FromArgb(220, 53, 69),
                "warning" => Color.FromArgb(255, 193, 7),
                "info" => Color.FromArgb(13, 110, 253),
                "success" => Color.FromArgb(40, 167, 69),
                "question" => Color.FromArgb(13, 110, 253),
                _ => Color.Gray
            };
        }

        var colorHex = name.ToLowerInvariant() switch
        {
            // Error: try Error, Danger, or derive from Primary with red tint
            "error" => skin.Palette.Error ?? skin.Palette.Danger ?? "#DC3545",
            // Warning: try Warning, or use orange fallback
            "warning" => skin.Palette.Warning ?? "#FFC107",
            // Info: try Info, Primary, or blue fallback
            "info" => skin.Palette.Info ?? skin.Palette.Primary ?? "#0D6EFD",
            // Success: try Success, or green fallback
            "success" => skin.Palette.Success ?? "#28A745",
            // Question: use Primary or blue fallback
            "question" => skin.Palette.Primary ?? "#0D6EFD",
            _ => "#6C757D" // Gray fallback
        };

        return ColorTranslator.FromHtml(colorHex);
    }

    /// <summary>
    /// Gets a palette color from the current theme with intelligent fallbacks.
    /// </summary>
    private static Color GetPaletteColor(string name)
    {
        var skin = ThemeManager.CurrentSkinDefinition;
        if (skin == null)
        {
            // Fallback to standard colors when theme not initialized
            return name.ToLowerInvariant() switch
            {
                "background" => Color.FromArgb(13, 17, 23),
                "surface" => Color.FromArgb(22, 27, 34),
                "text" => Color.FromArgb(230, 237, 243),
                "border" => Color.FromArgb(48, 54, 61),
                _ => Color.Gray
            };
        }

        var colorHex = name.ToLowerInvariant() switch
        {
            "background" => skin.Palette.Background ?? "#0D1117",
            "surface" => skin.Palette.Surface ?? "#161B22",
            "text" => skin.Palette.Text ?? "#E6EDF3",
            "border" => skin.Palette.Border ?? "#30363D",
            _ => "#6C757D" // Gray fallback
        };

        return ColorTranslator.FromHtml(colorHex);
    }

    private CustomMessageBox()
    {
        // Get theme font - ZERO TOLERANCE
        _themeFont = ThemeManager.CreateFont() 
            ?? throw new InvalidOperationException("Theme font creation failed. Theme system not initialized.");

        InitializeComponent();

        // Apply theme colors
        BackColor = BackgroundColor;
        _titleLabel.Font = new Font(_themeFont.FontFamily, 11F, FontStyle.Bold);
        _titleLabel.ForeColor = TextColor;
        _titleLabel.BackColor = SurfaceColor;
        _contentPanel.BackColor = SurfaceColor;
        _messageLabel.Font = new Font(_themeFont.FontFamily, 10F);
        _messageLabel.ForeColor = TextColor;
        _buttonPanel.BackColor = SurfaceColor;

        // Enable double buffering
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | 
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        
        base.OnPaint(e);
        
        // Draw border
        using var pen = new Pen(BorderColor, 2);
        e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
    }

    private void SetupIcon(MessageBoxIcon icon)
    {
        switch (icon)
        {
            case MessageBoxIcon.Error:
                _iconLabel.Text = "\uE711"; // Error/Cancel icon
                _iconLabel.ForeColor = ErrorColor;
                break;
            case MessageBoxIcon.Warning:
                _iconLabel.Text = "\uE7BA"; // Warning icon
                _iconLabel.ForeColor = WarningColor;
                break;
            case MessageBoxIcon.Information:
                _iconLabel.Text = "\uE946"; // Info icon
                _iconLabel.ForeColor = InfoColor;
                break;
            case MessageBoxIcon.Question:
                _iconLabel.Text = "\uE897"; // Help/Question icon
                _iconLabel.ForeColor = QuestionColor;
                break;
            default:
                throw new ArgumentException($"Unsupported MessageBoxIcon: {icon}");
        }
    }

    private void Button_Click(object? sender, EventArgs e)
    {
        if (sender is ModernButton button)
        {
            _result = button.DialogResult;
            Close();
        }
    }

    private void SetupButtons(MessageBoxButtons buttons)
    {
        // Hide all buttons first
        _btnOK.Visible = false;
        _btnCancel.Visible = false;
        _btnYes.Visible = false;
        _btnNo.Visible = false;
        _btnRetry.Visible = false;
        _btnAbort.Visible = false;
        _btnIgnore.Visible = false;

        // Show and set default button based on button type
        switch (buttons)
        {
            case MessageBoxButtons.OK:
                _btnOK.Visible = true;
                AcceptButton = _btnOK;
                break;

            case MessageBoxButtons.OKCancel:
                _btnOK.Visible = true;
                _btnCancel.Visible = true;
                AcceptButton = _btnOK;
                CancelButton = _btnCancel;
                break;

            case MessageBoxButtons.YesNo:
                _btnYes.Visible = true;
                _btnNo.Visible = true;
                AcceptButton = _btnYes;
                break;

            case MessageBoxButtons.YesNoCancel:
                _btnYes.Visible = true;
                _btnNo.Visible = true;
                _btnCancel.Visible = true;
                AcceptButton = _btnYes;
                CancelButton = _btnCancel;
                break;

            case MessageBoxButtons.RetryCancel:
                _btnRetry.Visible = true;
                _btnCancel.Visible = true;
                AcceptButton = _btnRetry;
                CancelButton = _btnCancel;
                break;

            case MessageBoxButtons.AbortRetryIgnore:
                _btnAbort.Visible = true;
                _btnRetry.Visible = true;
                _btnIgnore.Visible = true;
                AcceptButton = _btnAbort;
                break;

            default:
                throw new ArgumentException($"Unsupported MessageBoxButtons: {buttons}");
        }
    }

    private void AdjustSize(string message)
    {
        // ZERO TOLERANCE: message must not be null
        ArgumentNullException.ThrowIfNull(message);

        // Measure message text
        using var g = CreateGraphics();
        var size = g.MeasureString(message, _messageLabel.Font, 390);
        
        var messageHeight = (int)Math.Ceiling(size.Height) + 20;
        var contentHeight = Math.Max(messageHeight, 80);
        
        _messageLabel.Height = messageHeight;
        Height = _titleLabel.Height + contentHeight + _buttonPanel.Height + 4; // +4 for border
    }

    /// <summary>
    /// Displays a message box with specified text, caption, buttons, and icon.
    /// ZERO TOLERANCE: All parameters are validated.
    /// </summary>
    public static DialogResult Show(string message, string caption = "Message", 
        MessageBoxButtons buttons = MessageBoxButtons.OK, 
        MessageBoxIcon icon = MessageBoxIcon.Information)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(caption);

        using var msgBox = new CustomMessageBox();
        msgBox._titleLabel.Text = caption;
        msgBox._messageLabel.Text = message;
        msgBox.SetupIcon(icon);
        msgBox.SetupButtons(buttons);
        msgBox.AdjustSize(message);
        msgBox.ShowDialog();
        return msgBox._result == DialogResult.None ? DialogResult.OK : msgBox._result;
    }

    /// <summary>
    /// Displays a message box with specified text and caption.
    /// </summary>
    public static DialogResult Show(string message, string caption)
    {
        return Show(message, caption, MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    /// <summary>
    /// Displays a message box with specified text.
    /// </summary>
    public static DialogResult Show(string message)
    {
        return Show(message, "Message", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    /// <summary>
    /// Displays an error message box.
    /// </summary>
    public static DialogResult ShowError(string message, string caption = "Error")
    {
        return Show(message, caption, MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    /// <summary>
    /// Displays a warning message box.
    /// </summary>
    public static DialogResult ShowWarning(string message, string caption = "Warning")
    {
        return Show(message, caption, MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }

    /// <summary>
    /// Displays an information message box.
    /// </summary>
    public static DialogResult ShowInfo(string message, string caption = "Information")
    {
        return Show(message, caption, MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    /// <summary>
    /// Displays a question message box with Yes/No buttons.
    /// </summary>
    public static DialogResult ShowQuestion(string message, string caption = "Question")
    {
        return Show(message, caption, MessageBoxButtons.YesNo, MessageBoxIcon.Question);
    }

    /// <summary>
    /// Displays a confirmation message box with Yes/No buttons.
    /// </summary>
    public static bool Confirm(string message, string caption = "Confirm")
    {
        var result = Show(message, caption, MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        return result == DialogResult.Yes;
    }
}
