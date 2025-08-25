using SacksDataLayer;
using SacksDataLayer.FileProcessing.Configuration;
using SacksDataLayer.FileProcessing.Services;
using SacksDataLayer.FileProcessing.Normalizers;
using SacksDataLayer.FileProcessing.Interfaces;
using SacksAIPlatform.InfrastructuresLayer.FileProcessing;
using System.Text.Json;

namespace SacksConsoleApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("=== Sacks Product Management System ===");
            Console.WriteLine("Choose an option:");
            Console.WriteLine("1. Process File to Database (Auto-detect supplier)");
            Console.WriteLine("2. Run CRUD Functionality Demo");
            Console.WriteLine("3. Run Configuration Diagnostic");
            Console.WriteLine("4. Exit");
            Console.Write("\nEnter your choice (1-4): ");

            var choice = Console.ReadLine();

            // Temporary: Auto-run option 1 (process file) if input is problematic
            if (string.IsNullOrWhiteSpace(choice))
            {
                choice = "1";
                Console.WriteLine("1 (auto-selected - process file)");
            }

            switch (choice)
            {
                case "1":
                    Console.WriteLine("\n" + new string('=', 50));
                    
                    // Auto-detect files in Inputs folder
                    var inputsPath = Path.Combine("..", "SacksDataLayer", "Inputs");
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
                    break;
                
                case "2":
                    Console.WriteLine("\n" + new string('=', 50));
                    await CrudDemo.RunAsync();
                    break;
                
                case "3":
                    Console.WriteLine("\n" + new string('=', 50));
                    await ConfigDiagnostic.RunDiagnostic();
                    break;
                
                case "4":
                    Console.WriteLine("Goodbye!");
                    return;
                
                default:
                    Console.WriteLine("Invalid choice. Running unified file processor by default...");
                    Console.WriteLine("\n" + new string('=', 50));
                    
                    // Auto-detect files in Inputs folder
                    var defaultInputsPath = Path.Combine("..", "SacksDataLayer", "Inputs");
                    if (Directory.Exists(defaultInputsPath))
                    {
                        var defaultFiles = Directory.GetFiles(defaultInputsPath, "*.xlsx")
                                            .Where(f => !Path.GetFileName(f).StartsWith("~")) // Skip temp files
                                            .ToArray();
                        
                        if (defaultFiles.Length > 0)
                        {
                            var firstFile = defaultFiles.First();
                            Console.WriteLine($"📁 Processing: {Path.GetFileName(firstFile)}");
                            await UnifiedFileProcessor.ProcessFileAsync(firstFile);
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
                    break;
            }

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }
    }
}
