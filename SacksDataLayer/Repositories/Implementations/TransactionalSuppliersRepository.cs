using Microsoft.EntityFrameworkCore;
using SacksDataLayer.Data;
using SacksDataLayer.Repositories.Interfaces;
using SacksDataLayer.Entities;

namespace SacksDataLayer.Repositories.Implementations
{
    /// <summary>
    /// Transaction-aware repository implementation for Suppliers
    /// All operations are tracked in context but not automatically saved - use with UnitOfWork
    /// </summary>
    public class TransactionalSuppliersRepository : ITransactionalSuppliersRepository
    {
        private readonly SacksDbContext _context;

        public TransactionalSuppliersRepository(SacksDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        #region Query Operations

        public async Task<SupplierEntity?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _context.Suppliers
                .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        }

        public async Task<SupplierEntity?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(name))
                return null;

            return await _context.Suppliers
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Name == name, cancellationToken);
        }

        public async Task<IEnumerable<SupplierEntity>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Suppliers
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<SupplierEntity>> GetPagedAsync(int skip, int take, CancellationToken cancellationToken = default)
        {
            return await _context.Suppliers
                .AsNoTracking()
                .Skip(skip)
                .Take(take)
                .ToListAsync(cancellationToken);
        }

        public async Task<int> GetCountAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Suppliers.CountAsync(cancellationToken);
        }

        public async Task<bool> ExistsAsync(string name, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;

            return await _context.Suppliers.AnyAsync(s => s.Name == name, cancellationToken);
        }

        public async Task<IEnumerable<SupplierEntity>> SearchAsync(string searchTerm, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return Enumerable.Empty<SupplierEntity>();

            var lowerSearchTerm = searchTerm.ToLower();
            return await _context.Suppliers
                .AsNoTracking()
                .Where(s => s.Name.ToLower().Contains(lowerSearchTerm) ||
                           (s.Description != null && s.Description.ToLower().Contains(lowerSearchTerm)))
                .ToListAsync(cancellationToken);
        }

        #endregion

        #region Transaction-Aware CRUD Operations

        public void Add(SupplierEntity supplier)
        {
            if (supplier == null)
                throw new ArgumentNullException(nameof(supplier));

            supplier.CreatedAt = DateTime.UtcNow;
            _context.Suppliers.Add(supplier);
        }

        public void AddRange(IEnumerable<SupplierEntity> suppliers)
        {
            if (suppliers == null)
                throw new ArgumentNullException(nameof(suppliers));

            var suppliersToAdd = suppliers.ToList();
            foreach (var supplier in suppliersToAdd)
            {
                supplier.CreatedAt = DateTime.UtcNow;
            }

            _context.Suppliers.AddRange(suppliersToAdd);
        }

        public void Update(SupplierEntity supplier)
        {
            if (supplier == null)
                throw new ArgumentNullException(nameof(supplier));

            supplier.ModifiedAt = DateTime.UtcNow;
            _context.Suppliers.Update(supplier);
        }

        public void UpdateRange(IEnumerable<SupplierEntity> suppliers)
        {
            if (suppliers == null)
                throw new ArgumentNullException(nameof(suppliers));

            var suppliersToUpdate = suppliers.ToList();
            foreach (var supplier in suppliersToUpdate)
            {
                supplier.ModifiedAt = DateTime.UtcNow;
            }

            _context.Suppliers.UpdateRange(suppliersToUpdate);
        }

        public void Remove(SupplierEntity supplier)
        {
            if (supplier == null)
                throw new ArgumentNullException(nameof(supplier));

            _context.Suppliers.Remove(supplier);
        }

        public void RemoveRange(IEnumerable<SupplierEntity> suppliers)
        {
            if (suppliers == null)
                throw new ArgumentNullException(nameof(suppliers));

            _context.Suppliers.RemoveRange(suppliers);
        }

        public async Task<bool> RemoveByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var supplier = await _context.Suppliers.FindAsync(new object[] { id }, cancellationToken);
            if (supplier == null)
                return false;

            _context.Suppliers.Remove(supplier);
            return true;
        }

        #endregion
    }
}