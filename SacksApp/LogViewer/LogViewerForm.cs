// <copyright file="LogViewerForm.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System.Text;

namespace QMobileDeviceServiceMenu
{
    /// <summary>
    /// Windows Forms UI for viewing service logs with real-time monitoring
    /// Separated from business logic for better maintainability and designer support
    /// </summary>
    public partial class LogViewerForm : Form
    {
        private readonly LogViewerModel _model;
        private readonly LogViewerController _controller;

        /// <summary>
        /// Shows the log viewer for the most recent service log file - Windows Forms compatible version
        /// </summary>
        /// <param name="serviceDirectory">The directory where the service is located</param>
        public static void ShowServiceLogs(string serviceDirectory)
        {
            string logsDirectory = Path.Combine(serviceDirectory, "logs");

            if (!Directory.Exists(logsDirectory))
            {
                MessageBox.Show($"Logs directory not found: {logsDirectory}\n\nMake sure the service has been started at least once to create log files.",
                    "Logs Directory Not Found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var logFile = LogViewerController.GetMostRecentLogFile(serviceDirectory);
            if (logFile == null)
            {
                MessageBox.Show($"No log files found in: {logsDirectory}",
                    "No Log Files Found", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                // If Application.MessageLoop is already running (WinForms app), just show the form
                if (Application.MessageLoop)
                {
#pragma warning disable CA2000 // Dispose objects before losing scope
                    var logForm = new LogViewerForm(logFile);
#pragma warning restore CA2000
                    logForm.Show(); // Non-modal form - will manage its own disposal
                }
                else
                {
                    // Create and run the form on a separate thread with proper Windows Forms context
                    var formThread = new Thread(() =>
                    {
                        Application.EnableVisualStyles();
                        Application.SetCompatibleTextRenderingDefault(false);

                        using var logForm = new LogViewerForm(logFile);
                        Application.Run(logForm); // This creates a proper message loop
                    })
                    {
                        IsBackground = true,  // Background thread allows main app to exit
                    };

                    formThread.SetApartmentState(ApartmentState.STA);
                    formThread.Start();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening log viewer:\n{ex.Message}",
                    "Log Viewer Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Constructor for designer support
        /// </summary>
        public LogViewerForm() : this(string.Empty)
        {
        }

        /// <summary>
        /// Constructor with log file path
        /// </summary>
        public LogViewerForm(string logFilePath)
        {
            // Enable double buffering to reduce flicker
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.DoubleBuffer, true);

            _model = new LogViewerModel { LogFilePath = logFilePath };
            _controller = new LogViewerController(_model);

            InitializeComponent();

            if (!DesignMode)
            {
                SetupEventHandlers();
                InitializeControls();
                _controller.Initialize();
            }
        }

        private void SetupEventHandlers()
        {
            // Controller events
            _controller.NewLogLines += Controller_NewLogLines;
            _controller.StatusUpdated += Controller_StatusUpdated;
            _controller.SearchCompleted += Controller_SearchCompleted;

            // UI events
            autoScrollCheckBox.CheckedChanged += AutoScrollCheckBox_CheckedChanged;
            clearButton.Click += ClearButton_Click;
            colorLegendButton.Click += ColorLegendButton_Click;

            // Log level filter checkboxes
            allCheckBox.CheckedChanged += AllCheckBox_CheckedChanged;
            errorCheckBox.CheckedChanged += LogLevelCheckBox_CheckedChanged;
            warningCheckBox.CheckedChanged += LogLevelCheckBox_CheckedChanged;
            infoCheckBox.CheckedChanged += LogLevelCheckBox_CheckedChanged;
            debugCheckBox.CheckedChanged += LogLevelCheckBox_CheckedChanged;
            defaultCheckBox.CheckedChanged += LogLevelCheckBox_CheckedChanged;

            searchBox.KeyDown += SearchBox_KeyDown;
            searchButton.Click += SearchButton_Click;
            this.KeyDown += LogViewerForm_KeyDown;

            // Add mouse event for auto-copy functionality
            logTextBox.MouseDown += LogTextBox_MouseDown;
        }

        private void InitializeControls()
        {
            // Set form title
            this.Text = $"Service Log Viewer - {_model.LogFileName}";

            // Initialize checkboxes based on current model state
            UpdateCheckboxStates();

            // Set initial status
            statusLabel.Text = $"Monitoring: {_model.LogFileName}";
        }

        /// <summary>
        /// Updates checkbox states to match the model
        /// </summary>
        private void UpdateCheckboxStates()
        {
            // Temporarily disable events to prevent recursion
            allCheckBox.CheckedChanged -= AllCheckBox_CheckedChanged;
            errorCheckBox.CheckedChanged -= LogLevelCheckBox_CheckedChanged;
            warningCheckBox.CheckedChanged -= LogLevelCheckBox_CheckedChanged;
            infoCheckBox.CheckedChanged -= LogLevelCheckBox_CheckedChanged;
            debugCheckBox.CheckedChanged -= LogLevelCheckBox_CheckedChanged;
            defaultCheckBox.CheckedChanged -= LogLevelCheckBox_CheckedChanged;

            // Update checkbox states
            allCheckBox.Checked = _model.SelectedLogLevels.Count == 5;
            errorCheckBox.Checked = _model.SelectedLogLevels.Contains(LogLevel.Error);
            warningCheckBox.Checked = _model.SelectedLogLevels.Contains(LogLevel.Warning);
            infoCheckBox.Checked = _model.SelectedLogLevels.Contains(LogLevel.Info);
            debugCheckBox.Checked = _model.SelectedLogLevels.Contains(LogLevel.Debug);
            defaultCheckBox.Checked = _model.SelectedLogLevels.Contains(LogLevel.Default);

            // Re-enable events
            allCheckBox.CheckedChanged += AllCheckBox_CheckedChanged;
            errorCheckBox.CheckedChanged += LogLevelCheckBox_CheckedChanged;
            warningCheckBox.CheckedChanged += LogLevelCheckBox_CheckedChanged;
            infoCheckBox.CheckedChanged += LogLevelCheckBox_CheckedChanged;
            debugCheckBox.CheckedChanged += LogLevelCheckBox_CheckedChanged;
            defaultCheckBox.CheckedChanged += LogLevelCheckBox_CheckedChanged;
        }

        private void Controller_NewLogLines(object? sender, LogLinesEventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => Controller_NewLogLines(sender, e)));
                return;
            }

            if (e.ClearFirst)
            {
                // Use optimized refresh for full rebuilds (triggered by filter changes)
                RefreshLogDisplayOptimized(e.Lines);
            }
            else
            {
                // Use incremental append for new log entries (real-time updates)
                // Store current scroll position and selection
                int currentScrollPos = logTextBox.SelectionStart;
                bool wasAtEnd = logTextBox.SelectionStart >= logTextBox.Text.Length - 1;

                try
                {
                    // Suspend layout to prevent flicker
                    logTextBox.SuspendLayout();

                    // Add new lines incrementally
                    foreach (var line in e.Lines)
                    {
                        AppendColoredLogLine(line);
                    }

                    // Handle auto-scroll logic
                    if (e.AutoScroll && wasAtEnd)
                    {
                        logTextBox.SelectionStart = logTextBox.Text.Length;
                        logTextBox.ScrollToCaret();
                    }
                    else
                    {
                        // Restore previous scroll position
                        logTextBox.SelectionStart = Math.Min(currentScrollPos, logTextBox.Text.Length);
                    }
                }
                finally
                {
                    // Always restore layout
                    logTextBox.ResumeLayout();
                }
            }
        }

        private void Controller_StatusUpdated(object? sender, string e)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => Controller_StatusUpdated(sender, e)));
                return;
            }

            statusLabel.Text = $"Monitoring: {_model.LogFileName} | {e}";
        }

        private void Controller_SearchCompleted(object? sender, SearchResultEventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => Controller_SearchCompleted(sender, e)));
                return;
            }

            if (!e.Success)
            {
                if (e.ShowDialog)
                {
                    MessageBox.Show(e.Message, "Search", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                return;
            }

            // Perform the actual search in the UI
            if (e.IsFirstSearch)
            {
                SearchInText(e.SearchTerm, 0);
            }
            else
            {
                SearchInText(e.SearchTerm, logTextBox.SelectionStart + logTextBox.SelectionLength);
            }
        }

        private void AppendColoredLogLine(string line)
        {
            var color = _model.GetLogLineColor(line);

            logTextBox.SelectionStart = logTextBox.Text.Length;
            logTextBox.SelectionColor = color;
            logTextBox.AppendText(line + Environment.NewLine);
        }

        /// <summary>
        /// Efficiently updates the entire log display with minimal flicker
        /// </summary>
        private void RefreshLogDisplayOptimized(IList<string> lines)
        {
            if (lines.Count == 0)
            {
                logTextBox.Clear();
                return;
            }

            try
            {
                // Disable repainting during the update
                logTextBox.SuspendLayout();

                // Store current selection and scroll position
                int selectionStart = logTextBox.SelectionStart;
                int selectionLength = logTextBox.SelectionLength;
                bool wasAtEnd = selectionStart >= logTextBox.Text.Length - 10; // Allow some tolerance

                // Clear and rebuild content
                logTextBox.Clear();

                // Build all content in one operation for better performance
                var rtf = new StringBuilder();
                rtf.Append(@"{\rtf1\ansi\deff0 {\colortbl ;");

                // Build color table
                var colorMap = new Dictionary<Color, int>();
                int colorTableIndex = 1;

                foreach (var line in lines)
                {
                    var color = _model.GetLogLineColor(line);
                    if (!colorMap.ContainsKey(color))
                    {
                        colorMap[color] = colorTableIndex++;
                        rtf.Append($@"\red{color.R}\green{color.G}\blue{color.B};");
                    }
                }
                rtf.Append("}");

                // Add content with colors
                foreach (var line in lines)
                {
                    var color = _model.GetLogLineColor(line);
                    var lineColorIndex = colorMap[color];
                    rtf.Append($@"\cf{lineColorIndex} {EscapeRtfText(line)}\par");
                }

                rtf.Append("}");

                // Set RTF content in one operation (much faster than individual appends)
                logTextBox.Rtf = rtf.ToString();

                // Restore scroll position
                if (wasAtEnd)
                {
                    logTextBox.SelectionStart = logTextBox.Text.Length;
                    logTextBox.ScrollToCaret();
                }
                else
                {
                    logTextBox.SelectionStart = Math.Min(selectionStart, logTextBox.Text.Length);
                    logTextBox.SelectionLength = Math.Min(selectionLength, logTextBox.Text.Length - logTextBox.SelectionStart);
                }
            }
            catch (Exception)
            {
                // Fallback to simple method if RTF generation fails
                logTextBox.Clear();
                foreach (var line in lines)
                {
                    AppendColoredLogLine(line);
                }
            }
            finally
            {
                logTextBox.ResumeLayout();
            }
        }

        /// <summary>
        /// Escapes text for RTF format
        /// </summary>
        private static string EscapeRtfText(string text)
        {
            return text.Replace(@"\", @"\\")
                      .Replace("{", @"\{")
                      .Replace("}", @"\}")
                      .Replace("\n", @"\par")
                      .Replace("\r", "");
        }

        #region Event Handlers

        private void AutoScrollCheckBox_CheckedChanged(object? sender, EventArgs e)
        {
            _controller.SetAutoScroll(autoScrollCheckBox.Checked);
        }

        private void ClearButton_Click(object? sender, EventArgs e)
        {
            logTextBox.Clear();
        }

        private void ColorLegendButton_Click(object? sender, EventArgs e)
        {
            ShowColorLegend();
        }

        private void AllCheckBox_CheckedChanged(object? sender, EventArgs e)
        {
            _controller.SetAllLogLevels(allCheckBox.Checked);
            UpdateCheckboxStates();
        }

        private void LogLevelCheckBox_CheckedChanged(object? sender, EventArgs e)
        {
            if (sender is CheckBox checkBox)
            {
                var level = checkBox.Name switch
                {
                    "errorCheckBox" => LogLevel.Error,
                    "warningCheckBox" => LogLevel.Warning,
                    "infoCheckBox" => LogLevel.Info,
                    "debugCheckBox" => LogLevel.Debug,
                    "defaultCheckBox" => LogLevel.Default,
                    _ => LogLevel.Default
                };

                _controller.SetLogLevelFilter(level, checkBox.Checked);
                UpdateCheckboxStates();
            }
        }

        private void SearchBox_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                SearchButton_Click(sender, e);
                e.Handled = true;
            }
        }

        private void SearchButton_Click(object? sender, EventArgs e)
        {
            _controller.SearchLogs(searchBox.Text);
        }

        private void LogViewerForm_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.F)
            {
                searchBox.Focus();
                searchBox.SelectAll();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.F3)
            {
                if (!string.IsNullOrWhiteSpace(searchBox.Text))
                {
                    _controller.SearchLogs(searchBox.Text, true);
                }
                e.Handled = true;
            }
        }

        /// <summary>
        /// Handles mouse down events for auto-copy functionality similar to PowerShell
        /// </summary>
        private void LogTextBox_MouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right && logTextBox.SelectionLength > 0)
            {
                try
                {
                    // Copy selected text to clipboard
                    string selectedText = logTextBox.SelectedText;
                    if (!string.IsNullOrEmpty(selectedText))
                    {
                        Clipboard.SetText(selectedText);

                        // Update status to indicate copy operation
                        var originalStatus = statusLabel.Text;
                        statusLabel.Text = $"Copied {selectedText.Length} characters to clipboard";

                        // Restore original status after 2 seconds
                        using var timer = new System.Windows.Forms.Timer { Interval = 2000 };
                        timer.Tick += (s, args) =>
                        {
                            statusLabel.Text = originalStatus;
                            timer.Stop();
                        };
                        timer.Start();
                    }
                }
                catch (Exception ex)
                {
                    // Handle clipboard errors gracefully
                    statusLabel.Text = $"Failed to copy to clipboard: {ex.Message}";
                }
            }
        }

        #endregion

        private void ShowColorLegend()
        {
            using var legendForm = new Form
            {
                Text = "Log Color Legend",
                Size = new Size(450, 350),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = Color.Black,
            };

            var legendText = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                BackColor = Color.Black,
                Font = new Font("Consolas", 10),
                BorderStyle = BorderStyle.None,
                Margin = new Padding(10),
            };

            // Add color legend entries for Microsoft .NET logging patterns only
            AddLegendEntry(legendText, "Microsoft .NET Logging Patterns:", Color.White);
            AddLegendEntry(legendText, "[16:57:34.918 ERR]: Error messages", LogViewerModel.LogLevelColors["ERR"]);
            AddLegendEntry(legendText, "[16:57:34.918 WRN]: Warning messages", LogViewerModel.LogLevelColors["WRN"]);
            AddLegendEntry(legendText, "[16:57:34.918 INF]: Information messages", LogViewerModel.LogLevelColors["INF"]);
            AddLegendEntry(legendText, "[16:57:34.918 DBG]: Debug messages", LogViewerModel.LogLevelColors["DBG"]);
            AddLegendEntry(legendText, "Other/Default text", LogViewerModel.LogLevelColors["Default"]);

            AddLegendEntry(legendText, "", Color.White); // Empty line
            AddLegendEntry(legendText, "Filter Options:", Color.White);
            AddLegendEntry(legendText, "• Use checkboxes to select multiple log levels", Color.LightGray);
            AddLegendEntry(legendText, "• 'All' checkbox selects/deselects all levels", Color.LightGray);
            AddLegendEntry(legendText, "• Individual checkboxes allow custom filtering", Color.LightGray);

            legendForm.Controls.Add(legendText);
            legendForm.ShowDialog(this);
        }

        private void AddLegendEntry(RichTextBox rtb, string description, Color color)
        {
            rtb.SelectionStart = rtb.Text.Length;
            rtb.SelectionColor = color;
            rtb.AppendText($"● {description}\n");
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            if (!DesignMode)
            {
                // Ensure the form is visible and activated
                this.Visible = true;
                this.WindowState = FormWindowState.Normal;
                this.BringToFront();
                this.Activate();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _controller?.Dispose();
                components?.Dispose();
            }
            base.Dispose(disposing);
        }

        private void SearchInText(string searchTerm, int startIndex)
        {
            var text = logTextBox.Text;
            var index = text.IndexOf(searchTerm, startIndex, StringComparison.OrdinalIgnoreCase);

            if (index >= 0)
            {
                logTextBox.Select(index, searchTerm.Length);
                logTextBox.ScrollToCaret();
                logTextBox.Focus();
                statusLabel.Text = $"Monitoring: {_model.LogFileName} | Found '{searchTerm}' at position {index}";
            }
            else if (startIndex > 0)
            {
                // Wrap around to beginning
                index = text.IndexOf(searchTerm, 0, StringComparison.OrdinalIgnoreCase);
                if (index >= 0)
                {
                    logTextBox.Select(index, searchTerm.Length);
                    logTextBox.ScrollToCaret();
                    statusLabel.Text = $"Monitoring: {_model.LogFileName} | Wrapped to beginning - found '{searchTerm}' at position {index}";
                }
                else
                {
                    statusLabel.Text = $"Monitoring: {_model.LogFileName} | No more occurrences of '{searchTerm}' found";
                }
            }
            else
            {
                MessageBox.Show($"'{searchTerm}' not found in logs.", "Search Result",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                statusLabel.Text = $"Monitoring: {_model.LogFileName} | '{searchTerm}' not found";
            }
        }
    }
}
