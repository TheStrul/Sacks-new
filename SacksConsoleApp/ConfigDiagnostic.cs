using SacksDataLayer.FileProcessing.Configuration;
using SacksDataLayer.FileProcessing.Services;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SacksConsoleApp
{
    public static class ConfigDiagnostic
    {
        public static async Task RunDiagnostic()
        {
            Console.WriteLine("=== Configuration Diagnostic ===\n");

            try
            {
                // Show current working directory
                Console.WriteLine($"Current working directory: {Directory.GetCurrentDirectory()}");
                
                // Test different possible paths with more comprehensive search
                var possiblePaths = new[]
                {
                    Path.Combine("..", "..", "..", "SacksDataLayer", "Configuration", "supplier-formats.json"), // Debug build output
                    Path.Combine("..", "..", "..", "..", "SacksDataLayer", "Configuration", "supplier-formats.json"), // Release build output
                    Path.Combine("..", "SacksDataLayer", "Configuration", "supplier-formats.json"), // Solution root
                    Path.Combine("SacksDataLayer", "Configuration", "supplier-formats.json"), // Solution root alternative
                    Path.Combine(".", "SacksDataLayer", "Configuration", "supplier-formats.json"), // Current directory
                    Path.Combine("Configuration", "supplier-formats.json"), // If copied to output
                    "supplier-formats.json" // Current directory
                };

                Console.WriteLine("\n?? Testing possible configuration file paths:");
                string? validPath = null;
                
                foreach (var path in possiblePaths)
                {
                    var fullPath = Path.GetFullPath(path);
                    var exists = File.Exists(path);
                    Console.WriteLine($"   {(exists ? "?" : "?")} {path}");
                    Console.WriteLine($"      Full path: {fullPath}");
                    
                    if (exists && validPath == null)
                    {
                        validPath = path;
                    }
                }

                // Try searching up the directory tree if no file found
                if (validPath == null)
                {
                    Console.WriteLine("\n?? Searching up directory tree for configuration file...");
                    var currentDir = Directory.GetCurrentDirectory();
                    var dir = new DirectoryInfo(currentDir);
                    
                    while (dir != null && dir.Parent != null)
                    {
                        var configPath = Path.Combine(dir.FullName, "SacksDataLayer", "Configuration", "supplier-formats.json");
                        var exists = File.Exists(configPath);
                        Console.WriteLine($"   {(exists ? "?" : "?")} {configPath}");
                        
                        if (exists && validPath == null)
                        {
                            validPath = configPath;
                            break;
                        }
                        dir = dir.Parent;
                    }
                }

                if (validPath == null)
                {
                    Console.WriteLine("\n? No configuration file found!");
                    return;
                }

                Console.WriteLine($"\n? Using configuration file: {validPath}");

                // Try to load the configuration
                var configManager = new SupplierConfigurationManager(validPath);
                
                try
                {
                    var config = await configManager.GetConfigurationAsync();
                    Console.WriteLine($"\n?? Configuration loaded successfully!");
                    Console.WriteLine($"   Version: {config.Version}");
                    Console.WriteLine($"   Description: {config.Description}");
                    Console.WriteLine($"   Last Updated: {config.LastUpdated}");
                    Console.WriteLine($"   Suppliers count: {config.Suppliers.Count}");

                    Console.WriteLine("\n?? Available suppliers:");
                    foreach (var supplier in config.Suppliers)
                    {
                        Console.WriteLine($"   - Name: '{supplier.Name}'");
                        Console.WriteLine($"     Description: {supplier.Description}");
                        Console.WriteLine($"     Priority: {supplier.Detection.Priority}");
                        Console.WriteLine($"     Required Columns: [{string.Join(", ", supplier.Detection.RequiredColumns)}]");
                        Console.WriteLine($"     Column Mappings: {supplier.ColumnMappings.Count} mappings");
                        Console.WriteLine();
                    }

                    // Test specific DIOR lookup
                    Console.WriteLine("?? Testing DIOR lookup:");
                    var diorConfig = await configManager.GetSupplierConfigurationAsync("DIOR");
                    if (diorConfig != null)
                    {
                        Console.WriteLine($"? DIOR configuration found!");
                        Console.WriteLine($"   Name: '{diorConfig.Name}'");
                        Console.WriteLine($"   Detection patterns: [{string.Join(", ", diorConfig.Detection.FileNamePatterns)}]");
                    }
                    else
                    {
                        Console.WriteLine("? DIOR configuration not found via GetSupplierConfigurationAsync");
                        
                        // Manual search
                        var foundDior = config.Suppliers.FirstOrDefault(s => 
                            string.Equals(s.Name, "DIOR", StringComparison.OrdinalIgnoreCase));
                        
                        if (foundDior != null)
                        {
                            Console.WriteLine($"? Found DIOR manually: '{foundDior.Name}'");
                        }
                        else
                        {
                            Console.WriteLine("? DIOR not found even with manual search");
                        }
                    }

                    // Test validation
                    var validationResult = await configManager.ValidateConfigurationAsync();
                    Console.WriteLine($"\n?? Configuration validation:");
                    Console.WriteLine($"   Valid: {validationResult.IsValid}");
                    
                    if (validationResult.Errors.Any())
                    {
                        Console.WriteLine("   Errors:");
                        foreach (var error in validationResult.Errors)
                        {
                            Console.WriteLine($"     - {error}");
                        }
                    }
                    
                    if (validationResult.Warnings.Any())
                    {
                        Console.WriteLine("   Warnings:");
                        foreach (var warning in validationResult.Warnings)
                        {
                            Console.WriteLine($"     - {warning}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"? Error loading configuration: {ex.Message}");
                    Console.WriteLine($"   Inner exception: {ex.InnerException?.Message}");
                    Console.WriteLine($"   Stack trace: {ex.StackTrace}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"? Diagnostic failed: {ex.Message}");
                Console.WriteLine($"   Stack trace: {ex.StackTrace}");
            }

            Console.WriteLine("\nPress any key to continue...");
            Console.ReadKey();
        }
    }
}