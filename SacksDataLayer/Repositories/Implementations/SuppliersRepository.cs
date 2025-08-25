using Microsoft.EntityFrameworkCore;
using SacksDataLayer.Data;
using SacksDataLayer.Repositories.Interfaces;

namespace SacksDataLayer.Repositories.Implementations;

public class SuppliersRepository : ISuppliersRepository
{
    private readonly SacksDbContext _context;

    public SuppliersRepository(SacksDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<SupplierEntity?> GetByNameAsync(string name)
    {
        return await _context.Suppliers
            .FirstOrDefaultAsync(s => s.Name == name);
    }

    public async Task<SupplierEntity> CreateAsync(SupplierEntity supplier)
    {
        _context.Suppliers.Add(supplier);
        await _context.SaveChangesAsync();
        return supplier;
    }

    public async Task<SupplierEntity> UpdateAsync(SupplierEntity supplier)
    {
        _context.Suppliers.Update(supplier);
        await _context.SaveChangesAsync();
        return supplier;
    }

    public async Task<IEnumerable<SupplierEntity>> GetAllAsync()
    {
        return await _context.Suppliers.ToListAsync();
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
}
