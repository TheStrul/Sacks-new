namespace Sacks.Core.Services.Interfaces
{
    using Sacks.Core.Entities;

    /// <summary>
    /// Service interface for managing suppliers
    /// </summary>
    public interface ISuppliersService
    {
        /// <summary>
        /// Gets a supplier by ID
        /// </summary>
        Task<Supplier?> GetSupplierAsync(int id);

        /// <summary>
        /// Gets a supplier by name
        /// </summary>
        Task<Supplier?> GetSupplierByNameAsync(string name);

        /// <summary>
        /// Gets all suppliers with pagination
        /// </summary>
        Task<(IEnumerable<Supplier> Suppliers, int TotalCount)> GetSuppliersAsync(
            int pageNumber = 1, int pageSize = 50);

        /// <summary>
        /// Creates a new supplier
        /// </summary>
        Task<Supplier> CreateSupplierAsync(Supplier supplier, string? createdBy = null);

        /// <summary>
        /// Updates an existing supplier
        /// </summary>
        Task<Supplier> UpdateSupplierAsync(Supplier supplier, string? modifiedBy = null);

        /// <summary>
        /// Creates or gets an existing supplier based on configuration
        /// </summary>
        Task<Supplier> CreateOrGetSupplierByName(string supplierName, 
            string? description = null);

        /// <summary>
        /// Deletes a supplier
        /// </summary>
        Task<bool> DeleteSupplierAsync(int id);

        /// <summary>
        /// Searches suppliers by various criteria
        /// </summary>
        Task<IEnumerable<Supplier>> SearchSuppliersAsync(string searchTerm);
    }
}
