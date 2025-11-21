using Bogus;
using SacksDataLayer.Entities;

namespace SacksMcp.Tests.Helpers;

/// <summary>
/// Builder class for creating test Product entities with realistic fake data.
/// Uses Fluent API pattern for easy test data construction.
/// </summary>
public class TestProductBuilder
{
    private readonly Faker _faker = new();
    private int _id = 0; // 0 means auto-generate ID (for integration tests)
    private string _name = "Test Product";
    private string? _ean = "1234567890123";
    private DateTime _createdAt = DateTime.UtcNow;
    private DateTime? _modifiedAt = null;
    private Dictionary<string, object?>? _dynamicProperties = null;
    private List<ProductOffer> _offerProducts = new();

    public TestProductBuilder WithId(int id)
    {
        _id = id;
        return this;
    }

    public TestProductBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public TestProductBuilder WithRandomName()
    {
        _name = _faker.Commerce.ProductName();
        return this;
    }

    public TestProductBuilder WithEan(string ean)
    {
        _ean = ean;
        return this;
    }

    public TestProductBuilder WithRandomEan()
    {
        _ean = _faker.Commerce.Ean13();
        return this;
    }

    public TestProductBuilder WithoutEan()
    {
        _ean = null;
        return this;
    }

    public TestProductBuilder CreatedAt(DateTime createdAt)
    {
        _createdAt = createdAt;
        return this;
    }

    public TestProductBuilder ModifiedAt(DateTime modifiedAt)
    {
        _modifiedAt = modifiedAt;
        return this;
    }

    public TestProductBuilder WithDynamicProperties(Dictionary<string, object?> properties)
    {
        _dynamicProperties = properties;
        return this;
    }

    public TestProductBuilder WithOfferProducts(params ProductOffer[] offerProducts)
    {
        _offerProducts = offerProducts.ToList();
        return this;
    }

    public Product Build()
    {
        var product = new Product
        {
            Id = _id,
            Name = _name,
            EAN = _ean ?? string.Empty,
            CreatedAt = _createdAt,
            ModifiedAt = _modifiedAt,
            OfferProducts = _offerProducts
        };
        
        if (_dynamicProperties != null)
        {
            product.DynamicProperties = _dynamicProperties;
        }
        
        return product;
    }

    /// <summary>
    /// Builds a collection of products with sequential IDs and random data.
    /// </summary>
    public static List<Product> BuildMany(int count, Action<TestProductBuilder>? configure = null)
    {
        var products = new List<Product>();
        for (int i = 1; i <= count; i++)
        {
            var builder = new TestProductBuilder()
                .WithId(i)
                .WithRandomName()
                .WithRandomEan();
            
            configure?.Invoke(builder);
            products.Add(builder.Build());
        }
        return products;
    }
}
