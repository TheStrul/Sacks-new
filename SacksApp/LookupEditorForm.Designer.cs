namespace SacksApp
{
    partial class LookupEditorForm
    {
        private System.ComponentModel.IContainer components = null;
        private ModernWinForms.Controls.ModernLabel _titleLabel;
        private ModernWinForms.Controls.ModernComboBox _lookupCombo;
        private ModernWinForms.Controls.ModernDataGridView _grid;
        private ModernWinForms.Controls.ModernButton _addButton;
        private ModernWinForms.Controls.ModernButton _removeButton;
        private ModernWinForms.Controls.ModernButton _reloadButton;
        private ModernWinForms.Controls.ModernButton _saveButton;
        private ModernWinForms.Controls.ModernButton _closeButton;
        private ModernWinForms.Controls.ModernStatusStrip _statusStrip;
        private System.Windows.Forms.ToolStripStatusLabel _statusLabel;
        private ModernWinForms.Controls.ModernTableLayoutPanel _root;
        private ModernWinForms.Controls.ModernFlowLayoutPanel _buttonsPanel;
        private ModernWinForms.Controls.ModernFlowLayoutPanel _headerPanel;
        private System.Windows.Forms.DataGridViewTextBoxColumn colKey;
        private System.Windows.Forms.DataGridViewTextBoxColumn colVal;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            _titleLabel = new ModernWinForms.Controls.ModernLabel();
            _lookupCombo = new ModernWinForms.Controls.ModernComboBox();
            _grid = new ModernWinForms.Controls.ModernDataGridView();
            colKey = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colVal = new System.Windows.Forms.DataGridViewTextBoxColumn();
            _addButton = new ModernWinForms.Controls.ModernButton();
            _removeButton = new ModernWinForms.Controls.ModernButton();
            _reloadButton = new ModernWinForms.Controls.ModernButton();
            _saveButton = new ModernWinForms.Controls.ModernButton();
            _closeButton = new ModernWinForms.Controls.ModernButton();
            _statusStrip = new ModernWinForms.Controls.ModernStatusStrip();
            _statusLabel = new System.Windows.Forms.ToolStripStatusLabel();
            _root = new ModernWinForms.Controls.ModernTableLayoutPanel();
            _buttonsPanel = new ModernWinForms.Controls.ModernFlowLayoutPanel();
            _headerPanel = new ModernWinForms.Controls.ModernFlowLayoutPanel();
            ((System.ComponentModel.ISupportInitialize)_grid).BeginInit();
            _statusStrip.SuspendLayout();
            _root.SuspendLayout();
            _buttonsPanel.SuspendLayout();
            _headerPanel.SuspendLayout();
            SuspendLayout();
            // 
            // _titleLabel
            // 
            _titleLabel.AutoSize = true;
            _titleLabel.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            _titleLabel.Text = "Lookup:";
            _titleLabel.Margin = new System.Windows.Forms.Padding(0, 8, 8, 0);
            // 
            // _lookupCombo
            // 
            _lookupCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            _lookupCombo.Width = 260;
            // 
            // _headerPanel
            // 
            _headerPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            _headerPanel.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
            _headerPanel.AutoSize = true;
            _headerPanel.Controls.Add(_titleLabel);
            _headerPanel.Controls.Add(_lookupCombo);
            // 
            // _grid
            // 
            _grid.AllowUserToAddRows = false;
            _grid.AllowUserToResizeRows = false;
            _grid.AutoGenerateColumns = false;
            _grid.Dock = System.Windows.Forms.DockStyle.Fill;
            _grid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            _grid.MultiSelect = true;
            _grid.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] { colKey, colVal });
            // 
            // colKey
            // 
            colKey.HeaderText = "Canonical";
            colKey.DataPropertyName = "Canonical";
            colKey.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            colKey.FillWeight = 30F;
            // 
            // colVal
            // 
            colVal.HeaderText = "Aliases (comma-separated)";
            colVal.DataPropertyName = "Aliases";
            colVal.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            colVal.FillWeight = 70F;
            // 
            // _addButton
            // 
            _addButton.AutoSize = true;
            _addButton.Text = "Add";
            // 
            // _removeButton
            // 
            _removeButton.AutoSize = true;
            _removeButton.Text = "Remove";
            // 
            // _reloadButton
            // 
            _reloadButton.AutoSize = true;
            _reloadButton.Text = "Reload";
            // 
            // _saveButton
            // 
            _saveButton.AutoSize = true;
            _saveButton.Text = "Save";
            // 
            // _closeButton
            // 
            _closeButton.AutoSize = true;
            _closeButton.Text = "Close";
            // 
            // _statusStrip
            // 
            _statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { _statusLabel });
            _statusStrip.Location = new System.Drawing.Point(0, 0);
            _statusStrip.Name = "statusStrip1";
            _statusStrip.TabIndex = 0;
            // 
            // _statusLabel
            // 
            _statusLabel.Text = "Ready";
            // 
            // _buttonsPanel
            // 
            _buttonsPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            _buttonsPanel.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
            _buttonsPanel.AutoSize = true;
            _buttonsPanel.Controls.AddRange(new System.Windows.Forms.Control[] { _closeButton, _saveButton, _reloadButton, _removeButton, _addButton });
            // 
            // _root
            // 
            _root.Dock = System.Windows.Forms.DockStyle.Fill;
            _root.ColumnCount = 1;
            _root.RowCount = 3;
            _root.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            _root.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            _root.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            _root.Controls.Add(_headerPanel, 0, 0);
            _root.Controls.Add(_grid, 0, 1);
            _root.Controls.Add(_buttonsPanel, 0, 2);
            // 
            // LookupEditorForm
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            MinimumSize = new System.Drawing.Size(600, 400);
            Text = "Lookup Editor";
            Controls.Add(_root);
            Controls.Add(_statusStrip);
            _statusStrip.Dock = System.Windows.Forms.DockStyle.Bottom;
            Name = "LookupEditorForm";
            ((System.ComponentModel.ISupportInitialize)_grid).EndInit();
            _statusStrip.ResumeLayout(false);
            _statusStrip.PerformLayout();
            _root.ResumeLayout(false);
            _root.PerformLayout();
            _buttonsPanel.ResumeLayout(false);
            _buttonsPanel.PerformLayout();
            _headerPanel.ResumeLayout(false);
            _headerPanel.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }
    }
}
