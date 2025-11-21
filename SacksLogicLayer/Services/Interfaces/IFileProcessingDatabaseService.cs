namespace SacksLogicLayer.Services.Interfaces
{
    using Sacks.Core.Entities;
    using Sacks.Core.FileProcessing.Configuration;
    using Sacks.Core.FileProcessing.Models;

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
        Task<Supplier> CreateOrGetSupplierAsync(
            SupplierConfiguration supplierConfig, 
            CancellationToken cancellationToken = default);



        Task<Offer> CreateOfferAsync(
           Supplier supplier,
           string offerName,
           DateTime processingDate,
           string currency,
           string description,
           CancellationToken cancellationToken = default);

    /// <summary>
    /// Processes an Offer using the provided ProcessingContext. The implementation should
    /// update context.ProcessingResult.Statistics in-place.
    /// </summary>
        Task ProcessOfferAsync(
            ProcessingContext context,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Clears the EF change tracker to prevent entity tracking conflicts during reprocessing
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task representing the async operation</returns>
        Task ClearChangeTrackerAsync(CancellationToken cancellationToken = default);
    }
}
