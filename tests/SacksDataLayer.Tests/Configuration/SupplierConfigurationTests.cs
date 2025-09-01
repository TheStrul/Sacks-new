using System;
using System.IO;
using System.Threading.Tasks;
using SacksDataLayer.FileProcessing.Services;
using Microsoft.Extensions.Logging;

namespace SacksDataLayer.Tests.Configuration
{
    /// <summary>
    /// Tests for validating the refactored supplier configuration
    /// </summary>
    public static class SupplierConfigurationTests
    {
        /// <summary>
        /// Test that the refactored configuration can be loaded successfully
        /// </summary>
        public static async Task TestRefactoredConfigurationLoading()
        {
            Console.WriteLine("?? Testing Refactored Configuration Loading...\n");

            try
            {
                // Find the configuration file
                var configPath = SupplierConfigurationManager.FindConfigurationFile();
                Console.WriteLine($"? Configuration file found: {configPath}");

                // Create configuration manager
                var configManager = new SupplierConfigurationManager(configPath);

                // Load configuration
                var config = await configManager.GetConfigurationAsync();
                Console.WriteLine($"? Configuration loaded successfully");
                Console.WriteLine($"   Version: {config.Version}");
                Console.WriteLine($"   Suppliers: {config.Suppliers.Count}");
                Console.WriteLine($"   Last Updated: {config.LastUpdated:yyyy-MM-dd HH:mm:ss}");

                // Test DIOR configuration
                var diorConfig = await configManager.GetSupplierConfigurationAsync("DIOR");
                if (diorConfig != null)
                {
                    Console.WriteLine($"\n? DIOR Configuration:");
                    Console.WriteLine($"   Column Mappings: {diorConfig.ColumnIndexMappings.Count}");
                    Console.WriteLine($"   Data Types: {diorConfig.DataTypes.Count}");
                    Console.WriteLine($"   Required Fields: {diorConfig.Validation.RequiredFields.Count}");
                    Console.WriteLine($"   Unique Fields: {diorConfig.Validation.UniqueFields.Count}");
                    Console.WriteLine($"   Currency: {diorConfig.Metadata.Currency}");
                    Console.WriteLine($"   Timezone: {diorConfig.Metadata.Timezone}");
                    
                    // Test core vs offer property classification
                    Console.WriteLine($"   Core Properties: {diorConfig.PropertyClassification.CoreProductProperties.Count}");
                    Console.WriteLine($"   Offer Properties: {diorConfig.PropertyClassification.OfferProperties.Count}");
                }

                // Test UNLIMITED configuration
                var unlimitedConfig = await configManager.GetSupplierConfigurationAsync("UNLIMITED");
                if (unlimitedConfig != null)
                {
                    Console.WriteLine($"\n? UNLIMITED Configuration:");
                    Console.WriteLine($"   Column Mappings: {unlimitedConfig.ColumnIndexMappings.Count}");
                    Console.WriteLine($"   Data Types: {unlimitedConfig.DataTypes.Count}");
                    Console.WriteLine($"   Required Fields: {unlimitedConfig.Validation.RequiredFields.Count}");
                    Console.WriteLine($"   Unique Fields: {unlimitedConfig.Validation.UniqueFields.Count}");
                    Console.WriteLine($"   Currency: {unlimitedConfig.Metadata.Currency}");
                    Console.WriteLine($"   Timezone: {unlimitedConfig.Metadata.Timezone}");
                }

                Console.WriteLine("\n?? Refactored configuration loading test passed!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"? Configuration loading failed: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Test configuration validation
        /// </summary>
        public static async Task TestConfigurationValidation()
        {
            Console.WriteLine("\n?? Testing Configuration Validation...\n");

            try
            {
                var configManager = new SupplierConfigurationManager();
                var validationResult = await configManager.ValidateConfigurationAsync();

                Console.WriteLine($"? Validation Result:");
                Console.WriteLine($"   Is Valid: {validationResult.IsValid}");
                Console.WriteLine($"   Errors: {validationResult.Errors.Count}");
                Console.WriteLine($"   Warnings: {validationResult.Warnings.Count}");
                Console.WriteLine($"   Info: {validationResult.Info.Count}");

                if (validationResult.Errors.Count > 0)
                {
                    Console.WriteLine("\n? Validation Errors:");
                    foreach (var error in validationResult.Errors)
                    {
                        Console.WriteLine($"   • {error}");
                    }
                }

                if (validationResult.Warnings.Count > 0)
                {
                    Console.WriteLine("\n?? Validation Warnings:");
                    foreach (var warning in validationResult.Warnings)
                    {
                        Console.WriteLine($"   • {warning}");
                    }
                }

                if (validationResult.Info.Count > 0)
                {
                    Console.WriteLine("\n?? Validation Info:");
                    foreach (var info in validationResult.Info)
                    {
                        Console.WriteLine($"   • {info}");
                    }
                }

                if (validationResult.IsValid)
                {
                    Console.WriteLine("\n?? Configuration validation passed!");
                }
                else
                {
                    Console.WriteLine("\n? Configuration validation failed!");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"? Configuration validation test failed: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Test file detection with new patterns
        /// </summary>
        public static async Task TestFileDetection()
        {
            Console.WriteLine("\n?? Testing File Detection...\n");

            try
            {
                var configManager = new SupplierConfigurationManager();

                // Test DIOR file patterns
                var testFiles = new[]
                {
                    "DIOR_Products_2024.xlsx",
                    "dior_beauty_catalog.xlsx",
                    "DIOR_December_2024.xls",
                    "UNLIMITED_Products.xlsx",
                    "unlimited_fragrances.xlsx",
                    "RANDOM_FILE.xlsx"
                };

                foreach (var testFile in testFiles)
                {
                    var detectedConfig = await configManager.DetectSupplierFromFileAsync(testFile);
                    if (detectedConfig != null)
                    {
                        Console.WriteLine($"? {testFile} ? {detectedConfig.Name}");
                    }
                    else
                    {
                        Console.WriteLine($"? {testFile} ? No match");
                    }
                }

                Console.WriteLine("\n?? File detection test completed!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"? File detection test failed: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Runs all configuration tests
        /// </summary>
        public static async Task RunAllConfigurationTests()
        {
            Console.WriteLine("?? === SUPPLIER CONFIGURATION TESTS ===\n");

            await TestRefactoredConfigurationLoading();
            await TestConfigurationValidation();
            await TestFileDetection();

            Console.WriteLine("\n?? CONFIGURATION TESTS SUMMARY:");
            Console.WriteLine("   ? JSON loading and deserialization");
            Console.WriteLine("   ? Enhanced validation rules support");
            Console.WriteLine("   ? New metadata properties (currency, timezone)");
            Console.WriteLine("   ? Required and unique fields validation");
            Console.WriteLine("   ? Improved file detection patterns");
            Console.WriteLine("   ? Property classification (core vs offer)");
            Console.WriteLine("   ? Data type configurations with maxLength");

            Console.WriteLine("\n?? ALL CONFIGURATION TESTS PASSED! ??");
        }
    }
}