using SacksDataLayer.FileProcessing.Configuration;
using SacksDataLayer.FileProcessing.Services;
using SacksDataLayer.FileProcessing.Normalizers;
using SacksDataLayer.FileProcessing.Models;
using SacksDataLayer.Services.Interfaces;
using SacksDataLayer.Entities;
using SacksDataLayer.Extensions;
using SacksAIPlatform.InfrastructuresLayer.FileProcessing;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;

namespace SacksDataLayer.Services.Implementations
{
    /// <summary>
    /// Service implementation for unified file processing operations
    /// </summary>
    public class FileProcessingService : IFileProcessingService
    {
        private readonly IFileValidationService _fileValidationService;
        private readonly ISupplierConfigurationService _supplierConfigurationService;
        private readonly IFileProcessingDatabaseService _databaseService;
        private readonly IFileProcessingBatchService _batchService;
        private readonly IPerformanceMonitoringService _performanceMonitor;
        private readonly ILogger<FileProcessingService> _logger;

        public FileProcessingService(
            IFileValidationService fileValidationService,
            ISupplierConfigurationService supplierConfigurationService,
            IFileProcessingDatabaseService databaseService,
            IFileProcessingBatchService batchService,
            IPerformanceMonitoringService performanceMonitor,
            ILogger<FileProcessingService> logger)
        {
            _fileValidationService = fileValidationService ?? throw new ArgumentNullException(nameof(fileValidationService));
            _supplierConfigurationService = supplierConfigurationService ?? throw new ArgumentNullException(nameof(supplierConfigurationService));
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            _batchService = batchService ?? throw new ArgumentNullException(nameof(batchService));
            _performanceMonitor = performanceMonitor ?? throw new ArgumentNullException(nameof(performanceMonitor));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Processes a file (Excel, CSV, etc.) and imports data based on supplier configuration
        /// </summary>
        public async Task ProcessFileAsync(string filePath, CancellationToken cancellationToken = default)
        {
            using var operation = _performanceMonitor.StartOperation("FileProcessing", 
                metadata: new { FileName = Path.GetFileName(filePath) });

            var correlationId = _performanceMonitor.GetCurrentCorrelationId();
            
            Console.WriteLine("=== Unified File Processing ===\n");
            _logger.LogFileProcessingStart(Path.GetFileName(filePath), "Auto-Detection", correlationId);

            try
            {
                _performanceMonitor.LogMemoryUsage("FileProcessing_Start");

                // Step 1: Validate file exists using the file validation service
                using var fileValidationOp = _performanceMonitor.StartOperation("FileValidation", correlationId);
                
                if (!await _fileValidationService.ValidateFileExistsAsync(filePath, cancellationToken))
                {
                    Console.WriteLine($"❌ File not found: {filePath}");
                    _logger.LogErrorWithContext(new FileNotFoundException(), "FileValidation", 
                        new { FilePath = filePath }, correlationId);
                    operation.Fail(new FileNotFoundException($"File not found: {filePath}"));
                    return;
                }

                Console.WriteLine($"📁 Processing file: {Path.GetFileName(filePath)}");

                fileValidationOp.Complete();

                // Ensure database is created (dev environment)
                using var dbSetupOp = _performanceMonitor.StartOperation("DatabaseSetup", correlationId);
                Console.WriteLine("🔧 Ensuring database exists and is up-to-date...");
                await _databaseService.EnsureDatabaseReadyAsync(cancellationToken);
                Console.WriteLine("✅ Database ready!");
                dbSetupOp.Complete();

                // Auto-detect supplier from filename using the configuration service
                using var supplierDetectionOp = _performanceMonitor.StartOperation("SupplierDetection", correlationId);
                var supplierConfig = await _supplierConfigurationService.DetectSupplierFromFileAsync(filePath, cancellationToken);
                
                if (supplierConfig == null)
                {
                    var fileName = Path.GetFileName(filePath);
                    Console.WriteLine($"❌ No supplier configuration found for file: {fileName}");
                    Console.WriteLine("💡 Add a supplier configuration with matching fileNamePatterns in supplier-formats.json");
                    _logger.LogErrorWithContext(new InvalidOperationException("Supplier not detected"), 
                        "SupplierDetection", new { FileName = fileName }, correlationId);
                    operation.Fail(new InvalidOperationException($"No supplier configuration found for file: {fileName}"));
                    return;
                }

                _logger.LogSupplierDetection(Path.GetFileName(filePath), supplierConfig.Name, "FilePattern", correlationId);
                Console.WriteLine($"✅ Auto-detected supplier: {supplierConfig.Name}");
                Console.WriteLine($"   Description: {supplierConfig.Description}");
                supplierDetectionOp.Complete();

                // Step 2: Read file data using the file validation service
                using var fileReadOp = _performanceMonitor.StartOperation("FileDataReading", correlationId);
                Console.WriteLine("📖 Reading Excel file...");
                var fileData = await _fileValidationService.ReadFileDataAsync(filePath, cancellationToken);
                fileReadOp.AddMetadata("RowCount", fileData.DataRows.Count);
                fileReadOp.Complete();
                
                // Step 3: Validate file structure using the file validation service
                using var structureValidationOp = _performanceMonitor.StartOperation("StructureValidation", correlationId);
                Console.WriteLine("\n🔍 Validating file structure:");
                var validationResult = await _fileValidationService.ValidateFileStructureAsync(fileData, supplierConfig, cancellationToken);
                
                // Display validation results
                if (validationResult.FileHeaders.Any())
                {
                    Console.WriteLine($"📋 Found {validationResult.ColumnCount} columns: {string.Join(", ", validationResult.FileHeaders.Take(5))}{(validationResult.FileHeaders.Count > 5 ? "..." : "")}");
                }

                if (validationResult.ExpectedColumnCount > 0)
                {
                    var status = validationResult.ColumnCount == validationResult.ExpectedColumnCount ? "✓" : "❌";
                    Console.WriteLine($"   {status} Expected {validationResult.ExpectedColumnCount} columns, found {validationResult.ColumnCount}");
                }

                // Display warnings
                foreach (var warning in validationResult.ValidationWarnings)
                {
                    Console.WriteLine($"⚠️  {warning}");
                }

                _logger.LogValidationResult("FileStructure", 
                    validationResult.IsValid ? 1 : 0, 
                    validationResult.IsValid ? 0 : 1, 
                    correlationId);

                // Display errors and exit if validation failed
                if (!validationResult.IsValid)
                {
                    foreach (var error in validationResult.ValidationErrors)
                    {
                        Console.WriteLine($"❌ {error}");
                    }
                    structureValidationOp.Fail(new InvalidDataException("File structure validation failed"));
                    operation.Fail(new InvalidDataException("File structure validation failed"));
                    return;
                }

                structureValidationOp.Complete();

                // Initialize normalizer and process file
                var normalizer = new ConfigurationBasedNormalizer(supplierConfig);
                Console.WriteLine("\n🔄 Processing file as Supplier Offer...\n");

                // Process as SupplierOffer (suppliers + offers + offer-products)
                await ProcessSupplierOfferToDatabase(normalizer, fileData, filePath, supplierConfig, cancellationToken);

                _performanceMonitor.LogMemoryUsage("FileProcessing_End");
                _logger.LogFileProcessingComplete(Path.GetFileName(filePath), fileData.DataRows.Count, 
                    operation.Elapsed.Milliseconds, correlationId);

                Console.WriteLine("\n✅ File processing completed successfully!");
                operation.Complete();
                
            }
            catch (Exception ex)
            {
                _logger.LogErrorWithContext(ex, "FileProcessing", 
                    new { FileName = Path.GetFileName(filePath) }, correlationId);
                Console.WriteLine($"❌ Error during processing: {ex.Message}");
                Console.WriteLine($"   Stack trace: {ex.StackTrace}");
                operation.Fail(ex);
            }
        }

        private async Task ProcessSupplierOfferToDatabase(
            ConfigurationBasedNormalizer normalizer,
            FileData fileData,
            string filePath,
            SupplierConfiguration supplierConfig,
            CancellationToken cancellationToken = default)
        {
            try
            {
                Console.WriteLine($"📦 Processing file as Supplier Offer...");
                
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();

                // Create processing context for commercial mode
                var context = new ProcessingContext
                {
                    SourceFileName = Path.GetFileName(filePath),
                    ProcessingDate = DateTime.UtcNow
                };

                // Step 1: Create or get supplier using database service
                Console.WriteLine($"   🏢 Creating/finding supplier: {supplierConfig.Name}");
                var supplier = await _databaseService.CreateOrGetSupplierAsync(supplierConfig, "FileProcessor", cancellationToken);
                Console.WriteLine($"   ✅ Supplier ready: {supplier.Name} (ID: {supplier.Id})");

                // Step 2: Create new offer for this file processing session using database service
                Console.WriteLine($"   📋 Creating offer for file: {Path.GetFileName(filePath)}");
                var offer = await _databaseService.CreateOfferAsync(
                    supplier,
                    Path.GetFileName(filePath),
                    context.ProcessingDate,
                    "USD", // Default currency, could be extracted from config
                    "File Import",
                    "FileProcessor",
                    cancellationToken);
                Console.WriteLine($"   ✅ Offer created: {offer.OfferName} (ID: {offer.Id})");

                // Step 3: Normalize products and extract commercial data
                var result = await normalizer.NormalizeAsync(fileData, context);
                
                if (result.Errors.Any())
                {
                    Console.WriteLine($"   ❌ Processing errors: {string.Join(", ", result.Errors)}");
                    return;
                }

                Console.WriteLine($"   📊 Processed {result.Products.Count()} commercial records from file");

                // Process products using the dedicated batch service
                // 🚀 PERFORMANCE OPTIMIZATION: Use batch service for optimized processing
                const int batchSize = 500; // Optimized batch size
                var productList = result.Products.ToList();

                var batchResult = await _batchService.ProcessProductsInBatchesAsync(
                    productList, offer, supplierConfig, batchSize, "FileProcessor", cancellationToken);

                stopwatch.Stop();

                // Final statistics
                Console.WriteLine($"\n   📈 Supplier Offer Results:");
                Console.WriteLine($"      • Supplier: {supplier.Name} (ID: {supplier.Id})");
                Console.WriteLine($"      • Offer: {offer.OfferName} (ID: {offer.Id})");
                Console.WriteLine($"      • Products created: {batchResult.ProductsCreated}");
                Console.WriteLine($"      • Products updated: {batchResult.ProductsUpdated}");
                Console.WriteLine($"      • Offer-products created: {batchResult.OfferProductsCreated}");
                Console.WriteLine($"      • Offer-products updated: {batchResult.OfferProductsUpdated}");
                if (batchResult.Errors > 0)
                {
                    Console.WriteLine($"      • Errors: {batchResult.Errors}");
                    if (batchResult.Errors > 5)
                        Console.WriteLine($"        (Only first 5 errors shown)");
                }
                Console.WriteLine($"      • Processing time: {stopwatch.ElapsedMilliseconds}ms");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ❌ Failed to process Supplier Offer: {ex.Message}");
                Console.WriteLine($"   Stack trace: {ex.StackTrace}");
            }
        }
    }
}
