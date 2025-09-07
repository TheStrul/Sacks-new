namespace SacksApp
{
    partial class DashBoard
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
            viewLogsButton = new Button();
            sqlQueryButton = new Button();
            testConfigurationButton = new Button();
            showStatisticsButton = new Button();
            processFilesButton = new Button();
            clearDatabaseButton = new Button();
            titleLabel = new Label();
            tableLayoutPanel1 = new TableLayoutPanel();
            tableLayoutPanel1.SuspendLayout();
            SuspendLayout();
            // 
            // viewLogsButton
            // 
            viewLogsButton.AutoSize = true;
            viewLogsButton.Dock = DockStyle.Fill;
            viewLogsButton.Font = new Font("Segoe UI", 14F);
            viewLogsButton.Location = new Point(455, 265);
            viewLogsButton.Margin = new Padding(15);
            viewLogsButton.Name = "viewLogsButton";
            viewLogsButton.Padding = new Padding(30);
            viewLogsButton.Size = new Size(411, 96);
            viewLogsButton.TabIndex = 12;
            viewLogsButton.Text = "📊 View Logs";
            viewLogsButton.UseVisualStyleBackColor = true;
            viewLogsButton.Click += ViewLogsButton_Click;
            // 
            // sqlQueryButton
            // 
            sqlQueryButton.AutoSize = true;
            sqlQueryButton.Dock = DockStyle.Fill;
            sqlQueryButton.Font = new Font("Segoe UI", 14F);
            sqlQueryButton.Location = new Point(15, 265);
            sqlQueryButton.Margin = new Padding(15);
            sqlQueryButton.Name = "sqlQueryButton";
            sqlQueryButton.Padding = new Padding(30);
            sqlQueryButton.Size = new Size(410, 96);
            sqlQueryButton.TabIndex = 11;
            sqlQueryButton.Text = "🔍 SQL Query Tool";
            sqlQueryButton.UseVisualStyleBackColor = true;
            sqlQueryButton.Click += SqlQueryButton_Click;
            // 
            // testConfigurationButton
            // 
            testConfigurationButton.AutoSize = true;
            testConfigurationButton.Dock = DockStyle.Fill;
            testConfigurationButton.Font = new Font("Segoe UI", 14F);
            testConfigurationButton.Location = new Point(455, 140);
            testConfigurationButton.Margin = new Padding(15);
            testConfigurationButton.Name = "testConfigurationButton";
            testConfigurationButton.Padding = new Padding(30);
            testConfigurationButton.Size = new Size(411, 95);
            testConfigurationButton.TabIndex = 10;
            testConfigurationButton.Text = "\U0001f9ea Test Configuration";
            testConfigurationButton.UseVisualStyleBackColor = true;
            testConfigurationButton.Click += TestConfigurationButton_Click;
            // 
            // showStatisticsButton
            // 
            showStatisticsButton.AutoSize = true;
            showStatisticsButton.Dock = DockStyle.Fill;
            showStatisticsButton.FlatStyle = FlatStyle.Flat;
            showStatisticsButton.Font = new Font("Segoe UI", 14F);
            showStatisticsButton.Location = new Point(15, 140);
            showStatisticsButton.Margin = new Padding(15);
            showStatisticsButton.Name = "showStatisticsButton";
            showStatisticsButton.Padding = new Padding(30);
            showStatisticsButton.Size = new Size(410, 95);
            showStatisticsButton.TabIndex = 9;
            showStatisticsButton.Text = "📊 Show Statistics";
            showStatisticsButton.UseVisualStyleBackColor = true;
            showStatisticsButton.Click += ShowStatisticsButton_Click;
            // 
            // processFilesButton
            // 
            processFilesButton.AutoSize = true;
            processFilesButton.Dock = DockStyle.Fill;
            processFilesButton.Font = new Font("Segoe UI", 14F);
            processFilesButton.Location = new Point(15, 15);
            processFilesButton.Margin = new Padding(15);
            processFilesButton.Name = "processFilesButton";
            processFilesButton.Padding = new Padding(30);
            processFilesButton.Size = new Size(410, 95);
            processFilesButton.TabIndex = 7;
            processFilesButton.Text = "📁 Process Excel Files";
            processFilesButton.UseVisualStyleBackColor = true;
            processFilesButton.Click += ProcessFilesButton_Click;
            // 
            // clearDatabaseButton
            // 
            clearDatabaseButton.AutoSize = true;
            clearDatabaseButton.Dock = DockStyle.Fill;
            clearDatabaseButton.Font = new Font("Segoe UI", 14F);
            clearDatabaseButton.Location = new Point(455, 15);
            clearDatabaseButton.Margin = new Padding(15);
            clearDatabaseButton.Name = "clearDatabaseButton";
            clearDatabaseButton.Padding = new Padding(30);
            clearDatabaseButton.Size = new Size(411, 95);
            clearDatabaseButton.TabIndex = 8;
            clearDatabaseButton.Text = "\U0001f9f9 Clear Database";
            clearDatabaseButton.UseVisualStyleBackColor = true;
            clearDatabaseButton.Click += ClearDatabaseButton_Click;
            // 
            // titleLabel
            // 
            titleLabel.Dock = DockStyle.Top;
            titleLabel.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
            titleLabel.Location = new Point(0, 0);
            titleLabel.Margin = new Padding(4, 0, 4, 0);
            titleLabel.Name = "titleLabel";
            titleLabel.Size = new Size(881, 50);
            titleLabel.TabIndex = 13;
            titleLabel.Text = "🎯 Sacks Product Management System";
            titleLabel.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // tableLayoutPanel1
            // 
            tableLayoutPanel1.ColumnCount = 2;
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tableLayoutPanel1.Controls.Add(processFilesButton, 0, 0);
            tableLayoutPanel1.Controls.Add(showStatisticsButton, 0, 1);
            tableLayoutPanel1.Controls.Add(viewLogsButton, 1, 2);
            tableLayoutPanel1.Controls.Add(sqlQueryButton, 0, 2);
            tableLayoutPanel1.Controls.Add(testConfigurationButton, 1, 1);
            tableLayoutPanel1.Controls.Add(clearDatabaseButton, 1, 0);
            tableLayoutPanel1.Dock = DockStyle.Fill;
            tableLayoutPanel1.Location = new Point(0, 50);
            tableLayoutPanel1.Name = "tableLayoutPanel1";
            tableLayoutPanel1.RowCount = 3;
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 33.3333321F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 33.3333321F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 33.3333321F));
            tableLayoutPanel1.Size = new Size(881, 376);
            tableLayoutPanel1.TabIndex = 14;
            // 
            // DashBoard
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(881, 426);
            Controls.Add(tableLayoutPanel1);
            Controls.Add(titleLabel);
            Name = "DashBoard";
            Text = "DashBoard";
            tableLayoutPanel1.ResumeLayout(false);
            tableLayoutPanel1.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private Button viewLogsButton;
        private Button sqlQueryButton;
        private Button testConfigurationButton;
        private Button showStatisticsButton;
        private Button processFilesButton;
        private Button clearDatabaseButton;
        private Label titleLabel;
        private TableLayoutPanel tableLayoutPanel1;
    }
}