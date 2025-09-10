using SacksDataLayer.FileProcessing.Configuration;
using SacksDataLayer.FileProcessing.Models;
using SacksDataLayer.Entities;
using SacksDataLayer.Models;
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
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Supplier entity</returns>
        Task<SupplierEntity> CreateOrGetSupplierAsync(
            SupplierConfiguration supplierConfig, 
            CancellationToken cancellationToken = default);



        Task<SupplierOfferAnnex> CreateOfferAsync(
           SupplierEntity supplier,
           string offerName,
           DateTime processingDate,
           string currency,
           string description,
           CancellationToken cancellationToken = default);

        /// <summary>
        /// Processes an Offer that includs a list of products with 
        /// </summary>
        /// <param name="offer">Supplier offer entity</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Processing results with statistics</returns>
        Task<FileProcessingResult> ProcessOfferAsync(
            SupplierOfferAnnex offer,
            SacksDataLayer.FileProcessing.Configuration.SupplierConfiguration supplierConfig,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Clears the EF change tracker to prevent entity tracking conflicts during reprocessing
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task representing the async operation</returns>
        Task ClearChangeTrackerAsync(CancellationToken cancellationToken = default);
    }
}
