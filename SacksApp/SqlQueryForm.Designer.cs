namespace SacksApp
{
    partial class SqlQueryForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.SplitContainer mainSplitContainer;
        private System.Windows.Forms.TextBox sqlTextBox;
    private System.Windows.Forms.Panel queryBuilderPanel;
    private System.Windows.Forms.Label tableLabel;
    private System.Windows.Forms.ComboBox tableComboBox;
    private System.Windows.Forms.Label columnsLabel;
    private System.Windows.Forms.CheckedListBox columnsCheckedListBox;
    private System.Windows.Forms.Label filtersLabel;
    private System.Windows.Forms.ComboBox filterColumnComboBox;
    private System.Windows.Forms.ComboBox filterOperatorComboBox;
    private System.Windows.Forms.TextBox filterValueTextBox;
    private System.Windows.Forms.Button addFilterButton;
    private System.Windows.Forms.ListBox filtersListBox;
    private System.Windows.Forms.Button removeFilterButton;
    private System.Windows.Forms.Label orderByLabel;
    private System.Windows.Forms.ComboBox orderByComboBox;
    private System.Windows.Forms.ComboBox orderByDirectionComboBox;
    private System.Windows.Forms.Label topLabel;
    private System.Windows.Forms.NumericUpDown topNumericUpDown;
    private System.Windows.Forms.Button buildButton;
        private System.Windows.Forms.DataGridView resultsGrid;
        private System.Windows.Forms.Panel buttonPanel;
        private System.Windows.Forms.Button executeButton;
        private System.Windows.Forms.Button clearButton;
        private System.Windows.Forms.Button exportButton;
        private System.Windows.Forms.Button columnsButton;
        private System.Windows.Forms.StatusStrip statusStrip;
        private System.Windows.Forms.ToolStripStatusLabel statusLabel;
        private System.Windows.Forms.ToolStripProgressBar progressBar;
        private System.Windows.Forms.Label sqlLabel;

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.mainSplitContainer = new System.Windows.Forms.SplitContainer();
            this.sqlLabel = new System.Windows.Forms.Label();
            this.queryBuilderPanel = new System.Windows.Forms.Panel();
            this.tableLabel = new System.Windows.Forms.Label();
            this.tableComboBox = new System.Windows.Forms.ComboBox();
            this.columnsLabel = new System.Windows.Forms.Label();
            this.columnsCheckedListBox = new System.Windows.Forms.CheckedListBox();
            this.filtersLabel = new System.Windows.Forms.Label();
            this.filterColumnComboBox = new System.Windows.Forms.ComboBox();
            this.filterOperatorComboBox = new System.Windows.Forms.ComboBox();
            this.filterValueTextBox = new System.Windows.Forms.TextBox();
            this.addFilterButton = new System.Windows.Forms.Button();
            this.filtersListBox = new System.Windows.Forms.ListBox();
            this.removeFilterButton = new System.Windows.Forms.Button();
            this.orderByLabel = new System.Windows.Forms.Label();
            this.orderByComboBox = new System.Windows.Forms.ComboBox();
            this.orderByDirectionComboBox = new System.Windows.Forms.ComboBox();
            this.topLabel = new System.Windows.Forms.Label();
            this.topNumericUpDown = new System.Windows.Forms.NumericUpDown();
            this.buildButton = new System.Windows.Forms.Button();
            this.sqlTextBox = new System.Windows.Forms.TextBox();
            this.buttonPanel = new System.Windows.Forms.Panel();
            this.executeButton = new System.Windows.Forms.Button();
            this.clearButton = new System.Windows.Forms.Button();
            this.exportButton = new System.Windows.Forms.Button();
            this.columnsButton = new System.Windows.Forms.Button();
            this.resultsGrid = new System.Windows.Forms.DataGridView();
            this.statusStrip = new System.Windows.Forms.StatusStrip();
            this.statusLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.progressBar = new System.Windows.Forms.ToolStripProgressBar();
            
            ((System.ComponentModel.ISupportInitialize)(this.mainSplitContainer)).BeginInit();
            this.mainSplitContainer.Panel1.SuspendLayout();
            this.mainSplitContainer.Panel2.SuspendLayout();
            this.mainSplitContainer.SuspendLayout();
            this.buttonPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.resultsGrid)).BeginInit();
            this.statusStrip.SuspendLayout();
            this.SuspendLayout();
            
            // 
            // mainSplitContainer
            // 
            this.mainSplitContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mainSplitContainer.Location = new System.Drawing.Point(0, 0);
            this.mainSplitContainer.Name = "mainSplitContainer";
            this.mainSplitContainer.Orientation = System.Windows.Forms.Orientation.Horizontal;
            this.mainSplitContainer.Size = new System.Drawing.Size(1200, 700);
            this.mainSplitContainer.SplitterDistance = 280;
            this.mainSplitContainer.TabIndex = 0;
            
            // 
            // mainSplitContainer.Panel1
            // 
            this.mainSplitContainer.Panel1.Controls.Add(this.sqlTextBox);
            this.mainSplitContainer.Panel1.Controls.Add(this.buttonPanel);
            this.mainSplitContainer.Panel1.Controls.Add(this.queryBuilderPanel);
            this.mainSplitContainer.Panel1.Controls.Add(this.sqlLabel);
            
            // 
            // mainSplitContainer.Panel2
            // 
            this.mainSplitContainer.Panel2.Controls.Add(this.resultsGrid);
            this.mainSplitContainer.Panel2.Controls.Add(this.statusStrip);
            
            // 
            // sqlLabel
            // 
            this.sqlLabel.AutoSize = true;
            this.sqlLabel.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.sqlLabel.Location = new System.Drawing.Point(12, 15);
            this.sqlLabel.Name = "sqlLabel";
            this.sqlLabel.Size = new System.Drawing.Size(78, 19);
            this.sqlLabel.TabIndex = 0;
            this.sqlLabel.Text = "SQL Query:";
            
            // 
            // queryBuilderPanel
            // 
            this.queryBuilderPanel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.queryBuilderPanel.Location = new System.Drawing.Point(12, 40);
            this.queryBuilderPanel.Name = "queryBuilderPanel";
            this.queryBuilderPanel.Size = new System.Drawing.Size(1176, 80);
            this.queryBuilderPanel.TabIndex = 1;

            // table label and combo
            this.tableLabel.AutoSize = true;
            this.tableLabel.Location = new System.Drawing.Point(3, 6);
            this.tableLabel.Name = "tableLabel";
            this.tableLabel.Size = new System.Drawing.Size(40, 15);
            this.tableLabel.Text = "Table:";
            this.tableComboBox.Location = new System.Drawing.Point(50, 3);
            this.tableComboBox.Name = "tableComboBox";
            this.tableComboBox.Size = new System.Drawing.Size(220, 23);
            this.tableComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;

            // columns label and checklist
            this.columnsLabel.AutoSize = true;
            this.columnsLabel.Location = new System.Drawing.Point(3, 36);
            this.columnsLabel.Name = "columnsLabel";
            this.columnsLabel.Size = new System.Drawing.Size(52, 15);
            this.columnsLabel.Text = "Columns:";
            this.columnsCheckedListBox.Location = new System.Drawing.Point(60, 33);
            this.columnsCheckedListBox.Name = "columnsCheckedListBox";
            this.columnsCheckedListBox.Size = new System.Drawing.Size(300, 40);

            // filters area
            this.filtersLabel.AutoSize = true;
            this.filtersLabel.Location = new System.Drawing.Point(380, 6);
            this.filtersLabel.Name = "filtersLabel";
            this.filtersLabel.Size = new System.Drawing.Size(42, 15);
            this.filtersLabel.Text = "Filters:";
            this.filterColumnComboBox.Location = new System.Drawing.Point(430, 3);
            this.filterColumnComboBox.Name = "filterColumnComboBox";
            this.filterColumnComboBox.Size = new System.Drawing.Size(180, 23);
            this.filterOperatorComboBox.Location = new System.Drawing.Point(620, 3);
            this.filterOperatorComboBox.Name = "filterOperatorComboBox";
            this.filterOperatorComboBox.Size = new System.Drawing.Size(80, 23);
            this.filterOperatorComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.filterValueTextBox.Location = new System.Drawing.Point(705, 3);
            this.filterValueTextBox.Name = "filterValueTextBox";
            this.filterValueTextBox.Size = new System.Drawing.Size(180, 23);
            this.addFilterButton.Location = new System.Drawing.Point(895, 3);
            this.addFilterButton.Name = "addFilterButton";
            this.addFilterButton.Size = new System.Drawing.Size(75, 24);
            this.addFilterButton.Text = "Add";
            this.removeFilterButton.Location = new System.Drawing.Point(980, 3);
            this.removeFilterButton.Name = "removeFilterButton";
            this.removeFilterButton.Size = new System.Drawing.Size(75, 24);
            this.removeFilterButton.Text = "Remove";
            this.filtersListBox.Location = new System.Drawing.Point(430, 33);
            this.filtersListBox.Name = "filtersListBox";
            this.filtersListBox.Size = new System.Drawing.Size(360, 40);

            // order by and top
            this.orderByLabel.AutoSize = true;
            this.orderByLabel.Location = new System.Drawing.Point(3, 76);
            this.orderByLabel.Name = "orderByLabel";
            this.orderByLabel.Size = new System.Drawing.Size(58, 15);
            this.orderByLabel.Text = "Order By:";
            this.orderByComboBox.Location = new System.Drawing.Point(70, 72);
            this.orderByComboBox.Name = "orderByComboBox";
            this.orderByComboBox.Size = new System.Drawing.Size(200, 23);
            this.orderByDirectionComboBox.Location = new System.Drawing.Point(280, 72);
            this.orderByDirectionComboBox.Name = "orderByDirectionComboBox";
            this.orderByDirectionComboBox.Size = new System.Drawing.Size(80, 23);
            this.orderByDirectionComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.orderByDirectionComboBox.Items.AddRange(new object[] {"ASC","DESC"});
            this.topLabel.AutoSize = true;
            this.topLabel.Location = new System.Drawing.Point(380, 76);
            this.topLabel.Name = "topLabel";
            this.topLabel.Size = new System.Drawing.Size(30, 15);
            this.topLabel.Text = "Top:";
            this.topNumericUpDown.Location = new System.Drawing.Point(420, 72);
            this.topNumericUpDown.Minimum = 0;
            this.topNumericUpDown.Maximum = 1000000;
            this.topNumericUpDown.Name = "topNumericUpDown";
            this.topNumericUpDown.Size = new System.Drawing.Size(80, 23);
            this.buildButton.Location = new System.Drawing.Point(520, 72);
            this.buildButton.Name = "buildButton";
            this.buildButton.Size = new System.Drawing.Size(120, 24);
            this.buildButton.Text = "Build & Execute";

            // add controls to queryBuilderPanel
            this.queryBuilderPanel.Controls.Add(this.tableLabel);
            this.queryBuilderPanel.Controls.Add(this.tableComboBox);
            this.queryBuilderPanel.Controls.Add(this.columnsLabel);
            this.queryBuilderPanel.Controls.Add(this.columnsCheckedListBox);
            this.queryBuilderPanel.Controls.Add(this.filtersLabel);
            this.queryBuilderPanel.Controls.Add(this.filterColumnComboBox);
            this.queryBuilderPanel.Controls.Add(this.filterOperatorComboBox);
            this.queryBuilderPanel.Controls.Add(this.filterValueTextBox);
            this.queryBuilderPanel.Controls.Add(this.addFilterButton);
            this.queryBuilderPanel.Controls.Add(this.removeFilterButton);
            this.queryBuilderPanel.Controls.Add(this.filtersListBox);
            this.queryBuilderPanel.Controls.Add(this.orderByLabel);
            this.queryBuilderPanel.Controls.Add(this.orderByComboBox);
            this.queryBuilderPanel.Controls.Add(this.orderByDirectionComboBox);
            this.queryBuilderPanel.Controls.Add(this.topLabel);
            this.queryBuilderPanel.Controls.Add(this.topNumericUpDown);
            this.queryBuilderPanel.Controls.Add(this.buildButton);
            // 
            // buttonPanel
            // 
            this.buttonPanel.Controls.Add(this.executeButton);
            this.buttonPanel.Controls.Add(this.clearButton);
            this.buttonPanel.Controls.Add(this.exportButton);
            this.buttonPanel.Controls.Add(this.columnsButton);
            this.buttonPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.buttonPanel.Location = new System.Drawing.Point(0, 240);
            this.buttonPanel.Name = "buttonPanel";
            this.buttonPanel.Size = new System.Drawing.Size(1200, 40);
            this.buttonPanel.TabIndex = 1;
            
            // 
            // executeButton
            // 
            this.executeButton.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(122)))), ((int)(((byte)(204)))));
            this.executeButton.ForeColor = System.Drawing.Color.White;
            this.executeButton.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.executeButton.Location = new System.Drawing.Point(12, 8);
            this.executeButton.Name = "executeButton";
            this.executeButton.Size = new System.Drawing.Size(120, 28);
            this.executeButton.TabIndex = 0;
            this.executeButton.Text = "? Execute (F5)";
            this.executeButton.UseVisualStyleBackColor = false;
            this.executeButton.Click += new System.EventHandler(this.ExecuteButton_Click);
            
            // 
            // clearButton
            // 
            this.clearButton.Location = new System.Drawing.Point(145, 8);
            this.clearButton.Name = "clearButton";
            this.clearButton.Size = new System.Drawing.Size(80, 28);
            this.clearButton.TabIndex = 1;
            this.clearButton.Text = "?? Clear";
            this.clearButton.UseVisualStyleBackColor = true;
            this.clearButton.Click += new System.EventHandler(this.ClearButton_Click);
            
            // 
            // exportButton
            // 
            this.exportButton.Location = new System.Drawing.Point(240, 8);
            this.exportButton.Name = "exportButton";
            this.exportButton.Size = new System.Drawing.Size(100, 28);
            this.exportButton.TabIndex = 2;
            this.exportButton.Text = "?? Export CSV";
            this.exportButton.UseVisualStyleBackColor = true;
            this.exportButton.Click += new System.EventHandler(this.ExportButton_Click);
            
            // 
            // columnsButton
            // 
            this.columnsButton.Location = new System.Drawing.Point(355, 8);
            this.columnsButton.Name = "columnsButton";
            this.columnsButton.Size = new System.Drawing.Size(120, 28);
            this.columnsButton.TabIndex = 3;
            this.columnsButton.Text = "?? Columns ?";
            this.columnsButton.UseVisualStyleBackColor = true;
            this.columnsButton.Click += new System.EventHandler(this.ColumnsButton_Click);
            
            // 
            // sqlTextBox
            // 
            this.sqlTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.sqlTextBox.Font = new System.Drawing.Font("Consolas", 10F);
            this.sqlTextBox.Location = new System.Drawing.Point(12, 40);
            this.sqlTextBox.Multiline = true;
            this.sqlTextBox.Name = "sqlTextBox";
            this.sqlTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.sqlTextBox.Size = new System.Drawing.Size(1176, 190);
            this.sqlTextBox.TabIndex = 2;
            this.sqlTextBox.WordWrap = false;
            
            // 
            // resultsGrid
            // 
            this.resultsGrid.AllowUserToAddRows = false;
            this.resultsGrid.AllowUserToDeleteRows = false;
            this.resultsGrid.AllowUserToOrderColumns = true;
            this.resultsGrid.AllowUserToResizeColumns = true;
            this.resultsGrid.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.resultsGrid.BackgroundColor = System.Drawing.SystemColors.Window;
            this.resultsGrid.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.resultsGrid.ClipboardCopyMode = System.Windows.Forms.DataGridViewClipboardCopyMode.EnableWithAutoHeaderText;
            this.resultsGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.resultsGrid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.resultsGrid.Location = new System.Drawing.Point(0, 0);
            this.resultsGrid.MultiSelect = true;
            this.resultsGrid.Name = "resultsGrid";
            this.resultsGrid.ReadOnly = true;
            this.resultsGrid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.resultsGrid.Size = new System.Drawing.Size(1200, 394);
            this.resultsGrid.TabIndex = 0;
            
            // 
            // statusStrip
            // 
            this.statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this.statusLabel,
                this.progressBar});
            this.statusStrip.Location = new System.Drawing.Point(0, 394);
            this.statusStrip.Name = "statusStrip";
            this.statusStrip.Size = new System.Drawing.Size(1200, 26);
            this.statusStrip.TabIndex = 1;
            this.statusStrip.Text = "statusStrip1";
            
            // 
            // statusLabel
            // 
            this.statusLabel.Name = "statusLabel";
            this.statusLabel.Size = new System.Drawing.Size(400, 21);
            this.statusLabel.Text = "Ready";
            
            // 
            // progressBar
            // 
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new System.Drawing.Size(200, 16);
            this.progressBar.Style = System.Windows.Forms.ProgressBarStyle.Marquee;
            this.progressBar.Visible = false;
            
            // 
            // SqlQueryForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1200, 700);
            this.Controls.Add(this.mainSplitContainer);
            this.KeyPreview = true;
            this.MinimumSize = new System.Drawing.Size(800, 500);
            this.Name = "SqlQueryForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "SQL Query Tool - Sacks Database";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            
            // Add keyboard shortcuts
            this.KeyDown += (s, e) => {
                if (e.KeyCode == Keys.F5) 
                {
                    ExecuteButton_Click(this, EventArgs.Empty);
                    e.Handled = true;
                }
            };
            
            this.mainSplitContainer.Panel1.ResumeLayout(false);
            this.mainSplitContainer.Panel1.PerformLayout();
            this.mainSplitContainer.Panel2.ResumeLayout(false);
            this.mainSplitContainer.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.mainSplitContainer)).EndInit();
            this.mainSplitContainer.ResumeLayout(false);
            this.buttonPanel.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.resultsGrid)).EndInit();
            this.statusStrip.ResumeLayout(false);
            this.statusStrip.PerformLayout();
            this.ResumeLayout(false);
        }

        #endregion
    }
}