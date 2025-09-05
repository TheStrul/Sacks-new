// <copyright file="LogViewerForm.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System.Text;
using Microsoft.Extensions.Logging;

namespace SacksConsoleApp
{
    /// <summary>
    /// Windows Forms UI for viewing service logs with real-time monitoring
    /// Separated from business logic for better maintainability and designer support
    /// </summary>
    public partial class LogViewerForm : Form
    {
        private readonly LogViewerModel _model;
        private readonly LogViewerController _controller;
        private readonly ILogger<LogViewerForm>? _logger;

        /// <summary>
        /// Shows the log viewer for the most recent Sacks application log file
        /// </summary>
        /// <param name="serviceProvider">Optional service provider for dependency injection</param>
        public static void ShowSacksLogs(IServiceProvider? serviceProvider = null)
        {
            var logger = serviceProvider?.GetService(typeof(ILogger<LogViewerForm>)) as ILogger<LogViewerForm>;
            
            // Find the logs directory relative to application
            var baseDirectory = AppContext.BaseDirectory;
            var logsDirectory = Path.Combine(baseDirectory, "logs");

            if (!Directory.Exists(logsDirectory))
            {
                // Try alternative locations for logs
                var alternatives = new[]
                {
                    Path.Combine(Environment.CurrentDirectory, "logs"),
                    Path.Combine(Environment.CurrentDirectory, "..", "logs"),
                    Path.Combine(Environment.CurrentDirectory, "..", "..", "..", "logs")
                };

                foreach (var alt in alternatives)
                {
                    if (Directory.Exists(alt))
                    {
                        logsDirectory = alt;
                        break;
                    }
                }

                if (!Directory.Exists(logsDirectory))
                {
                    Console.WriteLine($"❌ Logs directory not found. Checked:");
                    Console.WriteLine($"   • {Path.Combine(baseDirectory, "logs")}");
                    foreach (var alt in alternatives)
                    {
                        Console.WriteLine($"   • {alt}");
                    }
                    Console.WriteLine("Make sure the application has been started at least once to create log files.");
                    Console.WriteLine("Press any key to return to menu...");
                    Console.ReadKey();
                    return;
                }
            }

            var logFile = LogViewerController.GetMostRecentLogFile(baseDirectory);
            if (logFile == null)
            {
                Console.WriteLine($"⚠️  No log files found in: {logsDirectory}");
                Console.WriteLine("Make sure the application has been started and has written some logs.");
                Console.WriteLine("Press any key to return to menu...");
                Console.ReadKey();
                return;
            }

            try
            {
                Console.WriteLine("✅ Opening log viewer in Windows Form...");
                Console.WriteLine("💡 You can now use both the log viewer and this menu simultaneously!");

                // Initialize Windows Forms context if not already done
                if (!Application.MessageLoop)
                {
                    // Create and run the form on a separate thread with proper Windows Forms context
                    var formThread = new Thread(() =>
                    {
                        Application.EnableVisualStyles();
                        Application.SetCompatibleTextRenderingDefault(false);

                        var logForm = new LogViewerForm(logFile, logger);
                        Application.Run(logForm); // This creates a proper message loop
                    })
                    {
                        IsBackground = true,  // Background thread allows main app to exit
                    };

                    formThread.SetApartmentState(ApartmentState.STA);
                    formThread.Start();

                    Console.WriteLine("✅ Log viewer window opened successfully!");
                }
                else
                {
                    // If Application.MessageLoop is already running, just show the form
                    var logForm = new LogViewerForm(logFile, logger);
                    logForm.Show();
                }

                Console.WriteLine("📋 Log Viewer Features:");
                Console.WriteLine("   • Real-time monitoring of log file changes");
                Console.WriteLine("   • Filter by log levels (Error, Warning, Info, Debug)");
                Console.WriteLine("   • Search functionality (Ctrl+F or F3 for find next)");
                Console.WriteLine("   • Auto-scroll for new entries");
                Console.WriteLine("   • Color-coded log levels");
                Console.WriteLine("   • Right-click to copy selected text");
                Console.WriteLine();
                Console.WriteLine("💡 You can continue using this menu while the log viewer is open");
                Console.WriteLine("Press any key to return to menu...");
                Console.ReadKey();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error opening Windows Form: {ex.Message}");
                logger?.LogError(ex, "Error opening log viewer form");
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
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
        public LogViewerForm(string logFilePath, ILogger<LogViewerForm>? logger = null)
        {
            _logger = logger;
            
            // Enable double buffering to reduce flicker
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.DoubleBuffer, true);

            _model = new LogViewerModel { LogFilePath = logFilePath };
            
            // Create controller with logger support
            var controllerLogger = logger?.CreateChildLogger("Controller");
            _controller = new LogViewerController(_model, controllerLogger);

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
            this.Text = $"Sacks Log Viewer - {_model.LogFileName}";

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
            catch (Exception ex)
            {
                // Fallback to simple method if RTF generation fails
                _logger?.LogWarning(ex, "RTF generation failed, falling back to simple append");
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
                        var timer = new System.Windows.Forms.Timer { Interval = 2000 };
                        timer.Tick += (s, args) =>
                        {
                            statusLabel.Text = originalStatus;
                            timer.Stop();
                            timer.Dispose();
                        };
                        timer.Start();
                    }
                }
                catch (Exception ex)
                {
                    // Handle clipboard errors gracefully
                    _logger?.LogWarning(ex, "Failed to copy text to clipboard");
                    statusLabel.Text = $"Failed to copy to clipboard: {ex.Message}";
                }
            }
        }

        #endregion

        private void ShowColorLegend()
        {
            var legendForm = new Form
            {
                Text = "Sacks Log Color Legend",
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

            // Add color legend entries for Serilog patterns
            AddLegendEntry(legendText, "Serilog Log Level Patterns:", Color.White);
            AddLegendEntry(legendText, "[yyyy-MM-dd HH:mm:ss.fff zzz ERR]: Error messages", LogViewerModel.LogLevelColors["ERR"]);
            AddLegendEntry(legendText, "[yyyy-MM-dd HH:mm:ss.fff zzz WRN]: Warning messages", LogViewerModel.LogLevelColors["WRN"]);
            AddLegendEntry(legendText, "[yyyy-MM-dd HH:mm:ss.fff zzz INF]: Information messages", LogViewerModel.LogLevelColors["INF"]);
            AddLegendEntry(legendText, "[yyyy-MM-dd HH:mm:ss.fff zzz DBG]: Debug messages", LogViewerModel.LogLevelColors["DBG"]);
            AddLegendEntry(legendText, "Other/Default text", LogViewerModel.LogLevelColors["Default"]);

            AddLegendEntry(legendText, "", Color.White); // Empty line
            AddLegendEntry(legendText, "Filter Options:", Color.White);
            AddLegendEntry(legendText, "• Use checkboxes to select multiple log levels", Color.LightGray);
            AddLegendEntry(legendText, "• 'All' checkbox selects/deselects all levels", Color.LightGray);
            AddLegendEntry(legendText, "• Individual checkboxes allow custom filtering", Color.LightGray);

            AddLegendEntry(legendText, "", Color.White); // Empty line
            AddLegendEntry(legendText, "Keyboard Shortcuts:", Color.White);
            AddLegendEntry(legendText, "• Ctrl+F: Focus search box", Color.LightGray);
            AddLegendEntry(legendText, "• F3: Find next occurrence", Color.LightGray);
            AddLegendEntry(legendText, "• Right-click: Copy selected text to clipboard", Color.LightGray);

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

    /// <summary>
    /// Extension methods for ILogger to create child loggers
    /// </summary>
    internal static class LoggerExtensions
    {
        internal static ILogger<T> CreateChildLogger<T>(this ILogger parent, string categoryName)
        {
            // This is a simplified implementation; in a real scenario you might want to use a proper logger factory
            return parent as ILogger<T> ?? throw new InvalidOperationException("Cannot create child logger");
        }
    }
}
