namespace SacksApp
{
    partial class ThemeTestForm
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
            _themeComboBox = new ComboBox();
            _skinComboBox = new ComboBox();
            _testButton = new ModernWinForms.Controls.ModernButton();
            _testGroupBox = new ModernWinForms.Controls.ModernGroupBox();
            _styleDetailsLabel = new Label();
            _styleInfoLabel = new Label();
            _disabledButton = new ModernWinForms.Controls.ModernButton();
            _normalButton = new ModernWinForms.Controls.ModernButton();
            _infoLabel = new Label();
            _themeLabel = new Label();
            _skinLabel = new Label();
            _testGroupBox.SuspendLayout();
            SuspendLayout();
            // 
            // _themeComboBox
            // 
            _themeComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            _themeComboBox.FormattingEnabled = true;
            _themeComboBox.Location = new Point(20, 45);
            _themeComboBox.Name = "_themeComboBox";
            _themeComboBox.Size = new Size(200, 23);
            _themeComboBox.TabIndex = 1;
            _themeComboBox.SelectedIndexChanged += OnThemeChanged;
            // 
            // _skinComboBox
            // 
            _skinComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            _skinComboBox.FormattingEnabled = true;
            _skinComboBox.Location = new Point(240, 45);
            _skinComboBox.Name = "_skinComboBox";
            _skinComboBox.Size = new Size(200, 23);
            _skinComboBox.TabIndex = 3;
            _skinComboBox.SelectedIndexChanged += OnSkinChanged;
            // 
            // _testButton
            // 
            _testButton.BackColor = Color.Transparent;
            _testButton.FlatStyle = FlatStyle.Flat;
            _testButton.Location = new Point(30, 50);
            _testButton.Name = "_testButton";
            _testButton.Size = new Size(200, 40);
            _testButton.TabIndex = 0;
            _testButton.Text = "Test Button - Hover Me!";
            _testButton.UseVisualStyleBackColor = false;
            // 
            // _testGroupBox
            // 
            _testGroupBox.BackColor = Color.Transparent;
            _testGroupBox.Controls.Add(_styleDetailsLabel);
            _testGroupBox.Controls.Add(_styleInfoLabel);
            _testGroupBox.Controls.Add(_disabledButton);
            _testGroupBox.Controls.Add(_normalButton);
            _testGroupBox.Controls.Add(_testButton);
            _testGroupBox.Location = new Point(20, 120);
            _testGroupBox.Name = "_testGroupBox";
            _testGroupBox.Size = new Size(740, 613);
            _testGroupBox.TabIndex = 5;
            _testGroupBox.TabStop = false;
            _testGroupBox.Text = "Test Controls";
            // 
            // _styleDetailsLabel
            // 
            _styleDetailsLabel.Location = new Point(30, 250);
            _styleDetailsLabel.Name = "_styleDetailsLabel";
            _styleDetailsLabel.Size = new Size(680, 150);
            _styleDetailsLabel.TabIndex = 5;
            // 
            // _styleInfoLabel
            // 
            _styleInfoLabel.AutoSize = true;
            _styleInfoLabel.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            _styleInfoLabel.Location = new Point(30, 220);
            _styleInfoLabel.Name = "_styleInfoLabel";
            _styleInfoLabel.Size = new Size(183, 19);
            _styleInfoLabel.TabIndex = 4;
            _styleInfoLabel.Text = "Current Style Information:";
            // 
            // _disabledButton
            // 
            _disabledButton.BackColor = Color.Transparent;
            _disabledButton.Enabled = false;
            _disabledButton.FlatStyle = FlatStyle.Flat;
            _disabledButton.Location = new Point(160, 160);
            _disabledButton.Name = "_disabledButton";
            _disabledButton.Size = new Size(120, 35);
            _disabledButton.TabIndex = 3;
            _disabledButton.Text = "Disabled State";
            _disabledButton.UseVisualStyleBackColor = false;
            // 
            // _normalButton
            // 
            _normalButton.BackColor = Color.Transparent;
            _normalButton.FlatStyle = FlatStyle.Flat;
            _normalButton.Location = new Point(30, 160);
            _normalButton.Name = "_normalButton";
            _normalButton.Size = new Size(120, 35);
            _normalButton.TabIndex = 2;
            _normalButton.Text = "Normal State";
            _normalButton.UseVisualStyleBackColor = false;
            // 
            // _infoLabel
            // 
            _infoLabel.AutoSize = true;
            _infoLabel.ForeColor = Color.DarkBlue;
            _infoLabel.Location = new Point(20, 85);
            _infoLabel.Name = "_infoLabel";
            _infoLabel.Size = new Size(28, 15);
            _infoLabel.TabIndex = 4;
            _infoLabel.Text = "Info";
            // 
            // _themeLabel
            // 
            _themeLabel.AutoSize = true;
            _themeLabel.Location = new Point(20, 20);
            _themeLabel.Name = "_themeLabel";
            _themeLabel.Size = new Size(135, 15);
            _themeLabel.TabIndex = 0;
            _themeLabel.Text = "Design System (Theme):";
            // 
            // _skinLabel
            // 
            _skinLabel.AutoSize = true;
            _skinLabel.Location = new Point(240, 20);
            _skinLabel.Name = "_skinLabel";
            _skinLabel.Size = new Size(111, 15);
            _skinLabel.TabIndex = 2;
            _skinLabel.Text = "Color Variant (Skin):";
            // 
            // ThemeTestForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 770);
            Controls.Add(_testGroupBox);
            Controls.Add(_infoLabel);
            Controls.Add(_skinComboBox);
            Controls.Add(_skinLabel);
            Controls.Add(_themeComboBox);
            Controls.Add(_themeLabel);
            Name = "ThemeTestForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Theme System Test - ModernWinForms";
            _testGroupBox.ResumeLayout(false);
            _testGroupBox.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Label _themeLabel;
        private System.Windows.Forms.ComboBox _themeComboBox;
        private System.Windows.Forms.Label _skinLabel;
        private System.Windows.Forms.ComboBox _skinComboBox;
        private System.Windows.Forms.Label _infoLabel;
        private ModernWinForms.Controls.ModernGroupBox _testGroupBox;
        private ModernWinForms.Controls.ModernButton _testButton;
        private ModernWinForms.Controls.ModernButton _normalButton;
        private ModernWinForms.Controls.ModernButton _disabledButton;
        private System.Windows.Forms.Label _styleInfoLabel;
        private System.Windows.Forms.Label _styleDetailsLabel;
    }
}
