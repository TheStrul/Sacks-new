// <copyright file="LogViewerController.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace QMobileDeviceServiceMenu
{
    /// <summary>
    /// Controller for log viewer business logic, separated from UI concerns.
    /// Manages real-time log file monitoring, filtering, and search functionality.
    /// </summary>
    internal sealed class LogViewerController : IDisposable
    {
        private readonly LogViewerModel _model;
        private readonly System.Windows.Forms.Timer _refreshTimer;
        private bool _disposed = false;

        /// <summary>
        /// Event raised when new log lines are available for display.
        /// </summary>
        internal event EventHandler<LogLinesEventArgs>? NewLogLines;

        /// <summary>
        /// Event raised when the status message should be updated.
        /// </summary>
        internal event EventHandler<string>? StatusUpdated;

        /// <summary>
        /// Event raised when a search operation is completed.
        /// </summary>
        internal event EventHandler<SearchResultEventArgs>? SearchCompleted;

        /// <summary>
        /// Initializes a new instance of the LogViewerController.
        /// </summary>
        /// <param name="model">The log viewer model containing state and configuration.</param>
        internal LogViewerController(LogViewerModel model)
        {
            _model = model ?? throw new ArgumentNullException(nameof(model));
            _refreshTimer = new System.Windows.Forms.Timer
            {
                Interval = 1000
            };
            _refreshTimer.Tick += RefreshTimer_Tick;
        }

        /// <summary>
        /// Initializes the controller and loads initial log content.
        /// </summary>
        internal void Initialize()
        {
            if (string.IsNullOrEmpty(_model.LogFilePath))
            {
                return;
            }

            LoadInitialLogContent();
            StartRealTimeMonitoring();
        }

        /// <summary>
        /// Updates the log level filter and triggers a refresh of the display.
        /// </summary>
        /// <param name="level">The log level to update.</param>
        /// <param name="isSelected">Whether the log level should be included in the filter.</param>
        internal void SetLogLevelFilter(LogLevel level, bool isSelected)
        {
            if (isSelected)
            {
                _model.SelectedLogLevels.Add(level);
            }
            else
            {
                _model.SelectedLogLevels.Remove(level);
            }

            RefreshLogDisplay();
        }

        /// <summary>
        /// Sets all log levels selection state at once.
        /// </summary>
        /// <param name="isSelected">Whether all log levels should be selected or deselected.</param>
        internal void SetAllLogLevels(bool isSelected)
        {
            if (isSelected)
            {
                _model.SelectedLogLevels = [LogLevel.Error, LogLevel.Warning, LogLevel.Info, LogLevel.Debug, LogLevel.Default];
            }
            else
            {
                _model.SelectedLogLevels.Clear();
            }

            RefreshLogDisplay();
        }

        /// <summary>
        /// Toggles auto-scroll functionality for new log entries.
        /// </summary>
        /// <param name="enabled">Whether auto-scroll should be enabled.</param>
        internal void SetAutoScroll(bool enabled)
        {
            _model.AutoScroll = enabled;
        }

        /// <summary>
        /// Searches for a term in the logs and triggers search completed event.
        /// </summary>
        /// <param name="searchTerm">The term to search for.</param>
        /// <param name="findNext">Whether this is a find-next operation.</param>
        internal void SearchLogs(string searchTerm, bool findNext = false)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                OnSearchCompleted(new SearchResultEventArgs
                {
                    Success = false,
                    Message = "Please enter a search term.",
                    ShowDialog = true
                });
                return;
            }

            _model.LastSearchTerm = searchTerm;

            OnSearchCompleted(new SearchResultEventArgs
            {
                SearchTerm = searchTerm,
                IsFirstSearch = !findNext,
                Success = true
            });
        }

        /// <summary>
        /// Gets the most recent log file from the service directory.
        /// </summary>
        /// <param name="serviceDirectory">The directory where the service is installed.</param>
        /// <returns>Path to the most recent log file, or null if none found.</returns>
        internal static string? GetMostRecentLogFile(string serviceDirectory)
        {
            string logsDirectory = Path.Combine(serviceDirectory, "logs");

            if (!Directory.Exists(logsDirectory))
            {
                return null;
            }

            var logFiles = Directory.GetFiles(logsDirectory, "*.log");
            if (logFiles.Length == 0)
            {
                return null;
            }

            return logFiles.OrderByDescending(f => File.GetLastWriteTime(f)).First();
        }

        private void LoadInitialLogContent()
        {
            if (!File.Exists(_model.LogFilePath))
            {
                return;
            }

            var lines = ReadLogFileLines();
            var filteredLines = lines.Where(_model.ShouldDisplayLine).ToList();

            OnNewLogLines(new LogLinesEventArgs
            {
                Lines = filteredLines,
                ClearFirst = true,
                AutoScroll = _model.AutoScroll
            });

            var fileInfo = new FileInfo(_model.LogFilePath);
            _model.LastFilePosition = fileInfo.Length;
        }

        private void StartRealTimeMonitoring()
        {
            _model.IsMonitoring = true;
            _refreshTimer.Start();
            OnStatusUpdated("Real-time monitoring active");
        }

        private void RefreshTimer_Tick(object? sender, EventArgs e)
        {
            CheckForNewLogEntries();
        }

        private void CheckForNewLogEntries()
        {
            if (!File.Exists(_model.LogFilePath))
            {
                return;
            }

            var fileInfo = new FileInfo(_model.LogFilePath);
            if (fileInfo.Length > _model.LastFilePosition)
            {
                var newLines = ReadNewLogLines(_model.LastFilePosition);
                if (newLines.Count > 0)
                {
                    var filteredLines = newLines.Where(_model.ShouldDisplayLine).ToList();

                    if (filteredLines.Count > 0)
                    {
                        OnNewLogLines(new LogLinesEventArgs
                        {
                            Lines = filteredLines,
                            ClearFirst = false,
                            AutoScroll = _model.AutoScroll
                        });
                    }

                    _model.LastFilePosition = fileInfo.Length;
                    OnStatusUpdated($"Updated: {DateTime.Now:HH:mm:ss}");
                }
            }
        }

        private void RefreshLogDisplay()
        {
            if (!File.Exists(_model.LogFilePath))
            {
                return;
            }

            var lines = ReadLogFileLines();
            var filteredLines = lines.Where(_model.ShouldDisplayLine).ToList();

            OnNewLogLines(new LogLinesEventArgs
            {
                Lines = filteredLines,
                ClearFirst = true,
                AutoScroll = _model.AutoScroll
            });
        }

        private List<string> ReadLogFileLines()
        {
            try
            {
                using var stream = new FileStream(_model.LogFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var reader = new StreamReader(stream);

                var lines = new List<string>();
                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    lines.Add(line);
                }

                return lines;
            }
            catch (IOException)
            {
                return new List<string> { "Error: Unable to read log file" };
            }
        }

        private List<string> ReadNewLogLines(long startPosition)
        {
            var newLines = new List<string>();

            try
            {
                using var stream = new FileStream(_model.LogFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                stream.Seek(startPosition, SeekOrigin.Begin);
                using var reader = new StreamReader(stream);

                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    newLines.Add(line);
                }
            }
            catch (IOException) { }

            return newLines;
        }

        private void OnNewLogLines(LogLinesEventArgs args)
        {
            NewLogLines?.Invoke(this, args);
        }

        private void OnStatusUpdated(string message)
        {
            StatusUpdated?.Invoke(this, message);
        }

        private void OnSearchCompleted(SearchResultEventArgs args)
        {
            SearchCompleted?.Invoke(this, args);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _refreshTimer?.Stop();
                _refreshTimer?.Dispose();
                _disposed = true;
            }
        }
    }

    /// <summary>
    /// Event arguments for new log lines being added to the display.
    /// </summary>
    internal sealed class LogLinesEventArgs : EventArgs
    {
        /// <summary>
        /// Gets or sets the list of log lines to display.
        /// </summary>
        internal List<string> Lines { get; set; } = new();

        /// <summary>
        /// Gets or sets whether the display should be cleared before adding these lines.
        /// </summary>
        internal bool ClearFirst { get; set; }

        /// <summary>
        /// Gets or sets whether auto-scroll should be applied after adding the lines.
        /// </summary>
        internal bool AutoScroll { get; set; }
    }

    /// <summary>
    /// Event arguments for search operation results.
    /// </summary>
    internal sealed class SearchResultEventArgs : EventArgs
    {
        /// <summary>
        /// Gets or sets the search term that was used.
        /// </summary>
        internal string SearchTerm { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets whether the search operation was successful.
        /// </summary>
        internal bool Success { get; set; }

        /// <summary>
        /// Gets or sets a message describing the search result.
        /// </summary>
        internal string Message { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets whether a dialog should be shown to the user.
        /// </summary>
        internal bool ShowDialog { get; set; }

        /// <summary>
        /// Gets or sets whether this is the first search (as opposed to find-next).
        /// </summary>
        internal bool IsFirstSearch { get; set; }

        /// <summary>
        /// Gets or sets the position of the found result, or -1 if not found.
        /// </summary>
        internal int Position { get; set; } = -1;
    }
}
