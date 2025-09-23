using System.ComponentModel.DataAnnotations;

namespace SacksDataLayer.Configuration
{
    /// <summary>
    /// Configuration settings for external configuration file paths
    /// </summary>
    public class ConfigurationFileSettings
    {
        /// <summary>
        /// Path to the supplier formats configuration file
        /// </summary>
        [Required]
        public string SupplierFormats { get; set; } = string.Empty;

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
