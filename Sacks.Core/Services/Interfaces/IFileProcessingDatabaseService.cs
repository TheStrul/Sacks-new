using Sacks.Core.Entities;
using Sacks.Core.FileProcessing.Configuration;
using Sacks.Core.FileProcessing.Models;

namespace Sacks.Core.Services.Interfaces;

/// <summary>
/// Service for database operations during file processing
/// </summary>
public interface IFileProcessingDatabaseService
{
    /// <summary>
    /// Ensures the database is ready for file processing operations
    /// </summary>
    Task EnsureDatabaseReadyAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates or retrieves a supplier based on the supplier configuration
    /// </summary>
    Task<Supplier> CreateOrGetSupplierAsync(
        SupplierConfiguration supplierConfig,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new offer for the file processing session
    /// </summary>
    Task<Offer> CreateOfferAsync(
        Supplier supplier,
        string offerName,
        DateTime processingDate,
        string currency,
        string description,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Processes a batch of products with optimized bulk operations
    /// </summary>
    Task ProcessOfferAsync(
        ProcessingContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears the EF change tracker to prevent entity tracking conflicts
    /// </summary>
    Task ClearChangeTrackerAsync(CancellationToken cancellationToken = default);
}
