using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SacksDataLayer.Configuration;
using SacksDataLayer.Services.Interfaces;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace SacksDataLayer.Services.Implementations
{
    /// <summary>
    /// Implementation of performance monitoring service with correlation tracking
    /// </summary>
    public class PerformanceMonitoringService : IPerformanceMonitoringService
    {
        private readonly ILogger<PerformanceMonitoringService> _logger;
        private readonly PerformanceMonitoringSettings _settings;
        private readonly AsyncLocal<string> _correlationId = new();
        private readonly ConcurrentDictionary<string, long> _operationCounts = new();

        public PerformanceMonitoringService(
            ILogger<PerformanceMonitoringService> logger,
            IOptions<PerformanceMonitoringSettings> settings)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        }

        public IOperationTracker StartOperation(string operationName, string? correlationId = null, object? metadata = null)
        {
            if (string.IsNullOrEmpty(operationName))
                throw new ArgumentException("Operation name cannot be null or empty", nameof(operationName));

            var finalCorrelationId = correlationId ?? GetCurrentCorrelationId();
            
            if (_settings.EnableCorrelationTracking)
            {
                SetCorrelationId(finalCorrelationId);
            }

            var tracker = new OperationTracker(operationName, finalCorrelationId, _settings, _logger);
            
            if (metadata != null)
            {
                foreach (var prop in metadata.GetType().GetProperties())
                {
                    tracker.AddMetadata(prop.Name, prop.GetValue(metadata) ?? "null");
                }
            }

            if (_settings.EnableDetailedTiming)
            {
                _logger.LogInformation("🚀 Operation started: {OperationName} [CorrelationId: {CorrelationId}]", 
                    operationName, finalCorrelationId);
            }

            return tracker;
        }

        public string GetCurrentCorrelationId()
        {
            return _correlationId.Value ?? GenerateCorrelationId();
        }

        public void SetCorrelationId(string correlationId)
        {
            if (string.IsNullOrEmpty(correlationId))
                throw new ArgumentException("Correlation ID cannot be null or empty", nameof(correlationId));

            _correlationId.Value = correlationId;
        }

        public void LogPerformanceMetrics(string operationName, long durationMs, object? metadata = null)
        {
            if (string.IsNullOrEmpty(operationName))
                return;

            // Track operation count
            _operationCounts.AddOrUpdate(operationName, 1, (key, oldValue) => oldValue + 1);

            var correlationId = GetCurrentCorrelationId();
            
            if (_settings.LogSlowOperations && durationMs >= _settings.SlowOperationThresholdMs)
            {
                _logger.LogWarning("🐌 Slow operation detected: {OperationName} took {DurationMs}ms [CorrelationId: {CorrelationId}]", 
                    operationName, durationMs, correlationId);
            }
            else if (_settings.EnableDetailedTiming)
            {
                _logger.LogInformation("⏱️ Operation completed: {OperationName} in {DurationMs}ms [CorrelationId: {CorrelationId}]", 
                    operationName, durationMs, correlationId);
            }

            // Log metadata if provided
            if (metadata != null && _settings.EnableDetailedTiming)
            {
                _logger.LogDebug("📊 Operation metadata: {OperationName} - {Metadata} [CorrelationId: {CorrelationId}]", 
                    operationName, metadata, correlationId);
            }
        }

        public MemoryUsageStats GetMemoryUsage()
        {
            var process = Process.GetCurrentProcess();
            
            return new MemoryUsageStats
            {
                WorkingSetBytes = process.WorkingSet64,
                PrivateMemoryBytes = process.PrivateMemorySize64,
                GCTotalMemoryBytes = GC.GetTotalMemory(false),
                Gen0Collections = GC.CollectionCount(0),
                Gen1Collections = GC.CollectionCount(1),
                Gen2Collections = GC.CollectionCount(2)
            };
        }

        public void LogMemoryUsage(string context)
        {
            if (!_settings.EnableMemoryTracking)
                return;

            var stats = GetMemoryUsage();
            var correlationId = GetCurrentCorrelationId();

            _logger.LogInformation("💾 Memory usage - {Context}: Working={WorkingSetMB:F1}MB, Private={PrivateMemoryMB:F1}MB, GC={GCMemoryMB:F1}MB [CorrelationId: {CorrelationId}]",
                context,
                stats.WorkingSetBytes / 1024.0 / 1024.0,
                stats.PrivateMemoryBytes / 1024.0 / 1024.0,
                stats.GCTotalMemoryBytes / 1024.0 / 1024.0,
                correlationId);
        }

        private static string GenerateCorrelationId()
        {
            return $"op_{DateTime.UtcNow:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}";
        }
    }

    /// <summary>
    /// Implementation of operation tracker for monitoring individual operations
    /// </summary>
    internal sealed class OperationTracker : IOperationTracker
    {
        private readonly Stopwatch _stopwatch;
        private readonly ILogger _logger;
        private readonly PerformanceMonitoringSettings _settings;
        private readonly Dictionary<string, object> _metadata = new();
        private bool _disposed;
        private bool _completed;

        public string CorrelationId { get; }
        public string OperationName { get; }
        public TimeSpan Elapsed => _stopwatch.Elapsed;

        public OperationTracker(string operationName, string correlationId, PerformanceMonitoringSettings settings, ILogger logger)
        {
            OperationName = operationName ?? throw new ArgumentNullException(nameof(operationName));
            CorrelationId = correlationId ?? throw new ArgumentNullException(nameof(correlationId));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            _stopwatch = Stopwatch.StartNew();
        }

        public void AddMetadata(string key, object value)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Key cannot be null or empty", nameof(key));

            _metadata[key] = value ?? "null";
        }

        public void Complete()
        {
            if (_completed || _disposed)
                return;

            _completed = true;
            _stopwatch.Stop();

            if (_settings.EnableDetailedTiming)
            {
                var durationMs = _stopwatch.ElapsedMilliseconds;
                
                if (_settings.LogSlowOperations && durationMs >= _settings.SlowOperationThresholdMs)
                {
                    _logger.LogWarning("✅🐌 Operation completed (SLOW): {OperationName} took {DurationMs}ms [CorrelationId: {CorrelationId}]", 
                        OperationName, durationMs, CorrelationId);
                }
                else
                {
                    _logger.LogInformation("✅ Operation completed: {OperationName} in {DurationMs}ms [CorrelationId: {CorrelationId}]", 
                        OperationName, durationMs, CorrelationId);
                }

                // Log metadata if any
                if (_metadata.Count > 0)
                {
                    _logger.LogDebug("📊 Final metadata for {OperationName}: {Metadata} [CorrelationId: {CorrelationId}]", 
                        OperationName, _metadata, CorrelationId);
                }
            }
        }

        public void Fail(Exception exception)
        {
            if (_completed || _disposed)
                return;

            _completed = true;
            _stopwatch.Stop();

            _logger.LogError(exception, "❌ Operation failed: {OperationName} after {DurationMs}ms [CorrelationId: {CorrelationId}]", 
                OperationName, _stopwatch.ElapsedMilliseconds, CorrelationId);

            // Log metadata for debugging
            if (_metadata.Count > 0)
            {
                _logger.LogError("🐛 Error context metadata: {Metadata} [CorrelationId: {CorrelationId}]", 
                    _metadata, CorrelationId);
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            if (!_completed)
            {
                Complete(); // Auto-complete if not explicitly completed
            }

            _disposed = true;
        }
    }
}
