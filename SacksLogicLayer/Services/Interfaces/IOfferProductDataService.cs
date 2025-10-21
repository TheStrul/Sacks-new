using System.Data;

namespace SacksLogicLayer.Services.Interfaces
{
    /// <summary>
    /// Service for managing data persistence operations for ProductOffers
    /// </summary>
    public interface IOfferProductDataService
    {
        /// <summary>
        /// Saves changes for a specific cell/field in an OfferProduct
        /// </summary>
        /// <param name="ean">Product EAN identifier</param>
        /// <param name="supplierName">Supplier name</param>
        /// <param name="offerName">Offer name</param>
        /// <param name="columnName">Column/field name being updated</param>
        /// <param name="newValue">New value to save</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if save was successful, false otherwise</returns>
        Task<bool> SaveCellChangeAsync(
            string ean, 
            string supplierName, 
            string offerName, 
            string columnName, 
            string newValue, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Saves all pending changes in a DataTable
        /// </summary>
        /// <param name="dataTable">DataTable containing changes</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Result indicating success/failure counts</returns>
        Task<SaveChangesResult> SaveAllChangesAsync(
            DataTable dataTable, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates that a value is appropriate for a specific column
        /// </summary>
        /// <param name="columnName">Column name</param>
        /// <param name="value">Value to validate</param>
        /// <returns>Validation result with any error messages</returns>
        ValidationResult ValidateValue(string columnName, string? value);

        /// <summary>
        /// Extracts identifying information from a DataRow
        /// </summary>
        /// <param name="row">DataRow containing offer product data</param>
        /// <returns>Identifying information or null if insufficient data</returns>
        OfferProductIdentifier? ExtractIdentifier(DataRow row);
    }

    /// <summary>
    /// Result of a save changes operation
    /// </summary>
    public sealed class SaveChangesResult
    {
        public int TotalChanges { get; init; }
        public int SuccessfulSaves { get; init; }
        public List<string> Errors { get; init; } = new();
        
        public bool IsFullySuccessful => SuccessfulSaves == TotalChanges && TotalChanges > 0;
        public bool HasAnySuccess => SuccessfulSaves > 0;
        public bool HasErrors => Errors.Count > 0;
    }

    /// <summary>
    /// Validation result for a field value
    /// </summary>
    public sealed class ValidationResult
    {
        public bool IsValid { get; init; }
        public string? ErrorMessage { get; init; }
        
        public static ValidationResult Success() => new() { IsValid = true };
        public static ValidationResult Error(string message) => new() { IsValid = false, ErrorMessage = message };
    }

    /// <summary>
    /// Identifies a specific OfferProduct record
    /// </summary>
    public sealed class OfferProductIdentifier
    {
        public required string EAN { get; init; }
        public required string SupplierName { get; init; }
        public required string OfferName { get; init; }
    }
}