using Microsoft.EntityFrameworkCore;
using SacksDataLayer.Data;
using System;
using System.Threading.Tasks;

namespace SacksConsoleApp
{
    public static class ClearDatabase
    {
        public static async Task ClearAllDataAsync()
        {
            try
            {
                Console.WriteLine("🗑️ Clearing all data from MariaDB database...");
                
                // MariaDB connection string
                var connectionString = "Server=localhost;Port=3306;Database=SacksProductsDb;Uid=root;Pwd=;";
                var optionsBuilder = new DbContextOptionsBuilder<SacksDbContext>();
                optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));

                await using var context = new SacksDbContext(optionsBuilder.Options);

                // Ensure database exists
                await context.Database.EnsureCreatedAsync();

                // Clear all tables in the correct order (respecting foreign key constraints)
                // Delete OfferProducts first (has foreign keys to both Offers and Products)
                var offerProductsDeleted = await context.Database.ExecuteSqlRawAsync("DELETE FROM OfferProducts");
                Console.WriteLine($"   ✓ Deleted {offerProductsDeleted} records from OfferProducts table");

                // Delete SupplierOffers (has foreign key to Suppliers)
                var supplierOffersDeleted = await context.Database.ExecuteSqlRawAsync("DELETE FROM SupplierOffers");
                Console.WriteLine($"   ✓ Deleted {supplierOffersDeleted} records from SupplierOffers table");

                // Delete Products (no foreign key dependencies)
                var productsDeleted = await context.Database.ExecuteSqlRawAsync("DELETE FROM Products");
                Console.WriteLine($"   ✓ Deleted {productsDeleted} records from Products table");

                // Delete Suppliers (no foreign key dependencies after offers are deleted)
                var suppliersDeleted = await context.Database.ExecuteSqlRawAsync("DELETE FROM Suppliers");
                Console.WriteLine($"   ✓ Deleted {suppliersDeleted} records from Suppliers table");

                // Reset auto-increment counters
                await context.Database.ExecuteSqlRawAsync("ALTER TABLE OfferProducts AUTO_INCREMENT = 1");
                await context.Database.ExecuteSqlRawAsync("ALTER TABLE SupplierOffers AUTO_INCREMENT = 1");
                await context.Database.ExecuteSqlRawAsync("ALTER TABLE Products AUTO_INCREMENT = 1");
                await context.Database.ExecuteSqlRawAsync("ALTER TABLE Suppliers AUTO_INCREMENT = 1");

                Console.WriteLine("   ✓ Reset all auto-increment counters");
                Console.WriteLine();
                Console.WriteLine("🎉 Database cleared successfully! All tables are now empty.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error clearing database: {ex.Message}");
                throw;
            }
        }

        public static async Task<bool> CheckDatabaseConnectionAsync()
        {
            try
            {
                Console.WriteLine("🔍 Testing MariaDB connection...");
                
                var connectionString = "Server=localhost;Port=3306;Database=SacksProductsDb;Uid=root;Pwd=;";
                var optionsBuilder = new DbContextOptionsBuilder<SacksDbContext>();
                optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));

                await using var context = new SacksDbContext(optionsBuilder.Options);
                
                // Try to connect and get server version
                var canConnect = await context.Database.CanConnectAsync();
                
                if (canConnect)
                {
                    Console.WriteLine("   ✓ Successfully connected to MariaDB");
                    
                    // Get table counts
                    var productsCount = await context.Products.CountAsync();
                    var suppliersCount = await context.Suppliers.CountAsync();
                    var supplierOffersCount = await context.SupplierOffers.CountAsync();
                    var offerProductsCount = await context.OfferProducts.CountAsync();

                    Console.WriteLine($"   📊 Current record counts:");
                    Console.WriteLine($"      - Products: {productsCount}");
                    Console.WriteLine($"      - Suppliers: {suppliersCount}");
                    Console.WriteLine($"      - SupplierOffers: {supplierOffersCount}");
                    Console.WriteLine($"      - OfferProducts: {offerProductsCount}");
                    
                    return true;
                }
                else
                {
                    Console.WriteLine("   ❌ Cannot connect to MariaDB");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ❌ Connection failed: {ex.Message}");
                return false;
            }
        }
    }
}
