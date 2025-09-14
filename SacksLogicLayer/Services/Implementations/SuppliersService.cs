using SacksDataLayer.Entities;
using SacksDataLayer.Repositories.Interfaces;
using SacksLogicLayer.Services.Interfaces;

namespace SacksLogicLayer.Services.Implementations
{
    /// <summary>
    /// Service implementation for managing suppliers
    /// </summary>
    public class SuppliersService : ISuppliersService
    {
        private readonly ITransactionalSuppliersRepository _suppliersRepository;
        private readonly IUnitOfWork _unitOfWork;

        public SuppliersService(
            ITransactionalSuppliersRepository suppliersRepository, 
            IUnitOfWork unitOfWork)
        {
            _suppliersRepository = suppliersRepository ?? throw new ArgumentNullException(nameof(suppliersRepository));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        /// <summary>
        /// Gets a supplier by ID
        /// </summary>
        public async Task<SupplierEntity?> GetSupplierAsync(int id)
        {
            return await _suppliersRepository.GetByIdAsync(id, CancellationToken.None);
        }

        /// <summary>
        /// Gets a supplier by name
        /// </summary>
        public async Task<SupplierEntity?> GetSupplierByNameAsync(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Supplier name cannot be null or empty", nameof(name));

            return await _suppliersRepository.GetByNameAsync(name, CancellationToken.None);
        }

        /// <summary>
        /// Gets all suppliers with pagination
        /// </summary>
        public async Task<(IEnumerable<SupplierEntity> Suppliers, int TotalCount)> GetSuppliersAsync(
            int pageNumber = 1, int pageSize = 50)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 50;
            if (pageSize > 100) pageSize = 100; // Max page size

            var skip = (pageNumber - 1) * pageSize;
            var suppliers = await _suppliersRepository.GetPagedAsync(skip, pageSize, CancellationToken.None);
            var totalCount = await _suppliersRepository.GetCountAsync(CancellationToken.None);

            return (suppliers, totalCount);
        }

        /// <summary>
        /// Creates a new supplier
        /// </summary>
        public async Task<SupplierEntity> CreateSupplierAsync(SupplierEntity supplier, string? createdBy = null)
        {
            if (supplier == null)
                throw new ArgumentNullException(nameof(supplier));

            if (string.IsNullOrWhiteSpace(supplier.Name))
                throw new ArgumentException("Supplier name is required", nameof(supplier));

            return await _unitOfWork.ExecuteInTransactionAsync(async ct =>
            {
                // Check if supplier with the same name already exists
                var existingSupplier = await _suppliersRepository.GetByNameAsync(supplier.Name, ct);
                if (existingSupplier != null)
                    throw new InvalidOperationException($"Supplier with name '{supplier.Name}' already exists");

                supplier.CreatedAt = DateTime.UtcNow;
                _suppliersRepository.Add(supplier);
                
                await _unitOfWork.SaveChangesAsync(ct);
                return supplier;
            });
        }

        /// <summary>
        /// Updates an existing supplier
        /// </summary>
        public async Task<SupplierEntity> UpdateSupplierAsync(SupplierEntity supplier, string? modifiedBy = null)
        {
            if (supplier == null)
                throw new ArgumentNullException(nameof(supplier));

            if (supplier.Id <= 0)
                throw new ArgumentException("Supplier ID must be provided for update", nameof(supplier));

            if (string.IsNullOrWhiteSpace(supplier.Name))
                throw new ArgumentException("Supplier name is required", nameof(supplier));

            return await _unitOfWork.ExecuteInTransactionAsync(async ct =>
            {
                var existingSupplier = await _suppliersRepository.GetByIdAsync(supplier.Id, ct);
                if (existingSupplier == null)
                    throw new InvalidOperationException($"Supplier with ID {supplier.Id} not found");

                // Check if another supplier with the same name exists (excluding current supplier)
                var nameConflict = await _suppliersRepository.GetByNameAsync(supplier.Name, ct);
                if (nameConflict != null && nameConflict.Id != supplier.Id)
                    throw new InvalidOperationException($"Another supplier with name '{supplier.Name}' already exists");

                // Update properties
                existingSupplier.Name = supplier.Name;
                existingSupplier.Description = supplier.Description;
                existingSupplier.UpdateModified();

                _suppliersRepository.Update(existingSupplier);
                await _unitOfWork.SaveChangesAsync(ct);
                
                return existingSupplier;
            });
        }

        /// <summary>
        /// Creates or gets an existing supplier based on configuration
        /// NOTE: Does not manage transactions - caller must handle transaction scope
        /// </summary>
        public async Task<SupplierEntity> CreateOrGetSupplierByName(string supplierName, 
            string? description = null)
        {
            if (string.IsNullOrWhiteSpace(supplierName))
                throw new ArgumentException("Supplier name cannot be null or empty", nameof(supplierName));

            // Try to get existing supplier first
            var existingSupplier = await _suppliersRepository.GetByNameAsync(supplierName, CancellationToken.None);
            if (existingSupplier != null)
            {
                return existingSupplier;
            }

            // Create new supplier without transaction wrapper (caller manages transaction)
            var newSupplier = new SupplierEntity
            {
                Name = supplierName,
                Description = description,
                CreatedAt = DateTime.UtcNow
            };

            _suppliersRepository.Add(newSupplier);
            return newSupplier;
        }

        /// <summary>
        /// Deletes a supplier
        /// </summary>
        public async Task<bool> DeleteSupplierAsync(int id)
        {
            if (id <= 0)
                throw new ArgumentException("Supplier ID must be greater than 0", nameof(id));

            return await _unitOfWork.ExecuteInTransactionAsync(async ct =>
            {
                var success = await _suppliersRepository.RemoveByIdAsync(id, ct);
                if (success)
                {
                    await _unitOfWork.SaveChangesAsync(ct);
                }
                return success;
            });
        }

        /// <summary>
        /// Searches suppliers by various criteria
        /// </summary>
        public async Task<IEnumerable<SupplierEntity>> SearchSuppliersAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return Enumerable.Empty<SupplierEntity>();

            return await _suppliersRepository.SearchAsync(searchTerm, CancellationToken.None);
        }
    }
}
