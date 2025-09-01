using SacksDataLayer.FileProcessing.Configuration;
using SacksDataLayer.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SacksDataLayer.Services.Interfaces
{
    /// <summary>
    /// Service interface for batch processing operations during file processing
    /// </summary>
    public interface IFileProcessingBatchService
    {
        /// <summary>
        /// Processes a collection of products in optimized batches
        /// </summary>
        /// <param name="products">The products to process</param>
        /// <param name="offer">The supplier offer these products belong to</param>
        /// <param name="supplierConfig">The supplier configuration for processing rules</param>
        /// <param name="batchSize">The size of each batch (default: 500)</param>
        /// <param name="createdBy">The user/system creating the records</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Aggregated batch processing results</returns>
        Task<FileProcessingBatchResult> ProcessProductsInBatchesAsync(
            IEnumerable<ProductEntity> products,
            SupplierOfferEntity offer,
            SupplierConfiguration supplierConfig,
            int batchSize = 500,
            string? createdBy = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Processes a single batch of products
        /// </summary>
        /// <param name="batch">The products in this batch</param>
        /// <param name="offer">The supplier offer these products belong to</param>
        /// <param name="supplierConfig">The supplier configuration for processing rules</param>
        /// <param name="batchIndex">The index of this batch (for logging)</param>
        /// <param name="totalBatches">The total number of batches (for logging)</param>
        /// <param name="createdBy">The user/system creating the records</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Batch processing results</returns>
        Task<FileProcessingBatchResult> ProcessSingleBatchAsync(
            List<ProductEntity> batch,
            SupplierOfferEntity offer,
            SupplierConfiguration supplierConfig,
            int batchIndex,
            int totalBatches,
            string? createdBy = null,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Results from batch processing operations
    /// </summary>
    public class FileProcessingBatchResult
    {
        /// <summary>
        /// Number of products created
        /// </summary>
        public int ProductsCreated { get; set; }

        /// <summary>
        /// Number of products updated
        /// </summary>
        public int ProductsUpdated { get; set; }

        /// <summary>
        /// Number of offer-products created
        /// </summary>
        public int OfferProductsCreated { get; set; }

        /// <summary>
        /// Number of offer-products updated
        /// </summary>
        public int OfferProductsUpdated { get; set; }

        /// <summary>
        /// Number of errors encountered
        /// </summary>
        public int Errors { get; set; }

        /// <summary>
        /// Collection of error messages
        /// </summary>
        public List<string> ErrorMessages { get; set; } = new List<string>();

        /// <summary>
        /// Processing time in milliseconds
        /// </summary>
        public long ProcessingTimeMs { get; set; }

        /// <summary>
        /// Combines this result with another result
        /// </summary>
        public FileProcessingBatchResult CombineWith(FileProcessingBatchResult other)
        {
            if (other == null) return this;

            return new FileProcessingBatchResult
            {
                ProductsCreated = ProductsCreated + other.ProductsCreated,
                ProductsUpdated = ProductsUpdated + other.ProductsUpdated,
                OfferProductsCreated = OfferProductsCreated + other.OfferProductsCreated,
                OfferProductsUpdated = OfferProductsUpdated + other.OfferProductsUpdated,
                Errors = Errors + other.Errors,
                ErrorMessages = ErrorMessages.Concat(other.ErrorMessages).ToList(),
                ProcessingTimeMs = ProcessingTimeMs + other.ProcessingTimeMs
            };
        }
    }
}
