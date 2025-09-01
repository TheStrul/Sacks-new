#nullable enable
using SacksDataLayer.Entities;
using System.Threading;

namespace SacksDataLayer.Repositories.Interfaces;

/// <summary>
/// Repository interface for managing suppliers
/// </summary>
public interface ISuppliersRepository
{
    /// <summary>
    /// Gets a supplier by ID
    /// </summary>
    Task<SupplierEntity?> GetByIdAsync(int id, CancellationToken cancellationToken);

    /// <summary>
    /// Gets a supplier by name
    /// </summary>
    Task<SupplierEntity?> GetByNameAsync(string name, CancellationToken cancellationToken);

    /// <summary>
    /// Gets all suppliers with pagination
    /// </summary>
    Task<IEnumerable<SupplierEntity>> GetPagedAsync(int skip, int take, CancellationToken cancellationToken);

    /// <summary>
    /// Gets all suppliers
    /// </summary>
    Task<IEnumerable<SupplierEntity>> GetAllAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Gets total count of suppliers
    /// </summary>
    Task<int> GetCountAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Creates a new supplier
    /// </summary>
    Task<SupplierEntity> CreateAsync(SupplierEntity supplier, CancellationToken cancellationToken);

    /// <summary>
    /// Updates an existing supplier
    /// </summary>
    Task<SupplierEntity> UpdateAsync(SupplierEntity supplier, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes a supplier
    /// </summary>
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken);

    /// <summary>
    /// Searches suppliers by various criteria
    /// </summary>
    Task<IEnumerable<SupplierEntity>> SearchAsync(string searchTerm, CancellationToken cancellationToken);

    /// <summary>
    /// Checks if a supplier exists by name
    /// </summary>
    Task<bool> ExistsAsync(string name, CancellationToken cancellationToken);
}
