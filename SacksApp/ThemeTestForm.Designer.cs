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
            _mainTableLayout = new ModernWinForms.Controls.ModernTableLayoutPanel();
            _themePanel = new ModernWinForms.Controls.ModernPanel();
            _themeLabel = new ModernWinForms.Controls.ModernLabel();
            _themeComboBox = new ModernWinForms.Controls.ModernComboBox();
            _skinLabel = new ModernWinForms.Controls.ModernLabel();
            _skinComboBox = new ModernWinForms.Controls.ModernComboBox();
            _infoLabel = new ModernWinForms.Controls.ModernLabel();
            _tabControl = new ModernWinForms.Controls.ModernTabControl();
            _tabBasicControls = new TabPage();
            _basicControlsFlow = new ModernWinForms.Controls.ModernFlowLayoutPanel();
            _buttonsGroup = new ModernWinForms.Controls.ModernGroupBox();
            _testButton = new ModernWinForms.Controls.ModernButton();
            _normalButton = new ModernWinForms.Controls.ModernButton();
            _disabledButton = new ModernWinForms.Controls.ModernButton();
            _inputGroup = new ModernWinForms.Controls.ModernGroupBox();
            _testTextBox = new ModernWinForms.Controls.ModernTextBox();
            _validTextBox = new ModernWinForms.Controls.ModernTextBox();
            _errorTextBox = new ModernWinForms.Controls.ModernTextBox();
            _warningTextBox = new ModernWinForms.Controls.ModernTextBox();
            _checkBoxGroup = new ModernWinForms.Controls.ModernGroupBox();
            _testCheckBox1 = new ModernWinForms.Controls.ModernCheckBox();
            _testCheckBox2 = new ModernWinForms.Controls.ModernCheckBox();
            _testCheckBox3 = new ModernWinForms.Controls.ModernCheckBox();
            _radioGroup = new ModernWinForms.Controls.ModernGroupBox();
            _radioButton1 = new ModernWinForms.Controls.ModernRadioButton();
            _radioButton2 = new ModernWinForms.Controls.ModernRadioButton();
            _radioButton3 = new ModernWinForms.Controls.ModernRadioButton();
            _tabDataControls = new TabPage();
            _splitContainer = new ModernWinForms.Controls.ModernSplitContainer();
            _dataGridView = new ModernWinForms.Controls.ModernDataGridView();
            _gridLabel = new ModernWinForms.Controls.ModernLabel();
            _statusStrip = new ModernWinForms.Controls.ModernStatusStrip();
            _statusLabel = new ToolStripStatusLabel();
            _mainTableLayout.SuspendLayout();
            _themePanel.SuspendLayout();
            _tabControl.SuspendLayout();
            _tabBasicControls.SuspendLayout();
            _basicControlsFlow.SuspendLayout();
            _buttonsGroup.SuspendLayout();
            _inputGroup.SuspendLayout();
            _checkBoxGroup.SuspendLayout();
            _radioGroup.SuspendLayout();
            _tabDataControls.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)_splitContainer).BeginInit();
            _splitContainer.Panel1.SuspendLayout();
            _splitContainer.Panel2.SuspendLayout();
            _splitContainer.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)_dataGridView).BeginInit();
            _statusStrip.SuspendLayout();
            SuspendLayout();
            // 
            // _mainTableLayout
            // 
            _mainTableLayout.ColumnCount = 1;
            _mainTableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            _mainTableLayout.Controls.Add(_themePanel, 0, 0);
            _mainTableLayout.Controls.Add(_tabControl, 0, 1);
            _mainTableLayout.Controls.Add(_statusStrip, 0, 2);
            _mainTableLayout.Dock = DockStyle.Fill;
            _mainTableLayout.Location = new Point(0, 0);
            _mainTableLayout.Name = "_mainTableLayout";
            _mainTableLayout.RowCount = 3;
            _mainTableLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 100F));
            _mainTableLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            _mainTableLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            _mainTableLayout.Size = new Size(1000, 700);
            _mainTableLayout.TabIndex = 0;
            // 
            // _themePanel
            // 
            _themePanel.Controls.Add(_themeLabel);
            _themePanel.Controls.Add(_themeComboBox);
            _themePanel.Controls.Add(_skinLabel);
            _themePanel.Controls.Add(_skinComboBox);
            _themePanel.Controls.Add(_infoLabel);
            _themePanel.Dock = DockStyle.Fill;
            _themePanel.Location = new Point(3, 3);
            _themePanel.Name = "_themePanel";
            _themePanel.Size = new Size(994, 94);
            _themePanel.TabIndex = 0;
            // 
            // _themeLabel
            // 
            _themeLabel.AutoSize = true;
            _themeLabel.Location = new Point(10, 10);
            _themeLabel.Name = "_themeLabel";
            _themeLabel.Size = new Size(135, 15);
            _themeLabel.TabIndex = 0;
            _themeLabel.Text = "Design System (Theme):";
            // 
            // _themeComboBox
            // 
            _themeComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            _themeComboBox.FormattingEnabled = true;
            _themeComboBox.Location = new Point(10, 30);
            _themeComboBox.Name = "_themeComboBox";
            _themeComboBox.Size = new Size(200, 23);
            _themeComboBox.TabIndex = 1;
            _themeComboBox.SelectedIndexChanged += OnThemeChanged;
            // 
            // _skinLabel
            // 
            _skinLabel.AutoSize = true;
            _skinLabel.Location = new Point(220, 10);
            _skinLabel.Name = "_skinLabel";
            _skinLabel.Size = new Size(111, 15);
            _skinLabel.TabIndex = 2;
            _skinLabel.Text = "Color Variant (Skin):";
            // 
            // _skinComboBox
            // 
            _skinComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            _skinComboBox.FormattingEnabled = true;
            _skinComboBox.Location = new Point(220, 30);
            _skinComboBox.Name = "_skinComboBox";
            _skinComboBox.Size = new Size(200, 23);
            _skinComboBox.TabIndex = 3;
            _skinComboBox.SelectedIndexChanged += OnSkinChanged;
            // 
            // _infoLabel
            // 
            _infoLabel.AutoSize = true;
            _infoLabel.Location = new Point(10, 65);
            _infoLabel.Name = "_infoLabel";
            _infoLabel.Size = new Size(300, 15);
            _infoLabel.TabIndex = 4;
            _infoLabel.Text = "Active: GitHub theme + light skin";
            // 
            // _tabControl
            // 
            _tabControl.Controls.Add(_tabBasicControls);
            _tabControl.Controls.Add(_tabDataControls);
            _tabControl.Dock = DockStyle.Fill;
            _tabControl.Location = new Point(3, 103);
            _tabControl.Name = "_tabControl";
            _tabControl.SelectedIndex = 0;
            _tabControl.Size = new Size(994, 564);
            _tabControl.TabIndex = 1;
            // 
            // _tabBasicControls
            // 
            _tabBasicControls.Controls.Add(_basicControlsFlow);
            _tabBasicControls.Location = new Point(4, 44);
            _tabBasicControls.Name = "_tabBasicControls";
            _tabBasicControls.Padding = new Padding(3);
            _tabBasicControls.Size = new Size(986, 516);
            _tabBasicControls.TabIndex = 0;
            _tabBasicControls.Text = "Basic Controls";
            _tabBasicControls.UseVisualStyleBackColor = true;
            // 
            // _basicControlsFlow
            // 
            _basicControlsFlow.AutoScroll = true;
            _basicControlsFlow.Controls.Add(_buttonsGroup);
            _basicControlsFlow.Controls.Add(_inputGroup);
            _basicControlsFlow.Controls.Add(_checkBoxGroup);
            _basicControlsFlow.Controls.Add(_radioGroup);
            _basicControlsFlow.Dock = DockStyle.Fill;
            _basicControlsFlow.FlowDirection = FlowDirection.TopDown;
            _basicControlsFlow.Location = new Point(3, 3);
            _basicControlsFlow.Name = "_basicControlsFlow";
            _basicControlsFlow.Padding = new Padding(10);
            _basicControlsFlow.Size = new Size(980, 510);
            _basicControlsFlow.TabIndex = 0;
            _basicControlsFlow.WrapContents = false;
            // 
            // _buttonsGroup
            // 
            _buttonsGroup.Controls.Add(_testButton);
            _buttonsGroup.Controls.Add(_normalButton);
            _buttonsGroup.Controls.Add(_disabledButton);
            _buttonsGroup.Location = new Point(13, 13);
            _buttonsGroup.Name = "_buttonsGroup";
            _buttonsGroup.Size = new Size(940, 100);
            _buttonsGroup.TabIndex = 0;
            _buttonsGroup.TabStop = false;
            _buttonsGroup.Text = "ModernButton (with hover animation)";
            // 
            // _testButton
            // 
            _testButton.Location = new Point(20, 30);
            _testButton.Name = "_testButton";
            _testButton.Size = new Size(200, 45);
            _testButton.TabIndex = 0;
            _testButton.Text = "Hover Me!";
            // 
            // _normalButton
            // 
            _normalButton.Location = new Point(240, 30);
            _normalButton.Name = "_normalButton";
            _normalButton.Size = new Size(150, 45);
            _normalButton.TabIndex = 1;
            _normalButton.Text = "Normal State";
            // 
            // _disabledButton
            // 
            _disabledButton.Enabled = false;
            _disabledButton.Location = new Point(410, 30);
            _disabledButton.Name = "_disabledButton";
            _disabledButton.Size = new Size(150, 45);
            _disabledButton.TabIndex = 2;
            _disabledButton.Text = "Disabled State";
            // 
            // _inputGroup
            // 
            _inputGroup.Controls.Add(_testTextBox);
            _inputGroup.Controls.Add(_validTextBox);
            _inputGroup.Controls.Add(_errorTextBox);
            _inputGroup.Controls.Add(_warningTextBox);
            _inputGroup.Location = new Point(13, 119);
            _inputGroup.Name = "_inputGroup";
            _inputGroup.Size = new Size(940, 120);
            _inputGroup.TabIndex = 1;
            _inputGroup.TabStop = false;
            _inputGroup.Text = "ModernTextBox (with validation states)";
            // 
            // _testTextBox
            // 
            _testTextBox.Location = new Point(20, 30);
            _testTextBox.Name = "_testTextBox";
            _testTextBox.PlaceholderText = "Enter text here...";
            _testTextBox.Size = new Size(400, 35);
            _testTextBox.TabIndex = 0;
            // 
            // _validTextBox
            // 
            _validTextBox.Location = new Point(20, 75);
            _validTextBox.Name = "_validTextBox";
            _validTextBox.Size = new Size(250, 35);
            _validTextBox.TabIndex = 1;
            _validTextBox.Text = "Valid input";
            _validTextBox.ValidationState = ModernWinForms.Validation.ValidationState.Success;
            // 
            // _errorTextBox
            // 
            _errorTextBox.Location = new Point(290, 75);
            _errorTextBox.Name = "_errorTextBox";
            _errorTextBox.Size = new Size(250, 35);
            _errorTextBox.TabIndex = 2;
            _errorTextBox.Text = "Error state";
            _errorTextBox.ValidationState = ModernWinForms.Validation.ValidationState.Error;
            _errorTextBox.ValidationMessage = "This field has an error";
            // 
            // _warningTextBox
            // 
            _warningTextBox.Location = new Point(560, 75);
            _warningTextBox.Name = "_warningTextBox";
            _warningTextBox.Size = new Size(250, 35);
            _warningTextBox.TabIndex = 3;
            _warningTextBox.Text = "Warning state";
            _warningTextBox.ValidationState = ModernWinForms.Validation.ValidationState.Warning;
            _warningTextBox.ValidationMessage = "This field has a warning";
            // 
            // _checkBoxGroup
            // 
            _checkBoxGroup.Controls.Add(_testCheckBox1);
            _checkBoxGroup.Controls.Add(_testCheckBox2);
            _checkBoxGroup.Controls.Add(_testCheckBox3);
            _checkBoxGroup.Location = new Point(13, 245);
            _checkBoxGroup.Name = "_checkBoxGroup";
            _checkBoxGroup.Size = new Size(940, 80);
            _checkBoxGroup.TabIndex = 2;
            _checkBoxGroup.TabStop = false;
            _checkBoxGroup.Text = "ModernCheckBox (with check animation)";
            // 
            // _testCheckBox1
            // 
            _testCheckBox1.AutoSize = true;
            _testCheckBox1.Location = new Point(20, 30);
            _testCheckBox1.Name = "_testCheckBox1";
            _testCheckBox1.Size = new Size(150, 19);
            _testCheckBox1.TabIndex = 0;
            _testCheckBox1.Text = "Option One";
            // 
            // _testCheckBox2
            // 
            _testCheckBox2.AutoSize = true;
            _testCheckBox2.Checked = true;
            _testCheckBox2.CheckState = CheckState.Checked;
            _testCheckBox2.Location = new Point(200, 30);
            _testCheckBox2.Name = "_testCheckBox2";
            _testCheckBox2.Size = new Size(150, 19);
            _testCheckBox2.TabIndex = 1;
            _testCheckBox2.Text = "Option Two (Checked)";
            // 
            // _testCheckBox3
            // 
            _testCheckBox3.AutoSize = true;
            _testCheckBox3.Enabled = false;
            _testCheckBox3.Location = new Point(380, 30);
            _testCheckBox3.Name = "_testCheckBox3";
            _testCheckBox3.Size = new Size(150, 19);
            _testCheckBox3.TabIndex = 2;
            _testCheckBox3.Text = "Option Three (Disabled)";
            // 
            // _radioGroup
            // 
            _radioGroup.Controls.Add(_radioButton1);
            _radioGroup.Controls.Add(_radioButton2);
            _radioGroup.Controls.Add(_radioButton3);
            _radioGroup.Location = new Point(13, 331);
            _radioGroup.Name = "_radioGroup";
            _radioGroup.Size = new Size(940, 80);
            _radioGroup.TabIndex = 3;
            _radioGroup.TabStop = false;
            _radioGroup.Text = "ModernRadioButton (with selection animation)";
            // 
            // _radioButton1
            // 
            _radioButton1.AutoSize = true;
            _radioButton1.Location = new Point(20, 30);
            _radioButton1.Name = "_radioButton1";
            _radioButton1.Size = new Size(120, 19);
            _radioButton1.TabIndex = 0;
            _radioButton1.Text = "Choice A";
            // 
            // _radioButton2
            // 
            _radioButton2.AutoSize = true;
            _radioButton2.Checked = true;
            _radioButton2.Location = new Point(200, 30);
            _radioButton2.Name = "_radioButton2";
            _radioButton2.Size = new Size(150, 19);
            _radioButton2.TabIndex = 1;
            _radioButton2.TabStop = true;
            _radioButton2.Text = "Choice B (Selected)";
            // 
            // _radioButton3
            // 
            _radioButton3.AutoSize = true;
            _radioButton3.Location = new Point(380, 30);
            _radioButton3.Name = "_radioButton3";
            _radioButton3.Size = new Size(120, 19);
            _radioButton3.TabIndex = 2;
            _radioButton3.Text = "Choice C";
            // 
            // _tabDataControls
            // 
            _tabDataControls.Controls.Add(_splitContainer);
            _tabDataControls.Location = new Point(4, 44);
            _tabDataControls.Name = "_tabDataControls";
            _tabDataControls.Padding = new Padding(3);
            _tabDataControls.Size = new Size(986, 516);
            _tabDataControls.TabIndex = 1;
            _tabDataControls.Text = "Data Controls";
            _tabDataControls.UseVisualStyleBackColor = true;
            // 
            // _splitContainer
            // 
            _splitContainer.Dock = DockStyle.Fill;
            _splitContainer.Location = new Point(3, 3);
            _splitContainer.Name = "_splitContainer";
            _splitContainer.Orientation = Orientation.Horizontal;
            // 
            // _splitContainer.Panel1
            // 
            _splitContainer.Panel1.Controls.Add(_gridLabel);
            // 
            // _splitContainer.Panel2
            // 
            _splitContainer.Panel2.Controls.Add(_dataGridView);
            _splitContainer.Size = new Size(980, 510);
            _splitContainer.SplitterDistance = 50;
            _splitContainer.TabIndex = 0;
            // 
            // _gridLabel
            // 
            _gridLabel.AutoSize = true;
            _gridLabel.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            _gridLabel.Location = new Point(10, 15);
            _gridLabel.Name = "_gridLabel";
            _gridLabel.Size = new Size(550, 21);
            _gridLabel.TabIndex = 0;
            _gridLabel.Text = "ModernDataGridView, ModernSplitContainer, ModernLabel";
            // 
            // _dataGridView
            // 
            _dataGridView.AllowUserToAddRows = false;
            _dataGridView.AllowUserToDeleteRows = false;
            _dataGridView.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            _dataGridView.Dock = DockStyle.Fill;
            _dataGridView.Location = new Point(0, 0);
            _dataGridView.Name = "_dataGridView";
            _dataGridView.ReadOnly = true;
            _dataGridView.Size = new Size(980, 456);
            _dataGridView.TabIndex = 0;
            // 
            // _statusStrip
            // 
            _statusStrip.Items.AddRange(new ToolStripItem[] { _statusLabel });
            _statusStrip.Location = new Point(0, 670);
            _statusStrip.Name = "_statusStrip";
            _statusStrip.Size = new Size(1000, 30);
            _statusStrip.TabIndex = 2;
            _statusStrip.Text = "modernStatusStrip1";
            // 
            // _statusLabel
            // 
            _statusLabel.Name = "_statusLabel";
            _statusLabel.Size = new Size(400, 25);
            _statusLabel.Text = "Ready - Showing all 15 Modern controls with theming support";
            // 
            // ThemeTestForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1000, 700);
            Controls.Add(_mainTableLayout);
            Name = "ThemeTestForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Modern Controls Theme Test - All 15 Controls";
            _mainTableLayout.ResumeLayout(false);
            _mainTableLayout.PerformLayout();
            _themePanel.ResumeLayout(false);
            _themePanel.PerformLayout();
            _tabControl.ResumeLayout(false);
            _tabBasicControls.ResumeLayout(false);
            _basicControlsFlow.ResumeLayout(false);
            _buttonsGroup.ResumeLayout(false);
            _inputGroup.ResumeLayout(false);
            _checkBoxGroup.ResumeLayout(false);
            _checkBoxGroup.PerformLayout();
            _radioGroup.ResumeLayout(false);
            _radioGroup.PerformLayout();
            _tabDataControls.ResumeLayout(false);
            _splitContainer.Panel1.ResumeLayout(false);
            _splitContainer.Panel1.PerformLayout();
            _splitContainer.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)_splitContainer).EndInit();
            _splitContainer.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)_dataGridView).EndInit();
            _statusStrip.ResumeLayout(false);
            _statusStrip.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private ModernWinForms.Controls.ModernTableLayoutPanel _mainTableLayout;
        private ModernWinForms.Controls.ModernPanel _themePanel;
        private ModernWinForms.Controls.ModernLabel _themeLabel;
        private ModernWinForms.Controls.ModernComboBox _themeComboBox;
        private ModernWinForms.Controls.ModernLabel _skinLabel;
        private ModernWinForms.Controls.ModernComboBox _skinComboBox;
        private ModernWinForms.Controls.ModernLabel _infoLabel;
        private ModernWinForms.Controls.ModernTabControl _tabControl;
        private TabPage _tabBasicControls;
        private TabPage _tabDataControls;
        private ModernWinForms.Controls.ModernFlowLayoutPanel _basicControlsFlow;
        private ModernWinForms.Controls.ModernGroupBox _buttonsGroup;
        private ModernWinForms.Controls.ModernButton _testButton;
        private ModernWinForms.Controls.ModernButton _normalButton;
        private ModernWinForms.Controls.ModernButton _disabledButton;
        private ModernWinForms.Controls.ModernGroupBox _inputGroup;
        private ModernWinForms.Controls.ModernTextBox _testTextBox;
        private ModernWinForms.Controls.ModernTextBox _validTextBox;
        private ModernWinForms.Controls.ModernTextBox _errorTextBox;
        private ModernWinForms.Controls.ModernTextBox _warningTextBox;
        private ModernWinForms.Controls.ModernGroupBox _checkBoxGroup;
        private ModernWinForms.Controls.ModernCheckBox _testCheckBox1;
        private ModernWinForms.Controls.ModernCheckBox _testCheckBox2;
        private ModernWinForms.Controls.ModernCheckBox _testCheckBox3;
        private ModernWinForms.Controls.ModernGroupBox _radioGroup;
        private ModernWinForms.Controls.ModernRadioButton _radioButton1;
        private ModernWinForms.Controls.ModernRadioButton _radioButton2;
        private ModernWinForms.Controls.ModernRadioButton _radioButton3;
        private ModernWinForms.Controls.ModernSplitContainer _splitContainer;
        private ModernWinForms.Controls.ModernDataGridView _dataGridView;
        private ModernWinForms.Controls.ModernLabel _gridLabel;
        private ModernWinForms.Controls.ModernStatusStrip _statusStrip;
        private ToolStripStatusLabel _statusLabel;
    }
}
