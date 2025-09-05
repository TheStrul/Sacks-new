// <copyright file="LogViewerModel.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System.Globalization;

namespace SacksConsoleApp
{
    /// <summary>
    /// Log level enumeration for type-safe handling in the log viewer.
    /// </summary>
    internal enum LogLevel
    {
        All,
        Error,
        Warning,
        Info,
        Debug,
        Default,
        Success,
        HighLight,
    }

    /// <summary>
    /// Represents a log level definition with its patterns and properties for log parsing and display.
    /// </summary>
    /// <param name="Level">The log level type.</param>
    /// <param name="DisplayName">Human-readable display name.</param>
    /// <param name="ShortCode">Short code for compact display.</param>
    /// <param name="Color">Color for UI display.</param>
    /// <param name="Patterns">Text patterns to match this log level in log lines.</param>
    internal record LogLevelDefinition(
        LogLevel Level,
        string DisplayName,
        string ShortCode,
        Color Color,
        string[] Patterns)
    {
        /// <summary>
        /// Checks if a log line matches this log level's patterns.
        /// </summary>
        /// <param name="line">The log line to check.</param>
        /// <returns>True if the line matches any of the patterns for this log level.</returns>
        internal bool MatchesLine(string line)
        {
            if (string.IsNullOrEmpty(line) || Patterns.Length == 0)
            {
                return false;
            }

            // Simple pattern matching - check if line contains any of the patterns
            foreach (var pattern in Patterns)
            {
                if (line.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }

    /// <summary>
    /// Parsed log line information with timestamp, level, and message components.
    /// </summary>
    /// <param name="Timestamp">The parsed timestamp from the log line.</param>
    /// <param name="Level">The detected log level.</param>
    /// <param name="Context">The source context (class/namespace).</param>
    /// <param name="Message">The log message text.</param>
    /// <param name="Exception">Exception information if present.</param>
    internal record LogLineInfo(
        DateTime? Timestamp,
        LogLevel Level,
        string Context,
        string Message,
        string? Exception);

    /// <summary>
    /// Data model for the log viewer, containing state and settings for log file monitoring and display.
    /// </summary>
    internal class LogViewerModel
    {
        /// <summary>
        /// Gets or sets the path to the log file being monitored.
        /// </summary>
        internal string LogFilePath { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the last known file position for incremental reading.
        /// </summary>
        internal long LastFilePosition { get; set; } = 0;

        /// <summary>
        /// Gets or sets whether auto-scroll is enabled for new log entries.
        /// </summary>
        internal bool AutoScroll { get; set; } = true;

        /// <summary>
        /// Gets or sets the last search term used in log filtering.
        /// </summary>
        internal string LastSearchTerm { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets whether real-time monitoring is active.
        /// </summary>
        internal bool IsMonitoring { get; set; } = false;

        /// <summary>
        /// Selected log levels for filtering (multiple selection support).
        /// </summary>
        internal HashSet<LogLevel> SelectedLogLevels { get; set; } = new()
        {
            LogLevel.Error, LogLevel.Warning, LogLevel.Info, LogLevel.Debug, LogLevel.Default
        };

        /// <summary>
        /// Serilog-based log level definitions with patterns and colors for log parsing.
        /// Aligned with appsettings.json Serilog configuration.
        /// </summary>
        internal static readonly Dictionary<LogLevel, LogLevelDefinition> LogLevelDefinitions = new()
        {
            [LogLevel.Error] = new(LogLevel.Error, "Error", "ERR", Color.Red, [" ERR] "]),
            [LogLevel.Warning] = new(LogLevel.Warning, "Warning", "WRN", Color.Orange, [" WRN] "]),
            [LogLevel.Info] = new(LogLevel.Info, "Information", "INF", Color.Green, [" INF] "]),
            [LogLevel.Debug] = new(LogLevel.Debug, "Debug", "DBG", Color.Gray, [" DBG] "]),
            [LogLevel.Default] = new(LogLevel.Default, "Default", "DEF", Color.LightGray, [])
        };

        /// <summary>
        /// Available log level filters for UI controls
        /// </summary>
        internal static readonly string[] LogLevelFilters =
        {
            "All", "Error", "Warning", "Info", "Debug"
        };

        /// <summary>
        /// Log level color mappings for UI display based on Serilog patterns
        /// </summary>
        internal static readonly Dictionary<string, Color> LogLevelColors =
            LogLevelDefinitions.ToDictionary(
                kvp => kvp.Value.ShortCode,
                kvp => kvp.Value.Color)
            .Concat(new[] {
                new KeyValuePair<string, Color>("Default", LogLevelDefinitions[LogLevel.Default].Color)
            }).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        /// <summary>
        /// Gets the filename from the log file path
        /// </summary>
        internal string LogFileName => string.IsNullOrEmpty(LogFilePath) ? "Unknown" : Path.GetFileName(LogFilePath);

        /// <summary>
        /// Detects the log level of a given line for Serilog output format.
        /// Expected format: [yyyy-MM-dd HH:mm:ss.fff zzz LEVEL] Context: Message
        /// </summary>
        /// <param name="line">The log line to analyze.</param>
        /// <returns>The detected log level, or Default if no specific level is found.</returns>
        internal static LogLevel DetectLogLevel(string line)
        {
            if (string.IsNullOrEmpty(line))
                return LogLevel.Default;

            // Check each defined log level for pattern matches
            foreach (var (level, definition) in LogLevelDefinitions)
            {
                if (level != LogLevel.Default && definition.MatchesLine(line))
                {
                    return level;
                }
            }

            return LogLevel.Default;
        }

        /// <summary>
        /// Determines if a log line should be displayed based on the selected filters
        /// </summary>
        /// <param name="line">The log line to check.</param>
        /// <returns>True if the line should be displayed, false otherwise.</returns>
        internal bool ShouldDisplayLine(string line)
        {
            if (SelectedLogLevels.Count == 0)
                return false;

            var detectedLevel = DetectLogLevel(line);
            return SelectedLogLevels.Contains(detectedLevel);
        }

        /// <summary>
        /// Gets the appropriate color for a log line based on detected log level
        /// </summary>
        /// <param name="line">The log line to analyze.</param>
        /// <returns>The color to use for displaying this log line.</returns>
        internal Color GetLogLineColor(string line)
        {
            var level = DetectLogLevel(line);
            var definition = GetLogLevelDefinition(level);
            return definition.Color;
        }

        /// <summary>
        /// Gets the log level definition for a specific level
        /// </summary>
        /// <param name="level">The log level to get the definition for.</param>
        /// <returns>The log level definition, or Default definition if not found.</returns>
        internal static LogLevelDefinition GetLogLevelDefinition(LogLevel level)
        {
            return LogLevelDefinitions.TryGetValue(level, out var definition) 
                ? definition 
                : LogLevelDefinitions[LogLevel.Default];
        }

        /// <summary>
        /// Parses Serilog formatted lines to extract components.
        /// Expected format: [yyyy-MM-dd HH:mm:ss.fff zzz LEVEL] Context: Message
        /// </summary>
        /// <param name="line">The log line to parse.</param>
        /// <returns>Parsed log line information, or null if parsing fails.</returns>
        internal static LogLineInfo? ParseLogLine(string line)
        {
            if (string.IsNullOrEmpty(line))
                return null;

            try
            {
                // Serilog format: [2024-01-15 10:30:45.123 +00:00 INF] SacksConsoleApp.Program: Message text
                var timestampEndIndex = line.IndexOf("] ");
                if (timestampEndIndex == -1 || !line.StartsWith('['))
                    return new LogLineInfo(null, LogLevel.Default, "", line, null);

                var timestampAndLevel = line.Substring(1, timestampEndIndex - 1);
                var messageStart = timestampEndIndex + 2;
                var remainingLine = messageStart < line.Length ? line.Substring(messageStart) : "";

                // Extract timestamp and level
                var parts = timestampAndLevel.Split(' ');
                if (parts.Length < 4)
                    return new LogLineInfo(null, LogLevel.Default, "", line, null);

                // Try to parse timestamp (first 3 parts: date, time, timezone)
                DateTime? timestamp = null;
                if (parts.Length >= 3)
                {
                    var timestampStr = $"{parts[0]} {parts[1]} {parts[2]}";
                    if (DateTime.TryParse(timestampStr, null, DateTimeStyles.RoundtripKind, out var parsedTime))
                    {
                        timestamp = parsedTime;
                    }
                }

                // Extract level (last part)
                var levelStr = parts[^1];
                var level = levelStr switch
                {
                    "ERR" => LogLevel.Error,
                    "WRN" => LogLevel.Warning,
                    "INF" => LogLevel.Info,
                    "DBG" => LogLevel.Debug,
                    _ => LogLevel.Default
                };

                // Extract context and message
                string context = "";
                string message = remainingLine;
                string? exception = null;

                var contextEndIndex = remainingLine.IndexOf(": ");
                if (contextEndIndex > 0)
                {
                    context = remainingLine.Substring(0, contextEndIndex);
                    message = remainingLine.Substring(contextEndIndex + 2);
                }

                // Check for exception information (simple heuristic)
                if (message.Contains("Exception") || message.Contains("Error:"))
                {
                    var lines = message.Split('\n');
                    if (lines.Length > 1)
                    {
                        message = lines[0];
                        exception = string.Join('\n', lines.Skip(1));
                    }
                }

                return new LogLineInfo(timestamp, level, context, message, exception);
            }
            catch
            {
                // If parsing fails, return the line as-is with default level
                return new LogLineInfo(null, LogLevel.Default, "", line, null);
            }
        }
    }
}
