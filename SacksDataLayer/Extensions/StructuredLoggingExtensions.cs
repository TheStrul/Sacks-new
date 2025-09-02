using Microsoft.Extensions.Logging;
using SacksDataLayer.Services.Interfaces;
using System;

namespace SacksDataLayer.Extensions
{
    /// <summary>
    /// Extension methods for structured logging with performance monitoring
    /// </summary>
    public static class StructuredLoggingExtensions
    {
        /// <summary>
        /// Logs file processing start with structured data
        /// </summary>
        public static void LogFileProcessingStart(this ILogger logger, string fileName, string supplierName, string correlationId)
        {
            logger.LogInformation("📁 File processing started: {FileName} for supplier {SupplierName} [CorrelationId: {CorrelationId}]",
                fileName, supplierName, correlationId);
        }

        /// <summary>
        /// Logs file processing completion with metrics
        /// </summary>
        public static void LogFileProcessingComplete(this ILogger logger, string fileName, int recordsProcessed, 
            long durationMs, string correlationId)
        {
            logger.LogInformation("✅ File processing completed: {FileName} - {RecordsProcessed} records in {DurationMs}ms [CorrelationId: {CorrelationId}]",
                fileName, recordsProcessed, durationMs, correlationId);
        }

        /// <summary>
        /// Logs database operation with EAN context
        /// </summary>
        public static void LogDatabaseOperation(this ILogger logger, string operation, string? ean, 
            long durationMs, string correlationId)
        {
            logger.LogInformation("🗄️ Database {Operation}: EAN={EAN} in {DurationMs}ms [CorrelationId: {CorrelationId}]",
                operation, ean ?? "N/A", durationMs, correlationId);
        }

        /// <summary>
        /// Logs bulk database operation with batch details
        /// </summary>
        public static void LogBulkDatabaseOperation(this ILogger logger, string operation, int itemCount, 
            long durationMs, string correlationId)
        {
            logger.LogInformation("🗄️ Bulk {Operation}: {ItemCount} items in {DurationMs}ms [CorrelationId: {CorrelationId}]",
                operation, itemCount, durationMs, correlationId);
        }

        /// <summary>
        /// Logs supplier configuration detection
        /// </summary>
        public static void LogSupplierDetection(this ILogger logger, string fileName, string detectedSupplier, 
            string detectionMethod, string correlationId)
        {
            logger.LogInformation("🔍 Supplier detected: {FileName} → {DetectedSupplier} via {DetectionMethod} [CorrelationId: {CorrelationId}]",
                fileName, detectedSupplier, detectionMethod, correlationId);
        }

        /// <summary>
        /// Logs data validation results
        /// </summary>
        public static void LogValidationResult(this ILogger logger, string context, int validItems, 
            int invalidItems, string correlationId)
        {
            if (invalidItems > 0)
            {
                logger.LogWarning("⚠️ Validation completed: {Context} - {ValidItems} valid, {InvalidItems} invalid [CorrelationId: {CorrelationId}]",
                    context, validItems, invalidItems, correlationId);
            }
            else
            {
                logger.LogInformation("✅ Validation passed: {Context} - {ValidItems} items [CorrelationId: {CorrelationId}]",
                    context, validItems, correlationId);
            }
        }

        /// <summary>
        /// Logs error with context
        /// </summary>
        public static void LogErrorWithContext(this ILogger logger, Exception exception, string operation, 
            object? context, string correlationId)
        {
            if (exception == null)
                throw new ArgumentNullException(nameof(exception));

            logger.LogError(exception, "❌ Error in {Operation}: {ErrorMessage} | Context: {Context} [CorrelationId: {CorrelationId}]",
                operation, exception.Message, context ?? "N/A", correlationId);
        }

        /// <summary>
        /// Logs performance warning for slow operations
        /// </summary>
        public static void LogPerformanceWarning(this ILogger logger, string operation, long actualDurationMs, 
            long expectedDurationMs, string correlationId)
        {
            logger.LogWarning("🐌 Performance issue: {Operation} took {ActualDurationMs}ms (expected <{ExpectedDurationMs}ms) [CorrelationId: {CorrelationId}]",
                operation, actualDurationMs, expectedDurationMs, correlationId);
        }

        /// <summary>
        /// Logs batch processing metrics
        /// </summary>
        public static void LogBatchMetrics(this ILogger logger, string batchType, int batchSize, int currentBatch, 
            int totalBatches, long batchDurationMs, string correlationId)
        {
            logger.LogInformation("📊 Batch {CurrentBatch}/{TotalBatches}: {BatchType} - {BatchSize} items in {BatchDurationMs}ms [CorrelationId: {CorrelationId}]",
                currentBatch, totalBatches, batchType, batchSize, batchDurationMs, correlationId);
        }

        /// <summary>
        /// Logs configuration loading or reloading
        /// </summary>
        public static void LogConfigurationLoad(this ILogger logger, string configurationType, int itemCount, 
            string source, string correlationId)
        {
            logger.LogInformation("⚙️ Configuration loaded: {ConfigurationType} - {ItemCount} items from {Source} [CorrelationId: {CorrelationId}]",
                configurationType, itemCount, source, correlationId);
        }

        /// <summary>
        /// Logs cache operations
        /// </summary>
        public static void LogCacheOperation(this ILogger logger, string operation, string cacheType, 
            int itemCount, long durationMs, string correlationId)
        {
            logger.LogInformation("🧠 Cache {Operation}: {CacheType} - {ItemCount} items in {DurationMs}ms [CorrelationId: {CorrelationId}]",
                operation, cacheType, itemCount, durationMs, correlationId);
        }
    }
}
