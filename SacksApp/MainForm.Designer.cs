using ModernWinForms.Controls;

namespace SacksApp
{
    partial class MainForm
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
            components = new System.ComponentModel.Container();
            menuStrip = new MenuStrip();
            fileToolStripMenuItem = new ToolStripMenuItem();
            exitToolStripMenuItem = new ToolStripMenuItem();
            toolsToolStripMenuItem = new ToolStripMenuItem();
            toolStripSeparator1 = new ToolStripSeparator();
            sqlQueryToolStripMenuItem = new ToolStripMenuItem();
            offersManagerToolStripMenuItem = new ToolStripMenuItem();
            lookupEditorToolStripMenuItem = new ToolStripMenuItem();
            logViewerToolStripMenuItem = new ToolStripMenuItem();
            viewToolStripMenuItem = new ToolStripMenuItem();
            themeToolStripMenuItem = new ToolStripMenuItem();
            skinToolStripMenuItem = new ToolStripMenuItem();
            windowToolStripMenuItem = new ToolStripMenuItem();
            cascadeToolStripMenuItem = new ToolStripMenuItem();
            tileHorizontalToolStripMenuItem = new ToolStripMenuItem();
            tileVerticalToolStripMenuItem = new ToolStripMenuItem();
            arrangeIconsToolStripMenuItem = new ToolStripMenuItem();
            toolStripSeparator2 = new ToolStripSeparator();
            closeAllToolStripMenuItem = new ToolStripMenuItem();
            dashboardToolStripMenuItem = new ToolStripMenuItem();
            panelButtuns = new Panel();
            tableLayoutPanel1 = new TableLayoutPanel();
            processFilesButton = new ModernButton();
            showStatisticsButton = new ModernButton();
            sqlQueryButton = new ModernButton();
            clearDatabaseButton = new ModernButton();
            testConfigurationButton = new ModernButton();
            viewLogsButton = new ModernButton();
            buttonEditMaps = new ModernButton();
            handleOffersButton = new ModernButton();
            executeAiQueryButton = new ModernButton();
            notificationClearButton = new ModernButton();
            panelAI = new Panel();
            aiQueryGroupBox = new ModernGroupBox();
            aiQueryTableLayout = new TableLayoutPanel();
            aiQueryLabel = new Label();
            responseModeLabel = new Label();
            responseModeComboBox = new ComboBox();
            aiQueryTextBox = new ModernTextBox();
            aiMetadataLabel = new Label();
            aiMetadataTextBox = new ModernTextBox();
            aiDataLabel = new Label();
            aiDataResultsTextBox = new ModernTextBox();
            notificationPanel = new Panel();
            notificationMessageLabel = new Label();
            notificationTimeLabel = new Label();
            notificationStatusIcon = new Label();
            notificationTimer = new System.Windows.Forms.Timer(components);
            menuStrip.SuspendLayout();
            panelButtuns.SuspendLayout();
            tableLayoutPanel1.SuspendLayout();
            panelAI.SuspendLayout();
            aiQueryGroupBox.SuspendLayout();
            aiQueryTableLayout.SuspendLayout();
            notificationPanel.SuspendLayout();
            SuspendLayout();
            // 
            // menuStrip
            // 
            menuStrip.Items.AddRange(new ToolStripItem[] { fileToolStripMenuItem, toolsToolStripMenuItem, viewToolStripMenuItem, windowToolStripMenuItem });
            menuStrip.Location = new Point(0, 0);
            menuStrip.MdiWindowListItem = windowToolStripMenuItem;
            menuStrip.Name = "menuStrip";
            menuStrip.Size = new Size(1264, 24);
            menuStrip.TabIndex = 0;
            menuStrip.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            fileToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { exitToolStripMenuItem });
            fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            fileToolStripMenuItem.Size = new Size(37, 20);
            fileToolStripMenuItem.Text = "&File";
            fileToolStripMenuItem.Click += FileToolStripMenuItem_Click;
            // 
            // exitToolStripMenuItem
            // 
            exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            exitToolStripMenuItem.ShortcutKeys = Keys.Alt | Keys.F4;
            exitToolStripMenuItem.Size = new Size(134, 22);
            exitToolStripMenuItem.Text = "E&xit";
            exitToolStripMenuItem.Click += ExitToolStripMenuItem_Click;
            // 
            // toolsToolStripMenuItem
            // 
            toolsToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { toolStripSeparator1, sqlQueryToolStripMenuItem, offersManagerToolStripMenuItem, lookupEditorToolStripMenuItem, logViewerToolStripMenuItem });
            toolsToolStripMenuItem.Name = "toolsToolStripMenuItem";
            toolsToolStripMenuItem.Size = new Size(47, 20);
            toolsToolStripMenuItem.Text = "&Tools";
            // 
            // toolStripSeparator1
            // 
            toolStripSeparator1.Name = "toolStripSeparator1";
            toolStripSeparator1.Size = new Size(196, 6);
            // 
            // sqlQueryToolStripMenuItem
            // 
            sqlQueryToolStripMenuItem.Name = "sqlQueryToolStripMenuItem";
            sqlQueryToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.Q;
            sqlQueryToolStripMenuItem.Size = new Size(199, 22);
            sqlQueryToolStripMenuItem.Text = "SQL &Query";
            sqlQueryToolStripMenuItem.Click += SqlQueryToolStripMenuItem_Click;
            // 
            // offersManagerToolStripMenuItem
            // 
            offersManagerToolStripMenuItem.Name = "offersManagerToolStripMenuItem";
            offersManagerToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.O;
            offersManagerToolStripMenuItem.Size = new Size(199, 22);
            offersManagerToolStripMenuItem.Text = "&Offers Manager";
            offersManagerToolStripMenuItem.Click += OffersManagerToolStripMenuItem_Click;
            // 
            // lookupEditorToolStripMenuItem
            // 
            lookupEditorToolStripMenuItem.Name = "lookupEditorToolStripMenuItem";
            lookupEditorToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.L;
            lookupEditorToolStripMenuItem.Size = new Size(199, 22);
            lookupEditorToolStripMenuItem.Text = "&Lookup Editor";
            lookupEditorToolStripMenuItem.Click += LookupEditorToolStripMenuItem_Click;
            // 
            // logViewerToolStripMenuItem
            // 
            logViewerToolStripMenuItem.Name = "logViewerToolStripMenuItem";
            logViewerToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.G;
            logViewerToolStripMenuItem.Size = new Size(199, 22);
            logViewerToolStripMenuItem.Text = "Lo&g Viewer";
            logViewerToolStripMenuItem.Click += LogViewerToolStripMenuItem_Click;
            // 
            // viewToolStripMenuItem
            // 
            viewToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { themeToolStripMenuItem, skinToolStripMenuItem });
            viewToolStripMenuItem.Name = "viewToolStripMenuItem";
            viewToolStripMenuItem.Size = new Size(44, 20);
            viewToolStripMenuItem.Text = "&View";
            // 
            // themeToolStripMenuItem
            // 
            themeToolStripMenuItem.Name = "themeToolStripMenuItem";
            themeToolStripMenuItem.Size = new Size(111, 22);
            themeToolStripMenuItem.Text = "&Theme";
            themeToolStripMenuItem.DropDownOpening += ThemeToolStripMenuItem_DropDownOpening;
            // 
            // skinToolStripMenuItem
            // 
            skinToolStripMenuItem.Name = "skinToolStripMenuItem";
            skinToolStripMenuItem.Size = new Size(111, 22);
            skinToolStripMenuItem.Text = "&Skin";
            skinToolStripMenuItem.DropDownOpening += SkinToolStripMenuItem_DropDownOpening;
            // 
            // windowToolStripMenuItem
            // 
            windowToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { cascadeToolStripMenuItem, tileHorizontalToolStripMenuItem, tileVerticalToolStripMenuItem, arrangeIconsToolStripMenuItem, toolStripSeparator2, closeAllToolStripMenuItem });
            windowToolStripMenuItem.Name = "windowToolStripMenuItem";
            windowToolStripMenuItem.Size = new Size(63, 20);
            windowToolStripMenuItem.Text = "&Window";
            // 
            // cascadeToolStripMenuItem
            // 
            cascadeToolStripMenuItem.Name = "cascadeToolStripMenuItem";
            cascadeToolStripMenuItem.Size = new Size(151, 22);
            cascadeToolStripMenuItem.Text = "&Cascade";
            cascadeToolStripMenuItem.Click += CascadeToolStripMenuItem_Click;
            // 
            // tileHorizontalToolStripMenuItem
            // 
            tileHorizontalToolStripMenuItem.Name = "tileHorizontalToolStripMenuItem";
            tileHorizontalToolStripMenuItem.Size = new Size(151, 22);
            tileHorizontalToolStripMenuItem.Text = "Tile &Horizontal";
            tileHorizontalToolStripMenuItem.Click += TileHorizontalToolStripMenuItem_Click;
            // 
            // tileVerticalToolStripMenuItem
            // 
            tileVerticalToolStripMenuItem.Name = "tileVerticalToolStripMenuItem";
            tileVerticalToolStripMenuItem.Size = new Size(151, 22);
            tileVerticalToolStripMenuItem.Text = "Tile &Vertical";
            tileVerticalToolStripMenuItem.Click += TileVerticalToolStripMenuItem_Click;
            // 
            // arrangeIconsToolStripMenuItem
            // 
            arrangeIconsToolStripMenuItem.Name = "arrangeIconsToolStripMenuItem";
            arrangeIconsToolStripMenuItem.Size = new Size(151, 22);
            arrangeIconsToolStripMenuItem.Text = "&Arrange Icons";
            arrangeIconsToolStripMenuItem.Click += ArrangeIconsToolStripMenuItem_Click;
            // 
            // toolStripSeparator2
            // 
            toolStripSeparator2.Name = "toolStripSeparator2";
            toolStripSeparator2.Size = new Size(148, 6);
            // 
            // closeAllToolStripMenuItem
            // 
            closeAllToolStripMenuItem.Name = "closeAllToolStripMenuItem";
            closeAllToolStripMenuItem.Size = new Size(151, 22);
            closeAllToolStripMenuItem.Text = "Close &All";
            closeAllToolStripMenuItem.Click += CloseAllToolStripMenuItem_Click;
            // 
            // dashboardToolStripMenuItem
            // 
            dashboardToolStripMenuItem.Name = "dashboardToolStripMenuItem";
            dashboardToolStripMenuItem.Size = new Size(32, 19);
            // 
            // panelButtuns
            // 
            panelButtuns.Controls.Add(tableLayoutPanel1);
            panelButtuns.Dock = DockStyle.Left;
            panelButtuns.Location = new Point(0, 24);
            panelButtuns.Name = "panelButtuns";
            panelButtuns.Padding = new Padding(10);
            panelButtuns.Size = new Size(285, 766);
            panelButtuns.TabIndex = 2;
            // 
            // tableLayoutPanel1
            // 
            tableLayoutPanel1.AutoSize = true;
            tableLayoutPanel1.BackColor = Color.Transparent;
            tableLayoutPanel1.ColumnCount = 1;
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanel1.Controls.Add(processFilesButton, 0, 0);
            tableLayoutPanel1.Controls.Add(showStatisticsButton, 0, 1);
            tableLayoutPanel1.Controls.Add(sqlQueryButton, 0, 2);
            tableLayoutPanel1.Controls.Add(clearDatabaseButton, 0, 3);
            tableLayoutPanel1.Controls.Add(testConfigurationButton, 0, 4);
            tableLayoutPanel1.Controls.Add(viewLogsButton, 0, 5);
            tableLayoutPanel1.Controls.Add(buttonEditMaps, 0, 6);
            tableLayoutPanel1.Controls.Add(handleOffersButton, 0, 7);
            tableLayoutPanel1.Dock = DockStyle.Top;
            tableLayoutPanel1.Location = new Point(10, 10);
            tableLayoutPanel1.Name = "tableLayoutPanel1";
            tableLayoutPanel1.Padding = new Padding(5);
            tableLayoutPanel1.RowCount = 8;
            tableLayoutPanel1.RowStyles.Add(new RowStyle());
            tableLayoutPanel1.RowStyles.Add(new RowStyle());
            tableLayoutPanel1.RowStyles.Add(new RowStyle());
            tableLayoutPanel1.RowStyles.Add(new RowStyle());
            tableLayoutPanel1.RowStyles.Add(new RowStyle());
            tableLayoutPanel1.RowStyles.Add(new RowStyle());
            tableLayoutPanel1.RowStyles.Add(new RowStyle());
            tableLayoutPanel1.RowStyles.Add(new RowStyle());
            tableLayoutPanel1.Size = new Size(265, 378);
            tableLayoutPanel1.TabIndex = 15;
            // 
            // processFilesButton
            // 
            processFilesButton.BackColor = Color.Transparent;
            processFilesButton.Dock = DockStyle.Fill;
            processFilesButton.FlatStyle = FlatStyle.Flat;
            processFilesButton.Location = new Point(8, 8);
            processFilesButton.Name = "processFilesButton";
            processFilesButton.Size = new Size(249, 40);
            processFilesButton.TabIndex = 0;
            processFilesButton.Text = "📁 Process Files";
            processFilesButton.UseVisualStyleBackColor = false;
            processFilesButton.Click += ProcessFilesButton_Click;
            // 
            // showStatisticsButton
            // 
            showStatisticsButton.BackColor = Color.Transparent;
            showStatisticsButton.Dock = DockStyle.Fill;
            showStatisticsButton.FlatStyle = FlatStyle.Flat;
            showStatisticsButton.Location = new Point(8, 54);
            showStatisticsButton.Name = "showStatisticsButton";
            showStatisticsButton.Size = new Size(249, 40);
            showStatisticsButton.TabIndex = 1;
            showStatisticsButton.Text = "📊 Show Statistics";
            showStatisticsButton.UseVisualStyleBackColor = false;
            showStatisticsButton.Click += ShowStatisticsButton_Click;
            // 
            // sqlQueryButton
            // 
            sqlQueryButton.BackColor = Color.Transparent;
            sqlQueryButton.Dock = DockStyle.Fill;
            sqlQueryButton.FlatStyle = FlatStyle.Flat;
            sqlQueryButton.Location = new Point(8, 100);
            sqlQueryButton.Name = "sqlQueryButton";
            sqlQueryButton.Size = new Size(249, 40);
            sqlQueryButton.TabIndex = 2;
            sqlQueryButton.Text = "🔍 SQL Query";
            sqlQueryButton.UseVisualStyleBackColor = false;
            sqlQueryButton.Click += SqlQueryButton_Click;
            // 
            // clearDatabaseButton
            // 
            clearDatabaseButton.BackColor = Color.Transparent;
            clearDatabaseButton.Dock = DockStyle.Fill;
            clearDatabaseButton.FlatStyle = FlatStyle.Flat;
            clearDatabaseButton.Location = new Point(8, 146);
            clearDatabaseButton.Name = "clearDatabaseButton";
            clearDatabaseButton.Size = new Size(249, 40);
            clearDatabaseButton.TabIndex = 3;
            clearDatabaseButton.Text = "🗑️ Clear Database";
            clearDatabaseButton.UseVisualStyleBackColor = false;
            clearDatabaseButton.Click += ClearDatabaseButton_Click;
            // 
            // testConfigurationButton
            // 
            testConfigurationButton.BackColor = Color.Transparent;
            testConfigurationButton.Dock = DockStyle.Fill;
            testConfigurationButton.FlatStyle = FlatStyle.Flat;
            testConfigurationButton.Location = new Point(8, 192);
            testConfigurationButton.Name = "testConfigurationButton";
            testConfigurationButton.Size = new Size(249, 40);
            testConfigurationButton.TabIndex = 4;
            testConfigurationButton.Text = "⚙️ Test Configuration";
            testConfigurationButton.UseVisualStyleBackColor = false;
            testConfigurationButton.Click += TestConfigurationButton_Click;
            // 
            // viewLogsButton
            // 
            viewLogsButton.BackColor = Color.Transparent;
            viewLogsButton.Dock = DockStyle.Fill;
            viewLogsButton.FlatStyle = FlatStyle.Flat;
            viewLogsButton.Location = new Point(8, 238);
            viewLogsButton.Name = "viewLogsButton";
            viewLogsButton.Size = new Size(249, 40);
            viewLogsButton.TabIndex = 5;
            viewLogsButton.Text = "📜 View Logs";
            viewLogsButton.UseVisualStyleBackColor = false;
            viewLogsButton.Click += ViewLogsButton_Click;
            // 
            // buttonEditMaps
            // 
            buttonEditMaps.BackColor = Color.Transparent;
            buttonEditMaps.Dock = DockStyle.Fill;
            buttonEditMaps.FlatStyle = FlatStyle.Flat;
            buttonEditMaps.Location = new Point(8, 284);
            buttonEditMaps.Name = "buttonEditMaps";
            buttonEditMaps.Size = new Size(249, 40);
            buttonEditMaps.TabIndex = 6;
            buttonEditMaps.Text = "📝 Edit Lookups";
            buttonEditMaps.UseVisualStyleBackColor = false;
            buttonEditMaps.Click += ButtonEditMaps_Click;
            // 
            // handleOffersButton
            // 
            handleOffersButton.BackColor = Color.Transparent;
            handleOffersButton.Dock = DockStyle.Fill;
            handleOffersButton.FlatStyle = FlatStyle.Flat;
            handleOffersButton.Location = new Point(8, 330);
            handleOffersButton.Name = "handleOffersButton";
            handleOffersButton.Size = new Size(249, 40);
            handleOffersButton.TabIndex = 7;
            handleOffersButton.Text = "💼 Handle Offers";
            handleOffersButton.UseVisualStyleBackColor = false;
            handleOffersButton.Click += HandleOffersButton_Click;
            // 
            // executeAiQueryButton
            // 
            executeAiQueryButton.BackColor = Color.Transparent;
            aiQueryTableLayout.SetColumnSpan(executeAiQueryButton, 2);
            executeAiQueryButton.Dock = DockStyle.Fill;
            executeAiQueryButton.FlatStyle = FlatStyle.Flat;
            executeAiQueryButton.Location = new Point(13, 163);
            executeAiQueryButton.Name = "executeAiQueryButton";
            executeAiQueryButton.Size = new Size(342, 40);
            executeAiQueryButton.TabIndex = 4;
            executeAiQueryButton.Text = "🚀 Execute Query";
            executeAiQueryButton.UseVisualStyleBackColor = false;
            executeAiQueryButton.Click += ExecuteAiQueryButton_Click;
            // 
            // notificationClearButton
            // 
            notificationClearButton.BackColor = Color.Transparent;
            notificationClearButton.FlatStyle = FlatStyle.Flat;
            notificationClearButton.Location = new Point(0, 0);
            notificationClearButton.Name = "notificationClearButton";
            notificationClearButton.Size = new Size(75, 23);
            notificationClearButton.TabIndex = 0;
            notificationClearButton.UseVisualStyleBackColor = false;
            // 
            // panelAI
            // 
            panelAI.Controls.Add(aiQueryGroupBox);
            panelAI.Dock = DockStyle.Right;
            panelAI.Location = new Point(872, 24);
            panelAI.Name = "panelAI";
            panelAI.Padding = new Padding(10);
            panelAI.Size = new Size(392, 766);
            panelAI.TabIndex = 4;
            // 
            // aiQueryGroupBox
            // 
            aiQueryGroupBox.BackColor = Color.Transparent;
            aiQueryGroupBox.Controls.Add(aiQueryTableLayout);
            aiQueryGroupBox.Dock = DockStyle.Fill;
            aiQueryGroupBox.ForeColor = Color.FromArgb(50, 49, 48);
            aiQueryGroupBox.Location = new Point(10, 10);
            aiQueryGroupBox.Name = "aiQueryGroupBox";
            aiQueryGroupBox.Padding = new Padding(2, 20, 2, 2);
            aiQueryGroupBox.Size = new Size(372, 746);
            aiQueryGroupBox.TabIndex = 0;
            aiQueryGroupBox.TabStop = false;
            aiQueryGroupBox.Text = "AI Query Panel";
            // 
            // aiQueryTableLayout
            // 
            aiQueryTableLayout.ColumnCount = 2;
            aiQueryTableLayout.ColumnStyles.Add(new ColumnStyle());
            aiQueryTableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            aiQueryTableLayout.Controls.Add(aiQueryLabel, 0, 0);
            aiQueryTableLayout.Controls.Add(responseModeLabel, 0, 1);
            aiQueryTableLayout.Controls.Add(responseModeComboBox, 1, 1);
            aiQueryTableLayout.Controls.Add(aiQueryTextBox, 0, 2);
            aiQueryTableLayout.Controls.Add(executeAiQueryButton, 0, 3);
            aiQueryTableLayout.Controls.Add(aiMetadataLabel, 0, 4);
            aiQueryTableLayout.Controls.Add(aiMetadataTextBox, 0, 5);
            aiQueryTableLayout.Controls.Add(aiDataLabel, 0, 6);
            aiQueryTableLayout.Controls.Add(aiDataResultsTextBox, 0, 7);
            aiQueryTableLayout.Dock = DockStyle.Fill;
            aiQueryTableLayout.Location = new Point(2, 36);
            aiQueryTableLayout.Margin = new Padding(10);
            aiQueryTableLayout.Name = "aiQueryTableLayout";
            aiQueryTableLayout.Padding = new Padding(10);
            aiQueryTableLayout.RowCount = 8;
            aiQueryTableLayout.RowStyles.Add(new RowStyle());
            aiQueryTableLayout.RowStyles.Add(new RowStyle());
            aiQueryTableLayout.RowStyles.Add(new RowStyle());
            aiQueryTableLayout.RowStyles.Add(new RowStyle());
            aiQueryTableLayout.RowStyles.Add(new RowStyle());
            aiQueryTableLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            aiQueryTableLayout.RowStyles.Add(new RowStyle());
            aiQueryTableLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            aiQueryTableLayout.Size = new Size(368, 708);
            aiQueryTableLayout.TabIndex = 0;
            // 
            // aiQueryLabel
            // 
            aiQueryLabel.Anchor = AnchorStyles.Left;
            aiQueryLabel.AutoSize = true;
            aiQueryTableLayout.SetColumnSpan(aiQueryLabel, 2);
            aiQueryLabel.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            aiQueryLabel.Location = new Point(13, 10);
            aiQueryLabel.Name = "aiQueryLabel";
            aiQueryLabel.Size = new Size(111, 15);
            aiQueryLabel.TabIndex = 0;
            aiQueryLabel.Text = "AI-Powered Query";
            // 
            // responseModeLabel
            // 
            responseModeLabel.Anchor = AnchorStyles.Left;
            responseModeLabel.AutoSize = true;
            responseModeLabel.Font = new Font("Segoe UI", 9F);
            responseModeLabel.Location = new Point(13, 32);
            responseModeLabel.Name = "responseModeLabel";
            responseModeLabel.Size = new Size(41, 15);
            responseModeLabel.TabIndex = 1;
            responseModeLabel.Text = "Mode:";
            // 
            // responseModeComboBox
            // 
            responseModeComboBox.Dock = DockStyle.Fill;
            responseModeComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            responseModeComboBox.Font = new Font("Segoe UI", 9F);
            responseModeComboBox.Items.AddRange(new object[] { "ToolOnly - Must use tools", "Conversational - Free responses" });
            responseModeComboBox.Location = new Point(60, 28);
            responseModeComboBox.Name = "responseModeComboBox";
            responseModeComboBox.Size = new Size(295, 23);
            responseModeComboBox.TabIndex = 2;
            responseModeComboBox.SelectedIndexChanged += ResponseModeComboBox_SelectedIndexChanged;
            // 
            // aiQueryTextBox
            // 
            aiQueryTextBox.BackColor = Color.FromArgb(255, 255, 255);
            aiQueryTableLayout.SetColumnSpan(aiQueryTextBox, 2);
            aiQueryTextBox.Dock = DockStyle.Fill;
            aiQueryTextBox.Location = new Point(13, 57);
            aiQueryTextBox.Multiline = true;
            aiQueryTextBox.Name = "aiQueryTextBox";
            aiQueryTextBox.PlaceholderText = "Enter your natural language query...";
            aiQueryTextBox.ScrollBars = ScrollBars.Vertical;
            aiQueryTextBox.Size = new Size(342, 100);
            aiQueryTextBox.TabIndex = 3;
            aiQueryTextBox.WordWrap = true;
            aiQueryTextBox.KeyDown += AiQueryTextBox_KeyDown;
            // 
            // aiMetadataLabel
            // 
            aiMetadataLabel.Anchor = AnchorStyles.Left;
            aiMetadataLabel.AutoSize = true;
            aiQueryTableLayout.SetColumnSpan(aiMetadataLabel, 2);
            aiMetadataLabel.Font = new Font("Segoe UI", 12F);
            aiMetadataLabel.Location = new Point(13, 206);
            aiMetadataLabel.Name = "aiMetadataLabel";
            aiMetadataLabel.Size = new Size(113, 21);
            aiMetadataLabel.TabIndex = 5;
            aiMetadataLabel.Text = "📋 Query Info:";
            // 
            // aiMetadataTextBox
            // 
            aiMetadataTextBox.BackColor = Color.FromArgb(255, 255, 255);
            aiQueryTableLayout.SetColumnSpan(aiMetadataTextBox, 2);
            aiMetadataTextBox.Dock = DockStyle.Fill;
            aiMetadataTextBox.Location = new Point(13, 230);
            aiMetadataTextBox.Multiline = true;
            aiMetadataTextBox.Name = "aiMetadataTextBox";
            aiMetadataTextBox.ReadOnly = true;
            aiMetadataTextBox.ScrollBars = ScrollBars.Vertical;
            aiMetadataTextBox.Size = new Size(342, 219);
            aiMetadataTextBox.TabIndex = 6;
            aiMetadataTextBox.WordWrap = true;
            // 
            // aiDataLabel
            // 
            aiDataLabel.Anchor = AnchorStyles.Left;
            aiDataLabel.AutoSize = true;
            aiQueryTableLayout.SetColumnSpan(aiDataLabel, 2);
            aiDataLabel.Font = new Font("Segoe UI", 12F);
            aiDataLabel.Location = new Point(13, 452);
            aiDataLabel.Name = "aiDataLabel";
            aiDataLabel.Size = new Size(89, 21);
            aiDataLabel.TabIndex = 7;
            aiDataLabel.Text = "📊 Results:";
            // 
            // aiDataResultsTextBox
            // 
            aiDataResultsTextBox.BackColor = Color.FromArgb(255, 255, 255);
            aiQueryTableLayout.SetColumnSpan(aiDataResultsTextBox, 2);
            aiDataResultsTextBox.Dock = DockStyle.Fill;
            aiDataResultsTextBox.Location = new Point(13, 476);
            aiDataResultsTextBox.Multiline = true;
            aiDataResultsTextBox.Name = "aiDataResultsTextBox";
            aiDataResultsTextBox.ReadOnly = true;
            aiDataResultsTextBox.ScrollBars = ScrollBars.Vertical;
            aiDataResultsTextBox.Size = new Size(342, 219);
            aiDataResultsTextBox.TabIndex = 8;
            aiDataResultsTextBox.WordWrap = true;
            // 
            // notificationPanel
            // 
            notificationPanel.BackColor = Color.FromArgb(240, 249, 235);
            notificationPanel.BorderStyle = BorderStyle.FixedSingle;
            notificationPanel.Controls.Add(notificationMessageLabel);
            notificationPanel.Controls.Add(notificationTimeLabel);
            notificationPanel.Controls.Add(notificationStatusIcon);
            notificationPanel.Dock = DockStyle.Bottom;
            notificationPanel.Location = new Point(0, 790);
            notificationPanel.Name = "notificationPanel";
            notificationPanel.Padding = new Padding(12, 8, 12, 8);
            notificationPanel.Size = new Size(1264, 41);
            notificationPanel.TabIndex = 3;
            // 
            // notificationMessageLabel
            // 
            notificationMessageLabel.Dock = DockStyle.Fill;
            notificationMessageLabel.Font = new Font("Segoe UI", 10F);
            notificationMessageLabel.ForeColor = Color.FromArgb(13, 17, 23);
            notificationMessageLabel.Location = new Point(44, 8);
            notificationMessageLabel.Name = "notificationMessageLabel";
            notificationMessageLabel.Size = new Size(1206, 23);
            notificationMessageLabel.TabIndex = 1;
            notificationMessageLabel.Text = "Status message";
            notificationMessageLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // notificationTimeLabel
            // 
            notificationTimeLabel.AutoSize = true;
            notificationTimeLabel.Font = new Font("Segoe UI", 8F);
            notificationTimeLabel.ForeColor = Color.FromArgb(107, 114, 129);
            notificationTimeLabel.Location = new Point(1050, 8);
            notificationTimeLabel.Name = "notificationTimeLabel";
            notificationTimeLabel.Size = new Size(49, 13);
            notificationTimeLabel.TabIndex = 2;
            notificationTimeLabel.Text = "12:34:56";
            // 
            // notificationStatusIcon
            // 
            notificationStatusIcon.AutoSize = true;
            notificationStatusIcon.Dock = DockStyle.Left;
            notificationStatusIcon.Font = new Font("Segoe UI", 12F);
            notificationStatusIcon.Location = new Point(12, 8);
            notificationStatusIcon.Margin = new Padding(0);
            notificationStatusIcon.Name = "notificationStatusIcon";
            notificationStatusIcon.Size = new Size(32, 21);
            notificationStatusIcon.TabIndex = 0;
            notificationStatusIcon.Text = "ℹ️";
            // 
            // notificationTimer
            // 
            notificationTimer.Interval = 5000;
            notificationTimer.Tick += NotificationTimer_Tick;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1264, 831);
            Controls.Add(panelAI);
            Controls.Add(panelButtuns);
            Controls.Add(menuStrip);
            Controls.Add(notificationPanel);
            IsMdiContainer = true;
            MainMenuStrip = menuStrip;
            Name = "MainForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Sacks Product Management System";
            WindowState = FormWindowState.Maximized;
            menuStrip.ResumeLayout(false);
            menuStrip.PerformLayout();
            panelButtuns.ResumeLayout(false);
            panelButtuns.PerformLayout();
            tableLayoutPanel1.ResumeLayout(false);
            panelAI.ResumeLayout(false);
            aiQueryGroupBox.ResumeLayout(false);
            aiQueryTableLayout.ResumeLayout(false);
            aiQueryTableLayout.PerformLayout();
            notificationPanel.ResumeLayout(false);
            notificationPanel.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private MenuStrip menuStrip;
        private ToolStripMenuItem fileToolStripMenuItem;
        private ToolStripMenuItem exitToolStripMenuItem;
        private ToolStripMenuItem toolsToolStripMenuItem;
        private ToolStripMenuItem dashboardToolStripMenuItem;
        private ToolStripMenuItem sqlQueryToolStripMenuItem;
        private ToolStripMenuItem offersManagerToolStripMenuItem;
        private ToolStripMenuItem lookupEditorToolStripMenuItem;
        private ToolStripMenuItem logViewerToolStripMenuItem;
        private ToolStripMenuItem viewToolStripMenuItem;
        private ToolStripMenuItem themeToolStripMenuItem;
        private ToolStripMenuItem skinToolStripMenuItem;
        private ToolStripMenuItem windowToolStripMenuItem;
        private ToolStripMenuItem cascadeToolStripMenuItem;
        private ToolStripMenuItem tileHorizontalToolStripMenuItem;
        private ToolStripMenuItem tileVerticalToolStripMenuItem;
        private ToolStripMenuItem arrangeIconsToolStripMenuItem;
        private ToolStripMenuItem closeAllToolStripMenuItem;
        private ToolStripSeparator toolStripSeparator1;
        private ToolStripSeparator toolStripSeparator2;
        private Panel panelButtuns;
        private TableLayoutPanel tableLayoutPanel1;
        private ModernButton buttonEditMaps;
        private ModernButton processFilesButton;
        private ModernButton showStatisticsButton;
        private ModernButton sqlQueryButton;
        private ModernButton clearDatabaseButton;
        private ModernButton testConfigurationButton;
        private ModernButton viewLogsButton;
        private ModernButton handleOffersButton;
        private ModernGroupBox aiQueryGroupBox;
        private TableLayoutPanel aiQueryTableLayout;
        private Label aiQueryLabel;
        private Label responseModeLabel;
        private ComboBox responseModeComboBox;
        private ModernTextBox aiQueryTextBox;
        private Label aiMetadataLabel;
        private ModernTextBox aiMetadataTextBox;
        private Label aiDataLabel;
        private ModernTextBox aiDataResultsTextBox;
        private Panel notificationPanel;
        private Label notificationStatusIcon;
        private Label notificationMessageLabel;
        private Label notificationTimeLabel;
        private ModernButton notificationClearButton;
        private System.Windows.Forms.Timer notificationTimer;
        private Panel panelAI;
        private ModernButton executeAiQueryButton;
    }
}

































































































































































































































































































































































































