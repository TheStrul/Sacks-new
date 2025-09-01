namespace SacksDataLayer.Configuration
{
    /// <summary>
    /// Configuration settings for performance monitoring and logging
    /// </summary>
    public class PerformanceMonitoringSettings
    {
        public const string SectionName = "PerformanceMonitoring";

        /// <summary>
        /// Enable detailed timing measurements for all operations
        /// </summary>
        public bool EnableDetailedTiming { get; set; } = true;

        /// <summary>
        /// Enable correlation ID tracking across all operations
        /// </summary>
        public bool EnableCorrelationTracking { get; set; } = true;

        /// <summary>
        /// Log operations that take longer than the threshold
        /// </summary>
        public bool LogSlowOperations { get; set; } = true;

        /// <summary>
        /// Threshold in milliseconds for slow operation logging
        /// </summary>
        public int SlowOperationThresholdMs { get; set; } = 1000;

        /// <summary>
        /// Enable memory usage tracking
        /// </summary>
        public bool EnableMemoryTracking { get; set; } = true;

        /// <summary>
        /// Enable detailed file processing metrics
        /// </summary>
        public bool EnableFileProcessingMetrics { get; set; } = true;
    }
}
