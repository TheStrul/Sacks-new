#nullable enable
using SacksDataLayer.Entities;
using System.Threading;

namespace SacksDataLayer.Repositories.Interfaces;

public interface ISupplierOffersRepository
{
    Task<SupplierOfferEntity?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<SupplierOfferEntity?> GetActiveOfferAsync(int productId, int supplierId, CancellationToken cancellationToken);
    Task<SupplierOfferEntity> CreateAsync(SupplierOfferEntity offer, CancellationToken cancellationToken);
    Task<SupplierOfferEntity> UpdateAsync(SupplierOfferEntity offer, CancellationToken cancellationToken);
    Task<IEnumerable<SupplierOfferEntity>> GetByProductIdAsync(int productId, CancellationToken cancellationToken);
    Task<IEnumerable<SupplierOfferEntity>> GetBySupplierIdAsync(int supplierId, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken);
    Task DeactivateOldOffersAsync(int productId, int supplierId, CancellationToken cancellationToken);
}
