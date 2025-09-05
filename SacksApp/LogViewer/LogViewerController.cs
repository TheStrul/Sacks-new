// <copyright file="LogViewerController.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Microsoft.Extensions.Logging;

namespace SacksConsoleApp
{
    /// <summary>
    /// Controller for log viewer business logic, separated from UI concerns.
    /// Manages real-time log file monitoring, filtering, and search functionality.
    /// </summary>
    internal class LogViewerController : IDisposable
    {
        private readonly LogViewerModel _model;
        private readonly System.Windows.Forms.Timer _refreshTimer;
        private readonly ILogger<LogViewerController>? _logger;
        private bool _disposed = false;

        /// <summary>
        /// Event triggered when new log lines are available for display
        /// </summary>
        internal event EventHandler<LogLinesEventArgs>? NewLogLines;

        /// <summary>
        /// Event triggered when the status message should be updated
        /// </summary>
        internal event EventHandler<string>? StatusUpdated;

        /// <summary>
        /// Event triggered when a search operation is completed
        /// </summary>
        internal event EventHandler<SearchResultEventArgs>? SearchCompleted;

        /// <summary>
        /// Initializes a new instance of the LogViewerController.
        /// </summary>
        /// <param name="model">The log viewer model containing state and configuration.</param>
        /// <param name="logger">Optional logger for diagnostics.</param>
        internal LogViewerController(LogViewerModel model, ILogger<LogViewerController>? logger = null)
        {
            _model = model ?? throw new ArgumentNullException(nameof(model));
            _logger = logger;
            
            _refreshTimer = new System.Windows.Forms.Timer
            {
                Interval = 1000, // Check for new log entries every second
                Enabled = false
            };
            _refreshTimer.Tick += RefreshTimer_Tick;
        }

        /// <summary>
        /// Initializes the controller and loads initial log content.
        /// </summary>
        internal void Initialize()
        {
            _logger?.LogDebug("Initializing log viewer controller for file: {LogFilePath}", _model.LogFilePath);
            
            LoadInitialLogContent();
            StartRealTimeMonitoring();
            
            _logger?.LogInformation("Log viewer controller initialized successfully");
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

            _logger?.LogDebug("Updated log level filter: {Level} = {IsSelected}", level, isSelected);
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

            _logger?.LogDebug("Set all log levels to: {IsSelected}", isSelected);
            RefreshLogDisplay();
        }

        /// <summary>
        /// Toggles auto-scroll functionality for new log entries.
        /// </summary>
        /// <param name="enabled">Whether auto-scroll should be enabled.</param>
        internal void SetAutoScroll(bool enabled)
        {
            _model.AutoScroll = enabled;
            _logger?.LogDebug("Auto-scroll set to: {Enabled}", enabled);
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
            _logger?.LogDebug("Searching logs for term: {SearchTerm}, FindNext: {FindNext}", searchTerm, findNext);

            OnSearchCompleted(new SearchResultEventArgs
            {
                SearchTerm = searchTerm,
                IsFirstSearch = !findNext,
                Success = true
            });
        }

        /// <summary>
        /// Gets the most recent log file from the logs directory.
        /// </summary>
        /// <param name="serviceDirectory">The directory where the application is located (optional).</param>
        /// <returns>Path to the most recent log file, or null if none found.</returns>
        internal static string? GetMostRecentLogFile(string? serviceDirectory = null)
        {
            // Default to current application directory if not specified
            var baseDirectory = serviceDirectory ?? AppContext.BaseDirectory;
            var logsDirectory = Path.Combine(baseDirectory, "logs");

            if (!Directory.Exists(logsDirectory))
            {
                // Try alternative locations
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
                    return null;
                }
            }

            // Look for Serilog-generated log files with pattern: sacks-*.log
            var logFiles = Directory.GetFiles(logsDirectory, "sacks-*.log");
            if (logFiles.Length == 0)
            {
                // Fallback to any .log files
                logFiles = Directory.GetFiles(logsDirectory, "*.log");
            }

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
                _logger?.LogWarning("Log file not found: {LogFilePath}", _model.LogFilePath);
                return;
            }

            try
            {
                var lines = ReadLogFileLines();
                var filteredLines = lines.Where(_model.ShouldDisplayLine).ToList();

                _logger?.LogDebug("Loaded {TotalLines} lines, {FilteredLines} after filtering", lines.Count, filteredLines.Count);

                OnNewLogLines(new LogLinesEventArgs
                {
                    Lines = filteredLines,
                    ClearFirst = true,
                    AutoScroll = false // Don't auto-scroll on initial load
                });

                OnStatusUpdated($"Loaded {filteredLines.Count} log entries");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error loading initial log content from {LogFilePath}", _model.LogFilePath);
                OnStatusUpdated($"Error loading log file: {ex.Message}");
            }
        }

        private void StartRealTimeMonitoring()
        {
            if (!File.Exists(_model.LogFilePath))
            {
                _logger?.LogWarning("Cannot start monitoring - log file not found: {LogFilePath}", _model.LogFilePath);
                return;
            }

            try
            {
                var fileInfo = new FileInfo(_model.LogFilePath);
                _model.LastFilePosition = fileInfo.Length;
                _model.IsMonitoring = true;
                _refreshTimer.Start();

                _logger?.LogDebug("Started real-time monitoring of {LogFilePath} from position {Position}", 
                    _model.LogFilePath, _model.LastFilePosition);
                OnStatusUpdated("Real-time monitoring started");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error starting real-time monitoring for {LogFilePath}", _model.LogFilePath);
                OnStatusUpdated($"Error starting monitoring: {ex.Message}");
            }
        }

        private void RefreshTimer_Tick(object? sender, EventArgs e)
        {
            if (!_model.IsMonitoring)
                return;

            try
            {
                CheckForNewLogEntries();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error checking for new log entries");
                OnStatusUpdated($"Monitoring error: {ex.Message}");
            }
        }

        private void CheckForNewLogEntries()
        {
            if (!File.Exists(_model.LogFilePath))
            {
                return;
            }

            var fileInfo = new FileInfo(_model.LogFilePath);
            
            // Check if file has grown
            if (fileInfo.Length > _model.LastFilePosition)
            {
                var newLines = ReadNewLogLines(_model.LastFilePosition);
                var filteredLines = newLines.Where(_model.ShouldDisplayLine).ToList();

                if (filteredLines.Count > 0)
                {
                    _logger?.LogDebug("Found {NewLines} new lines, {FilteredLines} after filtering", 
                        newLines.Count, filteredLines.Count);

                    OnNewLogLines(new LogLinesEventArgs
                    {
                        Lines = filteredLines,
                        ClearFirst = false,
                        AutoScroll = _model.AutoScroll
                    });
                }

                _model.LastFilePosition = fileInfo.Length;
            }
        }

        private void RefreshLogDisplay()
        {
            try
            {
                var lines = ReadLogFileLines();
                var filteredLines = lines.Where(_model.ShouldDisplayLine).ToList();

                _logger?.LogDebug("Refreshing display with {TotalLines} lines, {FilteredLines} after filtering", 
                    lines.Count, filteredLines.Count);

                OnNewLogLines(new LogLinesEventArgs
                {
                    Lines = filteredLines,
                    ClearFirst = true,
                    AutoScroll = false
                });

                OnStatusUpdated($"Display refreshed - {filteredLines.Count} entries");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error refreshing log display");
                OnStatusUpdated($"Refresh error: {ex.Message}");
            }
        }

        private List<string> ReadLogFileLines()
        {
            var lines = new List<string>();

            if (!File.Exists(_model.LogFilePath))
            {
                return lines;
            }

            try
            {
                using var fileStream = new FileStream(_model.LogFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var reader = new StreamReader(fileStream);
                
                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    lines.Add(line);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error reading log file: {LogFilePath}", _model.LogFilePath);
                throw;
            }

            return lines;
        }

        private List<string> ReadNewLogLines(long startPosition)
        {
            var lines = new List<string>();

            if (!File.Exists(_model.LogFilePath))
            {
                return lines;
            }

            try
            {
                using var fileStream = new FileStream(_model.LogFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                fileStream.Seek(startPosition, SeekOrigin.Begin);
                
                using var reader = new StreamReader(fileStream);
                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    lines.Add(line);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error reading new log lines from position {Position}", startPosition);
                throw;
            }

            return lines;
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
                _model.IsMonitoring = false;
                _disposed = true;
                
                _logger?.LogDebug("Log viewer controller disposed");
            }
        }
    }

    /// <summary>
    /// Event arguments for new log lines being added to the display.
    /// </summary>
    internal class LogLinesEventArgs : EventArgs
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
    internal class SearchResultEventArgs : EventArgs
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
