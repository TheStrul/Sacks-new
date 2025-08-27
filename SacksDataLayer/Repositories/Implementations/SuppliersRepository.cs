using Microsoft.EntityFrameworkCore;
using SacksDataLayer.Data;
using SacksDataLayer.Repositories.Interfaces;

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

        public async Task<SupplierEntity?> GetByIdAsync(int id)
        {
            return await _context.Suppliers
                .Include(s => s.Offers)
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task<SupplierEntity?> GetByNameAsync(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return null;

            return await _context.Suppliers
                .Include(s => s.Offers)
                .FirstOrDefaultAsync(s => s.Name == name);
        }

        public async Task<IEnumerable<SupplierEntity>> GetPagedAsync(int skip, int take)
        {
            return await _context.Suppliers
                .Include(s => s.Offers)
                .Skip(skip)
                .Take(take)
                .ToListAsync();
        }

        public async Task<IEnumerable<SupplierEntity>> GetAllAsync()
        {
            return await _context.Suppliers
                .Include(s => s.Offers)
                .ToListAsync();
        }

        public async Task<int> GetCountAsync()
        {
            return await _context.Suppliers.CountAsync();
        }

        public async Task<SupplierEntity> CreateAsync(SupplierEntity supplier)
        {
            if (supplier == null)
                throw new ArgumentNullException(nameof(supplier));

            supplier.UpdateModified();
            _context.Suppliers.Add(supplier);
            await _context.SaveChangesAsync();
            return supplier;
        }

        public async Task<SupplierEntity> UpdateAsync(SupplierEntity supplier)
        {
            if (supplier == null)
                throw new ArgumentNullException(nameof(supplier));

            supplier.UpdateModified();
            _context.Suppliers.Update(supplier);
            await _context.SaveChangesAsync();
            return supplier;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var supplier = await _context.Suppliers.FindAsync(id);
            if (supplier == null)
                return false;

            _context.Suppliers.Remove(supplier);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsAsync(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;

            return await _context.Suppliers.AnyAsync(s => s.Name == name);
        }

        public async Task<IEnumerable<SupplierEntity>> SearchAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return Enumerable.Empty<SupplierEntity>();

            return await _context.Suppliers
                .Include(s => s.Offers)
                .Where(s => s.Name.Contains(searchTerm) || 
                           (s.Description != null && s.Description.Contains(searchTerm)) ||
                           (s.Company != null && s.Company.Contains(searchTerm)))
                .ToListAsync();
        }
    }
}
