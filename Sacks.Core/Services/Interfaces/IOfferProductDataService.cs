using Sacks.Core.Services.Models;
using System.Data;

namespace Sacks.Core.Services.Interfaces;

/// <summary>
/// Service for managing data persistence operations for ProductOffers
/// </summary>
public interface IOfferProductDataService
{
    /// <summary>
    /// Saves a cell change to the database
    /// </summary>
    Task<bool> SaveCellChangeAsync(
        string ean,
        string supplierName,
        string offerName,
        string columnName,
        string newValue,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a value for a specific column
    /// </summary>
    (bool IsValid, string? ErrorMessage) ValidateValue(string columnName, string? value);

    /// <summary>
    /// Saves all changes from a DataTable
    /// </summary>
    Task<SaveChangesResult> SaveAllChangesAsync(
        DataTable original,
        DataTable modified,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Extracts identifier from a DataRow
    /// </summary>
    OfferProductIdentifier? ExtractIdentifier(DataRow row);
}
