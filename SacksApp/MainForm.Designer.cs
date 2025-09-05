namespace SacksApp
{
    partial class MainForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            titleLabel = new Label();
            processFilesButton = new Button();
            clearDatabaseButton = new Button();
            showStatisticsButton = new Button();
            testConfigurationButton = new Button();
            viewLogsButton = new Button();
            sqlQueryButton = new Button();
            SuspendLayout();
            // 
            // titleLabel
            // 
            titleLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            titleLabel.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
            titleLabel.Location = new Point(15, 25);
            titleLabel.Margin = new Padding(4, 0, 4, 0);
            titleLabel.Name = "titleLabel";
            titleLabel.Size = new Size(942, 50);
            titleLabel.TabIndex = 0;
            titleLabel.Text = "🎯 Sacks Product Management System";
            titleLabel.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // processFilesButton
            // 
            processFilesButton.Font = new Font("Segoe UI", 12F);
            processFilesButton.Location = new Point(62, 125);
            processFilesButton.Margin = new Padding(4);
            processFilesButton.Name = "processFilesButton";
            processFilesButton.Size = new Size(375, 75);
            processFilesButton.TabIndex = 1;
            processFilesButton.Text = "📁 Process Excel Files";
            processFilesButton.UseVisualStyleBackColor = true;
            processFilesButton.Click += ProcessFilesButton_Click;
            // 
            // clearDatabaseButton
            // 
            clearDatabaseButton.Font = new Font("Segoe UI", 12F);
            clearDatabaseButton.Location = new Point(500, 125);
            clearDatabaseButton.Margin = new Padding(4);
            clearDatabaseButton.Name = "clearDatabaseButton";
            clearDatabaseButton.Size = new Size(375, 75);
            clearDatabaseButton.TabIndex = 2;
            clearDatabaseButton.Text = "\U0001f9f9 Clear Database";
            clearDatabaseButton.UseVisualStyleBackColor = true;
            clearDatabaseButton.Click += ClearDatabaseButton_Click;
            // 
            // showStatisticsButton
            // 
            showStatisticsButton.FlatStyle = FlatStyle.Flat;
            showStatisticsButton.Font = new Font("Segoe UI", 12F);
            showStatisticsButton.Location = new Point(62, 250);
            showStatisticsButton.Margin = new Padding(4);
            showStatisticsButton.Name = "showStatisticsButton";
            showStatisticsButton.Size = new Size(375, 75);
            showStatisticsButton.TabIndex = 3;
            showStatisticsButton.Text = "📊 Show Statistics";
            showStatisticsButton.UseVisualStyleBackColor = true;
            showStatisticsButton.Click += ShowStatisticsButton_Click;
            // 
            // testConfigurationButton
            // 
            testConfigurationButton.Font = new Font("Segoe UI", 12F);
            testConfigurationButton.Location = new Point(500, 250);
            testConfigurationButton.Margin = new Padding(4);
            testConfigurationButton.Name = "testConfigurationButton";
            testConfigurationButton.Size = new Size(375, 75);
            testConfigurationButton.TabIndex = 4;
            testConfigurationButton.Text = "\U0001f9ea Test Configuration";
            testConfigurationButton.UseVisualStyleBackColor = true;
            testConfigurationButton.Click += TestConfigurationButton_Click;
            // 
            // viewLogsButton
            // 
            viewLogsButton.Font = new Font("Segoe UI", 12F);
            viewLogsButton.Location = new Point(500, 375);
            viewLogsButton.Margin = new Padding(4);
            viewLogsButton.Name = "viewLogsButton";
            viewLogsButton.Size = new Size(375, 75);
            viewLogsButton.TabIndex = 6;
            viewLogsButton.Text = "📊 View Logs";
            viewLogsButton.UseVisualStyleBackColor = true;
            viewLogsButton.Click += ViewLogsButton_Click;
            // 
            // sqlQueryButton
            // 
            sqlQueryButton.Font = new Font("Segoe UI", 12F);
            sqlQueryButton.Location = new Point(62, 375);
            sqlQueryButton.Margin = new Padding(4);
            sqlQueryButton.Name = "sqlQueryButton";
            sqlQueryButton.Size = new Size(375, 75);
            sqlQueryButton.TabIndex = 5;
            sqlQueryButton.Text = "🔍 SQL Query Tool";
            sqlQueryButton.UseVisualStyleBackColor = true;
            sqlQueryButton.Click += SqlQueryButton_Click;
            // 
            // MainForm
            // 
            AutoScaleMode = AutoScaleMode.Inherit;
            ClientSize = new Size(972, 530);
            Controls.Add(viewLogsButton);
            Controls.Add(sqlQueryButton);
            Controls.Add(testConfigurationButton);
            Controls.Add(showStatisticsButton);
            Controls.Add(processFilesButton);
            Controls.Add(titleLabel);
            Controls.Add(clearDatabaseButton);
            Margin = new Padding(4);
            MinimumSize = new Size(994, 586);
            Name = "MainForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Sacks Product Management System";
            ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label titleLabel;
        private System.Windows.Forms.Button processFilesButton;
        private System.Windows.Forms.Button clearDatabaseButton;
        private System.Windows.Forms.Button showStatisticsButton;
        private System.Windows.Forms.Button testConfigurationButton;
        private System.Windows.Forms.Button viewLogsButton;
        private System.Windows.Forms.Button sqlQueryButton;
    }
}
