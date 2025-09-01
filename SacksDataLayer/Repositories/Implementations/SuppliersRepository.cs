using Microsoft.EntityFrameworkCore;
using SacksDataLayer.Data;
using SacksDataLayer.Repositories.Interfaces;
using SacksDataLayer.Entities;

namespace SacksDataLayer.Repositories.Implementations
{
    /// <summary>
    /// Repository implementation for managing suppliers
    /// </summary>
    public class SuppliersRepository : ISuppliersRepository
    {
        private readonly SacksDbContext _context;

        public SuppliersRepository(SacksDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<SupplierEntity?> GetByIdAsync(int id, CancellationToken cancellationToken)
        {
            return await _context.Suppliers
                .Include(s => s.Offers)
                .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        }

        public async Task<SupplierEntity?> GetByNameAsync(string name, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(name))
                return null;

            return await _context.Suppliers
                .Include(s => s.Offers)
                .FirstOrDefaultAsync(s => s.Name == name, cancellationToken);
        }

        public async Task<IEnumerable<SupplierEntity>> GetPagedAsync(int skip, int take, CancellationToken cancellationToken)
        {
            return await _context.Suppliers
                .Include(s => s.Offers)
                .Skip(skip)
                .Take(take)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<SupplierEntity>> GetAllAsync(CancellationToken cancellationToken)
        {
            return await _context.Suppliers
                .Include(s => s.Offers)
                .ToListAsync(cancellationToken);
        }

        public async Task<int> GetCountAsync(CancellationToken cancellationToken)
        {
            return await _context.Suppliers.CountAsync(cancellationToken);
        }

        public async Task<SupplierEntity> CreateAsync(SupplierEntity supplier, CancellationToken cancellationToken)
        {
            if (supplier == null)
                throw new ArgumentNullException(nameof(supplier));

            supplier.UpdateModified();
            _context.Suppliers.Add(supplier);
            await _context.SaveChangesAsync(cancellationToken);
            return supplier;
        }

        public async Task<SupplierEntity> UpdateAsync(SupplierEntity supplier, CancellationToken cancellationToken)
        {
            if (supplier == null)
                throw new ArgumentNullException(nameof(supplier));

            supplier.UpdateModified();
            _context.Suppliers.Update(supplier);
            await _context.SaveChangesAsync(cancellationToken);
            return supplier;
        }

        public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken)
        {
            var supplier = await _context.Suppliers.FindAsync(new object[] { id }, cancellationToken);
            if (supplier == null)
                return false;

            _context.Suppliers.Remove(supplier);
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<bool> ExistsAsync(string name, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;

            return await _context.Suppliers.AnyAsync(s => s.Name == name, cancellationToken);
        }

        public async Task<IEnumerable<SupplierEntity>> SearchAsync(string searchTerm, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return Enumerable.Empty<SupplierEntity>();

            return await _context.Suppliers
                .Include(s => s.Offers)
                .Where(s => s.Name.Contains(searchTerm) || 
                           (s.Description != null && s.Description.Contains(searchTerm)) ||
                           (s.Company != null && s.Company.Contains(searchTerm)))
                .ToListAsync(cancellationToken);
        }
    }
}
