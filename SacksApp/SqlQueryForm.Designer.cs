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
    private System.Windows.Forms.NumericUpDown topNumericUpDown;
    private System.Windows.Forms.Button buildButton;
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
            mainSplitContainer = new SplitContainer();
            tableLayoutPanel1 = new TableLayoutPanel();
            filtersListBox = new ListBox();
            buildButton = new Button();
            addFilterButton = new Button();
            filterOperatorComboBox = new ComboBox();
            filterColumnComboBox = new ComboBox();
            labelFilter = new Label();
            labelSelect = new Label();
            panel5 = new Panel();
            radioButtonTop = new RadioButton();
            radioButtonSelectAll = new RadioButton();
            topNumericUpDown = new NumericUpDown();
            filterValueTextBox = new TextBox();
            columnsCheckedListBox = new CheckedListBox();
            removeFilterButton = new Button();
            collapseProductsCheckBox = new CheckBox();
            button1 = new Button();
            sqlLabel = new TextBox();
            resultsGrid = new DataGridView();
            statusStrip = new StatusStrip();
            statusLabel = new ToolStripStatusLabel();
            progressBar = new ToolStripProgressBar();
            ((System.ComponentModel.ISupportInitialize)mainSplitContainer).BeginInit();
            mainSplitContainer.Panel1.SuspendLayout();
            mainSplitContainer.Panel2.SuspendLayout();
            mainSplitContainer.SuspendLayout();
            tableLayoutPanel1.SuspendLayout();
            panel5.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)topNumericUpDown).BeginInit();
            ((System.ComponentModel.ISupportInitialize)resultsGrid).BeginInit();
            statusStrip.SuspendLayout();
            SuspendLayout();
            // 
            // mainSplitContainer
            // 
            mainSplitContainer.Dock = DockStyle.Fill;
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
            mainSplitContainer.SplitterDistance = 333;
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
            tableLayoutPanel1.Controls.Add(buildButton, 0, 4);
            tableLayoutPanel1.Controls.Add(addFilterButton, 1, 2);
            tableLayoutPanel1.Controls.Add(filterOperatorComboBox, 2, 1);
            tableLayoutPanel1.Controls.Add(filterColumnComboBox, 1, 1);
            tableLayoutPanel1.Controls.Add(labelFilter, 1, 0);
            tableLayoutPanel1.Controls.Add(labelSelect, 0, 0);
            tableLayoutPanel1.Controls.Add(panel5, 0, 1);
            tableLayoutPanel1.Controls.Add(filterValueTextBox, 3, 1);
            tableLayoutPanel1.Controls.Add(columnsCheckedListBox, 0, 3);
            tableLayoutPanel1.Controls.Add(removeFilterButton, 3, 2);
            tableLayoutPanel1.Controls.Add(collapseProductsCheckBox, 4, 0);
            tableLayoutPanel1.Controls.Add(button1, 4, 1);
            tableLayoutPanel1.Controls.Add(sqlLabel, 4, 3);
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
            tableLayoutPanel1.Size = new Size(1190, 323);
            tableLayoutPanel1.TabIndex = 16;
            // 
            // filtersListBox
            // 
            tableLayoutPanel1.SetColumnSpan(filtersListBox, 3);
            filtersListBox.Dock = DockStyle.Fill;
            filtersListBox.Location = new Point(247, 122);
            filtersListBox.Name = "filtersListBox";
            filtersListBox.Size = new Size(448, 122);
            filtersListBox.TabIndex = 10;
            // 
            // buildButton
            // 
            tableLayoutPanel1.SetColumnSpan(buildButton, 5);
            buildButton.Dock = DockStyle.Top;
            buildButton.Location = new Point(8, 250);
            buildButton.Name = "buildButton";
            buildButton.Size = new Size(1174, 50);
            buildButton.TabIndex = 16;
            buildButton.Text = "Run Query";
            buildButton.Click += BuildButton_Click;
            // 
            // addFilterButton
            // 
            addFilterButton.Dock = DockStyle.Top;
            addFilterButton.Location = new Point(247, 67);
            addFilterButton.Name = "addFilterButton";
            addFilterButton.Size = new Size(170, 35);
            addFilterButton.TabIndex = 7;
            addFilterButton.Text = "Add";
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
            filterColumnComboBox.Size = new Size(96, 23);
            filterColumnComboBox.TabIndex = 4;
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
            // labelSelect
            // 
            labelSelect.AutoSize = true;
            labelSelect.Dock = DockStyle.Top;
            labelSelect.Location = new Point(8, 5);
            labelSelect.Name = "labelSelect";
            labelSelect.Size = new Size(233, 15);
            labelSelect.TabIndex = 11;
            labelSelect.Text = "Select:";
            labelSelect.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // panel5
            // 
            panel5.Controls.Add(radioButtonTop);
            panel5.Controls.Add(radioButtonSelectAll);
            panel5.Controls.Add(topNumericUpDown);
            panel5.Location = new Point(8, 33);
            panel5.Name = "panel5";
            tableLayoutPanel1.SetRowSpan(panel5, 2);
            panel5.Size = new Size(233, 83);
            panel5.TabIndex = 13;
            // 
            // radioButtonTop
            // 
            radioButtonTop.AutoSize = true;
            radioButtonTop.Location = new Point(18, 29);
            radioButtonTop.Name = "radioButtonTop";
            radioButtonTop.Size = new Size(47, 19);
            radioButtonTop.TabIndex = 12;
            radioButtonTop.Text = "First";
            radioButtonTop.UseVisualStyleBackColor = true;
            radioButtonTop.CheckedChanged += RadioButton1_CheckedChanged;
            // 
            // radioButtonSelectAll
            // 
            radioButtonSelectAll.AutoSize = true;
            radioButtonSelectAll.Checked = true;
            radioButtonSelectAll.Location = new Point(18, 9);
            radioButtonSelectAll.Name = "radioButtonSelectAll";
            radioButtonSelectAll.Size = new Size(39, 19);
            radioButtonSelectAll.TabIndex = 12;
            radioButtonSelectAll.TabStop = true;
            radioButtonSelectAll.Text = "All";
            radioButtonSelectAll.UseVisualStyleBackColor = true;
            // 
            // topNumericUpDown
            // 
            topNumericUpDown.Enabled = false;
            topNumericUpDown.Location = new Point(71, 28);
            topNumericUpDown.Maximum = new decimal(new int[] { 100000, 0, 0, 0 });
            topNumericUpDown.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            topNumericUpDown.Name = "topNumericUpDown";
            topNumericUpDown.Size = new Size(90, 23);
            topNumericUpDown.TabIndex = 15;
            topNumericUpDown.Value = new decimal(new int[] { 1000, 0, 0, 0 });
            // 
            // filterValueTextBox
            // 
            filterValueTextBox.Location = new Point(525, 33);
            filterValueTextBox.Name = "filterValueTextBox";
            filterValueTextBox.Size = new Size(96, 23);
            filterValueTextBox.TabIndex = 6;
            // 
            // columnsCheckedListBox
            // 
            columnsCheckedListBox.CheckOnClick = true;
            columnsCheckedListBox.Dock = DockStyle.Fill;
            columnsCheckedListBox.Location = new Point(8, 122);
            columnsCheckedListBox.Name = "columnsCheckedListBox";
            columnsCheckedListBox.Size = new Size(233, 122);
            columnsCheckedListBox.TabIndex = 2;
            // 
            // removeFilterButton
            // 
            removeFilterButton.Dock = DockStyle.Top;
            removeFilterButton.Location = new Point(525, 67);
            removeFilterButton.Name = "removeFilterButton";
            removeFilterButton.Size = new Size(170, 35);
            removeFilterButton.TabIndex = 8;
            removeFilterButton.Text = "Remove";
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
            // 
            // button1
            // 
            button1.Location = new Point(701, 33);
            button1.Name = "button1";
            button1.Size = new Size(145, 28);
            button1.TabIndex = 22;
            button1.Text = "Manual";
            button1.UseVisualStyleBackColor = true;
            button1.Click += Button1_Click;
            // 
            // sqlLabel
            // 
            sqlLabel.Dock = DockStyle.Fill;
            sqlLabel.Location = new Point(701, 122);
            sqlLabel.Multiline = true;
            sqlLabel.Name = "sqlLabel";
            sqlLabel.Size = new Size(481, 122);
            sqlLabel.TabIndex = 21;
            // 
            // resultsGrid
            // 
            resultsGrid.AllowUserToAddRows = false;
            resultsGrid.AllowUserToDeleteRows = false;
            resultsGrid.AllowUserToOrderColumns = true;
            resultsGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            resultsGrid.BackgroundColor = SystemColors.Window;
            resultsGrid.BorderStyle = BorderStyle.Fixed3D;
            resultsGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            resultsGrid.Dock = DockStyle.Fill;
            resultsGrid.Location = new Point(0, 0);
            resultsGrid.Name = "resultsGrid";
            resultsGrid.ReadOnly = true;
            resultsGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            resultsGrid.Size = new Size(1200, 420);
            resultsGrid.TabIndex = 0;
            // 
            // statusStrip
            // 
            statusStrip.Items.AddRange(new ToolStripItem[] { statusLabel, progressBar });
            statusStrip.Location = new Point(0, 420);
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
            panel5.ResumeLayout(false);
            panel5.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)topNumericUpDown).EndInit();
            ((System.ComponentModel.ISupportInitialize)resultsGrid).EndInit();
            statusStrip.ResumeLayout(false);
            statusStrip.PerformLayout();
            ResumeLayout(false);
        }

        #endregion
        private TableLayoutPanel tableLayoutPanel1;
        private Label labelSelect;
        private Panel panel5;
        private RadioButton radioButtonTop;
        private RadioButton radioButtonSelectAll;
        private Label labelFilter;
        private CheckBox collapseProductsCheckBox;
        private TextBox sqlLabel;
        private Button button1;
    }
}