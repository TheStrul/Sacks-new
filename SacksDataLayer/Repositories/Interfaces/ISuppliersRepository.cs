using SacksDataLayer.Entities;

namespace SacksDataLayer.Repositories.Interfaces;

/// <summary>
/// Repository interface for managing suppliers
/// </summary>
public interface ISuppliersRepository
{
    /// <summary>
    /// Gets a supplier by ID
    /// </summary>
    Task<SupplierEntity?> GetByIdAsync(int id);

    /// <summary>
    /// Gets a supplier by name
    /// </summary>
    Task<SupplierEntity?> GetByNameAsync(string name);

    /// <summary>
    /// Gets all suppliers with pagination
    /// </summary>
    Task<IEnumerable<SupplierEntity>> GetPagedAsync(int skip, int take);

    /// <summary>
    /// Gets all suppliers
    /// </summary>
    Task<IEnumerable<SupplierEntity>> GetAllAsync();

    /// <summary>
    /// Gets total count of suppliers
    /// </summary>
    Task<int> GetCountAsync();

    /// <summary>
    /// Creates a new supplier
    /// </summary>
    Task<SupplierEntity> CreateAsync(SupplierEntity supplier);

    /// <summary>
    /// Updates an existing supplier
    /// </summary>
    Task<SupplierEntity> UpdateAsync(SupplierEntity supplier);

    /// <summary>
    /// Deletes a supplier
    /// </summary>
    Task<bool> DeleteAsync(int id);

    /// <summary>
    /// Searches suppliers by various criteria
    /// </summary>
    Task<IEnumerable<SupplierEntity>> SearchAsync(string searchTerm);

    /// <summary>
    /// Checks if a supplier exists by name
    /// </summary>
    Task<bool> ExistsAsync(string name);
}
