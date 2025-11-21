using Bogus;
using Sacks.Core.Entities;

namespace SacksMcp.Tests.Helpers;

/// <summary>
/// Builder class for creating test Supplier entities.
/// </summary>
public class TestSupplierBuilder
{
    private readonly Faker _faker = new();
    private int _id = 0; // 0 means auto-generate ID (for integration tests)
    private string _name = "Test Supplier";
    private string? _description = null;
    private List<Offer> _offers = new();

    public TestSupplierBuilder WithId(int id)
    {
        _id = id;
        return this;
    }

    public TestSupplierBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public TestSupplierBuilder WithRandomName()
    {
        _name = _faker.Company.CompanyName();
        return this;
    }

    public TestSupplierBuilder WithDescription(string description)
    {
        _description = description;
        return this;
    }

    public TestSupplierBuilder WithOffers(params Offer[] offers)
    {
        _offers = offers.ToList();
        return this;
    }

    public Supplier Build()
    {
        return new Supplier
        {
            Id = _id,
            Name = _name,
            Description = _description,
            Offers = _offers
        };
    }

    public static List<Supplier> BuildMany(int count, Action<TestSupplierBuilder>? configure = null)
    {
        var suppliers = new List<Supplier>();
        for (int i = 1; i <= count; i++)
        {
            var builder = new TestSupplierBuilder()
                .WithId(i)
                .WithRandomName();
            
            configure?.Invoke(builder);
            suppliers.Add(builder.Build());
        }
        return suppliers;
    }
}
