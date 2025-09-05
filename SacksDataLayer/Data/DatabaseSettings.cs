namespace SacksDataLayer.Configuration
{
    /// <summary>
    /// Configuration settings for database connections and behavior
    /// </summary>
    public class DatabaseSettings
    {
        public const string SectionName = "DatabaseSettings";

        /// <summary>
        /// Database provider (SqlServer, etc.)
        /// </summary>
        public string Provider { get; set; } = "SqlServer";

        /// <summary>
        /// Command timeout in seconds
        /// </summary>
        public int CommandTimeout { get; set; } = 30;

        /// <summary>
        /// Enable automatic retry on transient failures
        /// </summary>
        public bool RetryOnFailure { get; set; } = true;

        /// <summary>
        /// Maximum number of retry attempts
        /// </summary>
        public int MaxRetryCount { get; set; } = 3;

        /// <summary>
        /// Enable sensitive data logging (for development only)
        /// </summary>
        public bool EnableSensitiveDataLogging { get; set; } = false;
    }
}
