using System.Windows.Forms;
using ModernWinForms.Controls;

namespace SacksApp;

partial class CustomMessageBox
{
    /// <summary>
    /// Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    /// Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _themeFont?.Dispose();
            
            if (components != null)
            {
                components.Dispose();
            }
        }
        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    /// <summary>
    /// Required method for Designer support - do not modify
    /// the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        _titleLabel = new ModernLabel();
        _contentPanel = new ModernPanel();
        _messageLabel = new ModernLabel();
        _iconLabel = new ModernLabel();
        _buttonPanel = new ModernFlowLayoutPanel();
        _btnIgnore = new ModernButton();
        _btnAbort = new ModernButton();
        _btnRetry = new ModernButton();
        _btnNo = new ModernButton();
        _btnYes = new ModernButton();
        _btnCancel = new ModernButton();
        _btnOK = new ModernButton();
        _contentPanel.SuspendLayout();
        _buttonPanel.SuspendLayout();
        SuspendLayout();
        // 
        // _titleLabel
        // 
        _titleLabel.Dock = DockStyle.Top;
        _titleLabel.Location = new Point(0, 0);
        _titleLabel.Name = "_titleLabel";
        _titleLabel.Padding = new Padding(10, 8, 10, 8);
        _titleLabel.Size = new Size(788, 40);
        _titleLabel.TabIndex = 0;
        _titleLabel.Text = "Message";
        _titleLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // _contentPanel
        // 
        _contentPanel.Controls.Add(_messageLabel);
        _contentPanel.Controls.Add(_iconLabel);
        _contentPanel.Dock = DockStyle.Fill;
        _contentPanel.Location = new Point(0, 40);
        _contentPanel.Name = "_contentPanel";
        _contentPanel.Padding = new Padding(15, 15, 15, 10);
        _contentPanel.Size = new Size(788, 348);
        _contentPanel.TabIndex = 1;
        // 
        // _messageLabel
        // 
        _messageLabel.Dock = DockStyle.Fill;
        _messageLabel.Location = new Point(65, 15);
        _messageLabel.Name = "_messageLabel";
        _messageLabel.Padding = new Padding(10, 0, 0, 0);
        _messageLabel.Size = new Size(708, 323);
        _messageLabel.TabIndex = 1;
        _messageLabel.Text = "Message text goes here";
        // 
        // _iconLabel
        // 
        _iconLabel.Dock = DockStyle.Left;
        _iconLabel.Font = new Font("Segoe MDL2 Assets", 24F);
        _iconLabel.Location = new Point(15, 15);
        _iconLabel.Name = "_iconLabel";
        _iconLabel.Size = new Size(50, 323);
        _iconLabel.TabIndex = 0;
        _iconLabel.Text = "";
        _iconLabel.TextAlign = ContentAlignment.TopCenter;
        // 
        // _buttonPanel
        // 
        _buttonPanel.AutoSize = true;
        _buttonPanel.Controls.Add(_btnIgnore);
        _buttonPanel.Controls.Add(_btnAbort);
        _buttonPanel.Controls.Add(_btnRetry);
        _buttonPanel.Controls.Add(_btnNo);
        _buttonPanel.Controls.Add(_btnYes);
        _buttonPanel.Controls.Add(_btnCancel);
        _buttonPanel.Controls.Add(_btnOK);
        _buttonPanel.Dock = DockStyle.Bottom;
        _buttonPanel.FlowDirection = FlowDirection.RightToLeft;
        _buttonPanel.Location = new Point(0, 388);
        _buttonPanel.Name = "_buttonPanel";
        _buttonPanel.Padding = new Padding(10, 8, 10, 8);
        _buttonPanel.Size = new Size(788, 58);
        _buttonPanel.TabIndex = 2;
        // 
        // _btnIgnore
        // 
        _btnIgnore.Cursor = Cursors.Hand;
        _btnIgnore.DialogResult = DialogResult.Ignore;
        _btnIgnore.Location = new Point(668, 11);
        _btnIgnore.Margin = new Padding(5, 3, 0, 3);
        _btnIgnore.Name = "_btnIgnore";
        _btnIgnore.Size = new Size(100, 36);
        _btnIgnore.TabIndex = 6;
        _btnIgnore.Text = "Ignore";
        _btnIgnore.Visible = false;
        _btnIgnore.Click += Button_Click;
        // 
        // _btnAbort
        // 
        _btnAbort.Cursor = Cursors.Hand;
        _btnAbort.DialogResult = DialogResult.Abort;
        _btnAbort.Location = new Point(563, 11);
        _btnAbort.Margin = new Padding(5, 3, 0, 3);
        _btnAbort.Name = "_btnAbort";
        _btnAbort.Size = new Size(100, 36);
        _btnAbort.TabIndex = 5;
        _btnAbort.Text = "Abort";
        _btnAbort.Visible = false;
        _btnAbort.Click += Button_Click;
        // 
        // _btnRetry
        // 
        _btnRetry.Cursor = Cursors.Hand;
        _btnRetry.DialogResult = DialogResult.Retry;
        _btnRetry.Location = new Point(458, 11);
        _btnRetry.Margin = new Padding(5, 3, 0, 3);
        _btnRetry.Name = "_btnRetry";
        _btnRetry.Size = new Size(100, 36);
        _btnRetry.TabIndex = 4;
        _btnRetry.Text = "Retry";
        _btnRetry.Visible = false;
        _btnRetry.Click += Button_Click;
        // 
        // _btnNo
        // 
        _btnNo.Cursor = Cursors.Hand;
        _btnNo.DialogResult = DialogResult.No;
        _btnNo.Location = new Point(353, 11);
        _btnNo.Margin = new Padding(5, 3, 0, 3);
        _btnNo.Name = "_btnNo";
        _btnNo.Size = new Size(100, 36);
        _btnNo.TabIndex = 3;
        _btnNo.Text = "No";
        _btnNo.Visible = false;
        _btnNo.Click += Button_Click;
        // 
        // _btnYes
        // 
        _btnYes.Cursor = Cursors.Hand;
        _btnYes.DialogResult = DialogResult.Yes;
        _btnYes.Location = new Point(248, 11);
        _btnYes.Margin = new Padding(5, 3, 0, 3);
        _btnYes.Name = "_btnYes";
        _btnYes.Size = new Size(100, 36);
        _btnYes.TabIndex = 2;
        _btnYes.Text = "Yes";
        _btnYes.Visible = false;
        _btnYes.Click += Button_Click;
        // 
        // _btnCancel
        // 
        _btnCancel.Cursor = Cursors.Hand;
        _btnCancel.DialogResult = DialogResult.Cancel;
        _btnCancel.Location = new Point(143, 11);
        _btnCancel.Margin = new Padding(5, 3, 0, 3);
        _btnCancel.Name = "_btnCancel";
        _btnCancel.Size = new Size(100, 36);
        _btnCancel.TabIndex = 1;
        _btnCancel.Text = "Cancel";
        _btnCancel.Visible = false;
        _btnCancel.Click += Button_Click;
        // 
        // _btnOK
        // 
        _btnOK.Cursor = Cursors.Hand;
        _btnOK.DialogResult = DialogResult.OK;
        _btnOK.Location = new Point(38, 11);
        _btnOK.Margin = new Padding(5, 3, 0, 3);
        _btnOK.Name = "_btnOK";
        _btnOK.Size = new Size(100, 36);
        _btnOK.TabIndex = 0;
        _btnOK.Text = "OK";
        _btnOK.Visible = false;
        _btnOK.Click += Button_Click;
        // 
        // CustomMessageBox
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(788, 446);
        Controls.Add(_contentPanel);
        Controls.Add(_titleLabel);
        Controls.Add(_buttonPanel);
        FormBorderStyle = FormBorderStyle.None;
        MaximizeBox = false;
        MinimizeBox = false;
        Name = "CustomMessageBox";
        StartPosition = FormStartPosition.CenterParent;
        Text = "CustomMessageBox";
        _contentPanel.ResumeLayout(false);
        _buttonPanel.ResumeLayout(false);
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private ModernLabel _titleLabel;
    private ModernPanel _contentPanel;
    private ModernLabel _iconLabel;
    private ModernLabel _messageLabel;
    private ModernFlowLayoutPanel _buttonPanel;
    private ModernButton _btnOK;
    private ModernButton _btnCancel;
    private ModernButton _btnYes;
    private ModernButton _btnNo;
    private ModernButton _btnRetry;
    private ModernButton _btnAbort;
    private ModernButton _btnIgnore;
}
