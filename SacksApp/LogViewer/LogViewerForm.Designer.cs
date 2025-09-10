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
            toolbarPanel = new Panel();
            autoScrollCheckBox = new CheckBox();
            clearButton = new Button();
            colorLegendButton = new Button();
            logLevelFiltersPanel = new Panel();
            allCheckBox = new CheckBox();
            errorCheckBox = new CheckBox();
            warningCheckBox = new CheckBox();
            infoCheckBox = new CheckBox();
            debugCheckBox = new CheckBox();
            defaultCheckBox = new CheckBox();
            searchBox = new TextBox();
            searchButton = new Button();
            exportButton = new Button();
            logTextBox = new RichTextBox();
            statusPanel = new Panel();
            statusLabel = new Label();
            toolbarPanel.SuspendLayout();
            logLevelFiltersPanel.SuspendLayout();
            statusPanel.SuspendLayout();
            SuspendLayout();
            // 
            // toolbarPanel
            // 
            toolbarPanel.BackColor = SystemColors.Control;
            toolbarPanel.Controls.Add(autoScrollCheckBox);
            toolbarPanel.Controls.Add(clearButton);
            toolbarPanel.Controls.Add(colorLegendButton);
            toolbarPanel.Controls.Add(logLevelFiltersPanel);
            toolbarPanel.Controls.Add(searchBox);
            toolbarPanel.Controls.Add(searchButton);
            toolbarPanel.Controls.Add(exportButton);
            toolbarPanel.Dock = DockStyle.Top;
            toolbarPanel.Location = new Point(0, 0);
            toolbarPanel.Name = "toolbarPanel";
            toolbarPanel.Size = new Size(1000, 70);
            toolbarPanel.TabIndex = 8;
            // 
            // autoScrollCheckBox
            // 
            autoScrollCheckBox.AutoSize = true;
            autoScrollCheckBox.Checked = true;
            autoScrollCheckBox.CheckState = CheckState.Checked;
            autoScrollCheckBox.Location = new Point(10, 10);
            autoScrollCheckBox.Name = "autoScrollCheckBox";
            autoScrollCheckBox.Size = new Size(85, 19);
            autoScrollCheckBox.TabIndex = 0;
            autoScrollCheckBox.Text = "Auto-scroll";
            autoScrollCheckBox.UseVisualStyleBackColor = true;
            autoScrollCheckBox.CheckedChanged += AutoScrollCheckBox_CheckedChanged;
            // 
            // clearButton
            // 
            clearButton.Location = new Point(120, 8);
            clearButton.Name = "clearButton";
            clearButton.Size = new Size(70, 25);
            clearButton.TabIndex = 1;
            clearButton.Text = "Clear";
            clearButton.UseVisualStyleBackColor = true;
            clearButton.Click += ClearButton_Click;
            // 
            // colorLegendButton
            // 
            colorLegendButton.Location = new Point(200, 8);
            colorLegendButton.Name = "colorLegendButton";
            colorLegendButton.Size = new Size(90, 25);
            colorLegendButton.TabIndex = 3;
            colorLegendButton.Text = "Color Legend";
            colorLegendButton.UseVisualStyleBackColor = true;
            colorLegendButton.Click += ColorLegendButton_Click;
            // 
            // logLevelFiltersPanel
            // 
            logLevelFiltersPanel.Controls.Add(allCheckBox);
            logLevelFiltersPanel.Controls.Add(errorCheckBox);
            logLevelFiltersPanel.Controls.Add(warningCheckBox);
            logLevelFiltersPanel.Controls.Add(infoCheckBox);
            logLevelFiltersPanel.Controls.Add(debugCheckBox);
            logLevelFiltersPanel.Controls.Add(defaultCheckBox);
            logLevelFiltersPanel.Location = new Point(300, 5);
            logLevelFiltersPanel.Name = "logLevelFiltersPanel";
            logLevelFiltersPanel.Size = new Size(400, 60);
            logLevelFiltersPanel.TabIndex = 4;
            // 
            // allCheckBox
            // 
            allCheckBox.AutoSize = true;
            allCheckBox.Location = new Point(5, 5);
            allCheckBox.Name = "allCheckBox";
            allCheckBox.Size = new Size(40, 19);
            allCheckBox.TabIndex = 0;
            allCheckBox.Text = "All";
            allCheckBox.UseVisualStyleBackColor = true;
            allCheckBox.CheckedChanged += AllCheckBox_CheckedChanged;
            // 
            // errorCheckBox
            // 
            errorCheckBox.AutoSize = true;
            errorCheckBox.Checked = true;
            errorCheckBox.CheckState = CheckState.Checked;
            errorCheckBox.ForeColor = Color.Red;
            errorCheckBox.Location = new Point(75, 5);
            errorCheckBox.Name = "errorCheckBox";
            errorCheckBox.Size = new Size(51, 19);
            errorCheckBox.TabIndex = 1;
            errorCheckBox.Text = "Error";
            errorCheckBox.UseVisualStyleBackColor = true;
            errorCheckBox.CheckedChanged += LogLevelCheckBox_CheckedChanged;
            // 
            // warningCheckBox
            // 
            warningCheckBox.AutoSize = true;
            warningCheckBox.Checked = true;
            warningCheckBox.CheckState = CheckState.Checked;
            warningCheckBox.ForeColor = Color.Orange;
            warningCheckBox.Location = new Point(145, 5);
            warningCheckBox.Name = "warningCheckBox";
            warningCheckBox.Size = new Size(71, 19);
            warningCheckBox.TabIndex = 2;
            warningCheckBox.Text = "Warning";
            warningCheckBox.UseVisualStyleBackColor = true;
            warningCheckBox.CheckedChanged += LogLevelCheckBox_CheckedChanged;
            // 
            // infoCheckBox
            // 
            infoCheckBox.AutoSize = true;
            infoCheckBox.Checked = true;
            infoCheckBox.CheckState = CheckState.Checked;
            infoCheckBox.ForeColor = Color.DarkBlue;
            infoCheckBox.Location = new Point(5, 25);
            infoCheckBox.Name = "infoCheckBox";
            infoCheckBox.Size = new Size(47, 19);
            infoCheckBox.TabIndex = 3;
            infoCheckBox.Text = "Info";
            infoCheckBox.UseVisualStyleBackColor = true;
            infoCheckBox.CheckedChanged += LogLevelCheckBox_CheckedChanged;
            // 
            // debugCheckBox
            // 
            debugCheckBox.AutoSize = true;
            debugCheckBox.ForeColor = Color.Gray;
            debugCheckBox.Location = new Point(75, 25);
            debugCheckBox.Name = "debugCheckBox";
            debugCheckBox.Size = new Size(61, 19);
            debugCheckBox.TabIndex = 4;
            debugCheckBox.Text = "Debug";
            debugCheckBox.UseVisualStyleBackColor = true;
            debugCheckBox.CheckedChanged += LogLevelCheckBox_CheckedChanged;
            // 
            // defaultCheckBox
            // 
            defaultCheckBox.AutoSize = true;
            defaultCheckBox.ForeColor = Color.Gray;
            defaultCheckBox.Location = new Point(145, 25);
            defaultCheckBox.Name = "defaultCheckBox";
            defaultCheckBox.Size = new Size(64, 19);
            defaultCheckBox.TabIndex = 5;
            defaultCheckBox.Text = "Default";
            defaultCheckBox.UseVisualStyleBackColor = true;
            defaultCheckBox.CheckedChanged += LogLevelCheckBox_CheckedChanged;
            // 
            // searchBox
            // 
            searchBox.Location = new Point(720, 10);
            searchBox.Name = "searchBox";
            searchBox.PlaceholderText = "Search logs...";
            searchBox.Size = new Size(150, 23);
            searchBox.TabIndex = 5;
            searchBox.KeyDown += SearchBox_KeyDown;
            // 
            // searchButton
            // 
            searchButton.Location = new Point(880, 8);
            searchButton.Name = "searchButton";
            searchButton.Size = new Size(70, 25);
            searchButton.TabIndex = 6;
            searchButton.Text = "Search";
            searchButton.UseVisualStyleBackColor = true;
            searchButton.Click += SearchButton_Click;
            // 
            // exportButton
            // 
            exportButton.Location = new Point(960, 8);
            exportButton.Name = "exportButton";
            exportButton.Size = new Size(70, 25);
            exportButton.TabIndex = 7;
            exportButton.Text = "Export";
            exportButton.UseVisualStyleBackColor = true;
            exportButton.Click += ExportButton_Click;
            // 
            // logTextBox
            // 
            logTextBox.BackColor = SystemColors.Window;
            logTextBox.Dock = DockStyle.Fill;
            logTextBox.Font = new Font("Segoe UI Emoji", 9F);
            logTextBox.ForeColor = SystemColors.WindowText;
            logTextBox.Location = new Point(0, 70);
            logTextBox.Name = "logTextBox";
            logTextBox.ReadOnly = true;
            logTextBox.Size = new Size(1000, 605);
            logTextBox.TabIndex = 7;
            logTextBox.Text = "";
            logTextBox.WordWrap = false;
            logTextBox.MouseDown += LogTextBox_MouseDown;
            // 
            // statusPanel
            // 
            statusPanel.BackColor = SystemColors.Control;
            statusPanel.Controls.Add(statusLabel);
            statusPanel.Dock = DockStyle.Bottom;
            statusPanel.Location = new Point(0, 675);
            statusPanel.Name = "statusPanel";
            statusPanel.Size = new Size(1000, 25);
            statusPanel.TabIndex = 9;
            // 
            // statusLabel
            // 
            statusLabel.AutoSize = true;
            statusLabel.ForeColor = Color.DarkBlue;
            statusLabel.Location = new Point(10, 5);
            statusLabel.Name = "statusLabel";
            statusLabel.Size = new Size(100, 15);
            statusLabel.TabIndex = 0;
            statusLabel.Text = "Log Viewer Status";
            // 
            // LogViewerForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1000, 700);
            Controls.Add(logTextBox);
            Controls.Add(toolbarPanel);
            Controls.Add(statusPanel);
            KeyPreview = true;
            MinimumSize = new Size(600, 400);
            Name = "LogViewerForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Service Log Viewer";
            KeyDown += LogViewerForm_KeyDown;
            toolbarPanel.ResumeLayout(false);
            toolbarPanel.PerformLayout();
            logLevelFiltersPanel.ResumeLayout(false);
            logLevelFiltersPanel.PerformLayout();
            statusPanel.ResumeLayout(false);
            statusPanel.PerformLayout();
            ResumeLayout(false);
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
    private Button exportButton;
    private ContextMenuStrip logContextMenu;
        private RichTextBox logTextBox;
        private Panel statusPanel;
        private Label statusLabel;
    private bool suspendCheckboxEvents;
    }
}
