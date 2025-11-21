using Sacks.Core.Entities;

namespace SacksMcp.Tests.Helpers;

/// <summary>
/// Fluent builder for creating test ProductOffer entities.
/// </summary>
public class TestProductOfferBuilder
{
    private int _id = 0; // 0 means auto-generate ID (for integration tests)
    private int _productId = 0; // 0 means not set, will be determined by navigation property
    private int _offerId = 0; // 0 means not set, will be determined by navigation property
    private Product? _product;
    private Offer? _offer;
    private decimal _price = 10.00m;
    private string _currency = "USD";
    private string? _description;
    private DateTime _createdAt = DateTime.UtcNow;
    private DateTime _modifiedAt = DateTime.UtcNow;

    public TestProductOfferBuilder WithId(int id)
    {
        _id = id;
        return this;
    }

    public TestProductOfferBuilder WithProductId(int productId)
    {
        _productId = productId;
        return this;
    }

    public TestProductOfferBuilder WithOfferId(int offerId)
    {
        _offerId = offerId;
        return this;
    }

    public TestProductOfferBuilder WithProduct(Product product)
    {
        ArgumentNullException.ThrowIfNull(product);
        _product = product;
        _productId = product.Id;
        return this;
    }

    public TestProductOfferBuilder WithOffer(Offer offer)
    {
        ArgumentNullException.ThrowIfNull(offer);
        _offer = offer;
        _offerId = offer.Id;
        return this;
    }

    public TestProductOfferBuilder WithPrice(decimal price)
    {
        _price = price;
        return this;
    }

    public TestProductOfferBuilder WithCurrency(string currency)
    {
        _currency = currency;
        return this;
    }

    public TestProductOfferBuilder WithDescription(string? description)
    {
        _description = description;
        return this;
    }

    public TestProductOfferBuilder WithCreatedAt(DateTime createdAt)
    {
        _createdAt = createdAt;
        return this;
    }

    public TestProductOfferBuilder WithModifiedAt(DateTime modifiedAt)
    {
        _modifiedAt = modifiedAt;
        return this;
    }

    public ProductOffer Build()
    {
#pragma warning disable CS8601 // Possible null reference assignment - EF will handle this
        var productOffer = new ProductOffer
        {
            ProductId = _productId,
            OfferId = _offerId,
            Product = _product,
            Offer = _offer,
            Price = _price,
            Currency = _currency,
            Description = _description
        };
#pragma warning restore CS8601

        // Use reflection to set readonly properties
        typeof(ProductOffer).GetProperty(nameof(ProductOffer.Id))?.SetValue(productOffer, _id);
        typeof(ProductOffer).GetProperty(nameof(ProductOffer.CreatedAt))?.SetValue(productOffer, _createdAt);
        typeof(ProductOffer).GetProperty(nameof(ProductOffer.ModifiedAt))?.SetValue(productOffer, _modifiedAt);

        return productOffer;
    }

    public static TestProductOfferBuilder Create() => new();
}
