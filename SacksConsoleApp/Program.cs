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
            Console.WriteLine("1. Process DIOR File to Database");
            Console.WriteLine("2. Run CRUD Functionality Demo");
            Console.WriteLine("3. Run Configuration Diagnostic");
            Console.WriteLine("4. Exit");
            Console.Write("\nEnter your choice (1-4): ");

            var choice = Console.ReadLine();

            // Temporary: Auto-run option 1 (process file) if input is problematic
            if (string.IsNullOrWhiteSpace(choice))
            {
                choice = "1";
                Console.WriteLine("1 (auto-selected - process DIOR file)");
            }

            switch (choice)
            {
                case "1":
                    Console.WriteLine("\n" + new string('=', 50));
                    await DiorProcessor.ProcessDiorFile();
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
                    Console.WriteLine("Invalid choice. Running DIOR processor by default...");
                    Console.WriteLine("\n" + new string('=', 50));
                    await DiorProcessor.ProcessDiorFile();
                    break;
            }

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }
    }
}
