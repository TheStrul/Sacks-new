using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SacksDataLayer.Configuration;
using System.Diagnostics;

namespace SacksDataLayer.Services.Interfaces
{
    /// <summary>
    /// Interface for performance monitoring and correlation tracking
    /// </summary>
    public interface IPerformanceMonitoringService
    {
        /// <summary>
        /// Starts a new performance operation with correlation tracking
        /// </summary>
        IOperationTracker StartOperation(string operationName, string? correlationId = null, object? metadata = null);

        /// <summary>
        /// Gets the current correlation ID
        /// </summary>
        string GetCurrentCorrelationId();

        /// <summary>
        /// Sets the correlation ID for the current operation
        /// </summary>
        void SetCorrelationId(string correlationId);

        /// <summary>
        /// Logs performance metrics for an operation
        /// </summary>
        void LogPerformanceMetrics(string operationName, long durationMs, object? metadata = null);

        /// <summary>
        /// Gets current memory usage statistics
        /// </summary>
        MemoryUsageStats GetMemoryUsage();

        /// <summary>
        /// Logs memory usage if tracking is enabled
        /// </summary>
        void LogMemoryUsage(string context);
    }

    /// <summary>
    /// Interface for tracking individual operations
    /// </summary>
    public interface IOperationTracker : IDisposable
    {
        /// <summary>
        /// The correlation ID for this operation
        /// </summary>
        string CorrelationId { get; }

        /// <summary>
        /// The operation name
        /// </summary>
        string OperationName { get; }

        /// <summary>
        /// Elapsed time since operation started
        /// </summary>
        TimeSpan Elapsed { get; }

        /// <summary>
        /// Adds metadata to the operation
        /// </summary>
        void AddMetadata(string key, object value);

        /// <summary>
        /// Marks the operation as completed successfully
        /// </summary>
        void Complete();

        /// <summary>
        /// Marks the operation as failed with an error
        /// </summary>
        void Fail(Exception exception);
    }

    /// <summary>
    /// Memory usage statistics
    /// </summary>
    public class MemoryUsageStats
    {
        public long WorkingSetBytes { get; set; }
        public long PrivateMemoryBytes { get; set; }
        public long GCTotalMemoryBytes { get; set; }
        public int Gen0Collections { get; set; }
        public int Gen1Collections { get; set; }
        public int Gen2Collections { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
