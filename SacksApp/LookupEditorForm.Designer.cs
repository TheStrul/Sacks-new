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
            _titleLabel = new ModernWinForms.Controls.ModernLabel();
            _lookupCombo = new ModernWinForms.Controls.ModernComboBox();
            _grid = new ModernWinForms.Controls.ModernDataGridView();
            _addButton = new ModernWinForms.Controls.ModernButton();
            _removeButton = new ModernWinForms.Controls.ModernButton();
            _reloadButton = new ModernWinForms.Controls.ModernButton();
            _saveButton = new ModernWinForms.Controls.ModernButton();
            _closeButton = new ModernWinForms.Controls.ModernButton();
            _statusStrip = new ModernWinForms.Controls.ModernStatusStrip();
            _statusLabel = new ToolStripStatusLabel();
            _root = new ModernWinForms.Controls.ModernTableLayoutPanel();
            _headerPanel = new ModernWinForms.Controls.ModernFlowLayoutPanel();
            _buttonsPanel = new ModernWinForms.Controls.ModernFlowLayoutPanel();
            ((System.ComponentModel.ISupportInitialize)_grid).BeginInit();
            _statusStrip.SuspendLayout();
            _root.SuspendLayout();
            _headerPanel.SuspendLayout();
            _buttonsPanel.SuspendLayout();
            SuspendLayout();
            // 
            // _titleLabel
            // 
            _titleLabel.AutoSize = true;
            _titleLabel.BackColor = Color.FromArgb(243, 243, 243);
            _titleLabel.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            _titleLabel.ForeColor = Color.FromArgb(50, 49, 48);
            _titleLabel.Location = new Point(0, 8);
            _titleLabel.Margin = new Padding(0, 8, 8, 0);
            _titleLabel.Name = "_titleLabel";
            _titleLabel.Size = new Size(65, 20);
            _titleLabel.TabIndex = 0;
            _titleLabel.Text = "Lookup:";
            // 
            // _lookupCombo
            // 
            _lookupCombo.BackColor = Color.FromArgb(255, 255, 255);
            _lookupCombo.DrawMode = DrawMode.OwnerDrawFixed;
            _lookupCombo.DropDownStyle = ComboBoxStyle.DropDownList;
            _lookupCombo.FlatStyle = FlatStyle.Flat;
            _lookupCombo.Font = new Font("Segoe UI", 9F);
            _lookupCombo.ForeColor = Color.FromArgb(50, 49, 48);
            _lookupCombo.Location = new Point(76, 3);
            _lookupCombo.Name = "_lookupCombo";
            _lookupCombo.Size = new Size(260, 24);
            _lookupCombo.TabIndex = 1;
            // 
            // _grid
            // 
            _grid.AllowUserToAddRows = false;
            _grid.AllowUserToResizeRows = false;
            _grid.BackgroundColor = Color.FromArgb(255, 255, 255);
            _grid.Dock = DockStyle.Fill;
            _grid.Font = new Font("Segoe UI", 9F);
            _grid.GridColor = Color.FromArgb(138, 136, 134);
            _grid.Location = new Point(3, 38);
            _grid.Name = "_grid";
            _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            _grid.Size = new Size(578, 259);
            _grid.TabIndex = 1;
            // 
            // _addButton
            // 
            _addButton.AutoSize = true;
            _addButton.BackColor = Color.Transparent;
            _addButton.FlatStyle = FlatStyle.Flat;
            _addButton.Location = new Point(176, 3);
            _addButton.Name = "_addButton";
            _addButton.Size = new Size(75, 27);
            _addButton.TabIndex = 4;
            _addButton.Text = "Add";
            _addButton.UseVisualStyleBackColor = false;
            // 
            // _removeButton
            // 
            _removeButton.AutoSize = true;
            _removeButton.BackColor = Color.Transparent;
            _removeButton.FlatStyle = FlatStyle.Flat;
            _removeButton.Location = new Point(257, 3);
            _removeButton.Name = "_removeButton";
            _removeButton.Size = new Size(75, 27);
            _removeButton.TabIndex = 3;
            _removeButton.Text = "Remove";
            _removeButton.UseVisualStyleBackColor = false;
            // 
            // _reloadButton
            // 
            _reloadButton.AutoSize = true;
            _reloadButton.BackColor = Color.Transparent;
            _reloadButton.FlatStyle = FlatStyle.Flat;
            _reloadButton.Location = new Point(338, 3);
            _reloadButton.Name = "_reloadButton";
            _reloadButton.Size = new Size(75, 27);
            _reloadButton.TabIndex = 2;
            _reloadButton.Text = "Reload";
            _reloadButton.UseVisualStyleBackColor = false;
            // 
            // _saveButton
            // 
            _saveButton.AutoSize = true;
            _saveButton.BackColor = Color.Transparent;
            _saveButton.FlatStyle = FlatStyle.Flat;
            _saveButton.Location = new Point(419, 3);
            _saveButton.Name = "_saveButton";
            _saveButton.Size = new Size(75, 27);
            _saveButton.TabIndex = 1;
            _saveButton.Text = "Save";
            _saveButton.UseVisualStyleBackColor = false;
            // 
            // _closeButton
            // 
            _closeButton.AutoSize = true;
            _closeButton.BackColor = Color.Transparent;
            _closeButton.FlatStyle = FlatStyle.Flat;
            _closeButton.Location = new Point(500, 3);
            _closeButton.Name = "_closeButton";
            _closeButton.Size = new Size(75, 27);
            _closeButton.TabIndex = 0;
            _closeButton.Text = "Close";
            _closeButton.UseVisualStyleBackColor = false;
            // 
            // _statusStrip
            // 
            _statusStrip.BackColor = Color.FromArgb(243, 242, 241);
            _statusStrip.Font = new Font("Segoe UI", 9F);
            _statusStrip.ForeColor = Color.FromArgb(50, 49, 48);
            _statusStrip.Items.AddRange(new ToolStripItem[] { _statusLabel });
            _statusStrip.Location = new Point(0, 339);
            _statusStrip.Name = "_statusStrip";
            _statusStrip.Size = new Size(584, 22);
            _statusStrip.TabIndex = 0;
            // 
            // _statusLabel
            // 
            _statusLabel.Name = "_statusLabel";
            _statusLabel.Size = new Size(39, 17);
            _statusLabel.Text = "Ready";
            // 
            // _root
            // 
            _root.BackColor = Color.FromArgb(243, 243, 243);
            _root.ColumnCount = 1;
            _root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 20F));
            _root.Controls.Add(_headerPanel, 0, 0);
            _root.Controls.Add(_grid, 0, 1);
            _root.Controls.Add(_buttonsPanel, 0, 2);
            _root.Dock = DockStyle.Fill;
            _root.ForeColor = Color.FromArgb(50, 49, 48);
            _root.Location = new Point(0, 0);
            _root.Name = "_root";
            _root.RowCount = 3;
            _root.RowStyles.Add(new RowStyle());
            _root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            _root.RowStyles.Add(new RowStyle());
            _root.Size = new Size(584, 339);
            _root.TabIndex = 0;
            // 
            // _headerPanel
            // 
            _headerPanel.AutoSize = true;
            _headerPanel.BackColor = Color.FromArgb(243, 243, 243);
            _headerPanel.Controls.Add(_titleLabel);
            _headerPanel.Controls.Add(_lookupCombo);
            _headerPanel.Dock = DockStyle.Fill;
            _headerPanel.ForeColor = Color.FromArgb(50, 49, 48);
            _headerPanel.Location = new Point(3, 3);
            _headerPanel.Name = "_headerPanel";
            _headerPanel.Size = new Size(578, 29);
            _headerPanel.TabIndex = 0;
            // 
            // _buttonsPanel
            // 
            _buttonsPanel.AutoSize = true;
            _buttonsPanel.BackColor = Color.FromArgb(243, 243, 243);
            _buttonsPanel.Controls.Add(_closeButton);
            _buttonsPanel.Controls.Add(_saveButton);
            _buttonsPanel.Controls.Add(_reloadButton);
            _buttonsPanel.Controls.Add(_removeButton);
            _buttonsPanel.Controls.Add(_addButton);
            _buttonsPanel.Dock = DockStyle.Fill;
            _buttonsPanel.FlowDirection = FlowDirection.RightToLeft;
            _buttonsPanel.ForeColor = Color.FromArgb(50, 49, 48);
            _buttonsPanel.Location = new Point(3, 303);
            _buttonsPanel.Name = "_buttonsPanel";
            _buttonsPanel.Size = new Size(578, 33);
            _buttonsPanel.TabIndex = 2;
            // 
            // LookupEditorForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(584, 361);
            Controls.Add(_root);
            Controls.Add(_statusStrip);
            MinimumSize = new Size(600, 400);
            Name = "LookupEditorForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Lookup Editor";
            ((System.ComponentModel.ISupportInitialize)_grid).EndInit();
            _statusStrip.ResumeLayout(false);
            _statusStrip.PerformLayout();
            _root.ResumeLayout(false);
            _root.PerformLayout();
            _headerPanel.ResumeLayout(false);
            _headerPanel.PerformLayout();
            _buttonsPanel.ResumeLayout(false);
            _buttonsPanel.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }
    }
}
