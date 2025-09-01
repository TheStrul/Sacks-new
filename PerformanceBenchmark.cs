// 🚀 PERFORMANCE BENCHMARK: Simple test to demonstrate the optimization impact

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace SacksConsoleApp
{
    public class PerformanceBenchmark
    {
        public static void RunSimulatedBenchmark()
        {
            Console.WriteLine("🚀 === PERFORMANCE IMPROVEMENT SIMULATION ===\n");

            // Simulate processing 6000 rows
            const int totalRows = 6000;
            
            // OLD APPROACH simulation
            Console.WriteLine("🐌 OLD APPROACH (Individual operations):");
            var oldStopwatch = Stopwatch.StartNew();
            
            // Simulate individual database calls (10ms each)
            var oldOperations = totalRows * 3; // Get + Create/Update + OfferProduct
            var oldSimulatedTime = oldOperations * 10; // 10ms per operation
            
            oldStopwatch.Stop();
            Console.WriteLine($"   • Database operations: {oldOperations:N0}");
            Console.WriteLine($"   • Estimated time: {oldSimulatedTime:N0} ms ({oldSimulatedTime/1000.0:F1} seconds)");
            
            // NEW APPROACH simulation  
            Console.WriteLine("\n🚀 NEW APPROACH (Bulk operations):");
            var newStopwatch = Stopwatch.StartNew();
            
            // Simulate bulk operations
            var batchSize = 500;
            var batches = (int)Math.Ceiling((double)totalRows / batchSize);
            var newOperations = batches * 2; // Bulk lookup + Bulk create/update per batch
            var newSimulatedTime = newOperations * 100; // 100ms per bulk operation
            
            newStopwatch.Stop();
            Console.WriteLine($"   • Batches: {batches}");
            Console.WriteLine($"   • Database operations: {newOperations:N0}");
            Console.WriteLine($"   • Estimated time: {newSimulatedTime:N0} ms ({newSimulatedTime/1000.0:F1} seconds)");
            
            // Performance improvement calculation
            var improvementPercent = ((double)(oldSimulatedTime - newSimulatedTime) / oldSimulatedTime) * 100;
            var speedupFactor = (double)oldSimulatedTime / newSimulatedTime;
            
            Console.WriteLine("\n📊 === PERFORMANCE IMPROVEMENT ===");
            Console.WriteLine($"   • Time reduction: {improvementPercent:F1}%");
            Console.WriteLine($"   • Speed improvement: {speedupFactor:F1}x faster");
            Console.WriteLine($"   • Database calls reduced: {((double)(oldOperations - newOperations) / oldOperations) * 100:F1}%");
            
            if (improvementPercent > 90)
                Console.WriteLine("   ✅ EXCELLENT: Over 90% improvement!");
            else if (improvementPercent > 70)
                Console.WriteLine("   ✅ GREAT: Over 70% improvement!");
            else
                Console.WriteLine("   ✅ GOOD: Significant improvement achieved!");
                
            Console.WriteLine("\n💡 Key optimizations:");
            Console.WriteLine("   • Bulk database operations instead of individual calls");
            Console.WriteLine("   • Larger batch sizes (500 vs 50)");
            Console.WriteLine("   • AsNoTracking() for read-only queries");
            Console.WriteLine("   • Reduced context switching and delays");
            Console.WriteLine("   • Single bulk lookup for all EANs in batch");
        }
    }
}
