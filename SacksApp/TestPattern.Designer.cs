namespace SacksApp
{
    partial class TestPattern
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
            textBoxInputText = new TextBox();
            textBoxPatterm = new TextBox();
            textBoxResults = new TextBox();
            buttonRun = new Button();
            label1 = new Label();
            label2 = new Label();
            textBoxPatternKey = new TextBox();
            label4 = new Label();
            textBoxSeedKey = new TextBox();
            textBoxSeedValue = new TextBox();
            label5 = new Label();
            label6 = new Label();
            label7 = new Label();
            textBoxActionJson = new TextBox();
            chkIgnoreCase = new CheckBox();
            chkRemove = new CheckBox();
            tableLayoutPanelCommon = new TableLayoutPanel();
            textBoxCondition = new TextBox();
            textBoxInputKey = new TextBox();
            checkAssign = new CheckBox();
            label8 = new Label();
            textBoxOutputName = new TextBox();
            comboBoxOp = new ComboBox();
            label9 = new Label();
            labelCondition = new Label();
            label3 = new Label();
            tableLayoutPanel2 = new TableLayoutPanel();
            tableLayoutPanelFindAction = new TableLayoutPanel();
            chkAll = new RadioButton();
            chkFirst = new RadioButton();
            chkLast = new RadioButton();
            tableLayoutPanelMapAction = new TableLayoutPanel();
            textBoxMapTable = new TextBox();
            label10 = new Label();
            textBoxMapInputKey = new TextBox();
            label12 = new Label();
            tableLayoutPanelSplitAction = new TableLayoutPanel();
            textBoxDelimiter = new TextBox();
            label11 = new Label();
            textBoxSplitOutputKey = new TextBox();
            label13 = new Label();
            tableLayoutPanelCaseAction = new TableLayoutPanel();
            radioButtonTitle = new RadioButton();
            radioButtonUpper = new RadioButton();
            radioButtonLower = new RadioButton();
            tableLayoutPanelCommon.SuspendLayout();
            tableLayoutPanel2.SuspendLayout();
            tableLayoutPanelFindAction.SuspendLayout();
            tableLayoutPanelMapAction.SuspendLayout();
            tableLayoutPanelSplitAction.SuspendLayout();
            tableLayoutPanelCaseAction.SuspendLayout();
            SuspendLayout();
            // 
            // textBoxInputText
            // 
            textBoxInputText.Dock = DockStyle.Fill;
            textBoxInputText.Location = new Point(130, 32);
            textBoxInputText.Name = "textBoxInputText";
            textBoxInputText.PlaceholderText = "the raw input text (goes into bag[inputKey] when inputKey == \"Text\")";
            textBoxInputText.Size = new Size(566, 23);
            textBoxInputText.TabIndex = 0;
            // 
            // textBoxPatterm
            // 
            tableLayoutPanelFindAction.SetColumnSpan(textBoxPatterm, 3);
            textBoxPatterm.Dock = DockStyle.Fill;
            textBoxPatterm.Location = new Point(92, 3);
            textBoxPatterm.Name = "textBoxPatterm";
            textBoxPatterm.PlaceholderText = "regex pattern or “lookup:TableName”";
            textBoxPatterm.Size = new Size(606, 23);
            textBoxPatterm.TabIndex = 1;
            // 
            // textBoxResults
            // 
            textBoxResults.Dock = DockStyle.Fill;
            textBoxResults.Location = new Point(109, 3);
            textBoxResults.Multiline = true;
            textBoxResults.Name = "textBoxResults";
            textBoxResults.ReadOnly = true;
            textBoxResults.ScrollBars = ScrollBars.Vertical;
            textBoxResults.Size = new Size(587, 61);
            textBoxResults.TabIndex = 14;
            // 
            // buttonRun
            // 
            buttonRun.Anchor = AnchorStyles.Top;
            tableLayoutPanel2.SetColumnSpan(buttonRun, 2);
            buttonRun.Location = new Point(275, 137);
            buttonRun.Name = "buttonRun";
            buttonRun.Size = new Size(148, 36);
            buttonRun.TabIndex = 16;
            buttonRun.Text = "Run";
            buttonRun.UseVisualStyleBackColor = true;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(3, 29);
            label1.Name = "label1";
            label1.Size = new Size(35, 15);
            label1.TabIndex = 4;
            label1.Text = "Input";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(3, 0);
            label2.Name = "label2";
            label2.Size = new Size(45, 15);
            label2.TabIndex = 5;
            label2.Text = "Pattern";
            // 
            // textBoxPatternKey
            // 
            tableLayoutPanelFindAction.SetColumnSpan(textBoxPatternKey, 3);
            textBoxPatternKey.Dock = DockStyle.Fill;
            textBoxPatternKey.Location = new Point(92, 32);
            textBoxPatternKey.Name = "textBoxPatternKey";
            textBoxPatternKey.PlaceholderText = "optional dynamic key (e.g., Product.Brand)";
            textBoxPatternKey.Size = new Size(606, 23);
            textBoxPatternKey.TabIndex = 7;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(3, 29);
            label4.Name = "label4";
            label4.Size = new Size(64, 15);
            label4.TabIndex = 10;
            label4.Text = "PatternKey";
            // 
            // textBoxSeedKey
            // 
            textBoxSeedKey.Dock = DockStyle.Fill;
            textBoxSeedKey.Location = new Point(92, 61);
            textBoxSeedKey.Name = "textBoxSeedKey";
            textBoxSeedKey.PlaceholderText = "Seed Key (optional)";
            textBoxSeedKey.Size = new Size(200, 23);
            textBoxSeedKey.TabIndex = 8;
            // 
            // textBoxSeedValue
            // 
            textBoxSeedValue.Dock = DockStyle.Fill;
            textBoxSeedValue.Location = new Point(350, 61);
            textBoxSeedValue.Name = "textBoxSeedValue";
            textBoxSeedValue.PlaceholderText = "Seed Value";
            textBoxSeedValue.Size = new Size(348, 23);
            textBoxSeedValue.TabIndex = 9;
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new Point(3, 58);
            label5.Name = "label5";
            label5.Size = new Size(54, 15);
            label5.TabIndex = 13;
            label5.Text = "Seed Key";
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Location = new Point(298, 58);
            label6.Name = "label6";
            label6.Size = new Size(35, 15);
            label6.TabIndex = 14;
            label6.Text = "Value";
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Location = new Point(3, 67);
            label7.Name = "label7";
            label7.Size = new Size(100, 15);
            label7.TabIndex = 15;
            label7.Text = "Action JSON (RO)";
            // 
            // textBoxActionJson
            // 
            textBoxActionJson.Dock = DockStyle.Fill;
            textBoxActionJson.Location = new Point(109, 70);
            textBoxActionJson.Multiline = true;
            textBoxActionJson.Name = "textBoxActionJson";
            textBoxActionJson.ReadOnly = true;
            textBoxActionJson.ScrollBars = ScrollBars.Vertical;
            textBoxActionJson.Size = new Size(587, 61);
            textBoxActionJson.TabIndex = 15;
            // 
            // chkIgnoreCase
            // 
            chkIgnoreCase.AutoSize = true;
            chkIgnoreCase.Location = new Point(3, 115);
            chkIgnoreCase.Name = "chkIgnoreCase";
            chkIgnoreCase.Size = new Size(83, 19);
            chkIgnoreCase.TabIndex = 6;
            chkIgnoreCase.Text = "ignorecase";
            chkIgnoreCase.UseVisualStyleBackColor = true;
            // 
            // chkRemove
            // 
            chkRemove.AutoSize = true;
            chkRemove.Location = new Point(92, 115);
            chkRemove.Name = "chkRemove";
            chkRemove.Size = new Size(66, 19);
            chkRemove.TabIndex = 11;
            chkRemove.Text = "remove";
            chkRemove.UseVisualStyleBackColor = true;
            // 
            // tableLayoutPanelCommon
            // 
            tableLayoutPanelCommon.AutoSize = true;
            tableLayoutPanelCommon.ColumnCount = 2;
            tableLayoutPanelCommon.ColumnStyles.Add(new ColumnStyle());
            tableLayoutPanelCommon.ColumnStyles.Add(new ColumnStyle());
            tableLayoutPanelCommon.Controls.Add(textBoxCondition, 1, 3);
            tableLayoutPanelCommon.Controls.Add(textBoxInputKey, 1, 0);
            tableLayoutPanelCommon.Controls.Add(label1, 0, 1);
            tableLayoutPanelCommon.Controls.Add(textBoxInputText, 1, 1);
            tableLayoutPanelCommon.Controls.Add(checkAssign, 1, 4);
            tableLayoutPanelCommon.Controls.Add(label8, 0, 2);
            tableLayoutPanelCommon.Controls.Add(textBoxOutputName, 1, 2);
            tableLayoutPanelCommon.Controls.Add(comboBoxOp, 0, 4);
            tableLayoutPanelCommon.Controls.Add(label9, 0, 0);
            tableLayoutPanelCommon.Controls.Add(labelCondition, 0, 3);
            tableLayoutPanelCommon.Dock = DockStyle.Top;
            tableLayoutPanelCommon.Location = new Point(5, 5);
            tableLayoutPanelCommon.Name = "tableLayoutPanelCommon";
            tableLayoutPanelCommon.RowCount = 5;
            tableLayoutPanelCommon.RowStyles.Add(new RowStyle());
            tableLayoutPanelCommon.RowStyles.Add(new RowStyle());
            tableLayoutPanelCommon.RowStyles.Add(new RowStyle());
            tableLayoutPanelCommon.RowStyles.Add(new RowStyle());
            tableLayoutPanelCommon.RowStyles.Add(new RowStyle());
            tableLayoutPanelCommon.Size = new Size(699, 145);
            tableLayoutPanelCommon.TabIndex = 17;
            // 
            // textBoxCondition
            // 
            textBoxCondition.Dock = DockStyle.Fill;
            textBoxCondition.Location = new Point(130, 90);
            textBoxCondition.Name = "textBoxCondition";
            textBoxCondition.PlaceholderText = "optional condition";
            textBoxCondition.Size = new Size(566, 23);
            textBoxCondition.TabIndex = 18;
            // 
            // textBoxInputKey
            // 
            textBoxInputKey.Dock = DockStyle.Fill;
            textBoxInputKey.Location = new Point(130, 3);
            textBoxInputKey.Name = "textBoxInputKey";
            textBoxInputKey.PlaceholderText = "default \"Text\"";
            textBoxInputKey.Size = new Size(566, 23);
            textBoxInputKey.TabIndex = 17;
            // 
            // checkAssign
            // 
            checkAssign.AutoSize = true;
            checkAssign.Location = new Point(130, 119);
            checkAssign.Name = "checkAssign";
            checkAssign.Size = new Size(66, 19);
            checkAssign.TabIndex = 11;
            checkAssign.Text = "remove";
            checkAssign.UseVisualStyleBackColor = true;
            // 
            // label8
            // 
            label8.AutoSize = true;
            label8.Location = new Point(3, 58);
            label8.Name = "label8";
            label8.Size = new Size(80, 15);
            label8.TabIndex = 4;
            label8.Text = "Output Name";
            // 
            // textBoxOutputName
            // 
            textBoxOutputName.Dock = DockStyle.Fill;
            textBoxOutputName.Location = new Point(130, 61);
            textBoxOutputName.Name = "textBoxOutputName";
            textBoxOutputName.PlaceholderText = "default \"Out\"";
            textBoxOutputName.Size = new Size(566, 23);
            textBoxOutputName.TabIndex = 1;
            // 
            // comboBoxOp
            // 
            comboBoxOp.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBoxOp.FormattingEnabled = true;
            comboBoxOp.Items.AddRange(new object[] { "Find", "SetAssign", "Case", "Map", "Split" });
            comboBoxOp.Location = new Point(3, 119);
            comboBoxOp.Name = "comboBoxOp";
            comboBoxOp.Size = new Size(121, 23);
            comboBoxOp.TabIndex = 16;
            // 
            // label9
            // 
            label9.AutoSize = true;
            label9.Location = new Point(3, 0);
            label9.Name = "label9";
            label9.Size = new Size(60, 15);
            label9.TabIndex = 4;
            label9.Text = "Input Key:";
            // 
            // labelCondition
            // 
            labelCondition.AutoSize = true;
            labelCondition.Location = new Point(3, 87);
            labelCondition.Name = "labelCondition";
            labelCondition.Size = new Size(60, 15);
            labelCondition.TabIndex = 4;
            labelCondition.Text = "Condition";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(3, 0);
            label3.Name = "label3";
            label3.Size = new Size(47, 15);
            label3.TabIndex = 5;
            label3.Text = "Results:";
            // 
            // tableLayoutPanel2
            // 
            tableLayoutPanel2.ColumnCount = 2;
            tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle());
            tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle());
            tableLayoutPanel2.Controls.Add(label3, 0, 0);
            tableLayoutPanel2.Controls.Add(label7, 0, 1);
            tableLayoutPanel2.Controls.Add(textBoxResults, 1, 0);
            tableLayoutPanel2.Controls.Add(textBoxActionJson, 1, 1);
            tableLayoutPanel2.Controls.Add(buttonRun, 0, 2);
            tableLayoutPanel2.Dock = DockStyle.Fill;
            tableLayoutPanel2.Location = new Point(5, 440);
            tableLayoutPanel2.Name = "tableLayoutPanel2";
            tableLayoutPanel2.RowCount = 3;
            tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Percent, 49.9999962F));
            tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Percent, 50.0000076F));
            tableLayoutPanel2.RowStyles.Add(new RowStyle());
            tableLayoutPanel2.Size = new Size(699, 177);
            tableLayoutPanel2.TabIndex = 18;
            // 
            // tableLayoutPanelFindAction
            // 
            tableLayoutPanelFindAction.AutoSize = true;
            tableLayoutPanelFindAction.ColumnCount = 4;
            tableLayoutPanelFindAction.ColumnStyles.Add(new ColumnStyle());
            tableLayoutPanelFindAction.ColumnStyles.Add(new ColumnStyle());
            tableLayoutPanelFindAction.ColumnStyles.Add(new ColumnStyle());
            tableLayoutPanelFindAction.ColumnStyles.Add(new ColumnStyle());
            tableLayoutPanelFindAction.Controls.Add(label2, 0, 0);
            tableLayoutPanelFindAction.Controls.Add(label4, 0, 1);
            tableLayoutPanelFindAction.Controls.Add(label5, 0, 2);
            tableLayoutPanelFindAction.Controls.Add(chkRemove, 1, 4);
            tableLayoutPanelFindAction.Controls.Add(textBoxPatterm, 1, 0);
            tableLayoutPanelFindAction.Controls.Add(chkIgnoreCase, 0, 4);
            tableLayoutPanelFindAction.Controls.Add(textBoxPatternKey, 1, 1);
            tableLayoutPanelFindAction.Controls.Add(textBoxSeedKey, 1, 2);
            tableLayoutPanelFindAction.Controls.Add(label6, 2, 2);
            tableLayoutPanelFindAction.Controls.Add(textBoxSeedValue, 3, 2);
            tableLayoutPanelFindAction.Controls.Add(chkAll, 0, 3);
            tableLayoutPanelFindAction.Controls.Add(chkFirst, 1, 3);
            tableLayoutPanelFindAction.Controls.Add(chkLast, 2, 3);
            tableLayoutPanelFindAction.Dock = DockStyle.Top;
            tableLayoutPanelFindAction.Location = new Point(5, 150);
            tableLayoutPanelFindAction.Name = "tableLayoutPanelFindAction";
            tableLayoutPanelFindAction.RowCount = 5;
            tableLayoutPanelFindAction.RowStyles.Add(new RowStyle());
            tableLayoutPanelFindAction.RowStyles.Add(new RowStyle());
            tableLayoutPanelFindAction.RowStyles.Add(new RowStyle());
            tableLayoutPanelFindAction.RowStyles.Add(new RowStyle());
            tableLayoutPanelFindAction.RowStyles.Add(new RowStyle());
            tableLayoutPanelFindAction.Size = new Size(699, 137);
            tableLayoutPanelFindAction.TabIndex = 19;
            // 
            // chkAll
            // 
            chkAll.AutoSize = true;
            chkAll.Location = new Point(3, 90);
            chkAll.Name = "chkAll";
            chkAll.Size = new Size(39, 19);
            chkAll.TabIndex = 15;
            chkAll.TabStop = true;
            chkAll.Text = "All";
            chkAll.UseVisualStyleBackColor = true;
            // 
            // chkFirst
            // 
            chkFirst.AutoSize = true;
            chkFirst.Location = new Point(92, 90);
            chkFirst.Name = "chkFirst";
            chkFirst.Size = new Size(47, 19);
            chkFirst.TabIndex = 15;
            chkFirst.TabStop = true;
            chkFirst.Text = "First";
            chkFirst.UseVisualStyleBackColor = true;
            // 
            // chkLast
            // 
            chkLast.AutoSize = true;
            chkLast.Location = new Point(298, 90);
            chkLast.Name = "chkLast";
            chkLast.Size = new Size(46, 19);
            chkLast.TabIndex = 15;
            chkLast.TabStop = true;
            chkLast.Text = "Last";
            chkLast.UseVisualStyleBackColor = true;
            // 
            // tableLayoutPanelMapAction
            // 
            tableLayoutPanelMapAction.AutoSize = true;
            tableLayoutPanelMapAction.ColumnCount = 2;
            tableLayoutPanelMapAction.ColumnStyles.Add(new ColumnStyle());
            tableLayoutPanelMapAction.ColumnStyles.Add(new ColumnStyle());
            tableLayoutPanelMapAction.Controls.Add(textBoxMapTable, 1, 0);
            tableLayoutPanelMapAction.Controls.Add(label10, 0, 1);
            tableLayoutPanelMapAction.Controls.Add(textBoxMapInputKey, 1, 1);
            tableLayoutPanelMapAction.Controls.Add(label12, 0, 0);
            tableLayoutPanelMapAction.Dock = DockStyle.Top;
            tableLayoutPanelMapAction.Location = new Point(5, 287);
            tableLayoutPanelMapAction.Name = "tableLayoutPanelMapAction";
            tableLayoutPanelMapAction.RowCount = 5;
            tableLayoutPanelMapAction.RowStyles.Add(new RowStyle());
            tableLayoutPanelMapAction.RowStyles.Add(new RowStyle());
            tableLayoutPanelMapAction.RowStyles.Add(new RowStyle());
            tableLayoutPanelMapAction.RowStyles.Add(new RowStyle());
            tableLayoutPanelMapAction.RowStyles.Add(new RowStyle());
            tableLayoutPanelMapAction.Size = new Size(699, 58);
            tableLayoutPanelMapAction.TabIndex = 20;
            // 
            // textBoxMapTable
            // 
            textBoxMapTable.Dock = DockStyle.Fill;
            textBoxMapTable.Location = new Point(47, 3);
            textBoxMapTable.Name = "textBoxMapTable";
            textBoxMapTable.PlaceholderText = " lookup table name (Parameters[\"Table\"])";
            textBoxMapTable.Size = new Size(649, 23);
            textBoxMapTable.TabIndex = 17;
            // 
            // label10
            // 
            label10.AutoSize = true;
            label10.Location = new Point(3, 29);
            label10.Name = "label10";
            label10.Size = new Size(35, 15);
            label10.TabIndex = 4;
            label10.Text = "Input";
            // 
            // textBoxMapInputKey
            // 
            textBoxMapInputKey.Dock = DockStyle.Fill;
            textBoxMapInputKey.Location = new Point(47, 32);
            textBoxMapInputKey.Name = "textBoxMapInputKey";
            textBoxMapInputKey.PlaceholderText = "source key (defaults to textBoxInputKey if empty)";
            textBoxMapInputKey.Size = new Size(649, 23);
            textBoxMapInputKey.TabIndex = 0;
            // 
            // label12
            // 
            label12.AutoSize = true;
            label12.Location = new Point(3, 0);
            label12.Name = "label12";
            label12.Size = new Size(38, 15);
            label12.TabIndex = 4;
            label12.Text = "Table:";
            // 
            // tableLayoutPanelSplitAction
            // 
            tableLayoutPanelSplitAction.AutoSize = true;
            tableLayoutPanelSplitAction.ColumnCount = 2;
            tableLayoutPanelSplitAction.ColumnStyles.Add(new ColumnStyle());
            tableLayoutPanelSplitAction.ColumnStyles.Add(new ColumnStyle());
            tableLayoutPanelSplitAction.Controls.Add(textBoxDelimiter, 1, 0);
            tableLayoutPanelSplitAction.Controls.Add(label11, 0, 1);
            tableLayoutPanelSplitAction.Controls.Add(textBoxSplitOutputKey, 1, 1);
            tableLayoutPanelSplitAction.Controls.Add(label13, 0, 0);
            tableLayoutPanelSplitAction.Dock = DockStyle.Top;
            tableLayoutPanelSplitAction.Location = new Point(5, 345);
            tableLayoutPanelSplitAction.Name = "tableLayoutPanelSplitAction";
            tableLayoutPanelSplitAction.RowCount = 5;
            tableLayoutPanelSplitAction.RowStyles.Add(new RowStyle());
            tableLayoutPanelSplitAction.RowStyles.Add(new RowStyle());
            tableLayoutPanelSplitAction.RowStyles.Add(new RowStyle());
            tableLayoutPanelSplitAction.RowStyles.Add(new RowStyle());
            tableLayoutPanelSplitAction.RowStyles.Add(new RowStyle());
            tableLayoutPanelSplitAction.Size = new Size(699, 58);
            tableLayoutPanelSplitAction.TabIndex = 21;
            // 
            // textBoxDelimiter
            // 
            textBoxDelimiter.Dock = DockStyle.Fill;
            textBoxDelimiter.Location = new Point(67, 3);
            textBoxDelimiter.Name = "textBoxDelimiter";
            textBoxDelimiter.PlaceholderText = "Parameters[\"Delimiter\"] (e.g., \":\" or \"|\")";
            textBoxDelimiter.Size = new Size(649, 23);
            textBoxDelimiter.TabIndex = 17;
            // 
            // label11
            // 
            label11.AutoSize = true;
            label11.Location = new Point(3, 29);
            label11.Name = "label11";
            label11.Size = new Size(33, 15);
            label11.TabIndex = 4;
            label11.Text = "Parts";
            // 
            // textBoxSplitOutputKey
            // 
            textBoxSplitOutputKey.Dock = DockStyle.Fill;
            textBoxSplitOutputKey.Location = new Point(67, 32);
            textBoxSplitOutputKey.Name = "textBoxSplitOutputKey";
            textBoxSplitOutputKey.PlaceholderText = "default \"Parts\" (optional; if empty use textBoxOutputKey)";
            textBoxSplitOutputKey.Size = new Size(649, 23);
            textBoxSplitOutputKey.TabIndex = 0;
            // 
            // label13
            // 
            label13.AutoSize = true;
            label13.Location = new Point(3, 0);
            label13.Name = "label13";
            label13.Size = new Size(58, 15);
            label13.TabIndex = 4;
            label13.Text = "Delimiter:";
            // 
            // tableLayoutPanelCaseAction
            // 
            tableLayoutPanelCaseAction.ColumnCount = 3;
            tableLayoutPanelCaseAction.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3333321F));
            tableLayoutPanelCaseAction.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3333321F));
            tableLayoutPanelCaseAction.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3333321F));
            tableLayoutPanelCaseAction.Controls.Add(radioButtonTitle, 0, 0);
            tableLayoutPanelCaseAction.Controls.Add(radioButtonUpper, 1, 0);
            tableLayoutPanelCaseAction.Controls.Add(radioButtonLower, 2, 0);
            tableLayoutPanelCaseAction.Dock = DockStyle.Top;
            tableLayoutPanelCaseAction.Location = new Point(5, 403);
            tableLayoutPanelCaseAction.Name = "tableLayoutPanelCaseAction";
            tableLayoutPanelCaseAction.RowCount = 1;
            tableLayoutPanelCaseAction.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanelCaseAction.Size = new Size(699, 37);
            tableLayoutPanelCaseAction.TabIndex = 22;
            // 
            // radioButtonTitle
            // 
            radioButtonTitle.AutoSize = true;
            radioButtonTitle.Location = new Point(3, 3);
            radioButtonTitle.Name = "radioButtonTitle";
            radioButtonTitle.Size = new Size(48, 19);
            radioButtonTitle.TabIndex = 0;
            radioButtonTitle.TabStop = true;
            radioButtonTitle.Text = "Title";
            radioButtonTitle.UseVisualStyleBackColor = true;
            // 
            // radioButtonUpper
            // 
            radioButtonUpper.AutoSize = true;
            radioButtonUpper.Location = new Point(236, 3);
            radioButtonUpper.Name = "radioButtonUpper";
            radioButtonUpper.Size = new Size(57, 19);
            radioButtonUpper.TabIndex = 0;
            radioButtonUpper.TabStop = true;
            radioButtonUpper.Text = "Upper";
            radioButtonUpper.UseVisualStyleBackColor = true;
            // 
            // radioButtonLower
            // 
            radioButtonLower.AutoSize = true;
            radioButtonLower.Location = new Point(469, 3);
            radioButtonLower.Name = "radioButtonLower";
            radioButtonLower.Size = new Size(57, 19);
            radioButtonLower.TabIndex = 0;
            radioButtonLower.TabStop = true;
            radioButtonLower.Text = "Lower";
            radioButtonLower.UseVisualStyleBackColor = true;
            // 
            // TestPattern
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            AutoSize = true;
            ClientSize = new Size(709, 622);
            Controls.Add(tableLayoutPanel2);
            Controls.Add(tableLayoutPanelCaseAction);
            Controls.Add(tableLayoutPanelSplitAction);
            Controls.Add(tableLayoutPanelMapAction);
            Controls.Add(tableLayoutPanelFindAction);
            Controls.Add(tableLayoutPanelCommon);
            Name = "TestPattern";
            Padding = new Padding(5);
            Text = "Test Pattern / FindAction";
            tableLayoutPanelCommon.ResumeLayout(false);
            tableLayoutPanelCommon.PerformLayout();
            tableLayoutPanel2.ResumeLayout(false);
            tableLayoutPanel2.PerformLayout();
            tableLayoutPanelFindAction.ResumeLayout(false);
            tableLayoutPanelFindAction.PerformLayout();
            tableLayoutPanelMapAction.ResumeLayout(false);
            tableLayoutPanelMapAction.PerformLayout();
            tableLayoutPanelSplitAction.ResumeLayout(false);
            tableLayoutPanelSplitAction.PerformLayout();
            tableLayoutPanelCaseAction.ResumeLayout(false);
            tableLayoutPanelCaseAction.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private TextBox textBoxInputText;
        private TextBox textBoxPatterm;
        private TextBox textBoxResults;
        private Button buttonRun;
        private Label label1;
        private Label label2;
        private TextBox textBoxPatternKey;
        private Label label4;
        private TextBox textBoxSeedKey;
        private TextBox textBoxSeedValue;
        private Label label5;
        private Label label6;
        private Label label7;
        private TextBox textBoxActionJson;
        private CheckBox chkIgnoreCase;
        private CheckBox chkRemove;
        private TableLayoutPanel tableLayoutPanelCommon;
        private Label label3;
        private Label label8;
        private TextBox textBoxOutputName;
        private ComboBox comboBoxOp;
        private TableLayoutPanel tableLayoutPanel2;
        private TableLayoutPanel tableLayoutPanelFindAction;
        private TextBox textBoxInputKey;
        private Label label9;
        private TextBox textBoxCondition;
        private CheckBox checkAssign;
        private Label labelCondition;
        private RadioButton chkAll;
        private RadioButton chkFirst;
        private RadioButton chkLast;
        private TableLayoutPanel tableLayoutPanelMapAction;
        private TextBox textBoxMapTable;
        private Label label10;
        private TextBox textBoxMapInputKey;
        private Label label12;
        private TableLayoutPanel tableLayoutPanelSplitAction;
        private TextBox textBoxDelimiter;
        private Label label11;
        private TextBox textBoxSplitOutputKey;
        private Label label13;
        private TableLayoutPanel tableLayoutPanelCaseAction;
        private RadioButton radioButtonTitle;
        private RadioButton radioButtonUpper;
        private RadioButton radioButtonLower;
    }
}