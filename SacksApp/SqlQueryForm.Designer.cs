namespace SacksApp
{
    partial class SqlQueryForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.Windows.Forms.SplitContainer mainSplitContainer;
        private System.Windows.Forms.ComboBox filterColumnComboBox;
        private System.Windows.Forms.ComboBox filterOperatorComboBox;
        private System.Windows.Forms.TextBox filterValueTextBox;
        private System.Windows.Forms.Button addFilterButton;
        private System.Windows.Forms.CheckedListBox filtersListBox;
        private System.Windows.Forms.Button removeFilterButton;
        private System.Windows.Forms.Button runQueryButton;
        private System.Windows.Forms.DataGridView resultsGrid;
        private System.Windows.Forms.StatusStrip statusStrip;
        private System.Windows.Forms.ToolStripStatusLabel statusLabel;
        private System.Windows.Forms.ToolStripProgressBar progressBar;
        private System.Windows.Forms.TableLayoutPanel tableLayoutFilters;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
        private System.ComponentModel.IContainer components;
        private System.Windows.Forms.ToolStripMenuItem addToLookupToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem showHideCoulmnsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem groupByProductToolStripMenuItem;
        private System.Windows.Forms.Button buttonHideFilters;
        private System.Windows.Forms.Button buttonShowFilter;

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            DataGridViewCellStyle dataGridViewCellStyle1 = new DataGridViewCellStyle();
            mainSplitContainer = new SplitContainer();
            tableLayoutFilters = new TableLayoutPanel();
            addFilterButton = new Button();
            filterOperatorComboBox = new ComboBox();
            filterColumnComboBox = new ComboBox();
            filterValueTextBox = new TextBox();
            removeFilterButton = new Button();
            filtersListBox = new CheckedListBox();
            buttonHideFilters = new Button();
            buttonShowFilter = new Button();
            resultsGrid = new DataGridView();
            contextMenuStrip1 = new ContextMenuStrip(components);
            addToLookupToolStripMenuItem = new ToolStripMenuItem();
            showHideCoulmnsToolStripMenuItem = new ToolStripMenuItem();
            groupByProductToolStripMenuItem = new ToolStripMenuItem();
            runQueryButton = new Button();
            statusStrip = new StatusStrip();
            statusLabel = new ToolStripStatusLabel();
            progressBar = new ToolStripProgressBar();
            ((System.ComponentModel.ISupportInitialize)mainSplitContainer).BeginInit();
            mainSplitContainer.Panel1.SuspendLayout();
            mainSplitContainer.Panel2.SuspendLayout();
            mainSplitContainer.SuspendLayout();
            tableLayoutFilters.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)resultsGrid).BeginInit();
            contextMenuStrip1.SuspendLayout();
            statusStrip.SuspendLayout();
            SuspendLayout();
            // 
            // mainSplitContainer
            // 
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
            mainSplitContainer.Panel2.Controls.Add(runQueryButton);
            mainSplitContainer.Panel2.Controls.Add(statusStrip);
            mainSplitContainer.Size = new Size(1194, 773);
            mainSplitContainer.SplitterDistance = 68;
            mainSplitContainer.TabIndex = 0;
            // 
            // tableLayoutFilters
            // 
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
            addFilterButton.Location = new Point(188, 84);
            addFilterButton.Name = "addFilterButton";
            addFilterButton.Size = new Size(215, 73);
            addFilterButton.TabIndex = 7;
            addFilterButton.Text = "Add";
            addFilterButton.Click += AddFilterButton_Click;
            // 
            // filterOperatorComboBox
            // 
            filterOperatorComboBox.Dock = DockStyle.Top;
            filterOperatorComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            filterOperatorComboBox.Location = new Point(594, 3);
            filterOperatorComboBox.Name = "filterOperatorComboBox";
            filterOperatorComboBox.Size = new Size(587, 23);
            filterOperatorComboBox.TabIndex = 5;
            // 
            // filterColumnComboBox
            // 
            filterColumnComboBox.Dock = DockStyle.Top;
            filterColumnComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            filterColumnComboBox.Location = new Point(3, 3);
            filterColumnComboBox.Name = "filterColumnComboBox";
            filterColumnComboBox.Size = new Size(585, 23);
            filterColumnComboBox.TabIndex = 4;
            filterColumnComboBox.SelectedIndexChanged += FilterColumnComboBox_SelectedIndexChanged;
            // 
            // filterValueTextBox
            // 
            tableLayoutFilters.SetColumnSpan(filterValueTextBox, 2);
            filterValueTextBox.Dock = DockStyle.Fill;
            filterValueTextBox.Location = new Point(3, 32);
            filterValueTextBox.Multiline = true;
            filterValueTextBox.Name = "filterValueTextBox";
            filterValueTextBox.Size = new Size(1178, 46);
            filterValueTextBox.TabIndex = 6;
            // 
            // removeFilterButton
            // 
            removeFilterButton.Anchor = AnchorStyles.Top;
            removeFilterButton.Location = new Point(780, 84);
            removeFilterButton.Name = "removeFilterButton";
            removeFilterButton.Size = new Size(215, 73);
            removeFilterButton.TabIndex = 8;
            removeFilterButton.Text = "Remove";
            removeFilterButton.Click += RemoveFilterButton_Click;
            // 
            // filtersListBox
            // 
            filtersListBox.CheckOnClick = true;
            tableLayoutFilters.SetColumnSpan(filtersListBox, 2);
            filtersListBox.Dock = DockStyle.Fill;
            filtersListBox.FormattingEnabled = true;
            filtersListBox.Location = new Point(3, 163);
            filtersListBox.Name = "filtersListBox";
            filtersListBox.Size = new Size(1178, 81);
            filtersListBox.TabIndex = 11;
            filtersListBox.ItemCheck += FiltersListBox_ItemCheck;
            // 
            // buttonHideFilters
            // 
            buttonHideFilters.Dock = DockStyle.Top;
            buttonHideFilters.Location = new Point(5, 67);
            buttonHideFilters.Name = "buttonHideFilters";
            buttonHideFilters.Size = new Size(1184, 60);
            buttonHideFilters.TabIndex = 15;
            buttonHideFilters.Text = "Hide filters";
            buttonHideFilters.Visible = false;
            buttonHideFilters.Click += ButtonHideFilters_Click;
            // 
            // buttonShowFilter
            // 
            buttonShowFilter.Dock = DockStyle.Top;
            buttonShowFilter.Location = new Point(5, 5);
            buttonShowFilter.Name = "buttonShowFilter";
            buttonShowFilter.Size = new Size(1184, 62);
            buttonShowFilter.TabIndex = 15;
            buttonShowFilter.Text = "Show filters";
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
            resultsGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
            resultsGrid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.DisplayedCells;
            resultsGrid.BackgroundColor = SystemColors.Window;
            resultsGrid.BorderStyle = BorderStyle.Fixed3D;
            resultsGrid.ClipboardCopyMode = DataGridViewClipboardCopyMode.EnableAlwaysIncludeHeaderText;
            resultsGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            resultsGrid.ContextMenuStrip = contextMenuStrip1;
            resultsGrid.Dock = DockStyle.Fill;
            resultsGrid.EditMode = DataGridViewEditMode.EditOnEnter;
            resultsGrid.Location = new Point(0, 0);
            resultsGrid.MultiSelect = false;
            resultsGrid.Name = "resultsGrid";
            resultsGrid.SelectionMode = DataGridViewSelectionMode.CellSelect;
            resultsGrid.ShowEditingIcon = false;
            resultsGrid.Size = new Size(1194, 619);
            resultsGrid.TabIndex = 0;
            resultsGrid.CellEndEdit += ResultsGrid_CellEndEdit;
            resultsGrid.CellMouseUp += ResultsGrid_CellMouseUp;
            resultsGrid.EditingControlShowing += ResultsGrid_EditingControlShowing;
            // 
            // contextMenuStrip1
            // 
            contextMenuStrip1.Items.AddRange(new ToolStripItem[] { addToLookupToolStripMenuItem, showHideCoulmnsToolStripMenuItem, groupByProductToolStripMenuItem });
            contextMenuStrip1.Name = "contextMenuStrip1";
            contextMenuStrip1.Size = new Size(191, 70);
            // 
            // addToLookupToolStripMenuItem
            // 
            addToLookupToolStripMenuItem.Name = "addToLookupToolStripMenuItem";
            addToLookupToolStripMenuItem.Size = new Size(190, 22);
            addToLookupToolStripMenuItem.Text = "Add New Brand...";
            addToLookupToolStripMenuItem.Click += AddToBrandLookupToolStripMenuItem_Click;
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
            // runQueryButton
            // 
            runQueryButton.Dock = DockStyle.Bottom;
            runQueryButton.Location = new Point(0, 619);
            runQueryButton.Margin = new Padding(20);
            runQueryButton.Name = "runQueryButton";
            runQueryButton.Size = new Size(1194, 60);
            runQueryButton.TabIndex = 16;
            runQueryButton.Text = "Run Query";
            runQueryButton.Click += RunQueryButton_Click;
            // 
            // statusStrip
            // 
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
            tableLayoutFilters.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)resultsGrid).EndInit();
            contextMenuStrip1.ResumeLayout(false);
            statusStrip.ResumeLayout(false);
            statusStrip.PerformLayout();
            ResumeLayout(false);
        }

        #endregion
    }
}