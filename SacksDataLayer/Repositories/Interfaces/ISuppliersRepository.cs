namespace SacksDataLayer.Repositories.Interfaces;

public interface ISuppliersRepository
{
    Task<SupplierEntity?> GetByNameAsync(string name);
    Task<SupplierEntity> CreateAsync(SupplierEntity supplier);
    Task<SupplierEntity> UpdateAsync(SupplierEntity supplier);
    Task<IEnumerable<SupplierEntity>> GetAllAsync();
    Task<bool> DeleteAsync(int id);
}
