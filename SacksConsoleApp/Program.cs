using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SacksConsoleApp;
using Microsoft.EntityFrameworkCore;
using SacksDataLayer.Data;

namespace SacksConsoleApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("=== Sacks Product Management System ===");
            Console.WriteLine("🚀 Analyzing All Inputs...");
            Console.WriteLine("\n" + new string('=', 50));

            // Ensure LocalDB database exists so it appears in Visual Studio's SQL Server Object Explorer
            try
            {
                var localDbConnection = "Server=(localdb)\\mssqllocaldb;Database=SacksProductsDb;Trusted_Connection=True;";
                var optionsBuilder = new DbContextOptionsBuilder<SacksDbContext>();
                optionsBuilder.UseSqlServer(localDbConnection);

                using (var ctx = new SacksDbContext(optionsBuilder.Options))
                {
                    // Ensure database is created (safe for development); this will create database and tables
                    if (ctx.Database.EnsureCreated())
                    {
                        Console.WriteLine("LocalDB database 'SacksProductsDb' created (or schema initialized).");
                    }
                    else
                    {
                        Console.WriteLine("LocalDB database 'SacksProductsDb' already exists.");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to initialize LocalDB database: {ex.Message}");
            }

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