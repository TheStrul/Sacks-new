using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using SacksDataLayer.Data;
using SacksDataLayer.Services.Interfaces;
using SacksDataLayer.Services.Implementations;
using SacksDataLayer.Models;

namespace SacksConsoleApp
{
    /// <summary>
    /// Example demonstrating perfume product filtering and sorting
    /// </summary>
    public class PerfumeFilteringExample
    {
        private readonly IPerfumeProductService _perfumeService;

        public PerfumeFilteringExample(IPerfumeProductService perfumeService)
        {
            _perfumeService = perfumeService ?? throw new ArgumentNullException(nameof(perfumeService));
        }

        public async Task RunExamplesAsync()
        {
            Console.WriteLine("🌸 Perfume Product Filtering Examples");
            Console.WriteLine("=====================================\n");

            // Example 1: Get available filter values for UI dropdowns
            await ShowAvailableFiltersAsync();

            // Example 2: Filter by gender
            await FilterByGenderAsync();

            // Example 3: Filter by multiple criteria
            await FilterByCombinedCriteriaAsync();

            // Example 4: Search with text and sort
            await SearchAndSortAsync();

            // Example 5: Get specific product details
            await GetProductDetailsAsync();
        }

        private async Task ShowAvailableFiltersAsync()
        {
            Console.WriteLine("📋 Available Filter Values (for building UI dropdowns):");
            Console.WriteLine("--------------------------------------------------------");

            var filterValues = await _perfumeService.GetAvailableFilterValuesAsync();

            Console.WriteLine($"Genders: {string.Join(", ", filterValues.Genders)}");
            Console.WriteLine($"Sizes: {string.Join(", ", filterValues.Sizes.Take(10))}..."); // Show first 10
            Console.WriteLine($"Concentrations: {string.Join(", ", filterValues.Concentrations)}");
            Console.WriteLine($"Brands: {string.Join(", ", filterValues.Brands.Take(10))}..."); // Show first 10
            Console.WriteLine($"Product Lines: {string.Join(", ", filterValues.ProductLines.Take(10))}...");
            Console.WriteLine($"Fragrance Families: {string.Join(", ", filterValues.FragranceFamilies)}");
            Console.WriteLine();
        }

        private async Task FilterByGenderAsync()
        {
            Console.WriteLine("👩 Filtering by Gender = 'Women':");
            Console.WriteLine("----------------------------------");

            var filter = new PerfumeFilterModel { Gender = "Women" };
            var sort = new PerfumeSortModel { SortBy = PerfumeSortField.Name, Direction = SortDirection.Ascending };

            var results = await _perfumeService.SearchPerfumeProductsAsync(filter, sort, pageNumber: 1, pageSize: 5);

            Console.WriteLine($"Found {results.TotalCount} women's perfumes (showing first 5):");
            foreach (var product in results.Items)
            {
                Console.WriteLine($"- {product.Name} ({product.Brand}) - {product.Size} - {product.Concentration}");
            }
            Console.WriteLine();
        }

        private async Task FilterByCombinedCriteriaAsync()
        {
            Console.WriteLine("🔍 Combined Filter: Women + EDT + Specific Brand:");
            Console.WriteLine("------------------------------------------------");

            var filter = new PerfumeFilterModel 
            { 
                Gender = "Women",
                Concentration = "EDT",
                Brand = "Dior" // Adjust based on your data
            };
            var sort = new PerfumeSortModel { SortBy = PerfumeSortField.Name };

            var results = await _perfumeService.SearchPerfumeProductsAsync(filter, sort, pageNumber: 1, pageSize: 10);

            Console.WriteLine($"Found {results.TotalCount} products matching criteria:");
            foreach (var product in results.Items)
            {
                Console.WriteLine($"- {product.Name}");
                Console.WriteLine($"  Brand: {product.Brand}, Size: {product.Size}, Gender: {product.Gender}");
            }
            Console.WriteLine();
        }

        private async Task SearchAndSortAsync()
        {
            Console.WriteLine("🔎 Text Search + Sort by Name (Descending):");
            Console.WriteLine("-------------------------------------------");

            var filter = new PerfumeFilterModel { SearchText = "perfume" };
            var sort = new PerfumeSortModel 
            { 
                SortBy = PerfumeSortField.Name, 
                Direction = SortDirection.Descending 
            };

            var results = await _perfumeService.SearchPerfumeProductsAsync(filter, sort, pageNumber: 1, pageSize: 5);

            Console.WriteLine($"Found {results.TotalCount} products containing 'perfume' (showing first 5, sorted Z-A):");
            foreach (var product in results.Items)
            {
                Console.WriteLine($"- {product.Name} ({product.Brand})");
            }
            Console.WriteLine();
        }

        private async Task GetProductDetailsAsync()
        {
            Console.WriteLine("📄 Product Details Example:");
            Console.WriteLine("---------------------------");

            // Get first product by EAN (adjust EAN based on your data)
            var results = await _perfumeService.SearchPerfumeProductsAsync(
                new PerfumeFilterModel(), 
                new PerfumeSortModel(), 
                pageNumber: 1, 
                pageSize: 1);

            if (results.Items.Any())
            {
                var firstProduct = results.Items.First();
                var productDetails = await _perfumeService.GetPerfumeProductAsync(firstProduct.Id);

                if (productDetails != null)
                {
                    Console.WriteLine($"Product: {productDetails.Name}");
                    Console.WriteLine($"EAN: {productDetails.EAN}");
                    Console.WriteLine($"Brand: {productDetails.Brand ?? "N/A"}");
                    Console.WriteLine($"Gender: {productDetails.Gender ?? "N/A"}");
                    Console.WriteLine($"Size: {productDetails.Size ?? "N/A"}");
                    Console.WriteLine($"Concentration: {productDetails.Concentration ?? "N/A"}");
                    Console.WriteLine($"Product Line: {productDetails.ProductLine ?? "N/A"}");
                    Console.WriteLine($"Fragrance Family: {productDetails.FragranceFamily ?? "N/A"}");
                    Console.WriteLine($"Description: {productDetails.Description ?? "N/A"}");
                }
            }
            Console.WriteLine();
        }

        /// <summary>
        /// Configure services for dependency injection
        /// </summary>
        public static void ConfigureServices(IServiceCollection services, string connectionString)
        {
            services.AddDbContext<SacksDbContext>(options =>
                options.UseSqlServer(connectionString));

            services.AddScoped<IPerfumeProductService, PerfumeProductService>();
        }
    }
}
