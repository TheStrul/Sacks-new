using System.ComponentModel;
using ModernWinForms.Controls;
using ModernWinForms.Theming;

namespace SacksApp;

/// <summary>
/// Custom message box with modern styling from ThemeManager.
/// Uses theme colors with intelligent fallbacks when palette entries are missing.
/// </summary>
public class CustomMessageBox : Form
{
    private readonly ModernLabel _iconLabel;
    private readonly ModernLabel _messageLabel;
    private readonly ModernLabel _titleLabel;
    private readonly ModernPanel _buttonPanel;
    private readonly ModernPanel _contentPanel;
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
        // Form properties
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterParent;
        BackColor = BackgroundColor;
        ShowInTaskbar = false;
        MinimizeBox = false;
        MaximizeBox = false;
        Width = 500;
        Height = 200;
        Padding = new Padding(2); // Border

        // Enable double buffering
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | 
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);

        // Get theme font - ZERO TOLERANCE
        _themeFont = ThemeManager.CreateFont() 
            ?? throw new InvalidOperationException("Theme font creation failed. Theme system not initialized.");

        // Title label
        _titleLabel = new ModernLabel
        {
            Dock = DockStyle.Top,
            Font = new Font(_themeFont.FontFamily, 11F, FontStyle.Bold),
            ForeColor = TextColor,
            Height = 40,
            Padding = new Padding(15, 10, 15, 5),
            BackColor = SurfaceColor,
            TextAlign = ContentAlignment.MiddleLeft
        };

        // Content panel
        _contentPanel = new ModernPanel
        {
            Dock = DockStyle.Fill,
            BackColor = SurfaceColor,
            Padding = new Padding(15, 10, 15, 10)
        };

        // Icon label (using MDL2 glyphs)
        _iconLabel = new ModernLabel
        {
            Font = new Font("Segoe MDL2 Assets", 24F),
            AutoSize = false,
            Size = new Size(50, 50),
            TextAlign = ContentAlignment.MiddleCenter,
            Location = new Point(15, 10)
        };

        // Message label
        _messageLabel = new ModernLabel
        {
            Font = new Font(_themeFont.FontFamily, 10F),
            ForeColor = TextColor,
            AutoSize = false,
            Location = new Point(75, 10),
            Width = 390,
            Height = 80,
            TextAlign = ContentAlignment.TopLeft
        };

        // Button panel
        _buttonPanel = new ModernPanel
        {
            Dock = DockStyle.Bottom,
            Height = 60,
            BackColor = SurfaceColor,
            Padding = new Padding(10)
        };

        _contentPanel.Controls.Add(_iconLabel);
        _contentPanel.Controls.Add(_messageLabel);

        Controls.Add(_contentPanel);
        Controls.Add(_buttonPanel);
        Controls.Add(_titleLabel);
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

    private void AddButton(string text, DialogResult result, bool isDefault = false)
    {
        // ZERO TOLERANCE: text must not be empty
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("Button text cannot be null or whitespace.", nameof(text));

        var button = new ModernButton
        {
            Text = text,
            DialogResult = result,
            Width = 100,
            Height = 36,
            Cursor = Cursors.Hand,
            Dock = DockStyle.Right,
            Margin = new Padding(5, 0, 0, 0)
        };

        button.Click += (s, e) =>
        {
            _result = result;
            Close();
        };

        _buttonPanel.Controls.Add(button);
        
        if (isDefault)
        {
            AcceptButton = button;
        }
    }

    private void SetupButtons(MessageBoxButtons buttons)
    {
        switch (buttons)
        {
            case MessageBoxButtons.OK:
                AddButton("OK", DialogResult.OK, isDefault: true);
                break;

            case MessageBoxButtons.OKCancel:
                AddButton("Cancel", DialogResult.Cancel);
                AddButton("OK", DialogResult.OK, isDefault: true);
                break;

            case MessageBoxButtons.YesNo:
                AddButton("No", DialogResult.No);
                AddButton("Yes", DialogResult.Yes, isDefault: true);
                break;

            case MessageBoxButtons.YesNoCancel:
                AddButton("Cancel", DialogResult.Cancel);
                AddButton("No", DialogResult.No);
                AddButton("Yes", DialogResult.Yes, isDefault: true);
                break;

            case MessageBoxButtons.RetryCancel:
                AddButton("Cancel", DialogResult.Cancel);
                AddButton("Retry", DialogResult.Retry, isDefault: true);
                break;

            case MessageBoxButtons.AbortRetryIgnore:
                AddButton("Ignore", DialogResult.Ignore);
                AddButton("Retry", DialogResult.Retry);
                AddButton("Abort", DialogResult.Abort, isDefault: true);
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

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _themeFont?.Dispose();
        }
        base.Dispose(disposing);
    }
}
