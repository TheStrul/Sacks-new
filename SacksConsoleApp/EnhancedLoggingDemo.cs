using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SacksDataLayer.Configuration;
using SacksDataLayer.Extensions;
using SacksDataLayer.Services.Interfaces;
using System;
using System.Threading.Tasks;

namespace SacksConsoleApp
{
    /// <summary>
    /// Demonstrates the new Enhanced Structured Logging & Performance Monitoring features
    /// </summary>
    public class EnhancedLoggingDemo
    {
        public static async Task RunDemoAsync()
        {
            Console.WriteLine("=== 🚀 Enhanced Structured Logging & Performance Monitoring Demo ===\n");

            // Create a minimal service provider for demo
            var services = new ServiceCollection();
            
            // Add logging with our enhanced configuration
            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Information);
            });

            // Add performance monitoring settings
            services.Configure<PerformanceMonitoringSettings>(settings =>
            {
                settings.EnableDetailedTiming = true;
                settings.EnableCorrelationTracking = true;
                settings.LogSlowOperations = true;
                settings.SlowOperationThresholdMs = 500; // Demo threshold
                settings.EnableMemoryTracking = true;
                settings.EnableFileProcessingMetrics = true;
            });

            // Add performance monitoring service
            services.AddPerformanceMonitoring();

            var serviceProvider = services.BuildServiceProvider();
            var performanceMonitor = serviceProvider.GetRequiredService<IPerformanceMonitoringService>();
            var logger = serviceProvider.GetRequiredService<ILogger<EnhancedLoggingDemo>>();

            Console.WriteLine("🎯 Demonstrating key features:\n");

            // Demo 1: Correlation ID tracking
            await DemoCorrelationTracking(performanceMonitor, logger);

            // Demo 2: Performance operation tracking
            await DemoPerformanceTracking(performanceMonitor, logger);

            // Demo 3: Structured logging extensions
            await DemoStructuredLogging(performanceMonitor, logger);

            // Demo 4: Memory tracking
            await DemoMemoryTracking(performanceMonitor, logger);

            // Demo 5: Slow operation detection
            await DemoSlowOperationDetection(performanceMonitor, logger);

            Console.WriteLine("\n✅ Enhanced Logging & Performance Monitoring Demo Complete!");
            Console.WriteLine("\n🎯 Key Benefits Demonstrated:");
            Console.WriteLine("   ✅ Correlation ID tracking across operations");
            Console.WriteLine("   ✅ Automatic performance timing with structured output");
            Console.WriteLine("   ✅ Memory usage monitoring at checkpoints");
            Console.WriteLine("   ✅ Slow operation detection and alerting");
            Console.WriteLine("   ✅ Structured logging with consistent format");
            Console.WriteLine("   ✅ Error context tracking with correlation IDs");
        }

        private static async Task DemoCorrelationTracking(IPerformanceMonitoringService monitor, ILogger logger)
        {
            Console.WriteLine("1️⃣ Correlation ID Tracking:");
            
            // Start an operation with correlation tracking
            using var operation = monitor.StartOperation("DemoFileProcessing", 
                metadata: new { FileName = "demo.xlsx", FileSize = "2.5MB" });

            var correlationId = monitor.GetCurrentCorrelationId();
            Console.WriteLine($"   📋 Generated Correlation ID: {correlationId}");
            
            logger.LogFileProcessingStart("demo.xlsx", "DEMO_SUPPLIER", correlationId);
            
            // Simulate some work
            await Task.Delay(100);
            
            logger.LogFileProcessingComplete("demo.xlsx", 1000, 100, correlationId);
            operation.Complete();
            
            Console.WriteLine();
        }

        private static async Task DemoPerformanceTracking(IPerformanceMonitoringService monitor, ILogger logger)
        {
            Console.WriteLine("2️⃣ Performance Operation Tracking:");
            
            using var operation = monitor.StartOperation("DatabaseBulkOperation", 
                metadata: new { OperationType = "BulkInsert", ItemCount = 500 });

            operation.AddMetadata("TableName", "Products");
            operation.AddMetadata("BatchSize", 100);
            
            // Simulate database work
            await Task.Delay(300);
            
            var correlationId = monitor.GetCurrentCorrelationId();
            logger.LogBulkDatabaseOperation("CREATE", 500, 300, correlationId);
            
            operation.Complete();
            Console.WriteLine();
        }

        private static async Task DemoStructuredLogging(IPerformanceMonitoringService monitor, ILogger logger)
        {
            Console.WriteLine("3️⃣ Structured Logging Extensions:");
            
            var correlationId = monitor.GetCurrentCorrelationId();
            
            // Demo various structured logging methods
            logger.LogSupplierDetection("DIOR_2025.xlsx", "DIOR", "FilePattern", correlationId);
            logger.LogValidationResult("ProductData", 950, 50, correlationId);
            logger.LogBatchMetrics("ProductProcessing", 100, 5, 10, 120, correlationId);
            logger.LogConfigurationLoad("SupplierFormats", 2, "supplier-formats.json", correlationId);
            
            await Task.Delay(50);
            Console.WriteLine();
        }

        private static async Task DemoMemoryTracking(IPerformanceMonitoringService monitor, ILogger logger)
        {
            Console.WriteLine("4️⃣ Memory Usage Tracking:");
            
            var correlationId = monitor.GetCurrentCorrelationId();
            
            // Get memory stats and log them
            var memStats = monitor.GetMemoryUsage();
            logger.LogMemoryCheckpoint("BeforeProcessing", memStats, correlationId);
            
            // Simulate memory usage
            var tempData = new byte[1024 * 1024]; // 1MB allocation
            await Task.Delay(100);
            
            memStats = monitor.GetMemoryUsage();
            logger.LogMemoryCheckpoint("AfterProcessing", memStats, correlationId);
            
            // Clean up
            tempData = null;
            GC.Collect();
            Console.WriteLine();
        }

        private static async Task DemoSlowOperationDetection(IPerformanceMonitoringService monitor, ILogger logger)
        {
            Console.WriteLine("5️⃣ Slow Operation Detection:");
            
            using var operation = monitor.StartOperation("SlowDatabaseQuery", 
                metadata: new { QueryType = "ComplexJoin", ExpectedMs = 200 });

            var correlationId = monitor.GetCurrentCorrelationId();
            
            // Simulate a slow operation
            await Task.Delay(600); // This will trigger slow operation warning
            
            logger.LogPerformanceWarning("DatabaseQuery", 600, 200, correlationId);
            operation.Complete();
            Console.WriteLine();
        }
    }
}
