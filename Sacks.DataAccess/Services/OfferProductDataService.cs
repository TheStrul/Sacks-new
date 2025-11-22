using System.Data;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Sacks.Core.Services.Interfaces;
using Sacks.Core.Services.Models;
using Sacks.DataAccess.Data;

namespace Sacks.DataAccess.Services;

/// <summary>
/// Service for managing data persistence operations for ProductOffers
/// </summary>
public sealed class OfferProductDataService : IOfferProductDataService
{
    private readonly SacksDbContext _dbContext;
    private readonly ILogger<OfferProductDataService> _logger;

    public OfferProductDataService(SacksDbContext dbContext, ILogger<OfferProductDataService> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<bool> SaveCellChangeAsync(
        string ean, 
        string supplierName, 
        string offerName, 
        string columnName, 
        string newValue, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Saving cell change: EAN={EAN}, Supplier={Supplier}, Offer={Offer}, Column={Column}", 
                ean, supplierName, offerName, columnName);

            // Validate inputs
            if (string.IsNullOrWhiteSpace(ean) || string.IsNullOrWhiteSpace(supplierName) || string.IsNullOrWhiteSpace(offerName))
            {
                _logger.LogWarning("Invalid identifiers provided for cell save");
                return false;
            }

            // Validate the value for the column
            var validation = ValidateValue(columnName, newValue);
            if (!validation.IsValid)
            {
                _logger.LogWarning("Invalid value for column {Column}: {Error}", columnName, validation.ErrorMessage);
                return false;
            }

            // Find the entities
            var product = await _dbContext.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.EAN == ean, cancellationToken)
                .ConfigureAwait(false);

            if (product == null)
            {
                _logger.LogWarning("Product not found for EAN: {EAN}", ean);
                return false;
            }

            var supplier = await _dbContext.Suppliers
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Name == supplierName, cancellationToken)
                .ConfigureAwait(false);

            if (supplier == null)
            {
                _logger.LogWarning("Supplier not found: {SupplierName}", supplierName);
                return false;
            }

            var offer = await _dbContext.SupplierOffers
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.SupplierId == supplier.Id && o.OfferName == offerName, cancellationToken)
                .ConfigureAwait(false);

            if (offer == null)
            {
                _logger.LogWarning("Offer not found: {OfferName} for supplier {SupplierName}", offerName, supplierName);
                return false;
            }

            var offerProduct = await _dbContext.OfferProducts
                .FirstOrDefaultAsync(op => op.OfferId == offer.Id && op.ProductId == product.Id, cancellationToken)
                .ConfigureAwait(false);

            if (offerProduct == null)
            {
                _logger.LogWarning("ProductOffer not found for Product {ProductId} and Offer {OfferId}", product.Id, offer.Id);
                return false;
            }

            // Apply the change
            var success = ApplyChange(offerProduct, columnName, newValue);
            if (!success)
            {
                _logger.LogWarning("Failed to apply change to column {Column}", columnName);
                return false;
            }

            await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            
            _logger.LogDebug("Successfully saved cell change");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving cell change for EAN={EAN}, Column={Column}", ean, columnName);
            return false;
        }
    }

    public async Task<SaveChangesResult> SaveAllChangesAsync(
        DataTable original,
        DataTable modified, 
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(modified);

        var result = new SaveChangesResult();
        var errors = new List<string>();
        int totalChanges = 0;
        int successfulSaves = 0;

        try
        {
            var modifiedRows = modified.Rows.Cast<DataRow>()
                .Where(r => r.RowState == DataRowState.Modified)
                .ToList();

            foreach (var row in modifiedRows)
            {
                var identifier = ExtractIdentifier(row);
                if (identifier == null)
                {
                    errors.Add("Row missing required identification fields");
                    continue;
                }

                foreach (DataColumn column in modified.Columns)
                {
                    var columnName = column.ColumnName;
                    
                    // Skip non-editable columns
                    if (IsNonEditableColumn(columnName))
                        continue;

                    var currentValue = row[column, DataRowVersion.Current];
                    var originalValue = row[column, DataRowVersion.Original];
                    
                    if (!Equals(currentValue, originalValue))
                    {
                        totalChanges++;
                        var newValueStr = currentValue?.ToString() ?? string.Empty;
                        
                        var saved = await SaveCellChangeAsync(
                            identifier.EAN,
                            identifier.SupplierName,
                            identifier.OfferName,
                            columnName,
                            newValueStr,
                            cancellationToken).ConfigureAwait(false);

                        if (saved)
                        {
                            successfulSaves++;
                        }
                        else
                        {
                            errors.Add($"Failed to save {columnName} for {identifier.EAN}");
                        }
                    }
                }
            }

            return new SaveChangesResult
            {
                TotalChanges = totalChanges,
                SuccessfulSaves = successfulSaves,
                Errors = errors
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during batch save operation");
            errors.Add($"Batch save failed: {ex.Message}");
            
            return new SaveChangesResult
            {
                TotalChanges = totalChanges,
                SuccessfulSaves = successfulSaves,
                Errors = errors
            };
        }
    }

    public (bool IsValid, string? ErrorMessage) ValidateValue(string columnName, string? value)
    {
        switch (columnName)
        {
            case "Price":
                if (!decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var price))
                    return (false, "Invalid decimal format for Price");
                if (price < 0)
                    return (false, "Price cannot be negative");
                break;

            case "Currency":
                if (string.IsNullOrWhiteSpace(value))
                    return (false, "Currency cannot be empty");
                if (value.Length > 3)
                    return (false, "Currency code cannot exceed 3 characters");
                break;

            case "Quantity":
                if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var quantity))
                    return (false, "Invalid integer format for Quantity");
                if (quantity < 0)
                    return (false, "Quantity cannot be negative");
                break;

            case "Description":
            case "Details":
                // These can be any string value including empty
                break;

            default:
                return (false, $"Column '{columnName}' is not supported for editing");
        }

        return (true, null);
    }

    public OfferProductIdentifier? ExtractIdentifier(DataRow row)
    {
        ArgumentNullException.ThrowIfNull(row);
        try
        {
            var ean = GetFirstNonEmpty(row, "EAN");
            var supplierName = GetFirstNonEmpty(row, "Supplier Name", "SupplierName", "S_Name");
            var offerName = GetFirstNonEmpty(row, "Offer Name", "OfferName", "O_Name");

            if (string.IsNullOrWhiteSpace(ean) || 
                string.IsNullOrWhiteSpace(supplierName) || 
                string.IsNullOrWhiteSpace(offerName))
            {
                return null;
            }

            return new OfferProductIdentifier
            {
                EAN = ean,
                SupplierName = supplierName,
                OfferName = offerName
            };
        }
        catch
        {
            return null;
        }
    }

    private static string? GetFirstNonEmpty(DataRow row, params string[] columnNames)
    {
        foreach (var name in columnNames)
        {
            if (!row.Table.Columns.Contains(name)) 
                continue;
                
            var value = row[name]?.ToString();
            if (!string.IsNullOrWhiteSpace(value)) 
                return value;
        }
        return null;
    }

    private static bool IsNonEditableColumn(string columnName)
    {
        return string.Equals(columnName, "Row #", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(columnName, "EAN", StringComparison.OrdinalIgnoreCase);
    }

    private bool ApplyChange(Sacks.Core.Entities.ProductOffer offerProduct, string columnName, string newValue)
    {
        try
        {
            switch (columnName)
            {
                case "Price":
                    if (decimal.TryParse(newValue, NumberStyles.Number, CultureInfo.InvariantCulture, out var price))
                    {
                        offerProduct.Price = price;
                        return true;
                    }
                    break;

                case "Currency":
                    var currency = (newValue ?? string.Empty).Trim().ToUpperInvariant();
                    if (currency.Length > 3) 
                        currency = currency[..3];
                    offerProduct.Currency = currency;
                    return true;

                case "Quantity":
                    if (int.TryParse(newValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var quantity))
                    {
                        offerProduct.Quantity = quantity;
                        return true;
                    }
                    break;

                case "Description":
                case "Details":
                    offerProduct.Description = newValue;
                    return true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying change to column {Column}", columnName);
        }

        return false;
    }
}
