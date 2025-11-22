namespace Sacks.Core.Services.Models;

/// <summary>
/// Identifier for an OfferProduct record
/// </summary>
public sealed class OfferProductIdentifier
{
    public required string EAN { get; init; }
    public required string SupplierName { get; init; }
    public required string OfferName { get; init; }
}
