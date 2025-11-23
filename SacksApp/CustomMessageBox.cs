using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace SacksApp;

/// <summary>
/// Custom message box with modern styling matching the application theme.
/// Provides static Show methods similar to System.Windows.Forms.MessageBox.
/// </summary>
public class CustomMessageBox : Form
{
    private readonly Label _iconLabel;
    private readonly Label _messageLabel;
    private readonly Label _titleLabel;
    private readonly Panel _buttonPanel;
    private readonly Panel _contentPanel;
    private DialogResult _result = DialogResult.None;

    // Theme colors
    private static readonly Color ErrorColor = Color.FromArgb(244, 67, 54);
    private static readonly Color WarningColor = Color.FromArgb(255, 152, 0);
    private static readonly Color InfoColor = Color.FromArgb(33, 150, 243);
    private static readonly Color SuccessColor = Color.FromArgb(76, 175, 80);
    private static readonly Color QuestionColor = Color.FromArgb(156, 39, 176);
    private static readonly Color BackgroundColor = Color.FromArgb(250, 250, 252);
    private static readonly Color TextColor = Color.FromArgb(30, 30, 30);
    private static readonly Color BorderColor = Color.FromArgb(200, 200, 200);

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

        // Title label
        _titleLabel = new Label
        {
            Dock = DockStyle.Top,
            Font = new Font("Segoe UI", 11F, FontStyle.Bold),
            ForeColor = TextColor,
            Height = 40,
            Padding = new Padding(15, 10, 15, 5),
            BackColor = Color.White,
            TextAlign = ContentAlignment.MiddleLeft
        };

        // Content panel
        _contentPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Padding = new Padding(15, 10, 15, 10)
        };

        // Icon label (using MDL2 glyphs)
        _iconLabel = new Label
        {
            Font = new Font("Segoe MDL2 Assets", 24F),
            AutoSize = false,
            Size = new Size(50, 50),
            TextAlign = ContentAlignment.MiddleCenter,
            Location = new Point(15, 10)
        };

        // Message label
        _messageLabel = new Label
        {
            Font = new Font("Segoe UI", 10F),
            ForeColor = TextColor,
            AutoSize = false,
            Location = new Point(75, 10),
            Width = 390,
            Height = 80,
            TextAlign = ContentAlignment.TopLeft
        };

        // Button panel
        _buttonPanel = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 60,
            BackColor = Color.White,
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
                _iconLabel.Text = "\uE946"; // Info icon
                _iconLabel.ForeColor = InfoColor;
                break;
        }
    }

    private void AddButton(string text, DialogResult result, bool isDefault = false)
    {
        var button = new Button
        {
            Text = text,
            DialogResult = result,
            Width = 100,
            Height = 36,
            FlatStyle = FlatStyle.Flat,
            BackColor = isDefault ? Color.FromArgb(0, 120, 215) : Color.FromArgb(240, 240, 240),
            ForeColor = isDefault ? Color.White : TextColor,
            Font = new Font("Segoe UI", 10F),
            Cursor = Cursors.Hand,
            Dock = DockStyle.Right,
            Margin = new Padding(5, 0, 0, 0)
        };

        button.FlatAppearance.BorderSize = 1;
        button.FlatAppearance.BorderColor = isDefault ? Color.FromArgb(0, 90, 158) : BorderColor;

        button.Click += (s, e) =>
        {
            _result = result;
            Close();
        };

        button.MouseEnter += (s, e) =>
        {
            button.BackColor = isDefault 
                ? Color.FromArgb(0, 90, 158) 
                : Color.FromArgb(230, 230, 230);
        };

        button.MouseLeave += (s, e) =>
        {
            button.BackColor = isDefault 
                ? Color.FromArgb(0, 120, 215) 
                : Color.FromArgb(240, 240, 240);
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
        }
    }

    private void AdjustSize(string message)
    {
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
    /// </summary>
    public static DialogResult Show(string message, string caption = "Message", 
        MessageBoxButtons buttons = MessageBoxButtons.OK, 
        MessageBoxIcon icon = MessageBoxIcon.Information)
    {
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
