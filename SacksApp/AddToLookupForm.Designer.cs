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
            // _lblKey
            // 
            _lblKey.Text = "Key:";
            _lblKey.AutoSize = true;
            _lblKey.Anchor = System.Windows.Forms.AnchorStyles.Right;
            // 
            // _keyTextBox
            // 
            _keyTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
            _keyTextBox.PlaceholderText = "Enter key...";
            // 
            // _lblValue
            // 
            _lblValue.Text = "Value:";
            _lblValue.AutoSize = true;
            _lblValue.Anchor = System.Windows.Forms.AnchorStyles.Right;
            // 
            // _valueTextBox
            // 
            _valueTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
            _valueTextBox.PlaceholderText = "Enter value...";
            // 
            // _okButton
            // 
            _okButton.Text = "OK";
            _okButton.Width = 100;
            _okButton.Height = 36;
            _okButton.DialogResult = System.Windows.Forms.DialogResult.OK;
            _okButton.Anchor = System.Windows.Forms.AnchorStyles.Right;
            _okButton.Click += OkButton_Click;
            // 
            // _cancelButton
            // 
            _cancelButton.Text = "Cancel";
            _cancelButton.Width = 100;
            _cancelButton.Height = 36;
            _cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            _cancelButton.Anchor = System.Windows.Forms.AnchorStyles.Right;
            // 
            // _buttonPanel
            // 
            _buttonPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            _buttonPanel.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
            _buttonPanel.Padding = new System.Windows.Forms.Padding(0, 10, 0, 0);
            _buttonPanel.Controls.Add(_cancelButton);
            _buttonPanel.Controls.Add(_okButton);
            // 
            // _layoutPanel
            // 
            _layoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            _layoutPanel.ColumnCount = 2;
            _layoutPanel.RowCount = 3;
            _layoutPanel.Padding = new System.Windows.Forms.Padding(15);
            _layoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.AutoSize));
            _layoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            _layoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            _layoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            _layoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            _layoutPanel.Controls.Add(_lblKey, 0, 0);
            _layoutPanel.Controls.Add(_keyTextBox, 1, 0);
            _layoutPanel.Controls.Add(_lblValue, 0, 1);
            _layoutPanel.Controls.Add(_valueTextBox, 1, 1);
            _layoutPanel.Controls.Add(_buttonPanel, 0, 2);
            _layoutPanel.SetColumnSpan(_buttonPanel, 2);
            // 
            // AddToLookupForm
            // 
            ClientSize = new System.Drawing.Size(420, 180);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            AcceptButton = _okButton;
            CancelButton = _cancelButton;
            Controls.Add(_layoutPanel);
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
