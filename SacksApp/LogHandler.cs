// <copyright file="LogHandler.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace QMobileDeviceServiceMenu
{
    /// <summary>
    /// Centralized handler for all console output operations with color-coded log levels.
    /// This is the only class that should directly write to Console with colors for consistent UI presentation.
    /// </summary>
    internal static class LogHandler
    {
        /// <summary>
        /// Writes a log line to the console with automatic or explicit log level color coding.
        /// </summary>
        /// <param name="line">The text line to write.</param>
        /// <param name="explicitLevel">Optional explicit log level to override automatic detection.</param>
        public static void WriteLogLine(string line, LogLevel? explicitLevel = null)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                Console.WriteLine();
                return;
            }

            var level = explicitLevel ?? LogViewerModel.DetectLogLevel(line);
            SetConsoleColor(level);
            Console.WriteLine(line);
            Console.ResetColor();
        }

        /// <summary>
        /// Writes a message to the console with the specified log level formatting.
        /// </summary>
        /// <param name="message">Message to display.</param>
        /// <param name="level">Log level for color formatting.</param>
        public static void WriteMessage(string message, LogLevel level = LogLevel.Info)
        {
            WriteLogLine(message, level);
        }

        /// <summary>
        /// Writes an error message in red color.
        /// </summary>
        /// <param name="message">Error message to display.</param>
        public static void WriteError(string message) => WriteLogLine(message, LogLevel.Error);

        /// <summary>
        /// Writes a warning message in yellow color.
        /// </summary>
        /// <param name="message">Warning message to display.</param>
        public static void WriteWarning(string message) => WriteLogLine(message, LogLevel.Warning);

        /// <summary>
        /// Writes an info message in white color.
        /// </summary>
        /// <param name="message">Info message to display.</param>
        public static void WriteInfo(string message) => WriteLogLine(message, LogLevel.Info);

        /// <summary>
        /// Writes a success message (legacy method for backward compatibility).
        /// </summary>
        /// <param name="message">Success message to display.</param>
        public static void WriteSuccess(string message) => WriteLogLine(message, LogLevel.HighLight);

        /// <summary>
        /// Writes a highlighted message (legacy method for backward compatibility).
        /// </summary>
        /// <param name="message">Message to highlight.</param>
        public static void WriteHighlight(string message) => WriteLogLine(message, LogLevel.HighLight);

        /// <summary>
        /// Clears the console screen.
        /// </summary>
        public static void Clear() => Console.Clear();

        /// <summary>
        /// Writes a separator line to the console.
        /// </summary>
        /// <param name="length">Length of the separator line.</param>
        /// <param name="character">Character to use for the separator.</param>
        public static void WriteSeparator(int length = 80, char character = '=') => WriteLogLine(new string(character, length));

        /// <summary>
        /// Displays a message and waits for any key press.
        /// </summary>
        /// <param name="message">Message to display before waiting.</param>
        /// <returns>The key that was pressed.</returns>
        public static ConsoleKeyInfo WaitForKey(string message = "Press any key to continue...")
        {
            WriteMessage(message, LogLevel.Info);
            return Console.ReadKey(true);
        }

        /// <summary>
        /// Reads a line of input from the console with optional prompt.
        /// </summary>
        /// <param name="prompt">Optional prompt to display before reading input.</param>
        /// <returns>The input string, or null if no input was provided.</returns>
        public static string? ReadLine(string? prompt = null)
        {
            if (!string.IsNullOrEmpty(prompt))
            {
                Console.Write(prompt);
            }

            return Console.ReadLine();
        }

        private static void SetConsoleColor(LogLevel level)
        {
            Console.ForegroundColor = level switch
            {
                LogLevel.Error => ConsoleColor.Red,
                LogLevel.Warning => ConsoleColor.Yellow,
                LogLevel.Debug => ConsoleColor.Gray,
                LogLevel.Info => ConsoleColor.White,
                LogLevel.Success => ConsoleColor.Green,
                LogLevel.HighLight => ConsoleColor.Cyan,
                _ => ConsoleColor.White,
            };
        }
    }
}
