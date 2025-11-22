namespace Sacks.Core.Services.Models;

/// <summary>
/// Entity counts for database statistics
/// </summary>
public sealed class EntityCounts
{
    public int Products { get; init; }
    public int Suppliers { get; init; }
    public int SupplierOffers { get; init; }
    public int ProductOffers { get; init; }
}
