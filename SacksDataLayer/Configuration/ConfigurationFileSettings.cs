using System.ComponentModel.DataAnnotations;

namespace SacksDataLayer.Configuration
{
    /// <summary>
    /// Configuration settings for external configuration path(s).
    /// </summary>
    public class ConfigurationFileSettings
    {
        /// <summary>
        /// Folder name (relative to base directory) where configuration files reside, e.g., "Configuration"
        /// </summary>
        [Required]
        public string ConfigurationFolder { get; set; } = "Configuration";

        /// <summary>
        /// Mandatory main file name inside the configuration folder, e.g., "supplier-formats.json"
        /// </summary>
        [Required]
        public string MainFileName { get; set; } = "supplier-formats.json";
    }

    /// <summary>
    /// Configuration settings for logging behavior
    /// </summary>
    public class LoggingSettings
    {
        /// <summary>
        /// Whether to delete existing log files on application startup
        /// </summary>
        public bool DeleteLogFilesOnStartup { get; set; } = false;

        /// <summary>
        /// Paths to log directories to clean up on startup (relative to solution root)
        /// </summary>
        public string[] LogFilePaths { get; set; } = Array.Empty<string>();
    }
}
