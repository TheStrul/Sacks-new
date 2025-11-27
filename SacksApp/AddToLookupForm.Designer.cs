using ModernWinForms.Controls;

namespace SacksApp
{
    partial class AddToLookupForm
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
            if (disposing && (components != null))
            {
                components.Dispose();
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
            _keyTextBox = new ModernTextBox();
            _valueTextBox = new ModernTextBox();
            _okButton = new ModernButton();
            _cancelButton = new ModernButton();
            _lblKey = new ModernLabel();
            _lblValue = new ModernLabel();
            _layoutPanel = new ModernTableLayoutPanel();
            _buttonPanel = new ModernFlowLayoutPanel();
            _layoutPanel.SuspendLayout();
            _buttonPanel.SuspendLayout();
            SuspendLayout();
            // 
            // _keyTextBox
            // 
            _keyTextBox.BackColor = Color.FromArgb(255, 255, 255);
            _keyTextBox.Dock = DockStyle.Fill;
            _keyTextBox.Location = new Point(62, 18);
            _keyTextBox.Name = "_keyTextBox";
            _keyTextBox.PlaceholderText = "Enter key...";
            _keyTextBox.ScrollBars = ScrollBars.None;
            _keyTextBox.Size = new Size(414, 32);
            _keyTextBox.TabIndex = 1;
            _keyTextBox.WordWrap = true;
            // 
            // _valueTextBox
            // 
            _valueTextBox.BackColor = Color.FromArgb(255, 255, 255);
            _valueTextBox.Dock = DockStyle.Fill;
            _valueTextBox.Location = new Point(62, 56);
            _valueTextBox.Name = "_valueTextBox";
            _valueTextBox.PlaceholderText = "Enter value...";
            _valueTextBox.ScrollBars = ScrollBars.None;
            _valueTextBox.Size = new Size(414, 32);
            _valueTextBox.TabIndex = 3;
            _valueTextBox.WordWrap = true;
            // 
            // _okButton
            // 
            _okButton.Anchor = AnchorStyles.Right;
            _okButton.BackColor = Color.Transparent;
            _okButton.DialogResult = DialogResult.OK;
            _okButton.FlatStyle = FlatStyle.Flat;
            _okButton.Location = new Point(249, 13);
            _okButton.Name = "_okButton";
            _okButton.Size = new Size(100, 36);
            _okButton.TabIndex = 1;
            _okButton.Text = "OK";
            _okButton.UseVisualStyleBackColor = false;
            _okButton.Click += OkButton_Click;
            // 
            // _cancelButton
            // 
            _cancelButton.Anchor = AnchorStyles.Right;
            _cancelButton.BackColor = Color.Transparent;
            _cancelButton.DialogResult = DialogResult.Cancel;
            _cancelButton.FlatStyle = FlatStyle.Flat;
            _cancelButton.Location = new Point(355, 13);
            _cancelButton.Name = "_cancelButton";
            _cancelButton.Size = new Size(100, 36);
            _cancelButton.TabIndex = 0;
            _cancelButton.Text = "Cancel";
            _cancelButton.UseVisualStyleBackColor = false;
            // 
            // _lblKey
            // 
            _lblKey.Anchor = AnchorStyles.Right;
            _lblKey.AutoSize = true;
            _lblKey.BackColor = Color.FromArgb(243, 243, 243);
            _lblKey.Font = new Font("Segoe UI", 9F);
            _lblKey.ForeColor = Color.FromArgb(50, 49, 48);
            _lblKey.Location = new Point(27, 26);
            _lblKey.Name = "_lblKey";
            _lblKey.Size = new Size(29, 15);
            _lblKey.TabIndex = 0;
            _lblKey.Text = "Key:";
            // 
            // _lblValue
            // 
            _lblValue.Anchor = AnchorStyles.Right;
            _lblValue.AutoSize = true;
            _lblValue.BackColor = Color.FromArgb(243, 243, 243);
            _lblValue.Font = new Font("Segoe UI", 9F);
            _lblValue.ForeColor = Color.FromArgb(50, 49, 48);
            _lblValue.Location = new Point(18, 64);
            _lblValue.Name = "_lblValue";
            _lblValue.Size = new Size(38, 15);
            _lblValue.TabIndex = 2;
            _lblValue.Text = "Value:";
            // 
            // _layoutPanel
            // 
            _layoutPanel.BackColor = Color.FromArgb(243, 243, 243);
            _layoutPanel.ColumnCount = 2;
            _layoutPanel.ColumnStyles.Add(new ColumnStyle());
            _layoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            _layoutPanel.Controls.Add(_lblKey, 0, 0);
            _layoutPanel.Controls.Add(_keyTextBox, 1, 0);
            _layoutPanel.Controls.Add(_lblValue, 0, 1);
            _layoutPanel.Controls.Add(_valueTextBox, 1, 1);
            _layoutPanel.Controls.Add(_buttonPanel, 0, 2);
            _layoutPanel.Dock = DockStyle.Fill;
            _layoutPanel.ForeColor = Color.FromArgb(50, 49, 48);
            _layoutPanel.Location = new Point(0, 0);
            _layoutPanel.Name = "_layoutPanel";
            _layoutPanel.Padding = new Padding(15);
            _layoutPanel.RowCount = 3;
            _layoutPanel.RowStyles.Add(new RowStyle());
            _layoutPanel.RowStyles.Add(new RowStyle());
            _layoutPanel.RowStyles.Add(new RowStyle());
            _layoutPanel.Size = new Size(494, 311);
            _layoutPanel.TabIndex = 0;
            // 
            // _buttonPanel
            // 
            _buttonPanel.BackColor = Color.FromArgb(243, 243, 243);
            _layoutPanel.SetColumnSpan(_buttonPanel, 2);
            _buttonPanel.Controls.Add(_cancelButton);
            _buttonPanel.Controls.Add(_okButton);
            _buttonPanel.Dock = DockStyle.Fill;
            _buttonPanel.FlowDirection = FlowDirection.RightToLeft;
            _buttonPanel.ForeColor = Color.FromArgb(50, 49, 48);
            _buttonPanel.Location = new Point(18, 94);
            _buttonPanel.Name = "_buttonPanel";
            _buttonPanel.Padding = new Padding(0, 10, 0, 0);
            _buttonPanel.Size = new Size(458, 199);
            _buttonPanel.TabIndex = 4;
            // 
            // AddToLookupForm
            // 
            AcceptButton = _okButton;
            CancelButton = _cancelButton;
            ClientSize = new Size(494, 311);
            Controls.Add(_layoutPanel);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "AddToLookupForm";
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            _layoutPanel.ResumeLayout(false);
            _layoutPanel.PerformLayout();
            _buttonPanel.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private ModernTextBox _keyTextBox;
        private ModernTextBox _valueTextBox;
        private ModernButton _okButton;
        private ModernButton _cancelButton;
        private ModernLabel _lblKey;
        private ModernLabel _lblValue;
        private ModernTableLayoutPanel _layoutPanel;
        private ModernFlowLayoutPanel _buttonPanel;
    }
}
