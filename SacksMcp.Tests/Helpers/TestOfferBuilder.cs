using Bogus;
using Sacks.Core.Entities;

namespace SacksMcp.Tests.Helpers;

/// <summary>
/// Builder class for creating test Offer entities.
/// </summary>
public class TestOfferBuilder
{
    private readonly Faker _faker = new();
    private int _id = 0; // 0 means auto-generate ID (for integration tests)
    private string? _offerName = "Test Offer";
    private string? _description = null;
    private int _supplierId = 1;
    private Supplier? _supplier = null;
    private string _currency = "USD";
    private DateTime _createdAt = DateTime.UtcNow;
    private DateTime? _modifiedAt = null;
    private List<ProductOffer> _offerProducts = new();

    public TestOfferBuilder WithId(int id)
    {
        _id = id;
        return this;
    }

    public TestOfferBuilder WithOfferName(string offerName)
    {
        _offerName = offerName;
        return this;
    }

    public TestOfferBuilder WithRandomOfferName()
    {
        _offerName = $"{_faker.Commerce.Department()} {_faker.Date.Month()} Offer";
        return this;
    }

    public TestOfferBuilder WithDescription(string description)
    {
        _description = description;
        return this;
    }

    public TestOfferBuilder WithSupplierId(int supplierId)
    {
        _supplierId = supplierId;
        return this;
    }

    public TestOfferBuilder WithSupplier(Supplier supplier)
    {
        ArgumentNullException.ThrowIfNull(supplier);
        _supplier = supplier;
        _supplierId = supplier.Id;
        return this;
    }

    public TestOfferBuilder WithCurrency(string currency)
    {
        _currency = currency;
        return this;
    }

    public TestOfferBuilder CreatedAt(DateTime createdAt)
    {
        _createdAt = createdAt;
        return this;
    }

    public TestOfferBuilder ModifiedAt(DateTime modifiedAt)
    {
        _modifiedAt = modifiedAt;
        return this;
    }

    public TestOfferBuilder WithOfferProducts(params ProductOffer[] offerProducts)
    {
        _offerProducts = offerProducts.ToList();
        return this;
    }

    public Offer Build()
    {
        return new Offer
        {
            Id = _id,
            OfferName = _offerName!,
            Description = _description,
            SupplierId = _supplierId,
            Supplier = _supplier,
            Currency = _currency,
            CreatedAt = _createdAt,
            ModifiedAt = _modifiedAt,
            OfferProducts = _offerProducts
        };
    }

    public static List<Offer> BuildMany(int count, Action<TestOfferBuilder>? configure = null)
    {
        var offers = new List<Offer>();
        for (int i = 1; i <= count; i++)
        {
            var builder = new TestOfferBuilder()
                .WithId(i)
                .WithRandomOfferName();
            
            configure?.Invoke(builder);
            offers.Add(builder.Build());
        }
        return offers;
    }
}
