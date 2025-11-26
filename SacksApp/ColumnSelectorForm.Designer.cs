using ModernWinForms.Controls;

namespace SacksApp
{
    partial class ColumnSelectorForm
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
                // Dispose all checkbox controls
                foreach (var checkBox in _checkBoxes)
                {
                    checkBox?.Dispose();
                }
                _checkBoxes.Clear();
                
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
            _checkBoxContainer = new ModernFlowLayoutPanel();
            _scrollPanel = new ModernPanel();
            _okButton = new ModernButton();
            _cancelButton = new ModernButton();
            _selectAllButton = new ModernButton();
            _deselectAllButton = new ModernButton();
            _buttonPanel = new ModernPanel();
            _topButtonPanel = new ModernPanel();
            _scrollPanel.SuspendLayout();
            _buttonPanel.SuspendLayout();
            _topButtonPanel.SuspendLayout();
            SuspendLayout();
            // 
            // _checkBoxContainer
            // 
            _checkBoxContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            _checkBoxContainer.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            _checkBoxContainer.AutoSize = true;
            _checkBoxContainer.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            _checkBoxContainer.WrapContents = false;
            _checkBoxContainer.Padding = new System.Windows.Forms.Padding(5);
            // 
            // _scrollPanel
            // 
            _scrollPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            _scrollPanel.AutoScroll = true;
            _scrollPanel.Padding = new System.Windows.Forms.Padding(10);
            _scrollPanel.Controls.Add(_checkBoxContainer);
            // 
            // _selectAllButton
            // 
            _selectAllButton.Text = "Select All";
            _selectAllButton.Width = 120;
            _selectAllButton.Height = 36;
            _selectAllButton.Left = 10;
            _selectAllButton.Top = 7;
            _selectAllButton.Click += SelectAllButton_Click;
            // 
            // _deselectAllButton
            // 
            _deselectAllButton.Text = "Deselect All";
            _deselectAllButton.Width = 120;
            _deselectAllButton.Height = 36;
            _deselectAllButton.Left = 140;
            _deselectAllButton.Top = 7;
            _deselectAllButton.Click += DeselectAllButton_Click;
            // 
            // _topButtonPanel
            // 
            _topButtonPanel.Dock = System.Windows.Forms.DockStyle.Top;
            _topButtonPanel.Height = 50;
            _topButtonPanel.Padding = new System.Windows.Forms.Padding(10);
            _topButtonPanel.Controls.Add(_selectAllButton);
            _topButtonPanel.Controls.Add(_deselectAllButton);
            // 
            // _okButton
            // 
            _okButton.Text = "OK";
            _okButton.Width = 120;
            _okButton.Height = 36;
            _okButton.DialogResult = System.Windows.Forms.DialogResult.OK;
            _okButton.Dock = System.Windows.Forms.DockStyle.Right;
            _okButton.Margin = new System.Windows.Forms.Padding(5, 0, 0, 0);
            // 
            // _cancelButton
            // 
            _cancelButton.Text = "Cancel";
            _cancelButton.Width = 120;
            _cancelButton.Height = 36;
            _cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            _cancelButton.Dock = System.Windows.Forms.DockStyle.Right;
            _cancelButton.Margin = new System.Windows.Forms.Padding(5, 0, 0, 0);
            // 
            // _buttonPanel
            // 
            _buttonPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
            _buttonPanel.Height = 60;
            _buttonPanel.Padding = new System.Windows.Forms.Padding(10);
            _buttonPanel.Controls.Add(_okButton);
            _buttonPanel.Controls.Add(_cancelButton);
            // 
            // ColumnSelectorForm
            // 
            ClientSize = new System.Drawing.Size(400, 500);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            Text = "Show / Hide Columns";
            AcceptButton = _okButton;
            CancelButton = _cancelButton;
            Controls.Add(_scrollPanel);
            Controls.Add(_topButtonPanel);
            Controls.Add(_buttonPanel);
            _scrollPanel.ResumeLayout(false);
            _scrollPanel.PerformLayout();
            _buttonPanel.ResumeLayout(false);
            _topButtonPanel.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private ModernFlowLayoutPanel _checkBoxContainer;
        private ModernPanel _scrollPanel;
        private ModernButton _okButton;
        private ModernButton _cancelButton;
        private ModernButton _selectAllButton;
        private ModernButton _deselectAllButton;
        private ModernPanel _buttonPanel;
        private ModernPanel _topButtonPanel;
    }
}
