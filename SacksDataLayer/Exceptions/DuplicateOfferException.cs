namespace SacksDataLayer.Exceptions;

/// <summary>
/// Exception thrown when attempting to process a supplier offer that already exists
/// </summary>
public class DuplicateOfferException : InvalidOperationException
{
    public string SupplierName { get; }
    public string OfferName { get; }
    public string FileName { get; }

    public DuplicateOfferException()
        : base("A duplicate offer was detected.")
    {
        SupplierName = string.Empty;
        OfferName = string.Empty;
        FileName = string.Empty;
    }

    public DuplicateOfferException(string message)
        : base(message)
    {
        SupplierName = string.Empty;
        OfferName = string.Empty;
        FileName = string.Empty;
    }

    public DuplicateOfferException(string message, Exception innerException)
        : base(message, innerException)
    {
        SupplierName = string.Empty;
        OfferName = string.Empty;
        FileName = string.Empty;
    }

    public DuplicateOfferException(string supplierName, string offerName, string fileName)
        : base($"An offer with the name '{offerName}' already exists for supplier '{supplierName}'. " +
               $"Please rename the file '{fileName}' to process it as a new offer.")
    {
        SupplierName = supplierName;
        OfferName = offerName;
        FileName = fileName;
    }

    public DuplicateOfferException(string supplierName, string offerName, string fileName, Exception innerException)
        : base($"An offer with the name '{offerName}' already exists for supplier '{supplierName}'. " +
               $"Please rename the file '{fileName}' to process it as a new offer.", innerException)
    {
        SupplierName = supplierName;
        OfferName = offerName;
        FileName = fileName;
    }
}
