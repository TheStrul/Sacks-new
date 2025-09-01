using SacksDataLayer.FileProcessing.Models;
using SacksDataLayer.Entities;

namespace SacksDataLayer.Services.Interfaces
{
    /// <summary>
    /// Service interface for managing suppliers
    /// </summary>
    public interface ISuppliersService
    {
        /// <summary>
        /// Gets a supplier by ID
        /// </summary>
        Task<SupplierEntity?> GetSupplierAsync(int id);

        /// <summary>
        /// Gets a supplier by name
        /// </summary>
        Task<SupplierEntity?> GetSupplierByNameAsync(string name);

        /// <summary>
        /// Gets all suppliers with pagination
        /// </summary>
        Task<(IEnumerable<SupplierEntity> Suppliers, int TotalCount)> GetSuppliersAsync(
            int pageNumber = 1, int pageSize = 50);

        /// <summary>
        /// Creates a new supplier
        /// </summary>
        Task<SupplierEntity> CreateSupplierAsync(SupplierEntity supplier, string? createdBy = null);

        /// <summary>
        /// Updates an existing supplier
        /// </summary>
        Task<SupplierEntity> UpdateSupplierAsync(SupplierEntity supplier, string? modifiedBy = null);

        /// <summary>
        /// Creates or gets an existing supplier based on configuration
        /// </summary>
        Task<SupplierEntity> CreateOrGetSupplierFromConfigAsync(string supplierName, 
            string? description = null, string? industry = null, string? region = null, 
            string? createdBy = null);

        /// <summary>
        /// Deletes a supplier
        /// </summary>
        Task<bool> DeleteSupplierAsync(int id);

        /// <summary>
        /// Searches suppliers by various criteria
        /// </summary>
        Task<IEnumerable<SupplierEntity>> SearchSuppliersAsync(string searchTerm);
    }
}
