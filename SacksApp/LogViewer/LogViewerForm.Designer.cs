// <copyright file="LogViewerForm.Designer.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System.Reflection;

namespace QMobileDeviceServiceMenu
{
    partial class LogViewerForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.toolbarPanel = new Panel();
            this.autoScrollCheckBox = new CheckBox();
            this.clearButton = new Button();
            this.colorLegendButton = new Button();
            this.logLevelFiltersPanel = new Panel();
            this.searchBox = new TextBox();
            this.searchButton = new Button();
            this.logTextBox = new RichTextBox();
            this.statusPanel = new Panel();
            this.statusLabel = new Label();
            this.toolbarPanel.SuspendLayout();
            this.statusPanel.SuspendLayout();
            this.SuspendLayout();
            
            // 
            // toolbarPanel
            // 
            this.toolbarPanel.Controls.Add(this.autoScrollCheckBox);
            this.toolbarPanel.Controls.Add(this.clearButton);
            this.toolbarPanel.Controls.Add(this.colorLegendButton);
            this.toolbarPanel.Controls.Add(this.logLevelFiltersPanel);
            this.toolbarPanel.Controls.Add(this.searchBox);
            this.toolbarPanel.Controls.Add(this.searchButton);
            this.toolbarPanel.Dock = DockStyle.Top;
            this.toolbarPanel.Height = 70;
            this.toolbarPanel.BackColor = SystemColors.Control;
            this.toolbarPanel.Name = "toolbarPanel";
            
            // 
            // autoScrollCheckBox
            // 
            this.autoScrollCheckBox.AutoSize = true;
            this.autoScrollCheckBox.Checked = true;
            this.autoScrollCheckBox.CheckState = CheckState.Checked;
            this.autoScrollCheckBox.Location = new Point(10, 10);
            this.autoScrollCheckBox.Name = "autoScrollCheckBox";
            this.autoScrollCheckBox.Size = new Size(81, 19);
            this.autoScrollCheckBox.TabIndex = 0;
            this.autoScrollCheckBox.Text = "Auto-scroll";
            this.autoScrollCheckBox.UseVisualStyleBackColor = true;
            
            // 
            // clearButton
            // 
            this.clearButton.Location = new Point(120, 8);
            this.clearButton.Name = "clearButton";
            this.clearButton.Size = new Size(70, 25);
            this.clearButton.TabIndex = 1;
            this.clearButton.Text = "Clear";
            this.clearButton.UseVisualStyleBackColor = true;
            
            // 
            // colorLegendButton
            // 
            this.colorLegendButton.Location = new Point(200, 8);
            this.colorLegendButton.Name = "colorLegendButton";
            this.colorLegendButton.Size = new Size(90, 25);
            this.colorLegendButton.TabIndex = 3;
            this.colorLegendButton.Text = "Color Legend";
            this.colorLegendButton.UseVisualStyleBackColor = true;

            // 
            // logLevelFiltersPanel
            // 
            this.logLevelFiltersPanel.Location = new Point(300, 5);
            this.logLevelFiltersPanel.Name = "logLevelFiltersPanel";
            this.logLevelFiltersPanel.Size = new Size(400, 60);
            this.logLevelFiltersPanel.TabIndex = 4;

            // Initialize log level checkboxes
            InitializeLogLevelCheckBoxes();
            
            // 
            // searchBox
            // 
            this.searchBox.Location = new Point(720, 10);
            this.searchBox.Name = "searchBox";
            this.searchBox.PlaceholderText = "Search logs...";
            this.searchBox.Size = new Size(150, 23);
            this.searchBox.TabIndex = 5;
            
            // 
            // searchButton
            // 
            this.searchButton.Location = new Point(880, 8);
            this.searchButton.Name = "searchButton";
            this.searchButton.Size = new Size(70, 25);
            this.searchButton.TabIndex = 6;
            this.searchButton.Text = "Search";
            this.searchButton.UseVisualStyleBackColor = true;
            
            // 
            // logTextBox
            // 
            this.logTextBox.BackColor = Color.Black;
            this.logTextBox.Dock = DockStyle.Fill;
            this.logTextBox.Font = new Font("Consolas", 9F, FontStyle.Regular, GraphicsUnit.Point);
            this.logTextBox.ForeColor = Color.LightGray;
            this.logTextBox.Location = new Point(0, 70);
            this.logTextBox.Name = "logTextBox";
            this.logTextBox.ReadOnly = true;
            this.logTextBox.ScrollBars = RichTextBoxScrollBars.Both;
            this.logTextBox.Size = new Size(1000, 605);
            this.logTextBox.TabIndex = 7;
            this.logTextBox.Text = "";
            this.logTextBox.WordWrap = false;
            // Enable double buffering for the RichTextBox
            typeof(RichTextBox).InvokeMember("DoubleBuffered",
                BindingFlags.SetProperty | BindingFlags.Instance | BindingFlags.NonPublic,
                null, this.logTextBox, new object[] { true });
            
            // 
            // statusPanel
            // 
            this.statusPanel.Controls.Add(this.statusLabel);
            this.statusPanel.Dock = DockStyle.Bottom;
            this.statusPanel.Height = 25;
            this.statusPanel.BackColor = SystemColors.Control;
            this.statusPanel.Name = "statusPanel";
            
            // 
            // statusLabel
            // 
            this.statusLabel.AutoSize = true;
            this.statusLabel.ForeColor = Color.DarkBlue;
            this.statusLabel.Location = new Point(10, 5);
            this.statusLabel.Name = "statusLabel";
            this.statusLabel.Size = new Size(87, 15);
            this.statusLabel.TabIndex = 0;
            this.statusLabel.Text = "Log Viewer Status";
            
            // 
            // LogViewerForm
            // 
            this.AutoScaleDimensions = new SizeF(7F, 15F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new Size(1000, 700);
            this.Controls.Add(this.logTextBox);
            this.Controls.Add(this.toolbarPanel);
            this.Controls.Add(this.statusPanel);
            this.KeyPreview = true;
            this.MinimumSize = new Size(600, 400);
            this.Name = "LogViewerForm";
            this.ShowInTaskbar = true;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Text = "Service Log Viewer";
            this.toolbarPanel.ResumeLayout(false);
            this.toolbarPanel.PerformLayout();
            this.statusPanel.ResumeLayout(false);
            this.statusPanel.PerformLayout();
            this.ResumeLayout(false);
        }

        #endregion

        private Panel toolbarPanel;
        private CheckBox autoScrollCheckBox;
        private Button clearButton;
        private Button colorLegendButton;
        private Panel logLevelFiltersPanel;
        private CheckBox allCheckBox;
        private CheckBox errorCheckBox;
        private CheckBox warningCheckBox;
        private CheckBox infoCheckBox;
        private CheckBox debugCheckBox;
        private CheckBox defaultCheckBox;
        private TextBox searchBox;
        private Button searchButton;
        private RichTextBox logTextBox;
        private Panel statusPanel;
        private Label statusLabel;

        /// <summary>
        /// Initialize log level checkboxes dynamically
        /// </summary>
        private void InitializeLogLevelCheckBoxes()
        {
            int x = 5;
            int y = 5;
            int spacing = 70;

            // All checkbox
            this.allCheckBox = new CheckBox
            {
                AutoSize = true,
                Checked = true,
                Location = new Point(x, y),
                Name = "allCheckBox",
                Text = "All",
                UseVisualStyleBackColor = true,
                TabIndex = 0
            };
            this.logLevelFiltersPanel.Controls.Add(this.allCheckBox);
            x += spacing;

            // Error checkbox
            this.errorCheckBox = new CheckBox
            {
                AutoSize = true,
                Checked = true,
                Location = new Point(x, y),
                Name = "errorCheckBox",
                Text = "Error",
                ForeColor = Color.Red,
                UseVisualStyleBackColor = true,
                TabIndex = 1
            };
            this.logLevelFiltersPanel.Controls.Add(this.errorCheckBox);
            x += spacing;

            // Warning checkbox
            this.warningCheckBox = new CheckBox
            {
                AutoSize = true,
                Checked = true,
                Location = new Point(x, y),
                Name = "warningCheckBox",
                Text = "Warning",
                ForeColor = Color.Orange,
                UseVisualStyleBackColor = true,
                TabIndex = 2
            };
            this.logLevelFiltersPanel.Controls.Add(this.warningCheckBox);

            // Move to second row
            x = 5;
            y = 25;

            // Info checkbox
            this.infoCheckBox = new CheckBox
            {
                AutoSize = true,
                Checked = true,
                Location = new Point(x, y),
                Name = "infoCheckBox",
                Text = "Info",
                ForeColor = Color.Green,
                UseVisualStyleBackColor = true,
                TabIndex = 3
            };
            this.logLevelFiltersPanel.Controls.Add(this.infoCheckBox);
            x += spacing;

            // Debug checkbox
            this.debugCheckBox = new CheckBox
            {
                AutoSize = true,
                Checked = true,
                Location = new Point(x, y),
                Name = "debugCheckBox",
                Text = "Debug",
                ForeColor = Color.Gray,
                UseVisualStyleBackColor = true,
                TabIndex = 4
            };
            this.logLevelFiltersPanel.Controls.Add(this.debugCheckBox);
            x += spacing;

            // Default checkbox
            this.defaultCheckBox = new CheckBox
            {
                AutoSize = true,
                Checked = true,
                Location = new Point(x, y),
                Name = "defaultCheckBox",
                Text = "Default",
                ForeColor = Color.LightGray,
                UseVisualStyleBackColor = true,
                TabIndex = 5
            };
            this.logLevelFiltersPanel.Controls.Add(this.defaultCheckBox);
        }
    }
}
