using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SacksConsoleApp;

namespace SacksConsoleApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("=== Sacks Product Management System ===");
            Console.WriteLine("🚀 Analyzing All Inputs...");
            Console.WriteLine("\n" + new string('=', 50));
            
            // Get the solution directory using a robust method that works from both VS and command line
            var inputsPath = FindInputsFolder();
            if (Directory.Exists(inputsPath))
            {
                var files = Directory.GetFiles(inputsPath, "*.xlsx")
                                    .Where(f => !Path.GetFileName(f).StartsWith("~")) // Skip temp files
                                    .ToArray();
                
                if (files.Length > 0)
                {
                    Console.WriteLine($"📁 Found {files.Length} Excel file(s) in Inputs folder:");
                    foreach (var file in files)
                    {
                        Console.WriteLine($"   - {Path.GetFileName(file)}");
                        await UnifiedFileProcessor.ProcessFileAsync(file);
                        Console.WriteLine();
                    }
                }
                else
                {
                    Console.WriteLine("❌ No Excel files found in Inputs folder.");
                }
            }
            else
            {
                Console.WriteLine("❌ Inputs folder not found.");
            }
            
            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }

        private static string FindInputsFolder()
        {
            // Try different strategies to find the Inputs folder
            var currentDirectory = Environment.CurrentDirectory;
            
            // Strategy 1: Check if we're running from project folder (dotnet run)
            var strategy1 = Path.Combine(currentDirectory, "..", "SacksDataLayer", "Inputs");
            if (Directory.Exists(strategy1))
            {
                return Path.GetFullPath(strategy1);
            }
            
            // Strategy 2: Check if we're running from bin folder (Visual Studio)
            var strategy2 = Path.Combine(currentDirectory, "..", "..", "..", "..", "SacksDataLayer", "Inputs");
            if (Directory.Exists(strategy2))
            {
                return Path.GetFullPath(strategy2);
            }
            
            // Strategy 3: Search upward for solution file, then go to SacksDataLayer/Inputs
            var searchDir = new DirectoryInfo(currentDirectory);
            while (searchDir != null)
            {
                var solutionFile = searchDir.GetFiles("*.sln").FirstOrDefault();
                if (solutionFile != null)
                {
                    var solutionInputsPath = Path.Combine(searchDir.FullName, "SacksDataLayer", "Inputs");
                    if (Directory.Exists(solutionInputsPath))
                    {
                        return solutionInputsPath;
                    }
                }
                searchDir = searchDir.Parent;
            }
            
            // Strategy 4: Fallback - return a non-existent path so we can show a helpful error
            return Path.Combine(currentDirectory, "SacksDataLayer", "Inputs");
        }
    }
}