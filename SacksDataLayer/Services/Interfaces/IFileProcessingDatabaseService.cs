using SacksDataLayer.FileProcessing.Configuration;
using SacksDataLayer.FileProcessing.Models;
using SacksDataLayer.Entities;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SacksDataLayer.Services.Interfaces
{
    /// <summary>
    /// Service interface for database operations during file processing
    /// </summary>
    public interface IFileProcessingDatabaseService
    {
        /// <summary>
        /// Ensures the database is ready for file processing operations
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task representing the async operation</returns>
        Task EnsureDatabaseReadyAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates or retrieves a supplier based on the supplier configuration
        /// </summary>
        /// <param name="supplierConfig">Supplier configuration</param>
        /// <param name="createdBy">User who created the supplier</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Supplier entity</returns>
        Task<SupplierEntity> CreateOrGetSupplierAsync(
            SupplierConfiguration supplierConfig, 
            string? createdBy = null, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a new offer for the file processing session
        /// </summary>
        /// <param name="supplier">Supplier entity</param>
        /// <param name="fileName">Name of the file being processed</param>
        /// <param name="processingDate">Date of processing</param>
        /// <param name="currency">Currency for the offer</param>
        /// <param name="description">Offer description</param>
        /// <param name="createdBy">User who created the offer</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Supplier offer entity</returns>
        Task<SupplierOfferEntity> CreateOfferAsync(
            SupplierEntity supplier,
            string fileName,
            DateTime processingDate,
            string currency = "USD",
            string description = "File Import",
            string? createdBy = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Inserts or updates a supplier offer and all its products/offer-products in a single transaction
        /// This method handles the database state-dependent operations
        /// </summary>
        /// <param name="analysisOffer">Analyzed offer from file data (without database IDs)</param>
        /// <param name="dbOffer">Database offer entity with proper IDs</param>
        /// <param name="supplierConfig">Supplier configuration</param>
        /// <param name="createdBy">User who created the data</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Processing results with statistics</returns>
        Task<FileProcessingBatchResult> InsertOrUpdateSupplierOfferAsync(
            SupplierOfferEntity analysisOffer,
            SupplierOfferEntity dbOffer,
            SupplierConfiguration supplierConfig,
            string? createdBy = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Processes a batch of products with optimized bulk operations
        /// </summary>
        /// <param name="products">List of product entities to process</param>
        /// <param name="offer">Supplier offer entity</param>
        /// <param name="supplierConfig">Supplier configuration</param>
        /// <param name="createdBy">User who created the products</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Processing results with statistics</returns>
        Task<FileProcessingBatchResult> ProcessProductBatchAsync(
            List<OfferProductEntity> products,
            SupplierOfferEntity offer,
            SupplierConfiguration supplierConfig,
            string? createdBy = null,
            CancellationToken cancellationToken = default);
    }
}
