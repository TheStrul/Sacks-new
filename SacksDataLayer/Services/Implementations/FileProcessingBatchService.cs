using SacksDataLayer.FileProcessing.Configuration;
using SacksDataLayer.Services.Interfaces;
using SacksDataLayer.Entities;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SacksDataLayer.Services.Implementations
{
    /// <summary>
    /// Service implementation for batch processing operations during file processing
    /// </summary>
    public class FileProcessingBatchService : IFileProcessingBatchService
    {
        private readonly IFileProcessingDatabaseService _databaseService;
        private readonly ILogger<FileProcessingBatchService> _logger;

        public FileProcessingBatchService(
            IFileProcessingDatabaseService databaseService,
            ILogger<FileProcessingBatchService> logger)
        {
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Processes a collection of products in optimized batches
        /// </summary>
        public async Task<FileProcessingBatchResult> ProcessProductsInBatchesAsync(
            IEnumerable<OfferProductEntity> products,
            SupplierOfferEntity offer,
            SupplierConfiguration supplierConfig,
            int batchSize = 500,
            string? createdBy = null,
            CancellationToken cancellationToken = default)
        {
            if (products == null) throw new ArgumentNullException(nameof(products));
            if (offer == null) throw new ArgumentNullException(nameof(offer));
            if (supplierConfig == null) throw new ArgumentNullException(nameof(supplierConfig));
            if (batchSize <= 0) throw new ArgumentException("Batch size must be positive", nameof(batchSize));

            cancellationToken.ThrowIfCancellationRequested();

            var stopwatch = Stopwatch.StartNew();
            var aggregatedResult = new FileProcessingBatchResult();
            var processorName = createdBy ?? "FileProcessor";

            try
            {
                var productList = products.ToList();
                var totalBatches = (int)Math.Ceiling((double)productList.Count / batchSize);

                _logger.LogInformation("Starting batch processing: {ProductCount} products in {BatchCount} batches of {BatchSize}",
                    productList.Count, totalBatches, batchSize);

                Console.WriteLine($"   🔄 Processing {productList.Count} commercial records in {totalBatches} batches of {batchSize}...");

                if (totalBatches == 0)
                {
                    _logger.LogInformation("No products to process");
                    return aggregatedResult;
                }

                for (int batchIndex = 0; batchIndex < totalBatches; batchIndex++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var batch = productList.Skip(batchIndex * batchSize).Take(batchSize).ToList();
                    
                    try
                    {
                        var batchResult = await ProcessSingleBatchAsync(
                            batch, offer, supplierConfig, batchIndex, totalBatches, processorName, cancellationToken);
                        
                        aggregatedResult = aggregatedResult.CombineWith(batchResult);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing batch {BatchIndex}/{TotalBatches}", batchIndex + 1, totalBatches);
                        Console.WriteLine($"   ❌ Error processing batch {batchIndex + 1}: {ex.Message}");
                        
                        // Count all items in failed batch as errors
                        aggregatedResult.Errors += batch.Count;
                        aggregatedResult.ErrorMessages.Add($"Batch {batchIndex + 1} failed: {ex.Message}");
                    }

                    // Small delay between batches to prevent overwhelming the database
                    if (batchIndex < totalBatches - 1)
                    {
                        await Task.Delay(50, cancellationToken);
                    }
                }

                stopwatch.Stop();
                aggregatedResult.ProcessingTimeMs = stopwatch.ElapsedMilliseconds;

                _logger.LogInformation("Completed batch processing: {ProductsCreated} created, {ProductsUpdated} updated, " +
                    "{OfferProductsCreated} offer-products created, {OfferProductsUpdated} offer-products updated, " +
                    "{Errors} errors in {ElapsedMs}ms",
                    aggregatedResult.ProductsCreated, aggregatedResult.ProductsUpdated,
                    aggregatedResult.OfferProductsCreated, aggregatedResult.OfferProductsUpdated,
                    aggregatedResult.Errors, aggregatedResult.ProcessingTimeMs);

                return aggregatedResult;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                aggregatedResult.ProcessingTimeMs = stopwatch.ElapsedMilliseconds;
                
                _logger.LogError(ex, "Failed to process products in batches");
                aggregatedResult.Errors += products.Count(); // Count all as errors
                aggregatedResult.ErrorMessages.Add($"Batch processing failed: {ex.Message}");
                
                return aggregatedResult;
            }
        }

        /// <summary>
        /// Processes a single batch of products
        /// </summary>
        public async Task<FileProcessingBatchResult> ProcessSingleBatchAsync(
            List<OfferProductEntity> batch,
            SupplierOfferEntity offer,
            SupplierConfiguration supplierConfig,
            int batchIndex,
            int totalBatches,
            string? createdBy = null,
            CancellationToken cancellationToken = default)
        {
            if (batch == null) throw new ArgumentNullException(nameof(batch));
            if (offer == null) throw new ArgumentNullException(nameof(offer));
            if (supplierConfig == null) throw new ArgumentNullException(nameof(supplierConfig));

            cancellationToken.ThrowIfCancellationRequested();

            var batchStopwatch = Stopwatch.StartNew();
            var processorName = createdBy ?? "FileProcessor";

            try
            {
                Console.WriteLine($"   📦 Processing batch {batchIndex + 1}/{totalBatches} ({batch.Count} items)...");
                
                _logger.LogDebug("Processing batch {BatchIndex}/{TotalBatches} with {ItemCount} items",
                    batchIndex + 1, totalBatches, batch.Count);

                // Process the batch using the database service
                var batchResult = await _databaseService.ProcessProductBatchAsync(
                    batch, offer, supplierConfig, processorName, cancellationToken);

                batchStopwatch.Stop();
                batchResult.ProcessingTimeMs = batchStopwatch.ElapsedMilliseconds;

                // Log batch completion with summary
                if (batchResult.ProductsCreated > 0 || batchResult.ProductsUpdated > 0)
                {
                    Console.WriteLine($"   ✅ Batch {batchIndex + 1}: {batchResult.ProductsCreated} created, {batchResult.ProductsUpdated} updated");
                }

                if (batchResult.OfferProductsCreated > 0 || batchResult.OfferProductsUpdated > 0)
                {
                    Console.WriteLine($"   🔗 Batch {batchIndex + 1}: {batchResult.OfferProductsCreated} offer-products created, {batchResult.OfferProductsUpdated} updated");
                }

                if (batchResult.Errors > 0)
                {
                    Console.WriteLine($"   ⚠️  Batch {batchIndex + 1}: {batchResult.Errors} errors");
                }

                _logger.LogDebug("Completed batch {BatchIndex}/{TotalBatches}: {ProductsCreated} created, {ProductsUpdated} updated, " +
                    "{OfferProductsCreated} offer-products created, {OfferProductsUpdated} offer-products updated, " +
                    "{Errors} errors in {ElapsedMs}ms",
                    batchIndex + 1, totalBatches, batchResult.ProductsCreated, batchResult.ProductsUpdated,
                    batchResult.OfferProductsCreated, batchResult.OfferProductsUpdated,
                    batchResult.Errors, batchResult.ProcessingTimeMs);

                return batchResult;
            }
            catch (Exception ex)
            {
                batchStopwatch.Stop();
                
                _logger.LogError(ex, "Failed to process batch {BatchIndex}/{TotalBatches}",
                    batchIndex + 1, totalBatches);

                var errorResult = new FileProcessingBatchResult
                {
                    Errors = batch.Count, // Count all items as errors
                    ProcessingTimeMs = batchStopwatch.ElapsedMilliseconds
                };
                errorResult.ErrorMessages.Add($"Batch {batchIndex + 1} processing failed: {ex.Message}");

                return errorResult;
            }
        }
    }
}
