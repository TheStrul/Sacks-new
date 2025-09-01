using SacksDataLayer.Entities;

namespace SacksDataLayer.Repositories.Interfaces;

public interface ISupplierOffersRepository
{
    Task<SupplierOfferEntity?> GetByIdAsync(int id);
    Task<SupplierOfferEntity?> GetActiveOfferAsync(int productId, int supplierId);
    Task<SupplierOfferEntity> CreateAsync(SupplierOfferEntity offer);
    Task<SupplierOfferEntity> UpdateAsync(SupplierOfferEntity offer);
    Task<IEnumerable<SupplierOfferEntity>> GetByProductIdAsync(int productId);
    Task<IEnumerable<SupplierOfferEntity>> GetBySupplierIdAsync(int supplierId);
    Task<bool> DeleteAsync(int id);
    Task DeactivateOldOffersAsync(int productId, int supplierId);
}
