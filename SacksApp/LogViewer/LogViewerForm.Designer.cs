// <copyright file="LogViewerForm.Designer.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

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
            this.allCheckBox = new CheckBox();
            this.errorCheckBox = new CheckBox();
            this.warningCheckBox = new CheckBox();
            this.infoCheckBox = new CheckBox();
            this.debugCheckBox = new CheckBox();
            this.defaultCheckBox = new CheckBox();
            this.searchBox = new TextBox();
            this.searchButton = new Button();
            this.logTextBox = new RichTextBox();
            this.statusPanel = new Panel();
            this.statusLabel = new Label();
            this.toolbarPanel.SuspendLayout();
            this.logLevelFiltersPanel.SuspendLayout();
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
            this.autoScrollCheckBox.CheckedChanged += AutoScrollCheckBox_CheckedChanged;
            
            // 
            // clearButton
            // 
            this.clearButton.Location = new Point(120, 8);
            this.clearButton.Name = "clearButton";
            this.clearButton.Size = new Size(70, 25);
            this.clearButton.TabIndex = 1;
            this.clearButton.Text = "Clear";
            this.clearButton.UseVisualStyleBackColor = true;
            this.clearButton.Click += ClearButton_Click;

            
            // 
            // colorLegendButton
            // 
            this.colorLegendButton.Location = new Point(200, 8);
            this.colorLegendButton.Name = "colorLegendButton";
            this.colorLegendButton.Size = new Size(90, 25);
            this.colorLegendButton.TabIndex = 3;
            this.colorLegendButton.Text = "Color Legend";
            this.colorLegendButton.UseVisualStyleBackColor = true;
            this.colorLegendButton.Click += ColorLegendButton_Click;

            // 
            // logLevelFiltersPanel
            // 
            this.logLevelFiltersPanel.Controls.Add(this.allCheckBox);
            this.logLevelFiltersPanel.Controls.Add(this.errorCheckBox);
            this.logLevelFiltersPanel.Controls.Add(this.warningCheckBox);
            this.logLevelFiltersPanel.Controls.Add(this.infoCheckBox);
            this.logLevelFiltersPanel.Controls.Add(this.debugCheckBox);
            this.logLevelFiltersPanel.Controls.Add(this.defaultCheckBox);
            this.logLevelFiltersPanel.Location = new Point(300, 5);
            this.logLevelFiltersPanel.Name = "logLevelFiltersPanel";
            this.logLevelFiltersPanel.Size = new Size(400, 60);
            this.logLevelFiltersPanel.TabIndex = 4;

            // 
            // allCheckBox
            // 
            this.allCheckBox.AutoSize = true;
            this.allCheckBox.Checked = true;
            this.allCheckBox.Location = new Point(5, 5);
            this.allCheckBox.Name = "allCheckBox";
            this.allCheckBox.Size = new Size(40, 19);
            this.allCheckBox.TabIndex = 0;
            this.allCheckBox.Text = "All";
            this.allCheckBox.UseVisualStyleBackColor = true;
            this.allCheckBox.CheckedChanged += AllCheckBox_CheckedChanged;

            // 
            // errorCheckBox
            // 
            this.errorCheckBox.AutoSize = true;
            this.errorCheckBox.Checked = true;
            this.errorCheckBox.ForeColor = Color.Red;
            this.errorCheckBox.Location = new Point(75, 5);
            this.errorCheckBox.Name = "errorCheckBox";
            this.errorCheckBox.Size = new Size(52, 19);
            this.errorCheckBox.TabIndex = 1;
            this.errorCheckBox.Text = "Error";
            this.errorCheckBox.UseVisualStyleBackColor = true;
            this.errorCheckBox.CheckedChanged += LogLevelCheckBox_CheckedChanged;

            // 
            // warningCheckBox
            // 
            this.warningCheckBox.AutoSize = true;
            this.warningCheckBox.Checked = true;
            this.warningCheckBox.ForeColor = Color.Orange;
            this.warningCheckBox.Location = new Point(145, 5);
            this.warningCheckBox.Name = "warningCheckBox";
            this.warningCheckBox.Size = new Size(71, 19);
            this.warningCheckBox.TabIndex = 2;
            this.warningCheckBox.Text = "Warning";
            this.warningCheckBox.UseVisualStyleBackColor = true;
            this.warningCheckBox.CheckedChanged += LogLevelCheckBox_CheckedChanged;

            // 
            // infoCheckBox
            // 
            this.infoCheckBox.AutoSize = true;
            this.infoCheckBox.Checked = true;
            this.infoCheckBox.ForeColor = Color.Green;
            this.infoCheckBox.Location = new Point(5, 25);
            this.infoCheckBox.Name = "infoCheckBox";
            this.infoCheckBox.Size = new Size(46, 19);
            this.infoCheckBox.TabIndex = 3;
            this.infoCheckBox.Text = "Info";
            this.infoCheckBox.UseVisualStyleBackColor = true;
            this.infoCheckBox.CheckedChanged += LogLevelCheckBox_CheckedChanged;

            // 
            // debugCheckBox
            // 
            this.debugCheckBox.AutoSize = true;
            this.debugCheckBox.Checked = true;
            this.debugCheckBox.ForeColor = Color.Gray;
            this.debugCheckBox.Location = new Point(75, 25);
            this.debugCheckBox.Name = "debugCheckBox";
            this.debugCheckBox.Size = new Size(58, 19);
            this.debugCheckBox.TabIndex = 4;
            this.debugCheckBox.Text = "Debug";
            this.debugCheckBox.UseVisualStyleBackColor = true;
            this.debugCheckBox.CheckedChanged += LogLevelCheckBox_CheckedChanged;

            // 
            // defaultCheckBox
            // 
            this.defaultCheckBox.AutoSize = true;
            this.defaultCheckBox.Checked = true;
            this.defaultCheckBox.ForeColor = Color.LightGray;
            this.defaultCheckBox.Location = new Point(145, 25);
            this.defaultCheckBox.Name = "defaultCheckBox";
            this.defaultCheckBox.Size = new Size(64, 19);
            this.defaultCheckBox.TabIndex = 5;
            this.defaultCheckBox.Text = "Default";
            this.defaultCheckBox.UseVisualStyleBackColor = true;
            this.defaultCheckBox.CheckedChanged += LogLevelCheckBox_CheckedChanged;
            
            // 
            // searchBox
            // 
            this.searchBox.Location = new Point(720, 10);
            this.searchBox.Name = "searchBox";
            this.searchBox.PlaceholderText = "Search logs...";
            this.searchBox.Size = new Size(150, 23);
            this.searchBox.TabIndex = 5;
            this.searchBox.KeyDown += SearchBox_KeyDown;
            
            // 
            // searchButton
            // 
            this.searchButton.Location = new Point(880, 8);
            this.searchButton.Name = "searchButton";
            this.searchButton.Size = new Size(70, 25);
            this.searchButton.TabIndex = 6;
            this.searchButton.Text = "Search";
            this.searchButton.UseVisualStyleBackColor = true;
            this.searchButton.Click += SearchButton_Click;
            
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
            this.logTextBox.MouseDown += LogTextBox_MouseDown;
            
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
            this.KeyDown += LogViewerForm_KeyDown;
            this.toolbarPanel.ResumeLayout(false);
            this.toolbarPanel.PerformLayout();
            this.logLevelFiltersPanel.ResumeLayout(false);
            this.logLevelFiltersPanel.PerformLayout();
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
    }
}
