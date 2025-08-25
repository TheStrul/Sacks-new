using SacksDataLayer;
using SacksDataLayer.Data;
using Microsoft.EntityFrameworkCore;

namespace SacksConsoleApp
{
    public static class PropertyDemo
    {
        public static async Task ShowPropertySeparationAsync()
        {
            Console.WriteLine("\n🔍 Property Separation Demo");
            Console.WriteLine(new string('=', 50));

            var connectionString = "Server=(localdb)\\mssqllocaldb;Database=SacksDatabase;Trusted_Connection=true;";
            
            var options = new DbContextOptionsBuilder<SacksDbContext>()
                .UseSqlServer(connectionString)
                .Options;

            await using var context = new SacksDbContext(options);
            
            // Get the first product
            var product = await context.Products.FirstOrDefaultAsync();
            
            if (product == null)
            {
                Console.WriteLine("❌ No products found in database");
                return;
            }

            Console.WriteLine($"📦 Product: {product.Name}");
            Console.WriteLine($"🏷️ SKU: {product.SKU}");
            Console.WriteLine();

            // Show DynamicProperties (Core Product Attributes)
            Console.WriteLine("🔧 DynamicProperties (Core Product Attributes):");
            if (product.DynamicProperties.Any())
            {
                foreach (var prop in product.DynamicProperties)
                {
                    Console.WriteLine($"   • {prop.Key}: {prop.Value}");
                }
            }
            else
            {
                Console.WriteLine("   (No dynamic properties)");
            }

            Console.WriteLine();

            // Show OfferProperties (Supplier-Specific Data)
            Console.WriteLine("💰 OfferProperties (Supplier-Specific Data):");
            if (product.OfferProperties.Any())
            {
                foreach (var prop in product.OfferProperties)
                {
                    Console.WriteLine($"   • {prop.Key}: {prop.Value}");
                }
            }
            else
            {
                Console.WriteLine("   (No offer properties)");
            }

            Console.WriteLine();
            Console.WriteLine("✅ Property separation successfully implemented!");
        }
    }
}
