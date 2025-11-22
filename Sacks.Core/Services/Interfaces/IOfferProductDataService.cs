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
}
