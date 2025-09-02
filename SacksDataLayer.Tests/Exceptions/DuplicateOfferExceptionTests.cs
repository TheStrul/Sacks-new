using SacksDataLayer.Exceptions;
using Xunit;

namespace SacksDataLayer.Tests.Exceptions;

public class DuplicateOfferExceptionTests
{
    [Fact]
    public void Constructor_WithSupplierOfferAndFileName_SetsPropertiesCorrectly()
    {
        // Arrange
        var supplierName = "Test Supplier";
        var offerName = "Test Offer";
        var fileName = "test_file.xlsx";

        // Act
        var exception = new DuplicateOfferException(supplierName, offerName, fileName);

        // Assert
        Assert.Equal(supplierName, exception.SupplierName);
        Assert.Equal(offerName, exception.OfferName);
        Assert.Equal(fileName, exception.FileName);
        Assert.Contains(supplierName, exception.Message);
        Assert.Contains(offerName, exception.Message);
        Assert.Contains(fileName, exception.Message);
    }

    [Fact]
    public void Constructor_Default_SetsEmptyStrings()
    {
        // Act
        var exception = new DuplicateOfferException();

        // Assert
        Assert.Equal(string.Empty, exception.SupplierName);
        Assert.Equal(string.Empty, exception.OfferName);
        Assert.Equal(string.Empty, exception.FileName);
        Assert.Contains("duplicate offer", exception.Message.ToLower());
    }

    [Fact]
    public void Constructor_WithMessage_SetsMessageCorrectly()
    {
        // Arrange
        var message = "Custom error message";

        // Act
        var exception = new DuplicateOfferException(message);

        // Assert
        Assert.Equal(message, exception.Message);
        Assert.Equal(string.Empty, exception.SupplierName);
        Assert.Equal(string.Empty, exception.OfferName);
        Assert.Equal(string.Empty, exception.FileName);
    }
}
