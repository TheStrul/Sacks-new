namespace SacksApp
{
    partial class SqlQueryForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private ModernWinForms.Controls.ModernSplitContainer mainSplitContainer;
        private ModernWinForms.Controls.ModernComboBox filterColumnComboBox;
        private ModernWinForms.Controls.ModernComboBox filterOperatorComboBox;
        private ModernWinForms.Controls.ModernTextBox filterValueTextBox;
        private ModernWinForms.Controls.ModernButton addFilterButton;
        private System.Windows.Forms.CheckedListBox filtersListBox;
        private ModernWinForms.Controls.ModernButton removeFilterButton;
        private ModernWinForms.Controls.ModernButton runQueryButton;
        private ModernWinForms.Controls.ModernDataGridView resultsGrid;
        private ModernWinForms.Controls.ModernStatusStrip statusStrip;
        private System.Windows.Forms.ToolStripStatusLabel statusLabel;
        private System.Windows.Forms.ToolStripProgressBar progressBar;
        private ModernWinForms.Controls.ModernTableLayoutPanel tableLayoutFilters;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
        private System.ComponentModel.IContainer components;
        private System.Windows.Forms.ToolStripMenuItem OpenLookupsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem showHideCoulmnsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem groupByProductToolStripMenuItem;
        private ModernWinForms.Controls.ModernButton buttonHideFilters;
        private ModernWinForms.Controls.ModernButton buttonShowFilter;

        // New edit controls
        private ModernWinForms.Controls.ModernFlowLayoutPanel editControlsPanel;
        private ModernWinForms.Controls.ModernCheckBox editModeCheckBox;
        private ModernWinForms.Controls.ModernButton saveChangesButton;
        private ModernWinForms.Controls.ModernButton cancelAllButton;

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            DataGridViewCellStyle dataGridViewCellStyle1 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle2 = new DataGridViewCellStyle();
            mainSplitContainer = new ModernWinForms.Controls.ModernSplitContainer();
            tableLayoutFilters = new ModernWinForms.Controls.ModernTableLayoutPanel();
            addFilterButton = new ModernWinForms.Controls.ModernButton();
            filterOperatorComboBox = new ModernWinForms.Controls.ModernComboBox();
            filterColumnComboBox = new ModernWinForms.Controls.ModernComboBox();
            filterValueTextBox = new ModernWinForms.Controls.ModernTextBox();
            removeFilterButton = new ModernWinForms.Controls.ModernButton();
            filtersListBox = new CheckedListBox();
            buttonHideFilters = new ModernWinForms.Controls.ModernButton();
            buttonShowFilter = new ModernWinForms.Controls.ModernButton();
            resultsGrid = new ModernWinForms.Controls.ModernDataGridView();
            contextMenuStrip1 = new ContextMenuStrip(components);
            OpenLookupsToolStripMenuItem = new ToolStripMenuItem();
            showHideCoulmnsToolStripMenuItem = new ToolStripMenuItem();
            groupByProductToolStripMenuItem = new ToolStripMenuItem();
            editControlsPanel = new ModernWinForms.Controls.ModernFlowLayoutPanel();
            editModeCheckBox = new ModernWinForms.Controls.ModernCheckBox();
            saveChangesButton = new ModernWinForms.Controls.ModernButton();
            cancelAllButton = new ModernWinForms.Controls.ModernButton();
            runQueryButton = new ModernWinForms.Controls.ModernButton();
            statusStrip = new ModernWinForms.Controls.ModernStatusStrip();
            statusLabel = new ToolStripStatusLabel();
            progressBar = new ToolStripProgressBar();
            ((System.ComponentModel.ISupportInitialize)mainSplitContainer).BeginInit();
            mainSplitContainer.Panel1.SuspendLayout();
            mainSplitContainer.Panel2.SuspendLayout();
            mainSplitContainer.SuspendLayout();
            tableLayoutFilters.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)resultsGrid).BeginInit();
            contextMenuStrip1.SuspendLayout();
            editControlsPanel.SuspendLayout();
            statusStrip.SuspendLayout();
            SuspendLayout();
            // 
            // mainSplitContainer
            // 
            mainSplitContainer.BackColor = Color.FromArgb(243, 242, 241);
            mainSplitContainer.Dock = DockStyle.Fill;
            mainSplitContainer.FixedPanel = FixedPanel.Panel1;
            mainSplitContainer.Location = new Point(3, 3);
            mainSplitContainer.Name = "mainSplitContainer";
            mainSplitContainer.Orientation = Orientation.Horizontal;
            // 
            // mainSplitContainer.Panel1
            // 
            mainSplitContainer.Panel1.Controls.Add(tableLayoutFilters);
            mainSplitContainer.Panel1.Controls.Add(buttonHideFilters);
            mainSplitContainer.Panel1.Controls.Add(buttonShowFilter);
            mainSplitContainer.Panel1.Margin = new Padding(5);
            mainSplitContainer.Panel1.Padding = new Padding(5);
            mainSplitContainer.Panel1MinSize = 60;
            // 
            // mainSplitContainer.Panel2
            // 
            mainSplitContainer.Panel2.Controls.Add(resultsGrid);
            mainSplitContainer.Panel2.Controls.Add(editControlsPanel);
            mainSplitContainer.Panel2.Controls.Add(runQueryButton);
            mainSplitContainer.Panel2.Controls.Add(statusStrip);
            mainSplitContainer.Size = new Size(1194, 773);
            mainSplitContainer.SplitterDistance = 68;
            mainSplitContainer.TabIndex = 0;
            // 
            // tableLayoutFilters
            // 
            tableLayoutFilters.BackColor = Color.FromArgb(243, 243, 243);
            tableLayoutFilters.ColumnCount = 2;
            tableLayoutFilters.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tableLayoutFilters.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50.0000076F));
            tableLayoutFilters.Controls.Add(addFilterButton, 0, 2);
            tableLayoutFilters.Controls.Add(filterOperatorComboBox, 1, 0);
            tableLayoutFilters.Controls.Add(filterColumnComboBox, 0, 0);
            tableLayoutFilters.Controls.Add(filterValueTextBox, 0, 1);
            tableLayoutFilters.Controls.Add(removeFilterButton, 1, 2);
            tableLayoutFilters.Controls.Add(filtersListBox, 0, 3);
            tableLayoutFilters.Dock = DockStyle.Fill;
            tableLayoutFilters.ForeColor = Color.FromArgb(50, 49, 48);
            tableLayoutFilters.Location = new Point(5, 127);
            tableLayoutFilters.Margin = new Padding(5);
            tableLayoutFilters.Name = "tableLayoutFilters";
            tableLayoutFilters.RowCount = 4;
            tableLayoutFilters.RowStyles.Add(new RowStyle());
            tableLayoutFilters.RowStyles.Add(new RowStyle());
            tableLayoutFilters.RowStyles.Add(new RowStyle());
            tableLayoutFilters.RowStyles.Add(new RowStyle());
            tableLayoutFilters.Size = new Size(1184, 0);
            tableLayoutFilters.TabIndex = 16;
            tableLayoutFilters.Visible = false;
            // 
            // addFilterButton
            // 
            addFilterButton.Anchor = AnchorStyles.Top;
            addFilterButton.BackColor = Color.Transparent;
            addFilterButton.FlatStyle = FlatStyle.Flat;
            addFilterButton.Location = new Point(188, 85);
            addFilterButton.Name = "addFilterButton";
            addFilterButton.Size = new Size(215, 73);
            addFilterButton.TabIndex = 7;
            addFilterButton.Text = "Add";
            addFilterButton.UseVisualStyleBackColor = false;
            addFilterButton.Click += AddFilterButton_Click;
            // 
            // filterOperatorComboBox
            // 
            filterOperatorComboBox.BackColor = Color.FromArgb(255, 255, 255);
            filterOperatorComboBox.Dock = DockStyle.Top;
            filterOperatorComboBox.DrawMode = DrawMode.OwnerDrawFixed;
            filterOperatorComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            filterOperatorComboBox.FlatStyle = FlatStyle.Flat;
            filterOperatorComboBox.Font = new Font("Segoe UI", 9F);
            filterOperatorComboBox.ForeColor = Color.FromArgb(50, 49, 48);
            filterOperatorComboBox.Location = new Point(594, 3);
            filterOperatorComboBox.Name = "filterOperatorComboBox";
            filterOperatorComboBox.Size = new Size(587, 24);
            filterOperatorComboBox.TabIndex = 5;
            // 
            // filterColumnComboBox
            // 
            filterColumnComboBox.BackColor = Color.FromArgb(255, 255, 255);
            filterColumnComboBox.Dock = DockStyle.Top;
            filterColumnComboBox.DrawMode = DrawMode.OwnerDrawFixed;
            filterColumnComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            filterColumnComboBox.FlatStyle = FlatStyle.Flat;
            filterColumnComboBox.Font = new Font("Segoe UI", 9F);
            filterColumnComboBox.ForeColor = Color.FromArgb(50, 49, 48);
            filterColumnComboBox.Location = new Point(3, 3);
            filterColumnComboBox.Name = "filterColumnComboBox";
            filterColumnComboBox.Size = new Size(585, 24);
            filterColumnComboBox.TabIndex = 4;
            filterColumnComboBox.SelectedIndexChanged += FilterColumnComboBox_SelectedIndexChanged;
            // 
            // filterValueTextBox
            // 
            filterValueTextBox.BackColor = Color.FromArgb(255, 255, 255);
            tableLayoutFilters.SetColumnSpan(filterValueTextBox, 2);
            filterValueTextBox.Dock = DockStyle.Fill;
            filterValueTextBox.Location = new Point(3, 33);
            filterValueTextBox.Multiline = true;
            filterValueTextBox.Name = "filterValueTextBox";
            filterValueTextBox.ScrollBars = ScrollBars.None;
            filterValueTextBox.Size = new Size(1178, 46);
            filterValueTextBox.TabIndex = 6;
            filterValueTextBox.WordWrap = true;
            // 
            // removeFilterButton
            // 
            removeFilterButton.Anchor = AnchorStyles.Top;
            removeFilterButton.BackColor = Color.Transparent;
            removeFilterButton.FlatStyle = FlatStyle.Flat;
            removeFilterButton.Location = new Point(780, 85);
            removeFilterButton.Name = "removeFilterButton";
            removeFilterButton.Size = new Size(215, 73);
            removeFilterButton.TabIndex = 8;
            removeFilterButton.Text = "Remove";
            removeFilterButton.UseVisualStyleBackColor = false;
            removeFilterButton.Click += RemoveFilterButton_Click;
            // 
            // filtersListBox
            // 
            filtersListBox.CheckOnClick = true;
            tableLayoutFilters.SetColumnSpan(filtersListBox, 2);
            filtersListBox.Dock = DockStyle.Fill;
            filtersListBox.FormattingEnabled = true;
            filtersListBox.Location = new Point(3, 164);
            filtersListBox.Name = "filtersListBox";
            filtersListBox.Size = new Size(1178, 81);
            filtersListBox.TabIndex = 11;
            filtersListBox.ItemCheck += FiltersListBox_ItemCheck;
            // 
            // buttonHideFilters
            // 
            buttonHideFilters.BackColor = Color.Transparent;
            buttonHideFilters.Dock = DockStyle.Top;
            buttonHideFilters.FlatStyle = FlatStyle.Flat;
            buttonHideFilters.Location = new Point(5, 67);
            buttonHideFilters.Name = "buttonHideFilters";
            buttonHideFilters.Size = new Size(1184, 60);
            buttonHideFilters.TabIndex = 15;
            buttonHideFilters.Text = "Hide filters";
            buttonHideFilters.UseVisualStyleBackColor = false;
            buttonHideFilters.Visible = false;
            buttonHideFilters.Click += ButtonHideFilters_Click;
            // 
            // buttonShowFilter
            // 
            buttonShowFilter.BackColor = Color.Transparent;
            buttonShowFilter.Dock = DockStyle.Top;
            buttonShowFilter.FlatStyle = FlatStyle.Flat;
            buttonShowFilter.Location = new Point(5, 5);
            buttonShowFilter.Name = "buttonShowFilter";
            buttonShowFilter.Size = new Size(1184, 62);
            buttonShowFilter.TabIndex = 15;
            buttonShowFilter.Text = "Show filters";
            buttonShowFilter.UseVisualStyleBackColor = false;
            buttonShowFilter.Click += ButtonShowFilter_Click;
            // 
            // resultsGrid
            // 
            resultsGrid.AllowUserToAddRows = false;
            resultsGrid.AllowUserToDeleteRows = false;
            resultsGrid.AllowUserToOrderColumns = true;
            resultsGrid.AllowUserToResizeRows = false;
            dataGridViewCellStyle1.BackColor = Color.FromArgb(224, 224, 224);
            resultsGrid.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle1;
            resultsGrid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.DisplayedCells;
            resultsGrid.BackgroundColor = SystemColors.Window;
            resultsGrid.BorderStyle = BorderStyle.Fixed3D;
            resultsGrid.ClipboardCopyMode = DataGridViewClipboardCopyMode.EnableAlwaysIncludeHeaderText;
            resultsGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            resultsGrid.ContextMenuStrip = contextMenuStrip1;
            dataGridViewCellStyle2.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle2.BackColor = SystemColors.Window;
            dataGridViewCellStyle2.Font = new Font("Segoe UI", 9F);
            dataGridViewCellStyle2.ForeColor = Color.FromArgb(50, 49, 48);
            dataGridViewCellStyle2.SelectionBackColor = SystemColors.Highlight;
            dataGridViewCellStyle2.SelectionForeColor = SystemColors.HighlightText;
            dataGridViewCellStyle2.WrapMode = DataGridViewTriState.False;
            resultsGrid.DefaultCellStyle = dataGridViewCellStyle2;
            resultsGrid.Dock = DockStyle.Fill;
            resultsGrid.EditMode = DataGridViewEditMode.EditOnEnter;
            resultsGrid.Font = new Font("Segoe UI", 9F);
            resultsGrid.GridColor = Color.FromArgb(138, 136, 134);
            resultsGrid.Location = new Point(0, 45);
            resultsGrid.MultiSelect = false;
            resultsGrid.Name = "resultsGrid";
            resultsGrid.SelectionMode = DataGridViewSelectionMode.CellSelect;
            resultsGrid.ShowEditingIcon = false;
            resultsGrid.Size = new Size(1194, 574);
            resultsGrid.TabIndex = 0;
            resultsGrid.CellEndEdit += ResultsGrid_CellEndEdit;
            resultsGrid.CellMouseUp += ResultsGrid_CellMouseUp;
            resultsGrid.EditingControlShowing += ResultsGrid_EditingControlShowing;
            // 
            // contextMenuStrip1
            // 
            contextMenuStrip1.Items.AddRange(new ToolStripItem[] { OpenLookupsToolStripMenuItem, showHideCoulmnsToolStripMenuItem, groupByProductToolStripMenuItem });
            contextMenuStrip1.Name = "contextMenuStrip1";
            contextMenuStrip1.Size = new Size(191, 70);
            // 
            // OpenLookupsToolStripMenuItem
            // 
            OpenLookupsToolStripMenuItem.Name = "OpenLookupsToolStripMenuItem";
            OpenLookupsToolStripMenuItem.Size = new Size(190, 22);
            OpenLookupsToolStripMenuItem.Text = "Open Dictionaries...";
            OpenLookupsToolStripMenuItem.Click += OpenLookupsToolStripMenuItem_Click;
            // 
            // showHideCoulmnsToolStripMenuItem
            // 
            showHideCoulmnsToolStripMenuItem.Name = "showHideCoulmnsToolStripMenuItem";
            showHideCoulmnsToolStripMenuItem.Size = new Size(190, 22);
            showHideCoulmnsToolStripMenuItem.Text = "Show / Hide Coulmns";
            showHideCoulmnsToolStripMenuItem.Click += ShowHideCoulmnsToolStripMenuItem_Click;
            // 
            // groupByProductToolStripMenuItem
            // 
            groupByProductToolStripMenuItem.CheckOnClick = true;
            groupByProductToolStripMenuItem.Name = "groupByProductToolStripMenuItem";
            groupByProductToolStripMenuItem.Size = new Size(190, 22);
            groupByProductToolStripMenuItem.Text = "Group by Product";
            // 
            // editControlsPanel
            // 
            editControlsPanel.AutoSize = true;
            editControlsPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            editControlsPanel.BackColor = Color.FromArgb(243, 243, 243);
            editControlsPanel.Controls.Add(editModeCheckBox);
            editControlsPanel.Controls.Add(saveChangesButton);
            editControlsPanel.Controls.Add(cancelAllButton);
            editControlsPanel.Dock = DockStyle.Top;
            editControlsPanel.ForeColor = Color.FromArgb(50, 49, 48);
            editControlsPanel.Location = new Point(0, 0);
            editControlsPanel.Name = "editControlsPanel";
            editControlsPanel.Padding = new Padding(6);
            editControlsPanel.Size = new Size(1194, 45);
            editControlsPanel.TabIndex = 18;
            // 
            // editModeCheckBox
            // 
            editModeCheckBox.AutoSize = true;
            editModeCheckBox.BackColor = Color.Transparent;
            editModeCheckBox.FlatStyle = FlatStyle.Flat;
            editModeCheckBox.Location = new Point(9, 14);
            editModeCheckBox.Margin = new Padding(3, 8, 12, 3);
            editModeCheckBox.Name = "editModeCheckBox";
            editModeCheckBox.Size = new Size(77, 19);
            editModeCheckBox.TabIndex = 0;
            editModeCheckBox.Text = "Edit mode";
            editModeCheckBox.UseVisualStyleBackColor = false;
            editModeCheckBox.CheckedChanged += EditModeCheckBox_CheckedChanged;
            // 
            // saveChangesButton
            // 
            saveChangesButton.AutoSize = true;
            saveChangesButton.BackColor = Color.Transparent;
            saveChangesButton.Enabled = false;
            saveChangesButton.FlatStyle = FlatStyle.Flat;
            saveChangesButton.Location = new Point(101, 9);
            saveChangesButton.Name = "saveChangesButton";
            saveChangesButton.Size = new Size(90, 27);
            saveChangesButton.TabIndex = 1;
            saveChangesButton.Text = "Save changes";
            saveChangesButton.UseVisualStyleBackColor = false;
            saveChangesButton.Click += SaveChangesButton_Click;
            // 
            // cancelAllButton
            // 
            cancelAllButton.AutoSize = true;
            cancelAllButton.BackColor = Color.Transparent;
            cancelAllButton.Enabled = false;
            cancelAllButton.FlatStyle = FlatStyle.Flat;
            cancelAllButton.Location = new Point(200, 9);
            cancelAllButton.Margin = new Padding(6, 3, 3, 3);
            cancelAllButton.Name = "cancelAllButton";
            cancelAllButton.Size = new Size(75, 27);
            cancelAllButton.TabIndex = 2;
            cancelAllButton.Text = "Cancel All";
            cancelAllButton.UseVisualStyleBackColor = false;
            cancelAllButton.Click += CancelAllButton_Click;
            // 
            // runQueryButton
            // 
            runQueryButton.BackColor = Color.Transparent;
            runQueryButton.Dock = DockStyle.Bottom;
            runQueryButton.FlatStyle = FlatStyle.Flat;
            runQueryButton.Location = new Point(0, 619);
            runQueryButton.Margin = new Padding(20);
            runQueryButton.Name = "runQueryButton";
            runQueryButton.Size = new Size(1194, 60);
            runQueryButton.TabIndex = 16;
            runQueryButton.Text = "Run Query";
            runQueryButton.UseVisualStyleBackColor = false;
            runQueryButton.Click += RunQueryButton_Click;
            // 
            // statusStrip
            // 
            statusStrip.BackColor = Color.FromArgb(243, 242, 241);
            statusStrip.Font = new Font("Segoe UI", 9F);
            statusStrip.ForeColor = Color.FromArgb(50, 49, 48);
            statusStrip.Items.AddRange(new ToolStripItem[] { statusLabel, progressBar });
            statusStrip.Location = new Point(0, 679);
            statusStrip.Name = "statusStrip";
            statusStrip.Size = new Size(1194, 22);
            statusStrip.TabIndex = 1;
            // 
            // statusLabel
            // 
            statusLabel.Name = "statusLabel";
            statusLabel.Size = new Size(39, 17);
            statusLabel.Text = "Ready";
            // 
            // progressBar
            // 
            progressBar.Name = "progressBar";
            progressBar.Size = new Size(200, 18);
            progressBar.Style = ProgressBarStyle.Marquee;
            progressBar.Visible = false;
            // 
            // SqlQueryForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1200, 779);
            Controls.Add(mainSplitContainer);
            KeyPreview = true;
            MinimumSize = new Size(900, 550);
            Name = "SqlQueryForm";
            Padding = new Padding(3);
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Product Offers Explorer";
            WindowState = FormWindowState.Maximized;
            mainSplitContainer.Panel1.ResumeLayout(false);
            mainSplitContainer.Panel2.ResumeLayout(false);
            mainSplitContainer.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)mainSplitContainer).EndInit();
            mainSplitContainer.ResumeLayout(false);
            tableLayoutFilters.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)resultsGrid).EndInit();
            contextMenuStrip1.ResumeLayout(false);
            editControlsPanel.ResumeLayout(false);
            editControlsPanel.PerformLayout();
            statusStrip.ResumeLayout(false);
            statusStrip.PerformLayout();
            ResumeLayout(false);
        }

        #endregion
    }
}
