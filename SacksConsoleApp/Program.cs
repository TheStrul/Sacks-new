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
            // Run DIOR analysis directly
            await DiorAnalysis.AnalyzeDiorFile();
        }
    }
}
