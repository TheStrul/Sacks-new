namespace SacksApp
{
    partial class SqlQueryForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.Windows.Forms.SplitContainer mainSplitContainer;
    private System.Windows.Forms.CheckedListBox columnsCheckedListBox;
    private System.Windows.Forms.ComboBox filterColumnComboBox;
    private System.Windows.Forms.ComboBox filterOperatorComboBox;
    private System.Windows.Forms.TextBox filterValueTextBox;
    private System.Windows.Forms.Button addFilterButton;
    private System.Windows.Forms.ListBox filtersListBox;
    private System.Windows.Forms.Button removeFilterButton;
    private System.Windows.Forms.Button runQueryButton;
        private System.Windows.Forms.DataGridView resultsGrid;
        private System.Windows.Forms.StatusStrip statusStrip;
        private System.Windows.Forms.ToolStripStatusLabel statusLabel;
        private System.Windows.Forms.ToolStripProgressBar progressBar;

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            DataGridViewCellStyle dataGridViewCellStyle2 = new DataGridViewCellStyle();
            mainSplitContainer = new SplitContainer();
            tableLayoutPanel1 = new TableLayoutPanel();
            filtersListBox = new ListBox();
            runQueryButton = new Button();
            addFilterButton = new Button();
            filterOperatorComboBox = new ComboBox();
            filterColumnComboBox = new ComboBox();
            labelFilter = new Label();
            filterValueTextBox = new TextBox();
            columnsCheckedListBox = new CheckedListBox();
            removeFilterButton = new Button();
            collapseProductsCheckBox = new CheckBox();
            sqlLabel = new TextBox();
            resultsGrid = new DataGridView();
            statusStrip = new StatusStrip();
            statusLabel = new ToolStripStatusLabel();
            progressBar = new ToolStripProgressBar();
            contextMenuStrip1 = new ContextMenuStrip(components);
            addToLookupToolStripMenuItem = new ToolStripMenuItem();
            ((System.ComponentModel.ISupportInitialize)mainSplitContainer).BeginInit();
            mainSplitContainer.Panel1.SuspendLayout();
            mainSplitContainer.Panel2.SuspendLayout();
            mainSplitContainer.SuspendLayout();
            tableLayoutPanel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)resultsGrid).BeginInit();
            statusStrip.SuspendLayout();
            contextMenuStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // mainSplitContainer
            // 
            mainSplitContainer.Dock = DockStyle.Fill;
            mainSplitContainer.FixedPanel = FixedPanel.Panel1;
            mainSplitContainer.Location = new Point(0, 0);
            mainSplitContainer.Name = "mainSplitContainer";
            mainSplitContainer.Orientation = Orientation.Horizontal;
            // 
            // mainSplitContainer.Panel1
            // 
            mainSplitContainer.Panel1.Controls.Add(tableLayoutPanel1);
            mainSplitContainer.Panel1.Margin = new Padding(5);
            mainSplitContainer.Panel1.Padding = new Padding(5);
            // 
            // mainSplitContainer.Panel2
            // 
            mainSplitContainer.Panel2.Controls.Add(resultsGrid);
            mainSplitContainer.Panel2.Controls.Add(statusStrip);
            mainSplitContainer.Size = new Size(1200, 779);
            mainSplitContainer.SplitterDistance = 299;
            mainSplitContainer.TabIndex = 0;
            // 
            // tableLayoutPanel1
            // 
            tableLayoutPanel1.ColumnCount = 5;
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle());
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle());
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle());
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle());
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanel1.Controls.Add(filtersListBox, 1, 3);
            tableLayoutPanel1.Controls.Add(runQueryButton, 0, 4);
            tableLayoutPanel1.Controls.Add(addFilterButton, 1, 2);
            tableLayoutPanel1.Controls.Add(filterOperatorComboBox, 2, 1);
            tableLayoutPanel1.Controls.Add(filterColumnComboBox, 1, 1);
            tableLayoutPanel1.Controls.Add(labelFilter, 1, 0);
            tableLayoutPanel1.Controls.Add(filterValueTextBox, 3, 1);
            tableLayoutPanel1.Controls.Add(columnsCheckedListBox, 0, 0);
            tableLayoutPanel1.Controls.Add(removeFilterButton, 3, 2);
            tableLayoutPanel1.Controls.Add(collapseProductsCheckBox, 4, 0);
            tableLayoutPanel1.Controls.Add(sqlLabel, 4, 1);
            tableLayoutPanel1.Dock = DockStyle.Fill;
            tableLayoutPanel1.Location = new Point(5, 5);
            tableLayoutPanel1.Margin = new Padding(5);
            tableLayoutPanel1.Name = "tableLayoutPanel1";
            tableLayoutPanel1.Padding = new Padding(5);
            tableLayoutPanel1.RowCount = 5;
            tableLayoutPanel1.RowStyles.Add(new RowStyle());
            tableLayoutPanel1.RowStyles.Add(new RowStyle());
            tableLayoutPanel1.RowStyles.Add(new RowStyle());
            tableLayoutPanel1.RowStyles.Add(new RowStyle());
            tableLayoutPanel1.RowStyles.Add(new RowStyle());
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
            tableLayoutPanel1.Size = new Size(1190, 289);
            tableLayoutPanel1.TabIndex = 16;
            // 
            // filtersListBox
            // 
            tableLayoutPanel1.SetColumnSpan(filtersListBox, 3);
            filtersListBox.Dock = DockStyle.Fill;
            filtersListBox.Location = new Point(247, 103);
            filtersListBox.Name = "filtersListBox";
            filtersListBox.Size = new Size(448, 122);
            filtersListBox.TabIndex = 10;
            // 
            // runQueryButton
            // 
            tableLayoutPanel1.SetColumnSpan(runQueryButton, 5);
            runQueryButton.Dock = DockStyle.Top;
            runQueryButton.Location = new Point(8, 231);
            runQueryButton.Name = "runQueryButton";
            runQueryButton.Size = new Size(1174, 58);
            runQueryButton.TabIndex = 16;
            runQueryButton.Text = "Run Query";
            runQueryButton.Click += RunQueryButton_Click;
            // 
            // addFilterButton
            // 
            addFilterButton.Dock = DockStyle.Top;
            addFilterButton.Location = new Point(247, 62);
            addFilterButton.Name = "addFilterButton";
            addFilterButton.Size = new Size(170, 35);
            addFilterButton.TabIndex = 7;
            addFilterButton.Text = "Add";
            addFilterButton.Click += AddFilterButton_Click;
            // 
            // filterOperatorComboBox
            // 
            filterOperatorComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            filterOperatorComboBox.Location = new Point(423, 33);
            filterOperatorComboBox.Name = "filterOperatorComboBox";
            filterOperatorComboBox.Size = new Size(96, 23);
            filterOperatorComboBox.TabIndex = 5;
            // 
            // filterColumnComboBox
            // 
            filterColumnComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            filterColumnComboBox.Location = new Point(247, 33);
            filterColumnComboBox.Name = "filterColumnComboBox";
            filterColumnComboBox.Size = new Size(170, 23);
            filterColumnComboBox.TabIndex = 4;
            filterColumnComboBox.SelectedIndexChanged += FilterColumnComboBox_SelectedIndexChanged;
            // 
            // labelFilter
            // 
            labelFilter.AutoSize = true;
            tableLayoutPanel1.SetColumnSpan(labelFilter, 3);
            labelFilter.Dock = DockStyle.Top;
            labelFilter.Location = new Point(247, 5);
            labelFilter.Name = "labelFilter";
            labelFilter.Size = new Size(448, 15);
            labelFilter.TabIndex = 14;
            labelFilter.Text = "Filter:";
            labelFilter.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // filterValueTextBox
            // 
            filterValueTextBox.Location = new Point(525, 33);
            filterValueTextBox.Name = "filterValueTextBox";
            filterValueTextBox.Size = new Size(170, 23);
            filterValueTextBox.TabIndex = 6;
            // 
            // columnsCheckedListBox
            // 
            columnsCheckedListBox.CheckOnClick = true;
            columnsCheckedListBox.Dock = DockStyle.Fill;
            columnsCheckedListBox.Location = new Point(8, 8);
            columnsCheckedListBox.Name = "columnsCheckedListBox";
            tableLayoutPanel1.SetRowSpan(columnsCheckedListBox, 4);
            columnsCheckedListBox.Size = new Size(233, 217);
            columnsCheckedListBox.TabIndex = 2;
            // 
            // removeFilterButton
            // 
            removeFilterButton.Dock = DockStyle.Top;
            removeFilterButton.Location = new Point(525, 62);
            removeFilterButton.Name = "removeFilterButton";
            removeFilterButton.Size = new Size(170, 35);
            removeFilterButton.TabIndex = 8;
            removeFilterButton.Text = "Remove";
            removeFilterButton.Click += RemoveFilterButton_Click;
            // 
            // collapseProductsCheckBox
            // 
            collapseProductsCheckBox.AutoSize = true;
            collapseProductsCheckBox.Location = new Point(701, 8);
            collapseProductsCheckBox.Name = "collapseProductsCheckBox";
            collapseProductsCheckBox.Size = new Size(120, 19);
            collapseProductsCheckBox.TabIndex = 20;
            collapseProductsCheckBox.Text = "Group by Product";
            collapseProductsCheckBox.UseVisualStyleBackColor = true;
            collapseProductsCheckBox.CheckedChanged += CollapseProductsCheckBox_CheckedChanged;
            // 
            // sqlLabel
            // 
            sqlLabel.Dock = DockStyle.Fill;
            sqlLabel.Location = new Point(701, 33);
            sqlLabel.Multiline = true;
            sqlLabel.Name = "sqlLabel";
            tableLayoutPanel1.SetRowSpan(sqlLabel, 3);
            sqlLabel.Size = new Size(481, 192);
            sqlLabel.TabIndex = 21;
            // 
            // resultsGrid
            // 
            resultsGrid.AllowUserToAddRows = false;
            resultsGrid.AllowUserToDeleteRows = false;
            resultsGrid.AllowUserToOrderColumns = true;
            resultsGrid.AllowUserToResizeRows = false;
            dataGridViewCellStyle2.BackColor = Color.FromArgb(224, 224, 224);
            resultsGrid.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle2;
            resultsGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
            resultsGrid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.DisplayedCells;
            resultsGrid.BackgroundColor = SystemColors.Window;
            resultsGrid.BorderStyle = BorderStyle.Fixed3D;
            resultsGrid.ClipboardCopyMode = DataGridViewClipboardCopyMode.EnableAlwaysIncludeHeaderText;
            resultsGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            resultsGrid.Dock = DockStyle.Fill;
            resultsGrid.EditMode = DataGridViewEditMode.EditOnEnter;
            resultsGrid.Location = new Point(0, 0);
            resultsGrid.MultiSelect = false;
            resultsGrid.Name = "resultsGrid";
            resultsGrid.SelectionMode = DataGridViewSelectionMode.CellSelect;
            resultsGrid.ShowEditingIcon = false;
            resultsGrid.Size = new Size(1200, 454);
            resultsGrid.TabIndex = 0;
            resultsGrid.ColumnDisplayIndexChanged += ResultsGrid_ColumnDisplayIndexChanged;
            resultsGrid.ColumnHeaderMouseClick += ResultsGrid_ColumnHeaderMouseClick;
            resultsGrid.ColumnWidthChanged += ResultsGrid_ColumnWidthChanged;
            resultsGrid.Sorted += ResultsGrid_Sorted;
            // 
            // statusStrip
            // 
            statusStrip.Items.AddRange(new ToolStripItem[] { statusLabel, progressBar });
            statusStrip.Location = new Point(0, 454);
            statusStrip.Name = "statusStrip";
            statusStrip.Size = new Size(1200, 22);
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
            // contextMenuStrip1
            // 
            contextMenuStrip1.Items.AddRange(new ToolStripItem[] { addToLookupToolStripMenuItem });
            contextMenuStrip1.Name = "contextMenuStrip1";
            contextMenuStrip1.Size = new Size(181, 48);
            // 
            // addToLookupToolStripMenuItem
            // 
            addToLookupToolStripMenuItem.Name = "addToLookupToolStripMenuItem";
            addToLookupToolStripMenuItem.Size = new Size(180, 22);
            addToLookupToolStripMenuItem.Text = "Add to lookup...";
            addToLookupToolStripMenuItem.Click += AddToLookupToolStripMenuItem_Click;
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
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Product Offers Explorer";
            WindowState = FormWindowState.Maximized;
            mainSplitContainer.Panel1.ResumeLayout(false);
            mainSplitContainer.Panel2.ResumeLayout(false);
            mainSplitContainer.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)mainSplitContainer).EndInit();
            mainSplitContainer.ResumeLayout(false);
            tableLayoutPanel1.ResumeLayout(false);
            tableLayoutPanel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)resultsGrid).EndInit();
            statusStrip.ResumeLayout(false);
            statusStrip.PerformLayout();
            contextMenuStrip1.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion
        private TableLayoutPanel tableLayoutPanel1;
        private Label labelFilter;
        private CheckBox collapseProductsCheckBox;
        private TextBox sqlLabel;
        private ContextMenuStrip contextMenuStrip1;
        private System.ComponentModel.IContainer components;
        private ToolStripMenuItem addToLookupToolStripMenuItem;
    }
}