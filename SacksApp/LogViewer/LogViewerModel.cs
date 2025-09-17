// <copyright file="LogViewerModel.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System.Globalization;

namespace QMobileDeviceServiceMenu
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
    internal sealed record LogLevelDefinition(
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
                if (line.Contains(pattern))
                {
                    return true;
                }
            }

            return false;
        }
    }

    /// <summary>
    /// Data model for the log viewer, containing state and settings for log file monitoring and display.
    /// </summary>
    internal sealed class LogViewerModel
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
        internal HashSet<LogLevel> SelectedLogLevels { get; set; } = new HashSet<LogLevel>
        {
            LogLevel.Error,
            LogLevel.Warning,
            LogLevel.Info,
        };

        /// <summary>
        /// Microsoft .NET logging level definitions with patterns and colors for log parsing.
        /// Supports both Serilog formats: [timestamp LEVEL]: and [timestamp LEVEL] :
        /// </summary>
        internal static readonly Dictionary<LogLevel, LogLevelDefinition> LogLevelDefinitions = new()
        {
            [LogLevel.Error] = new(LogLevel.Error, "Error", "ERR", Color.Red, new[] { " ERR]:", " ERR] :" }),
            [LogLevel.Warning] = new(LogLevel.Warning, "Warning", "WRN", Color.DarkOrchid, new[] { " WRN]:", " WRN] :" }),
            [LogLevel.Info] = new(LogLevel.Info, "Info", "INF", Color.DarkBlue, new[] { " INF]:", " INF] :" }),
            [LogLevel.Debug] = new(LogLevel.Debug, "Debug", "DBG", Color.DimGray, new[] { " DBG]:", " DBG] :" }),
            [LogLevel.Default] = new(LogLevel.Default, "Default", "DEF", Color.Black, Array.Empty<string>())
        };

        /// <summary>
        /// Available log level filters for UI controls
        /// </summary>
        internal static readonly string[] LogLevelFilters =
        {
            "All", "Error", "Warning", "Info", "Debug"
        };

        /// <summary>
        /// Log level color mappings for UI display
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
        internal string LogFileName => string.IsNullOrEmpty(LogFilePath) ? "Log Viewer" : Path.GetFileName(LogFilePath);

        /// <summary>
        /// Detects the log level of a given line for Microsoft .NET logging
        /// Format: [HH:mm:ss.fff LEVEL]: Message or [yyyy-MM-dd HH:mm:ss.fff +offset LEVEL] : Message
        /// </summary>
        internal static LogLevel DetectLogLevel(string line)
        {
            if (string.IsNullOrEmpty(line))
            {
                return LogLevel.Default;
            }

            // Serilog patterns: [timestamp LEVEL] : or [timestamp LEVEL]:
            // Handle both formats with and without space after ]
            if (line.Contains(" INF]"))
            {
                return LogLevel.Info;
            }

            if (line.Contains(" ERR]"))
            {
                return LogLevel.Error;
            }

            if (line.Contains(" WRN]"))
            {
                return LogLevel.Warning;
            }

            if (line.Contains(" DBG]"))
            {
                return LogLevel.Debug;
            }

            return LogLevel.Default;
        }

        /// <summary>
        /// Determines if a log line should be displayed based on the selected filters
        /// </summary>
        internal bool ShouldDisplayLine(string line)
        {
            if (SelectedLogLevels.Count == 0 || SelectedLogLevels.Contains(LogLevel.All))
            {
                return true;
            }

            var lineLevel = DetectLogLevel(line);
            return SelectedLogLevels.Contains(lineLevel);
        }

        /// <summary>
        /// Gets the appropriate color for a log line
        /// </summary>
        internal Color GetLogLineColor(string line)
        {
            var level = DetectLogLevel(line);
            return LogLevelDefinitions.TryGetValue(level, out var definition)
                ? definition.Color
                : LogLevelDefinitions[LogLevel.Default].Color;
        }

        /// <summary>
        /// Gets the log level definition for a specific level
        /// </summary>
        internal static LogLevelDefinition GetLogLevelDefinition(LogLevel level)
        {
            return LogLevelDefinitions.TryGetValue(level, out var definition)
                ? definition
                : LogLevelDefinitions[LogLevel.Default];
        }

        /// <summary>
        /// Parses Microsoft .NET logging formatted lines to extract components
        /// Format: [HH:mm:ss.fff LEVEL]: Message
        /// </summary>
        internal static LogLineInfo ParseLogLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                return new LogLineInfo(line, LogLevel.Default, null);
            }

            // Microsoft .NET Logging pattern: [HH:mm:ss.fff LEVEL]: Message
            if (line.StartsWith('[') && line.Contains("]: "))
            {
                var timestampEndIndex = line.IndexOf(']');
                if (timestampEndIndex > 0)
                {
                    var headerPart = line.Substring(1, timestampEndIndex - 1); // Remove [ and ]
                    var messagePart = line.Substring(timestampEndIndex + 3); // Skip ]: 

                    var parts = headerPart.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 2)
                    {
                        var timestampStr = parts[0];
                        var levelStr = parts[1];

                        // Parse Microsoft .NET logging timestamp (HH:mm:ss.fff)
                        if (DateTime.TryParseExact(timestampStr, "HH:mm:ss.fff", null, DateTimeStyles.None, out var time))
                        {
                            var timestamp = DateTime.Today.Add(time.TimeOfDay);

                            // Map Microsoft .NET logging levels
                            var level = levelStr switch
                            {
                                "INF" => LogLevel.Info,
                                "ERR" => LogLevel.Error,
                                "WRN" => LogLevel.Warning,
                                "DBG" => LogLevel.Debug,
                                _ => LogLevel.Default
                            };

                            return new LogLineInfo(messagePart, level, timestamp);
                        }
                    }
                }
            }

            return new LogLineInfo(line, DetectLogLevel(line), null);
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
    internal sealed record LogLineInfo(
        string Message,
        LogLevel Level,
        DateTime? Timestamp);
}
