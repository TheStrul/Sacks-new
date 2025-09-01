using Microsoft.Extensions.DependencyInjection;
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
    /// Quick demo of Enhanced Structured Logging & Performance Monitoring features
    /// </summary>
    public class QuickLoggingDemo
    {
        public static async Task RunQuickDemoAsync()
        {
            Console.WriteLine("🚀 Enhanced Logging & Performance Monitoring Quick Demo");
            Console.WriteLine(new string('=', 60));

            // Create a minimal service provider for demo
            var services = new ServiceCollection();
            
            // Add logging
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
                settings.SlowOperationThresholdMs = 200; // Demo threshold
                settings.EnableMemoryTracking = true;
            });

            services.AddPerformanceMonitoring();

            var serviceProvider = services.BuildServiceProvider();
            var performanceMonitor = serviceProvider.GetRequiredService<IPerformanceMonitoringService>();
            var logger = serviceProvider.GetRequiredService<ILogger<QuickLoggingDemo>>();

            Console.WriteLine("📋 Demonstrating 5 key features:\n");

            // 1. Correlation ID tracking
            Console.WriteLine("1️⃣ Correlation ID Tracking:");
            using (var operation = performanceMonitor.StartOperation("DemoProcess"))
            {
                var correlationId = performanceMonitor.GetCurrentCorrelationId();
                Console.WriteLine($"   Generated: {correlationId}");
                logger.LogFileProcessingStart("demo.xlsx", "DEMO", correlationId);
                await Task.Delay(50);
                logger.LogFileProcessingComplete("demo.xlsx", 1000, 100, correlationId);
                operation.Complete();
            }

            // 2. Performance operation tracking  
            Console.WriteLine("\n2️⃣ Performance Operation Tracking:");
            using (var operation = performanceMonitor.StartOperation("DatabaseOperation"))
            {
                await Task.Delay(150);
                var correlationId = performanceMonitor.GetCurrentCorrelationId();
                logger.LogBulkDatabaseOperation("INSERT", 500, 150, correlationId);
                operation.Complete();
            }

            // 3. Memory tracking
            Console.WriteLine("\n3️⃣ Memory Usage Tracking:");
            var memStats = performanceMonitor.GetMemoryUsage();
            var correlationId2 = performanceMonitor.GetCurrentCorrelationId();
            logger.LogMemoryCheckpoint("QuickDemo", memStats, correlationId2);

            // 4. Structured logging formats
            Console.WriteLine("\n4️⃣ Structured Logging Extensions:");
            logger.LogSupplierDetection("test.xlsx", "TEST_SUPPLIER", "FilePattern", correlationId2);
            logger.LogValidationResult("DataValidation", 950, 50, correlationId2);
            logger.LogBatchMetrics("Processing", 100, 5, 2, 98, correlationId2);

            // 5. Slow operation detection
            Console.WriteLine("\n5️⃣ Slow Operation Detection:");
            using (var operation = performanceMonitor.StartOperation("SlowQuery"))
            {
                await Task.Delay(250); // This triggers slow operation warning
                var correlationId3 = performanceMonitor.GetCurrentCorrelationId();
                logger.LogPerformanceWarning("SlowQuery", 250, 200, correlationId3);
                operation.Complete();
            }

            Console.WriteLine("\n✅ Enhanced Logging Demo Complete!");
            Console.WriteLine("\n🎯 Key Benefits:");
            Console.WriteLine("   📋 Correlation tracking across operations");
            Console.WriteLine("   ⏱️ Automatic performance timing");
            Console.WriteLine("   📊 Memory usage monitoring");
            Console.WriteLine("   🚨 Slow operation alerts");
            Console.WriteLine("   📝 Consistent structured logging");
            Console.WriteLine("\nPress any key to continue...");
            Console.ReadKey();
        }
    }
}
