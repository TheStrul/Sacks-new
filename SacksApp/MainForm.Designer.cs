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
            menuStrip = new MenuStrip();
            fileToolStripMenuItem = new ToolStripMenuItem();
            exitToolStripMenuItem = new ToolStripMenuItem();
            toolsToolStripMenuItem = new ToolStripMenuItem();
            dashboardToolStripMenuItem = new ToolStripMenuItem();
            toolStripSeparator1 = new ToolStripSeparator();
            sqlQueryToolStripMenuItem = new ToolStripMenuItem();
            offersManagerToolStripMenuItem = new ToolStripMenuItem();
            lookupEditorToolStripMenuItem = new ToolStripMenuItem();
            logViewerToolStripMenuItem = new ToolStripMenuItem();
            windowToolStripMenuItem = new ToolStripMenuItem();
            cascadeToolStripMenuItem = new ToolStripMenuItem();
            tileHorizontalToolStripMenuItem = new ToolStripMenuItem();
            tileVerticalToolStripMenuItem = new ToolStripMenuItem();
            arrangeIconsToolStripMenuItem = new ToolStripMenuItem();
            toolStripSeparator2 = new ToolStripSeparator();
            closeAllToolStripMenuItem = new ToolStripMenuItem();
            panel1 = new Panel();
            tableLayoutPanel1 = new TableLayoutPanel();
            buttonEditMaps = new ModernButton();
            processFilesButton = new ModernButton();
            showStatisticsButton = new ModernButton();
            sqlQueryButton = new ModernButton();
            clearDatabaseButton = new ModernButton();
            testConfigurationButton = new ModernButton();
            viewLogsButton = new ModernButton();
            handleOffersButton = new ModernButton();
            notificationPanel = new Panel();
            notificationStatusIcon = new Label();
            notificationMessageLabel = new Label();
            notificationTimeLabel = new Label();
            notificationClearButton = new ModernButton();
            notificationTimer = new System.Windows.Forms.Timer();
            menuStrip.SuspendLayout();
            panel1.SuspendLayout();
            tableLayoutPanel1.SuspendLayout();
            notificationPanel.SuspendLayout();
            aiQueryGroupBox = new GroupBox();
            aiQueryTableLayout = new TableLayoutPanel();
            aiQueryLabel = new Label();
            responseModeLabel = new Label();
            responseModeComboBox = new ComboBox();
            aiQueryTextBox = new TextBox();
            executeAiQueryButton = new ModernButton();
            aiMetadataLabel = new Label();
            aiMetadataTextBox = new RichTextBox();
            aiDataLabel = new Label();
            aiDataResultsTextBox = new RichTextBox();
            aiQueryGroupBox.SuspendLayout();
            aiQueryTableLayout.SuspendLayout();
            SuspendLayout();
            // 
            // menuStrip
            // 
            menuStrip.Items.AddRange(new ToolStripItem[] { fileToolStripMenuItem, toolsToolStripMenuItem, windowToolStripMenuItem });
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
            // panel1
            // 
            panel1.Controls.Add(aiQueryGroupBox);
            panel1.Controls.Add(tableLayoutPanel1);
            panel1.Controls.Add(notificationPanel);
            panel1.Dock = DockStyle.Left;
            panel1.Location = new Point(0, 24);
            panel1.Name = "panel1";
            panel1.Padding = new Padding(10);
            panel1.Size = new Size(534, 807);
            panel1.TabIndex = 2;
            // 
            // tableLayoutPanel1
            // 
            tableLayoutPanel1.AutoSize = true;
            tableLayoutPanel1.BackColor = Color.Transparent;
            tableLayoutPanel1.ColumnCount = 1;
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanel1.Controls.Add(buttonEditMaps, 0, 3);
            tableLayoutPanel1.Controls.Add(processFilesButton, 0, 0);
            tableLayoutPanel1.Controls.Add(showStatisticsButton, 0, 1);
            tableLayoutPanel1.Controls.Add(sqlQueryButton, 0, 2);
            tableLayoutPanel1.Controls.Add(clearDatabaseButton, 0, 4);
            tableLayoutPanel1.Controls.Add(testConfigurationButton, 0, 5);
            tableLayoutPanel1.Controls.Add(viewLogsButton, 0, 6);
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
            tableLayoutPanel1.Size = new Size(514, 704);
            tableLayoutPanel1.TabIndex = 15;
            // 
            // aiQueryGroupBox
            // 
            aiQueryGroupBox.Controls.Add(aiQueryTableLayout);
            aiQueryGroupBox.Dock = DockStyle.Bottom;
            aiQueryGroupBox.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            aiQueryGroupBox.ForeColor = Color.FromArgb(30, 30, 30);
            aiQueryGroupBox.Location = new Point(10, 501);
            aiQueryGroupBox.Name = "aiQueryGroupBox";
            aiQueryGroupBox.Padding = new Padding(12);
            aiQueryGroupBox.Size = new Size(514, 296);
            aiQueryGroupBox.TabIndex = 16;
            aiQueryGroupBox.TabStop = false;
            aiQueryGroupBox.Text = "🤖 AI Query";
            // 
            // aiQueryTableLayout
            // 
            aiQueryTableLayout.ColumnCount = 2;
            aiQueryTableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));
            aiQueryTableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            aiQueryTableLayout.Controls.Add(aiQueryLabel, 0, 0);
            aiQueryTableLayout.Controls.Add(responseModeLabel, 0, 1);
            aiQueryTableLayout.Controls.Add(responseModeComboBox, 1, 1);
            aiQueryTableLayout.Controls.Add(aiQueryTextBox, 1, 2);
            aiQueryTableLayout.Controls.Add(executeAiQueryButton, 1, 3);
            aiQueryTableLayout.Controls.Add(aiMetadataLabel, 0, 4);
            aiQueryTableLayout.Controls.Add(aiMetadataTextBox, 0, 5);
            aiQueryTableLayout.Controls.Add(aiDataLabel, 0, 6);
            aiQueryTableLayout.Controls.Add(aiDataResultsTextBox, 0, 7);
            aiQueryTableLayout.Dock = DockStyle.Fill;
            aiQueryTableLayout.Location = new Point(12, 30);
            aiQueryTableLayout.Name = "aiQueryTableLayout";
            aiQueryTableLayout.RowCount = 8;
            aiQueryTableLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            aiQueryTableLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
            aiQueryTableLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
            aiQueryTableLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
            aiQueryTableLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
            aiQueryTableLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 30F));
            aiQueryTableLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
            aiQueryTableLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 70F));
            aiQueryTableLayout.Size = new Size(490, 254);
            aiQueryTableLayout.TabIndex = 0;
            // 
            // aiQueryLabel
            // 
            aiQueryLabel.Anchor = AnchorStyles.Left;
            aiQueryLabel.AutoSize = true;
            aiQueryTableLayout.SetColumnSpan(aiQueryLabel, 2);
            aiQueryLabel.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            aiQueryLabel.Location = new Point(3, 7);
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
            responseModeLabel.Location = new Point(3, 40);
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
            responseModeComboBox.Location = new Point(123, 33);
            responseModeComboBox.Name = "responseModeComboBox";
            responseModeComboBox.Size = new Size(364, 23);
            responseModeComboBox.TabIndex = 2;
            responseModeComboBox.SelectedIndexChanged += ResponseModeComboBox_SelectedIndexChanged;
            // 
            // aiQueryTextBox
            // 
            aiQueryTextBox.Dock = DockStyle.Fill;
            aiQueryTextBox.Font = new Font("Segoe UI", 10F);
            aiQueryTextBox.Location = new Point(123, 69);
            aiQueryTextBox.Name = "aiQueryTextBox";
            aiQueryTextBox.PlaceholderText = "Ask anything... (Press Enter to send)";
            aiQueryTextBox.Size = new Size(364, 25);
            aiQueryTextBox.TabIndex = 3;
            aiQueryTextBox.KeyDown += AiQueryTextBox_KeyDown;
            // 
            // executeAiQueryButton
            // 
            executeAiQueryButton.AutoSize = true;
            executeAiQueryButton.Dock = DockStyle.Right;
            executeAiQueryButton.FlatStyle = FlatStyle.Flat;
            executeAiQueryButton.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            executeAiQueryButton.ForeColor = Color.FromArgb(30, 30, 30);
            executeAiQueryButton.Location = new Point(341, 105);
            executeAiQueryButton.Name = "executeAiQueryButton";
            executeAiQueryButton.Padding = new Padding(62, 12, 12, 12);
            executeAiQueryButton.Size = new Size(146, 34);
            executeAiQueryButton.TabIndex = 4;
            executeAiQueryButton.Text = "Send";
            executeAiQueryButton.UseVisualStyleBackColor = false;
            executeAiQueryButton.Click += ExecuteAiQueryButton_Click;
            // 
            // aiMetadataLabel
            // 
            aiMetadataLabel.Anchor = AnchorStyles.Left;
            aiMetadataLabel.AutoSize = true;
            aiQueryTableLayout.SetColumnSpan(aiMetadataLabel, 2);
            aiMetadataLabel.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            aiMetadataLabel.Location = new Point(3, 146);
            aiMetadataLabel.Name = "aiMetadataLabel";
            aiMetadataLabel.Size = new Size(86, 15);
            aiMetadataLabel.TabIndex = 6;
            aiMetadataLabel.Text = "📋 Query Info:";
            // 
            // aiMetadataTextBox
            // 
            aiQueryTableLayout.SetColumnSpan(aiMetadataTextBox, 2);
            aiMetadataTextBox.Dock = DockStyle.Fill;
            aiMetadataTextBox.Font = new Font("Segoe UI", 9F);
            aiMetadataTextBox.Location = new Point(3, 169);
            aiMetadataTextBox.Name = "aiMetadataTextBox";
            aiMetadataTextBox.ReadOnly = true;
            aiMetadataTextBox.Size = new Size(484, 13);
            aiMetadataTextBox.TabIndex = 7;
            aiMetadataTextBox.Text = "";
            // 
            // aiDataLabel
            // 
            aiDataLabel.Anchor = AnchorStyles.Left;
            aiDataLabel.AutoSize = true;
            aiQueryTableLayout.SetColumnSpan(aiDataLabel, 2);
            aiDataLabel.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            aiDataLabel.Location = new Point(3, 189);
            aiDataLabel.Name = "aiDataLabel";
            aiDataLabel.Size = new Size(66, 15);
            aiDataLabel.TabIndex = 8;
            aiDataLabel.Text = "📊 Results:";
            // 
            // aiDataResultsTextBox
            // 
            aiQueryTableLayout.SetColumnSpan(aiDataResultsTextBox, 2);
            aiDataResultsTextBox.Dock = DockStyle.Fill;
            aiDataResultsTextBox.Font = new Font("Consolas", 9F);
            aiDataResultsTextBox.Location = new Point(3, 212);
            aiDataResultsTextBox.Name = "aiDataResultsTextBox";
            aiDataResultsTextBox.ReadOnly = true;
            aiDataResultsTextBox.Size = new Size(484, 39);
            aiDataResultsTextBox.TabIndex = 9;
            aiDataResultsTextBox.Text = "";
            aiDataResultsTextBox.WordWrap = false;
            // 
            // buttonEditMaps
            // 
            buttonEditMaps.AutoSize = true;
            buttonEditMaps.BackColor = Color.White;
            buttonEditMaps.Dock = DockStyle.Top;
            buttonEditMaps.FlatStyle = FlatStyle.Flat;
            buttonEditMaps.Font = new Font("Segoe UI", 12F);
            buttonEditMaps.ForeColor = Color.FromArgb(30, 30, 30);
            buttonEditMaps.Location = new Point(10, 268);
            buttonEditMaps.Margin = new Padding(5);
            buttonEditMaps.Name = "buttonEditMaps";
            buttonEditMaps.Padding = new Padding(77, 12, 12, 12);
            buttonEditMaps.Size = new Size(494, 79);
            buttonEditMaps.TabIndex = 13;
            buttonEditMaps.Text = "Handle Fix Names";
            buttonEditMaps.UseVisualStyleBackColor = false;
            buttonEditMaps.Click += ButtonEditMaps_Click;
            // 
            // processFilesButton
            // 
            processFilesButton.AutoSize = true;
            processFilesButton.BackColor = Color.White;
            processFilesButton.Dock = DockStyle.Top;
            processFilesButton.FlatStyle = FlatStyle.Flat;
            processFilesButton.Font = new Font("Segoe UI", 12F);
            processFilesButton.ForeColor = Color.FromArgb(30, 30, 30);
            processFilesButton.Location = new Point(10, 10);
            processFilesButton.Margin = new Padding(5);
            processFilesButton.Name = "processFilesButton";
            processFilesButton.Padding = new Padding(76, 12, 12, 12);
            processFilesButton.Size = new Size(494, 76);
            processFilesButton.TabIndex = 7;
            processFilesButton.Text = "Process Excel Files";
            processFilesButton.UseVisualStyleBackColor = false;
            processFilesButton.Click += ProcessFilesButton_Click;
            // 
            // showStatisticsButton
            // 
            showStatisticsButton.AutoSize = true;
            showStatisticsButton.BackColor = Color.White;
            showStatisticsButton.Dock = DockStyle.Top;
            showStatisticsButton.FlatStyle = FlatStyle.Flat;
            showStatisticsButton.Font = new Font("Segoe UI", 12F);
            showStatisticsButton.ForeColor = Color.FromArgb(30, 30, 30);
            showStatisticsButton.Location = new Point(10, 96);
            showStatisticsButton.Margin = new Padding(5);
            showStatisticsButton.Name = "showStatisticsButton";
            showStatisticsButton.Padding = new Padding(76, 12, 12, 12);
            showStatisticsButton.Size = new Size(494, 76);
            showStatisticsButton.TabIndex = 9;
            showStatisticsButton.Text = "Show Statistics";
            showStatisticsButton.UseVisualStyleBackColor = false;
            showStatisticsButton.Click += ShowStatisticsButton_Click;
            // 
            // sqlQueryButton
            // 
            sqlQueryButton.AutoSize = true;
            sqlQueryButton.BackColor = Color.White;
            sqlQueryButton.Dock = DockStyle.Top;
            sqlQueryButton.FlatStyle = FlatStyle.Flat;
            sqlQueryButton.Font = new Font("Segoe UI", 12F);
            sqlQueryButton.ForeColor = Color.FromArgb(30, 30, 30);
            sqlQueryButton.Location = new Point(10, 182);
            sqlQueryButton.Margin = new Padding(5);
            sqlQueryButton.Name = "sqlQueryButton";
            sqlQueryButton.Padding = new Padding(76, 12, 12, 12);
            sqlQueryButton.Size = new Size(494, 76);
            sqlQueryButton.TabIndex = 11;
            sqlQueryButton.Text = "SQL Query Tool";
            sqlQueryButton.UseVisualStyleBackColor = false;
            sqlQueryButton.Click += SqlQueryButton_Click;
            // 
            // clearDatabaseButton
            // 
            clearDatabaseButton.AutoSize = true;
            clearDatabaseButton.BackColor = Color.White;
            clearDatabaseButton.Dock = DockStyle.Top;
            clearDatabaseButton.FlatStyle = FlatStyle.Flat;
            clearDatabaseButton.Font = new Font("Segoe UI", 12F);
            clearDatabaseButton.ForeColor = Color.FromArgb(30, 30, 30);
            clearDatabaseButton.Location = new Point(10, 357);
            clearDatabaseButton.Margin = new Padding(5);
            clearDatabaseButton.Name = "clearDatabaseButton";
            clearDatabaseButton.Padding = new Padding(76, 12, 12, 12);
            clearDatabaseButton.Size = new Size(494, 76);
            clearDatabaseButton.TabIndex = 8;
            clearDatabaseButton.Text = "Clear Database";
            clearDatabaseButton.UseVisualStyleBackColor = false;
            clearDatabaseButton.Click += ClearDatabaseButton_Click;
            // 
            // testConfigurationButton
            // 
            testConfigurationButton.AutoSize = true;
            testConfigurationButton.BackColor = Color.White;
            testConfigurationButton.Dock = DockStyle.Top;
            testConfigurationButton.FlatStyle = FlatStyle.Flat;
            testConfigurationButton.Font = new Font("Segoe UI", 12F);
            testConfigurationButton.ForeColor = Color.FromArgb(30, 30, 30);
            testConfigurationButton.Location = new Point(10, 443);
            testConfigurationButton.Margin = new Padding(5);
            testConfigurationButton.Name = "testConfigurationButton";
            testConfigurationButton.Padding = new Padding(76, 12, 12, 12);
            testConfigurationButton.Size = new Size(494, 76);
            testConfigurationButton.TabIndex = 10;
            testConfigurationButton.Text = "Test Configuration";
            testConfigurationButton.UseVisualStyleBackColor = false;
            testConfigurationButton.Click += TestConfigurationButton_Click;
            // 
            // viewLogsButton
            // 
            viewLogsButton.AutoSize = true;
            viewLogsButton.BackColor = Color.White;
            viewLogsButton.Dock = DockStyle.Top;
            viewLogsButton.FlatStyle = FlatStyle.Flat;
            viewLogsButton.Font = new Font("Segoe UI", 12F);
            viewLogsButton.ForeColor = Color.FromArgb(30, 30, 30);
            viewLogsButton.Location = new Point(10, 529);
            viewLogsButton.Margin = new Padding(5);
            viewLogsButton.Name = "viewLogsButton";
            viewLogsButton.Padding = new Padding(76, 12, 12, 12);
            viewLogsButton.Size = new Size(494, 76);
            viewLogsButton.TabIndex = 12;
            viewLogsButton.Text = "View Logs";
            viewLogsButton.UseVisualStyleBackColor = false;
            viewLogsButton.Click += ViewLogsButton_Click;
            // 
            // handleOffersButton
            // 
            handleOffersButton.AutoSize = true;
            handleOffersButton.BackColor = Color.White;
            handleOffersButton.Dock = DockStyle.Top;
            handleOffersButton.FlatStyle = FlatStyle.Flat;
            handleOffersButton.Font = new Font("Segoe UI", 12F);
            handleOffersButton.ForeColor = Color.FromArgb(30, 30, 30);
            handleOffersButton.Location = new Point(10, 615);
            handleOffersButton.Margin = new Padding(5);
            handleOffersButton.Name = "handleOffersButton";
            handleOffersButton.Padding = new Padding(77, 12, 12, 12);
            handleOffersButton.Size = new Size(494, 79);
            handleOffersButton.TabIndex = 14;
            handleOffersButton.Text = "Handle Offers";
            handleOffersButton.UseVisualStyleBackColor = false;
            handleOffersButton.Click += HandleOffersButton_Click;
            // 
            // notificationPanel
            // 
            notificationPanel.BackColor = Color.FromArgb(240, 249, 235);
            notificationPanel.BorderStyle = BorderStyle.FixedSingle;
            notificationPanel.Controls.Add(notificationClearButton);
            notificationPanel.Controls.Add(notificationTimeLabel);
            notificationPanel.Controls.Add(notificationMessageLabel);
            notificationPanel.Controls.Add(notificationStatusIcon);
            notificationPanel.Dock = DockStyle.Bottom;
            notificationPanel.Location = new Point(0, 800);
            notificationPanel.Name = "notificationPanel";
            notificationPanel.Padding = new Padding(12, 8, 12, 8);
            notificationPanel.Size = new Size(1264, 60);
            notificationPanel.TabIndex = 3;
            notificationPanel.Visible = false;
            // 
            // notificationStatusIcon
            // 
            notificationStatusIcon.AutoSize = true;
            notificationStatusIcon.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
            notificationStatusIcon.Location = new Point(12, 8);
            notificationStatusIcon.Name = "notificationStatusIcon";
            notificationStatusIcon.Size = new Size(28, 30);
            notificationStatusIcon.TabIndex = 0;
            notificationStatusIcon.Text = "ℹ️";
            // 
            // notificationMessageLabel
            // 
            notificationMessageLabel.AutoSize = false;
            notificationMessageLabel.Font = new Font("Segoe UI", 10F);
            notificationMessageLabel.ForeColor = Color.FromArgb(13, 17, 23);
            notificationMessageLabel.Location = new Point(50, 8);
            notificationMessageLabel.Name = "notificationMessageLabel";
            notificationMessageLabel.Size = new Size(800, 44);
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
            notificationTimeLabel.Size = new Size(60, 13);
            notificationTimeLabel.TabIndex = 2;
            notificationTimeLabel.Text = "12:34:56";
            // 
            // notificationClearButton
            // 
            notificationClearButton.Font = new Font("Segoe UI", 9F);
            notificationClearButton.Location = new Point(1170, 12);
            notificationClearButton.Name = "notificationClearButton";
            notificationClearButton.Size = new Size(70, 32);
            notificationClearButton.TabIndex = 3;
            notificationClearButton.Text = "✕";
            notificationClearButton.UseVisualStyleBackColor = true;
            notificationClearButton.Click += NotificationClearButton_Click;
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
            Controls.Add(panel1);
            Controls.Add(menuStrip);
            IsMdiContainer = true;
            MainMenuStrip = menuStrip;
            Name = "MainForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Sacks Product Management System";
            WindowState = FormWindowState.Maximized;
            menuStrip.ResumeLayout(false);
            menuStrip.PerformLayout();
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            tableLayoutPanel1.ResumeLayout(false);
            tableLayoutPanel1.PerformLayout();
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
        private ToolStripMenuItem windowToolStripMenuItem;
        private ToolStripMenuItem cascadeToolStripMenuItem;
        private ToolStripMenuItem tileHorizontalToolStripMenuItem;
        private ToolStripMenuItem tileVerticalToolStripMenuItem;
        private ToolStripMenuItem arrangeIconsToolStripMenuItem;
        private ToolStripMenuItem closeAllToolStripMenuItem;
        private ToolStripSeparator toolStripSeparator1;
        private ToolStripSeparator toolStripSeparator2;
        private Panel panel1;
        private TableLayoutPanel tableLayoutPanel1;
        private ModernButton buttonEditMaps;
        private ModernButton processFilesButton;
        private ModernButton showStatisticsButton;
        private ModernButton sqlQueryButton;
        private ModernButton clearDatabaseButton;
        private ModernButton testConfigurationButton;
        private ModernButton viewLogsButton;
        private ModernButton handleOffersButton;
        private GroupBox aiQueryGroupBox;
        private TableLayoutPanel aiQueryTableLayout;
        private Label aiQueryLabel;
        private Label responseModeLabel;
        private ComboBox responseModeComboBox;
        private TextBox aiQueryTextBox;
        private ModernButton executeAiQueryButton;
        private Label aiMetadataLabel;
        private RichTextBox aiMetadataTextBox;
        private Label aiDataLabel;
        private RichTextBox aiDataResultsTextBox;
        private Panel notificationPanel;
        private Label notificationStatusIcon;
        private Label notificationMessageLabel;
        private Label notificationTimeLabel;
        private ModernButton notificationClearButton;
        private System.Windows.Forms.Timer notificationTimer;
    }
}




