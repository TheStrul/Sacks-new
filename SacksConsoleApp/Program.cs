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
            Console.WriteLine("1. Run DIOR File Analysis");
            Console.WriteLine("2. Run CRUD Functionality Demo");
            Console.WriteLine("3. Exit");
            Console.Write("\nEnter your choice (1-3): ");

            var choice = Console.ReadLine();

            switch (choice)
            {
                case "1":
                    Console.WriteLine("\n" + new string('=', 50));
                    await DiorAnalysis.AnalyzeDiorFile();
                    break;
                
                case "2":
                    Console.WriteLine("\n" + new string('=', 50));
                    await CrudDemo.RunAsync();
                    break;
                
                case "3":
                    Console.WriteLine("Goodbye!");
                    return;
                
                default:
                    Console.WriteLine("Invalid choice. Running DIOR analysis by default...");
                    Console.WriteLine("\n" + new string('=', 50));
                    await DiorAnalysis.AnalyzeDiorFile();
                    break;
            }

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }
    }
}
