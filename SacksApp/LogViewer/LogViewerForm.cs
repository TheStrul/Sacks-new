// <copyright file="LogViewerForm.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System.Text;
using System.Reflection;
using Microsoft.Extensions.Configuration;

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
    // When true the form will use BeginInvoke (async) for cross-thread marshaling
    // Set to false to use synchronous Invoke if deterministic ordering is required
    private readonly bool _useBeginInvoke = true;

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

            // Enable double buffering for the RichTextBox using reflection
            // This should be done after InitializeComponent to ensure logTextBox exists
            EnableRichTextBoxDoubleBuffering();

            if (!DesignMode)
            {
                SetupControllerEventHandlers();
                InitializeControls();
                _controller.Initialize();
            }
        }

        /// <summary>
        /// Enable double buffering for RichTextBox to reduce flicker
        /// </summary>
        private void EnableRichTextBoxDoubleBuffering()
        {
            try
            {
                typeof(RichTextBox).InvokeMember("DoubleBuffered",
                    BindingFlags.SetProperty | BindingFlags.Instance | BindingFlags.NonPublic,
                    null, this.logTextBox, new object[] { true });
            }
            catch
            {
                // Ignore if double buffering cannot be enabled
            }
        }

        /// <summary>
        /// Setup event handlers for controller events (non-designer events)
        /// </summary>
        private void SetupControllerEventHandlers()
        {
            // Controller events - these are not designer-generated events
            _controller.NewLogLines += Controller_NewLogLines;
            _controller.StatusUpdated += Controller_StatusUpdated;
            _controller.SearchCompleted += Controller_SearchCompleted;
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
            // Prevent CheckedChanged handlers from reacting to programmatic updates
            try
            {
                suspendCheckboxEvents = true;

                allCheckBox.Checked = _model.SelectedLogLevels.Count == 5;
                errorCheckBox.Checked = _model.SelectedLogLevels.Contains(LogLevel.Error);
                warningCheckBox.Checked = _model.SelectedLogLevels.Contains(LogLevel.Warning);
                infoCheckBox.Checked = _model.SelectedLogLevels.Contains(LogLevel.Info);
                debugCheckBox.Checked = _model.SelectedLogLevels.Contains(LogLevel.Debug);
                defaultCheckBox.Checked = _model.SelectedLogLevels.Contains(LogLevel.Default);
            }
            finally
            {
                suspendCheckboxEvents = false;
            }
        }

        private void Controller_NewLogLines(object? sender, LogLinesEventArgs e)
        {
            if (InvokeRequired)
            {
                if (_useBeginInvoke)
                {
                    BeginInvoke(new Action(() => Controller_NewLogLines(sender, e)));
                    return;
                }

                // Fallback to synchronous Invoke when BeginInvoke is disabled
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
                int currentSelectionLength = logTextBox.SelectionLength;
                // Consider selection length when determining if the view is at the end
                bool wasAtEnd = (logTextBox.SelectionStart + currentSelectionLength) >= Math.Max(0, logTextBox.Text.Length - 1);

                try
                {
                    // Use a more aggressive update suspension for bulk appends to reduce flicker
                    BeginUpdate(logTextBox);

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
                        // Restore previous scroll position (clamped)
                        logTextBox.SelectionStart = Math.Min(currentScrollPos, logTextBox.Text.Length);
                    }
                }
                finally
                {
                    // Always ensure updates are re-enabled
                    EndUpdate(logTextBox);
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

        private void ExportButton_Click(object? sender, EventArgs e)
        {
            try
            {
                using var sfd = new SaveFileDialog
                {
                    Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*",
                    Title = "Export log content",
                    FileName = _model.LogFileName + ".txt",
                    InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
                };

                if (sfd.ShowDialog(this) != DialogResult.OK) return;

                File.WriteAllText(sfd.FileName, logTextBox.Text, Encoding.UTF8);
                statusLabel.Text = $"Monitoring: {_model.LogFileName} | Exported to {Path.GetFileName(sfd.FileName)}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Export failed: {ex.Message}", "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void EnsureContextMenuInitialized()
        {
            if (logContextMenu != null) return;
            logContextMenu = new ContextMenuStrip();
            var copyItem = new ToolStripMenuItem("Copy", null, (s, e) => { if (!string.IsNullOrEmpty(logTextBox.SelectedText)) Clipboard.SetText(logTextBox.SelectedText); });
            var selectAllItem = new ToolStripMenuItem("Select All", null, (s, e) => { logTextBox.SelectAll(); });
            var exportItem = new ToolStripMenuItem("Export...", null, (s, e) => ExportButton_Click(s, e));
            logContextMenu.Items.AddRange(new ToolStripItem[] { copyItem, selectAllItem, exportItem });
            logTextBox.ContextMenuStrip = logContextMenu;
        }


        private void AppendColoredLogLine(string line)
        {
            var color = _model.GetLogLineColor(line);

            logTextBox.SelectionStart = logTextBox.Text.Length;
            logTextBox.SelectionColor = color;
            logTextBox.AppendText(line + Environment.NewLine);
        }

        /// <summary>
        /// BeginUpdate/EndUpdate helpers to suspend painting for RichTextBox more effectively
        /// Uses WM_SETREDRAW to avoid flicker during bulk updates.
        /// </summary>
        private const int WM_SETREDRAW = 0x000B;

        private void BeginUpdate(Control c)
        {
            if (c == null) return;
            var handle = c.Handle; // ensure handle created
            NativeMethods.SendMessage(handle, WM_SETREDRAW, IntPtr.Zero, IntPtr.Zero);
        }

        private void EndUpdate(Control c)
        {
            if (c == null) return;
            var handle = c.Handle;
            NativeMethods.SendMessage(handle, WM_SETREDRAW, new IntPtr(1), IntPtr.Zero);
            c.Invalidate();
            c.Refresh();
        }

        // Minimal native helper for SendMessage
        private static class NativeMethods
        {
            [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
            internal static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);
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

            // Disable repainting during the update
            logTextBox.SuspendLayout();

            try
            {
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
            if (string.IsNullOrEmpty(text)) return string.Empty;

            var sb = new StringBuilder(text.Length * 2);
            foreach (var ch in text)
            {
                switch (ch)
                {
                    case '\\': sb.Append(@"\\\\"); break;
                    case '{': sb.Append(@"\{"); break;
                    case '}': sb.Append(@"\}"); break;
                    case '\n': sb.Append(@"\par"); break;
                    case '\r': /* skip carriage return - handled with \n */ break;
                    default:
                        if (ch <= 0x7f)
                        {
                            // ASCII - safe to append directly
                            sb.Append(ch);
                        }
                        else
                        {
                            // Use RTF unicode escape \uN? where N is signed 16-bit value
                            sb.AppendFormat("\\u{0}?", (short)ch);
                        }
                        break;
                }
            }

            return sb.ToString();
        }

        #region Event Handlers

        private void AutoScrollCheckBox_CheckedChanged(object? sender, EventArgs e)
        {
            _controller.SetAutoScroll(autoScrollCheckBox.Checked);
        }

        private void ClearButton_Click(object? sender, EventArgs e)
        {
            try
            {
                // Stop monitoring to release LogViewer's file handle
                _controller.StopMonitoring();
                
                // Always clear the display immediately for user feedback
                logTextBox.Clear();
                statusLabel.Text = $"Monitoring: {_model.LogFileName} | Display cleared";
                
                if (File.Exists(_model.LogFilePath))
                {
                    // Try the soft approach: attempt file operations but don't fail if they don't work
                    var clearResult = AttemptLogFileClear();
                    
                    switch (clearResult.Result)
                    {
                        case ClearResult.Success:
                            statusLabel.Text = $"Monitoring: {_model.LogFileName} | Log file cleared successfully";
                            break;
                            
                        case ClearResult.PartialSuccess:
                            statusLabel.Text = $"Monitoring: {_model.LogFileName} | Display cleared (file may still contain data)";
                            break;
                            
                        case ClearResult.Failed:
                            statusLabel.Text = $"Monitoring: {_model.LogFileName} | Display cleared (file locked by Serilog)";
                            
                            // Show informational message without making it seem like an error
                            break;
                    }
                }
                else
                {
                    statusLabel.Text = $"Monitoring: {_model.LogFileName} | Display cleared (no log file)";
                }
            }
            catch (Exception ex)
            {
                statusLabel.Text = $"Monitoring: {_model.LogFileName} | Error: {ex.Message}";
                MessageBox.Show($"Error during clear operation: {ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                // Always restart monitoring regardless of file clear success
                _controller.StartMonitoring();
            }
        }

        /// <summary>
        /// Attempts to clear the log file using multiple strategies, but gracefully handles failures
        /// </summary>
        private (ClearResult Result, string Method) AttemptLogFileClear()
        {
            // Strategy 1: Try gentle truncation (least likely to conflict)
            try
            {
                using var fileStream = new FileStream(_model.LogFilePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite);
                fileStream.SetLength(0);
                fileStream.Flush();
                return (ClearResult.Success, "truncation");
            }
            catch (IOException)
            {
                // Expected when Serilog has exclusive access
            }
            catch (UnauthorizedAccessException)
            {
                // File permissions issue
            }

            // Strategy 2: Try overwrite with empty content (sometimes works when truncation doesn't)
            try
            {
                File.WriteAllText(_model.LogFilePath, "", System.Text.Encoding.UTF8);
                return (ClearResult.Success, "overwrite");
            }
            catch (IOException)
            {
                // Expected when Serilog has exclusive access
            }
            catch (UnauthorizedAccessException)
            {
                // File permissions issue
            }

            // Strategy 3: Try deletion (least likely to work but worth attempting)
            try
            {
                File.Delete(_model.LogFilePath);
                // Verify deletion
                if (!File.Exists(_model.LogFilePath))
                {
                    return (ClearResult.Success, "deletion");
                }
            }
            catch (IOException)
            {
                // Expected when Serilog has exclusive access
            }
            catch (UnauthorizedAccessException)
            {
                // File permissions issue
            }

            // All strategies failed - this is normal when Serilog is actively writing
            return (ClearResult.Failed, "none");
        }

        /// <summary>
        /// Result codes for log file clearing attempts
        /// </summary>
        private enum ClearResult
        {
            Success,        // File was successfully cleared
            PartialSuccess, // Display cleared but file may still have content
            Failed          // Unable to clear file (normal when Serilog is active)
        }

        private void ColorLegendButton_Click(object? sender, EventArgs e)
        {
            ShowColorLegend();
        }

        private void AllCheckBox_CheckedChanged(object? sender, EventArgs e)
        {
            if (suspendCheckboxEvents) return;

            _controller.SetAllLogLevels(allCheckBox.Checked);
            UpdateCheckboxStates();
        }

        private void LogLevelCheckBox_CheckedChanged(object? sender, EventArgs e)
        {
            if (suspendCheckboxEvents) return;

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
            EnsureContextMenuInitialized();
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

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            // Ctrl+E to export
            if (keyData == (Keys.Control | Keys.E))
            {
                ExportButton_Click(this, EventArgs.Empty);
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
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
                // Restore persisted window location if available (controlled by configuration setting)
                try
                {
                    var configuration = SacksApp.Utils.ConfigurationLoader.BuildConfiguration();
                    var restore = configuration.GetValue<bool>("UISettings:RestoreWindowPositions", true);
                    SacksApp.Utils.WindowStateHelper.RestoreWindowState(this, WindowStateFileName, restore);
                }
                catch
                {
                    // Ignore restore errors
                }

                // Ensure the form is visible and activated
                this.Visible = true;
                // Do not force Normal, allow restored maximized state
                // this.WindowState = FormWindowState.Normal;
                this.BringToFront();
                this.Activate();
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            try
            {
                SacksApp.Utils.WindowStateHelper.SaveWindowState(this, WindowStateFileName);
            }
            catch
            {
                // Ignore save errors
            }

            base.OnFormClosing(e);
        }

        /// <summary>
        /// Position the form on the same screen as the owner or the application's main window.
        /// Falls back to primary screen if no suitable owner is found.
        /// </summary>
        private void PositionOnOwnerOrPrimaryScreen()
        {
            // Only perform manual positioning when the form has a reasonable size
            if (this.Width <= 0 || this.Height <= 0) return;

            // Determine a candidate owner form from the application's open forms
            Form? owner = this.Owner;

            if (owner == null)
            {
                // Prefer the active form if available
                owner = Application.OpenForms.Cast<Form>().FirstOrDefault(f => f != this && f.Visible && f.WindowState != FormWindowState.Minimized);
            }

            System.Windows.Forms.Screen? screen = null;

            if (owner != null && owner.IsHandleCreated)
            {
                try
                {
                    screen = System.Windows.Forms.Screen.FromControl(owner);
                }
                catch
                {
                    screen = System.Windows.Forms.Screen.PrimaryScreen;
                }
            }
            else
            {
                // If no owner can be determined, use the screen containing the cursor as a best-effort
                try
                {
                    screen = System.Windows.Forms.Screen.FromPoint(Cursor.Position);
                }
                catch
                {
                    screen = System.Windows.Forms.Screen.PrimaryScreen;
                }
            }

            // Ensure we have a non-null screen (PrimaryScreen is the last resort)
            if (screen == null)
            {
                screen = System.Windows.Forms.Screen.PrimaryScreen;
            }

            var wa = (screen ?? System.Windows.Forms.Screen.AllScreens.First()).WorkingArea;

            // Ensure StartPosition is Manual so Location is respected
            this.StartPosition = FormStartPosition.Manual;

            // Center the form in the chosen screen's working area
            var x = wa.Left + Math.Max(0, (wa.Width - this.Width) / 2);
            var y = wa.Top + Math.Max(0, (wa.Height - this.Height) / 2);

            // Clamp to working area
            x = Math.Min(Math.Max(wa.Left, x), wa.Right - this.Width);
            y = Math.Min(Math.Max(wa.Top, y), wa.Bottom - this.Height);

            this.Location = new Point(x, y);
        }

    private const string WindowStateFileName = "LogViewerForm.WindowState.json";

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
