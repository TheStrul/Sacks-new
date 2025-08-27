using Microsoft.EntityFrameworkCore;
using SacksDataLayer;
using SacksDataLayer.Data;
using SacksDataLayer.FileProcessing.Models;
using SacksDataLayer.Repositories.Implementations;
using SacksDataLayer.Services.Implementations;

namespace SacksConsoleApp
{
    /// <summary>
    /// Demonstrates CRUD operations with ProductsRepository and ProductsService
    /// </summary>
    public class CrudDemo
    {
        public static async Task RunAsync()
        {
            Console.WriteLine("=== Products CRUD Functionality Demo ===\n");

            // Setup SQL Server database connection
            var connectionString = @"Server=(localdb)\mssqllocaldb;Database=SacksProductsDb;Trusted_Connection=true;MultipleActiveResultSets=true";
            var options = new DbContextOptionsBuilder<SacksDbContext>()
                .UseSqlServer(connectionString)
                .Options;

            await using var context = new SacksDbContext(options);
            var repository = new ProductsRepository(context);
            var service = new ProductsService(repository);

            try
            {
                // Demo 1: Create products
                Console.WriteLine("🔹 Demo 1: Creating Products");
                await DemoCreateProducts(service);

                // Demo 2: Read operations
                Console.WriteLine("\n🔹 Demo 2: Reading Products");
                await DemoReadOperations(service);

                // Demo 3: Update operations
                Console.WriteLine("\n🔹 Demo 3: Updating Products");
                await DemoUpdateOperations(service);

                // Demo 4: Search operations
                Console.WriteLine("\n🔹 Demo 4: Search Operations");
                await DemoSearchOperations(service);

                // Demo 5: Bulk operations
                Console.WriteLine("\n🔹 Demo 5: Bulk Operations");
                await DemoBulkOperations(service);

                // Demo 6: Processing integration
                Console.WriteLine("\n🔹 Demo 6: Processing Integration");
                await DemoProcessingIntegration(service);

                // Demo 7: Delete operations
                Console.WriteLine("\n🔹 Demo 7: Delete and Restore Operations");
                await DemoDeleteOperations(service);

                // Demo 8: Statistics
                Console.WriteLine("\n🔹 Demo 8: Statistics and Analytics");
                await DemoStatistics(service);

                Console.WriteLine("\n✅ All CRUD demos completed successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ Demo failed with error: {ex.Message}");
                Console.WriteLine($"   Stack trace: {ex.StackTrace}");
            }
        }

        private static async Task DemoCreateProducts(ProductsService service)
        {
            // Create individual products
            var product1 = new ProductEntity
            {
                Name = "DIOR J'adore Eau de Parfum",
                Description = "A floral fragrance with notes of ylang-ylang, rose, and jasmine",
                SKU = "DIOR-JADORE-EDP-100ML"
            };
            product1.SetDynamicProperty("Price", 125.99m);
            product1.SetDynamicProperty("Size", "100ml");
            product1.SetDynamicProperty("Category", "Fragrance");

            var created1 = await service.CreateProductAsync(product1, "DemoUser");
            Console.WriteLine($"   ✓ Created product: {created1.Name} (ID: {created1.Id})");

            var product2 = new ProductEntity
            {
                Name = "DIOR Rouge Lipstick",
                Description = "Couture color with 16-hour wear",
                SKU = "DIOR-ROUGE-999"
            };
            product2.SetDynamicProperty("Price", 47.00m);
            product2.SetDynamicProperty("Color", "999 - Classic Red");
            product2.SetDynamicProperty("Category", "Makeup");

            var created2 = await service.CreateProductAsync(product2, "DemoUser");
            Console.WriteLine($"   ✓ Created product: {created2.Name} (ID: {created2.Id})");
        }

        private static async Task DemoReadOperations(ProductsService service)
        {
            // Get all products
            var (products, totalCount) = await service.GetProductsAsync(pageNumber: 1, pageSize: 10);
            Console.WriteLine($"   📊 Total products: {totalCount}");
            
            foreach (var product in products)
            {
                Console.WriteLine($"   📦 {product.Name} (SKU: {product.SKU})");
                var price = product.GetDynamicProperty<decimal?>("Price");
                if (price.HasValue)
                {
                    Console.WriteLine($"      💰 Price: ${price.Value:F2}");
                }
            }

            // Get specific product by SKU
            var specificProduct = await service.GetProductBySKUAsync("DIOR-JADORE-EDP-100ML");
            if (specificProduct != null)
            {
                Console.WriteLine($"   🎯 Found by SKU: {specificProduct.Name}");
            }
        }

        private static async Task DemoUpdateOperations(ProductsService service)
        {
            var product = await service.GetProductBySKUAsync("DIOR-ROUGE-999");
            if (product != null)
            {
                Console.WriteLine($"   📝 Updating product: {product.Name}");
                
                // Update properties
                product.Description = "Couture color with 16-hour wear - Updated formula";
                product.SetDynamicProperty("Price", 52.00m); // Price increase
                product.SetDynamicProperty("UpdateReason", "Price adjustment and formula improvement");

                var updated = await service.UpdateProductAsync(product, "DemoUser");
                Console.WriteLine($"   ✓ Updated: {updated.Name}");
                Console.WriteLine($"      New price: ${updated.GetDynamicProperty<decimal>("Price"):F2}");
            }
        }

        private static async Task DemoSearchOperations(ProductsService service)
        {
            // Search by name
            var searchResults = await service.SearchProductsByNameAsync("DIOR");
            Console.WriteLine($"   🔍 Search for 'DIOR': {searchResults.Count()} results");
            
            foreach (var product in searchResults)
            {
                Console.WriteLine($"      • {product.Name}");
            }

            // Get all products and filter by category (dynamic property)
            var (allProducts, _) = await service.GetProductsAsync(pageNumber: 1, pageSize: 1000);
            var fragrances = allProducts.Where(p => 
                p.GetDynamicProperty<string>("Category") == "Fragrance");
            
            Console.WriteLine($"   🌸 Fragrance products: {fragrances.Count()}");
            foreach (var fragrance in fragrances)
            {
                Console.WriteLine($"      • {fragrance.Name}");
            }
        }

        private static async Task DemoBulkOperations(ProductsService service)
        {
            Console.WriteLine("   ⚠️ Note: Using simplified bulk operations for in-memory demo");
            
            // Create products individually for in-memory demo
            var bulkProducts = new List<ProductEntity>
            {
                new ProductEntity
                {
                    Name = "DIOR Prestige Cream",
                    Description = "Exceptional regenerating care",
                    SKU = "DIOR-PRESTIGE-CREAM-50ML"
                },
                new ProductEntity
                {
                    Name = "DIOR Addict Lipstick",
                    Description = "Hydrating shine lipstick",
                    SKU = "DIOR-ADDICT-LIP-001"
                },
                new ProductEntity
                {
                    Name = "DIOR Sauvage EDT",
                    Description = "Fresh and woody fragrance",
                    SKU = "DIOR-SAUVAGE-EDT-100ML"
                }
            };

            // Set dynamic properties
            bulkProducts[0].SetDynamicProperty("Price", 295.00m);
            bulkProducts[0].SetDynamicProperty("Category", "Skincare");
            
            bulkProducts[1].SetDynamicProperty("Price", 42.00m);
            bulkProducts[1].SetDynamicProperty("Category", "Makeup");
            
            bulkProducts[2].SetDynamicProperty("Price", 110.00m);
            bulkProducts[2].SetDynamicProperty("Category", "Fragrance");

            // Create products individually for demo
            var successCount = 0;
            var errorCount = 0;
            var startTime = DateTime.UtcNow;

            foreach (var product in bulkProducts)
            {
                try
                {
                    await service.CreateProductAsync(product, "DemoUser");
                    successCount++;
                    Console.WriteLine($"      ✓ Created: {product.Name}");
                }
                catch (Exception ex)
                {
                    errorCount++;
                    Console.WriteLine($"      ❌ Error creating {product.Name}: {ex.Message}");
                }
            }
            
            var processingTime = DateTime.UtcNow - startTime;

            Console.WriteLine($"   📦 Bulk creation result:");
            Console.WriteLine($"      • Total processed: {bulkProducts.Count}");
            Console.WriteLine($"      • Successful: {successCount}");
            Console.WriteLine($"      • Errors: {errorCount}");
            Console.WriteLine($"      • Processing time: {processingTime.TotalMilliseconds:F0}ms");
        }

        private static async Task DemoProcessingIntegration(ProductsService service)
        {
            Console.WriteLine("   ⚠️ Note: Using simplified processing integration for in-memory demo");
            
            // Create demo products manually for in-memory database
            var demoProducts = new List<ProductEntity>
            {
                new ProductEntity
                {
                    Name = "Demo Product 1",
                    SKU = "DEMO-001"
                },
                new ProductEntity
                {
                    Name = "Demo Product 2",
                    SKU = "DEMO-002"
                }
            };

            // Enhance with processing metadata and dynamic properties
            foreach (var product in demoProducts)
            {
                product.SetDynamicProperty("Price", 25.00m);
                product.SetDynamicProperty("DemoFlag", true);
                product.SetDynamicProperty("ProcessingMode", ProcessingMode.UnifiedProductCatalog.ToString());
                product.SetDynamicProperty("SourceFile", "demo-suppliers.xlsx");
                product.SetDynamicProperty("SupplierName", "Demo Supplier");
                product.SetDynamicProperty("ProcessedAt", DateTime.UtcNow);
            }

            var successCount = 0;
            foreach (var product in demoProducts)
            {
                try
                {
                    await service.CreateProductAsync(product, "ProcessingEngine");
                    successCount++;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"      ❌ Error creating {product.Name}: {ex.Message}");
                }
            }
            
            Console.WriteLine($"   🔄 Processing result saved:");
            Console.WriteLine($"      • Mode: {ProcessingMode.UnifiedProductCatalog}");
            Console.WriteLine($"      • Source: demo-suppliers.xlsx");
            Console.WriteLine($"      • Products saved: {successCount}");

            // Verify total products after processing
            var totalProducts = await service.GetProductCountAsync();
            Console.WriteLine($"      • Total products in system: {totalProducts}");
        }

        private static async Task DemoDeleteOperations(ProductsService service)
        {
            // Find a product to delete
            var productToDelete = await service.GetProductBySKUAsync("DEMO-001");
            if (productToDelete != null)
            {
                Console.WriteLine($"   🗑️ Deleting: {productToDelete.Name}");
                var deleted = await service.DeleteProductAsync(productToDelete.Id);
                Console.WriteLine($"      Result: {(deleted ? "Success" : "Failed")}");

                // Verify it's gone from queries
                var checkDeleted = await service.GetProductBySKUAsync("DEMO-001");
                Console.WriteLine($"      Product exists after deletion: {checkDeleted != null}");
            }
            else
            {
                Console.WriteLine("   ⚠️ No DEMO-001 product found to delete");
            }
        }

        private static async Task DemoStatistics(ProductsService service)
        {
            var stats = await service.GetProcessingStatisticsAsync();
            var totalCount = await service.GetProductCountAsync();
            
            Console.WriteLine($"   📈 Processing Statistics:");
            Console.WriteLine($"      • Total products: {totalCount}");
            
            if (stats.Any())
            {
                Console.WriteLine($"      • Statistics breakdown:");
                foreach (var stat in stats)
                {
                    Console.WriteLine($"        - {stat.Key}: {stat.Value}");
                }
            }
            else
            {
                Console.WriteLine($"      • No detailed statistics available yet");
            }
        }
    }
}
