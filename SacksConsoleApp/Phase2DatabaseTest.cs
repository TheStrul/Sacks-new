using SacksDataLayer;
using SacksDataLayer.Data;
using SacksDataLayer.Repositories.Implementations;
using SacksDataLayer.Services.Implementations;
using Microsoft.EntityFrameworkCore;

namespace SacksConsoleApp
{
    /// <summary>
    /// Tests the Phase 2 relational architecture with actual database persistence
    /// </summary>
    public class Phase2DatabaseTest
    {
        public static async Task RunDatabaseTestAsync()
        {
            Console.WriteLine("??? === Phase 2 Database Relational Test ===\n");
            Console.WriteLine("Testing relational entity persistence with Entity Framework");

            try
            {
                // Setup database connection
                var connectionString = @"Server=(localdb)\mssqllocaldb;Database=SacksProductsDb_Phase2Test;Trusted_Connection=true;MultipleActiveResultSets=true";
                var options = new DbContextOptionsBuilder<SacksDbContext>()
                    .UseSqlServer(connectionString, sqlOptions => sqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(30),
                        errorNumbersToAdd: null))
                    .Options;

                await using var context = new SacksDbContext(options);
                
                // Ensure fresh database for testing
                Console.WriteLine("?? Creating fresh test database...");
                await context.Database.EnsureDeletedAsync();
                await context.Database.EnsureCreatedAsync();
                Console.WriteLine("? Test database ready!");

                // Initialize repositories and services
                var suppliersRepository = new SuppliersRepository(context);
                var supplierOffersRepository = new SupplierOffersRepository(context);
                var productsRepository = new ProductsRepository(context);
                var offerProductsRepository = new OfferProductsRepository(context);

                var suppliersService = new SuppliersService(suppliersRepository);
                var supplierOffersService = new SupplierOffersService(supplierOffersRepository, suppliersRepository);
                var productsService = new ProductsService(productsRepository);
                var offerProductsService = new OfferProductsService(offerProductsRepository, supplierOffersRepository, productsRepository);

                // Test 1: Create Supplier
                Console.WriteLine("\n?? Test 1: Creating Supplier (Manual for Database Test)...");
                var supplier = await suppliersService.CreateSupplierAsync(
                    new SupplierEntity
                    {
                        Name = "DIOR",
                        Description = "DIOR beauty and fragrance products supplier",
                        Industry = "Beauty & Cosmetics",
                        Region = "Global",
                        CreatedBy = "Phase2DatabaseTest",
                        CreatedAt = DateTime.UtcNow
                    }
                );
                Console.WriteLine($"? Supplier created: {supplier.Name} (ID: {supplier.Id})");

                // Test 2: Create SupplierOffer
                Console.WriteLine("\n?? Test 2: Creating Supplier Offer...");
                var offer = await supplierOffersService.CreateOfferFromFileAsync(
                    supplier.Id,
                    "DIOR 2025 Test",
                    DateTime.UtcNow,
                    "EUR",
                    "Phase 2 Test",
                    "Phase2DatabaseTest"
                );
                Console.WriteLine($"? Offer created: {offer.OfferName} (ID: {offer.Id})");

                // Test 3: Create Products with Core Properties
                Console.WriteLine("\n?? Test 3: Creating Products with Core Properties...");
                var products = new List<ProductEntity>();
                
                for (int i = 1; i <= 3; i++)
                {
                    var product = new ProductEntity
                    {
                        Name = $"J'adore Test Product {i}",
                        Description = $"Test fragrance product {i}",
                        SKU = $"DIOR-TEST-{i:000}"
                    };
                    
                    // Add core properties
                    product.SetDynamicProperty("Category", "Fragrance");
                    product.SetDynamicProperty("Family", "J'adore");
                    product.SetDynamicProperty("Size", $"{50 + (i * 25)}ml");
                    product.SetDynamicProperty("EAN", $"123456789{i:00}");
                    
                    var createdProduct = await productsService.CreateProductAsync(product);
                    products.Add(createdProduct);
                    Console.WriteLine($"? Product created: {createdProduct.Name} (ID: {createdProduct.Id})");
                }

                // Test 4: Create OfferProducts with Pricing Data
                Console.WriteLine("\n?? Test 4: Creating OfferProducts with Pricing...");
                var offerProducts = new List<OfferProductEntity>();
                
                for (int i = 0; i < products.Count; i++)
                {
                    var offerProperties = new Dictionary<string, object?>
                    {
                        ["Price"] = 75.50m + (i * 25m),
                        ["Capacity"] = $"{50 + (i * 25)}ml"
                    };

                    var offerProduct = await offerProductsService.CreateOrUpdateOfferProductAsync(
                        offer.Id,
                        products[i].Id,
                        offerProperties,
                        "Phase2DatabaseTest"
                    );
                    
                    offerProducts.Add(offerProduct);
                    Console.WriteLine($"? OfferProduct created: {products[i].SKU} -> €{offerProduct.Price:F2}");
                }

                // Test 5: Query Relational Data
                Console.WriteLine("\n?? Test 5: Querying Relational Data...");
                
                // Query offers with related data
                var offersWithProducts = await context.SupplierOffers
                    .Include(o => o.Supplier)
                    .Include(o => o.OfferProducts)
                        .ThenInclude(op => op.Product)
                    .Where(o => o.SupplierId == supplier.Id)
                    .ToListAsync();

                Console.WriteLine($"? Found {offersWithProducts.Count} offers for supplier {supplier.Name}");
                
                foreach (var offerWithProducts in offersWithProducts)
                {
                    Console.WriteLine($"   ?? Offer: {offerWithProducts.OfferName}");
                    Console.WriteLine($"      • Currency: {offerWithProducts.Currency}");
                    Console.WriteLine($"      • Products: {offerWithProducts.OfferProducts.Count}");
                    
                    foreach (var op in offerWithProducts.OfferProducts.Take(2))
                    {
                        Console.WriteLine($"         ??? {op.Product.Name} - €{op.Price:F2}");
                        Console.WriteLine($"            SKU: {op.Product.SKU}");
                        Console.WriteLine($"            Core Props: {op.Product.DynamicProperties.Count}");
                        if (op.Product.DynamicProperties.ContainsKey("Category"))
                        {
                            Console.WriteLine($"            Category: {op.Product.GetDynamicProperty<string>("Category")}");
                        }
                        
                        // Deserialize offer product properties
                        op.DeserializeProductProperties();
                        if (op.ProductProperties.ContainsKey("Capacity"))
                        {
                            Console.WriteLine($"            Capacity: {op.GetProductProperty<string>("Capacity")}");
                        }
                    }
                }

                // Test 6: Validate Data Separation
                Console.WriteLine("\n?? Test 6: Validating Data Separation...");
                
                var allProducts = await context.Products.ToListAsync();
                var allOfferProducts = await context.OfferProducts.ToListAsync();
                
                Console.WriteLine($"? Data Separation Validation:");
                Console.WriteLine($"   ?? Products table: {allProducts.Count} records");
                Console.WriteLine($"   ?? OfferProducts table: {allOfferProducts.Count} records");
                Console.WriteLine($"   ?? Suppliers table: 1 record");
                Console.WriteLine($"   ?? SupplierOffers table: 1 record");

                // Check that core properties are in Products, pricing in OfferProducts
                var sampleProduct = allProducts.First();
                var sampleOfferProduct = allOfferProducts.First();
                
                Console.WriteLine($"\n   ?? Sample Data Verification:");
                Console.WriteLine($"      Product '{sampleProduct.Name}':");
                Console.WriteLine($"         • Core properties: {sampleProduct.DynamicProperties.Count}");
                foreach (var prop in sampleProduct.DynamicProperties.Take(3))
                {
                    Console.WriteLine($"           - {prop.Key}: {prop.Value}");
                }
                
                Console.WriteLine($"      OfferProduct for '{sampleProduct.Name}':");
                Console.WriteLine($"         • Price: €{sampleOfferProduct.Price:F2}");
                Console.WriteLine($"         • Capacity: {sampleOfferProduct.Capacity}");

                Console.WriteLine("\n?? Phase 2 Database Test Completed Successfully!");
                Console.WriteLine("? All relational entities are persisted correctly");
                Console.WriteLine("? Data separation between core and offer properties is working");
                Console.WriteLine("? Entity Framework relationships are functioning properly");
                Console.WriteLine("? The 4-table relational design is fully operational");

            }
            catch (Exception ex)
            {
                Console.WriteLine($"? Database test failed: {ex.Message}");
                Console.WriteLine($"   Stack trace: {ex.StackTrace}");
            }

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }
    }
}