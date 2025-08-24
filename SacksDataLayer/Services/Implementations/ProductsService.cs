using Microsoft.EntityFrameworkCore;
using SacksDataLayer.FileProcessing.Models;
using SacksDataLayer.Repositories.Interfaces;
using SacksDataLayer.Services.Interfaces;
using System.Diagnostics;
using System.Text.Json;

namespace SacksDataLayer.Services.Implementations
{
    /// <summary>
    /// Service implementation for products with comprehensive business logic
    /// </summary>
    public class ProductsService : IProductsService
    {
        private readonly IProductsRepository _repository;

        public ProductsService(IProductsRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        #region Basic Operations

        public async Task<ProductEntity?> GetProductAsync(int id, bool includeDeleted = false)
        {
            return await _repository.GetByIdAsync(id, includeDeleted);
        }

        public async Task<ProductEntity?> GetProductBySKUAsync(string sku, bool includeDeleted = false)
        {
            return await _repository.GetBySKUAsync(sku, includeDeleted);
        }

        public async Task<(IEnumerable<ProductEntity> Products, int TotalCount)> GetProductsAsync(
            int pageNumber = 1, int pageSize = 50, bool includeDeleted = false)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 50;
            if (pageSize > 1000) pageSize = 1000; // Limit max page size

            var skip = (pageNumber - 1) * pageSize;
            var products = await _repository.GetPagedAsync(skip, pageSize, includeDeleted);
            var totalCount = await _repository.GetCountAsync(includeDeleted);

            return (products, totalCount);
        }

        public async Task<ProductEntity> CreateProductAsync(ProductEntity product, string? createdBy = null)
        {
            var validation = await ValidateProductAsync(product);
            if (!validation.IsValid)
            {
                throw new InvalidOperationException($"Product validation failed: {string.Join(", ", validation.Errors)}");
            }

            // Check for duplicate SKU
            if (!string.IsNullOrWhiteSpace(product.SKU))
            {
                var existingProduct = await _repository.GetBySKUAsync(product.SKU);
                if (existingProduct != null)
                {
                    throw new InvalidOperationException($"Product with SKU '{product.SKU}' already exists");
                }
            }

            return await _repository.CreateAsync(product, createdBy);
        }

        public async Task<ProductEntity> UpdateProductAsync(ProductEntity product, string? modifiedBy = null)
        {
            var validation = await ValidateProductAsync(product);
            if (!validation.IsValid)
            {
                throw new InvalidOperationException($"Product validation failed: {string.Join(", ", validation.Errors)}");
            }

            // Check for duplicate SKU (excluding current product)
            if (!string.IsNullOrWhiteSpace(product.SKU))
            {
                var hasDuplicate = await HasDuplicateSKUAsync(product.SKU, product.Id);
                if (hasDuplicate)
                {
                    throw new InvalidOperationException($"Another product with SKU '{product.SKU}' already exists");
                }
            }

            return await _repository.UpdateAsync(product, modifiedBy);
        }

        public async Task<bool> DeleteProductAsync(int id, string? deletedBy = null, bool hardDelete = false)
        {
            if (hardDelete)
            {
                return await _repository.HardDeleteAsync(id);
            }
            else
            {
                return await _repository.SoftDeleteAsync(id, deletedBy);
            }
        }

        public async Task<bool> RestoreProductAsync(int id)
        {
            return await _repository.RestoreAsync(id);
        }

        #endregion

        #region Search and Filtering

        public async Task<IEnumerable<ProductEntity>> SearchProductsAsync(
            string? searchTerm = null,
            string? sku = null,
            ProcessingMode? processingMode = null,
            string? sourceFile = null,
            bool includeDeleted = false)
        {
            var results = new List<ProductEntity>();

            // Start with all products
            var allProducts = await _repository.GetAllAsync(includeDeleted);

            // Apply filters
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var searchResults = await _repository.SearchByNameAsync(searchTerm, includeDeleted);
                allProducts = allProducts.Intersect(searchResults);
            }

            if (!string.IsNullOrWhiteSpace(sku))
            {
                allProducts = allProducts.Where(p => 
                    !string.IsNullOrEmpty(p.SKU) && 
                    p.SKU.Contains(sku, StringComparison.OrdinalIgnoreCase));
            }

            if (processingMode.HasValue)
            {
                var modeResults = await _repository.GetByProcessingModeAsync(processingMode.Value, includeDeleted);
                allProducts = allProducts.Intersect(modeResults);
            }

            if (!string.IsNullOrWhiteSpace(sourceFile))
            {
                var sourceResults = await _repository.GetBySourceFileAsync(sourceFile, includeDeleted);
                allProducts = allProducts.Intersect(sourceResults);
            }

            return allProducts.ToList();
        }

        public async Task<IEnumerable<ProductEntity>> GetProductsByModeAsync(
            ProcessingMode mode,
            DateTime? dateFrom = null,
            DateTime? dateTo = null,
            bool includeDeleted = false)
        {
            var products = await _repository.GetByProcessingModeAsync(mode, includeDeleted);

            if (dateFrom.HasValue || dateTo.HasValue)
            {
                products = products.Where(p =>
                {
                    if (dateFrom.HasValue && p.CreatedAt < dateFrom.Value)
                        return false;
                    if (dateTo.HasValue && p.CreatedAt > dateTo.Value)
                        return false;
                    return true;
                });
            }

            return products;
        }

        public async Task<IEnumerable<ProductEntity>> GetProductsBySourceAsync(string sourceFile, bool includeDeleted = false)
        {
            return await _repository.GetBySourceFileAsync(sourceFile, includeDeleted);
        }

        #endregion

        #region Bulk Operations

        public async Task<BulkOperationResult> CreateProductsBulkAsync(
            IEnumerable<ProductEntity> products,
            string? createdBy = null,
            bool skipDuplicates = true)
        {
            var stopwatch = Stopwatch.StartNew();
            var result = new BulkOperationResult();
            var successfulProducts = new List<ProductEntity>();
            var errors = new List<BulkOperationError>();
            var productList = products.ToList();

            result.TotalProcessed = productList.Count;

            using var transaction = await _repository.BeginTransactionAsync();
            try
            {
                foreach (var product in productList)
                {
                    try
                    {
                        // Validate product
                        var validation = await ValidateProductAsync(product);
                        if (!validation.IsValid)
                        {
                            errors.Add(new BulkOperationError
                            {
                                Product = product,
                                ErrorMessage = string.Join(", ", validation.Errors),
                                ErrorType = "Validation"
                            });
                            continue;
                        }

                        // Check for duplicate SKU
                        if (!string.IsNullOrWhiteSpace(product.SKU))
                        {
                            var existingProduct = await _repository.GetBySKUAsync(product.SKU);
                            if (existingProduct != null)
                            {
                                if (skipDuplicates)
                                {
                                    errors.Add(new BulkOperationError
                                    {
                                        Product = product,
                                        ErrorMessage = $"Product with SKU '{product.SKU}' already exists (skipped)",
                                        ErrorType = "Duplicate"
                                    });
                                    continue;
                                }
                                else
                                {
                                    errors.Add(new BulkOperationError
                                    {
                                        Product = product,
                                        ErrorMessage = $"Product with SKU '{product.SKU}' already exists",
                                        ErrorType = "Duplicate"
                                    });
                                    continue;
                                }
                            }
                        }

                        var createdProduct = await _repository.CreateAsync(product, createdBy);
                        successfulProducts.Add(createdProduct);
                    }
                    catch (Exception ex)
                    {
                        errors.Add(new BulkOperationError
                        {
                            Product = product,
                            ErrorMessage = ex.Message,
                            ErrorType = "Exception",
                            Exception = ex
                        });
                    }
                }

                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                throw new InvalidOperationException("Bulk operation failed and was rolled back", ex);
            }

            stopwatch.Stop();

            result.SuccessfulProducts = successfulProducts;
            result.Errors = errors;
            result.SuccessCount = successfulProducts.Count;
            result.ErrorCount = errors.Count;
            result.ProcessingTime = stopwatch.Elapsed;

            return result;
        }

        public async Task<BulkOperationResult> UpdateProductsBulkAsync(
            IEnumerable<ProductEntity> products,
            string? modifiedBy = null)
        {
            var stopwatch = Stopwatch.StartNew();
            var result = new BulkOperationResult();
            var successfulProducts = new List<ProductEntity>();
            var errors = new List<BulkOperationError>();
            var productList = products.ToList();

            result.TotalProcessed = productList.Count;

            using var transaction = await _repository.BeginTransactionAsync();
            try
            {
                foreach (var product in productList)
                {
                    try
                    {
                        // Validate product
                        var validation = await ValidateProductAsync(product);
                        if (!validation.IsValid)
                        {
                            errors.Add(new BulkOperationError
                            {
                                Product = product,
                                ErrorMessage = string.Join(", ", validation.Errors),
                                ErrorType = "Validation"
                            });
                            continue;
                        }

                        // Check for duplicate SKU (excluding current product)
                        if (!string.IsNullOrWhiteSpace(product.SKU))
                        {
                            var hasDuplicate = await HasDuplicateSKUAsync(product.SKU, product.Id);
                            if (hasDuplicate)
                            {
                                errors.Add(new BulkOperationError
                                {
                                    Product = product,
                                    ErrorMessage = $"Another product with SKU '{product.SKU}' already exists",
                                    ErrorType = "Duplicate"
                                });
                                continue;
                            }
                        }

                        var updatedProduct = await _repository.UpdateAsync(product, modifiedBy);
                        successfulProducts.Add(updatedProduct);
                    }
                    catch (Exception ex)
                    {
                        errors.Add(new BulkOperationError
                        {
                            Product = product,
                            ErrorMessage = ex.Message,
                            ErrorType = "Exception",
                            Exception = ex
                        });
                    }
                }

                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                throw new InvalidOperationException("Bulk update failed and was rolled back", ex);
            }

            stopwatch.Stop();

            result.SuccessfulProducts = successfulProducts;
            result.Errors = errors;
            result.SuccessCount = successfulProducts.Count;
            result.ErrorCount = errors.Count;
            result.ProcessingTime = stopwatch.Elapsed;

            return result;
        }

        public async Task<int> DeleteProductsBulkAsync(IEnumerable<int> ids, string? deletedBy = null, bool hardDelete = false)
        {
            if (hardDelete)
            {
                // Hard delete needs individual operations for safety
                var count = 0;
                foreach (var id in ids)
                {
                    if (await _repository.HardDeleteAsync(id))
                        count++;
                }
                return count;
            }
            else
            {
                return await _repository.SoftDeleteBulkAsync(ids, deletedBy);
            }
        }

        #endregion

        #region Processing Integration

        public async Task<BulkOperationResult> SaveProcessingResultAsync(
            ProcessingResult processingResult,
            string? createdBy = null,
            bool skipDuplicates = true)
        {
            // Enhance products with processing metadata
            var enhancedProducts = processingResult.Products.Select(p =>
            {
                p.SetDynamicProperty("ProcessingMode", processingResult.Mode.ToString());
                p.SetDynamicProperty("SourceFile", processingResult.SourceFile);
                p.SetDynamicProperty("SupplierName", processingResult.SupplierName);
                p.SetDynamicProperty("ProcessedAt", processingResult.ProcessedAt);
                return p;
            });

            return await CreateProductsBulkAsync(enhancedProducts, createdBy, skipDuplicates);
        }

        public async Task<ProcessingStatisticsReport> GetProcessingStatisticsAsync(
            string? sourceFile = null,
            DateTime? dateFrom = null,
            DateTime? dateTo = null,
            bool includeDeleted = false)
        {
            var report = new ProcessingStatisticsReport();

            // Get base statistics
            report.ProductsByMode = await _repository.GetProcessingStatisticsAsync(sourceFile, includeDeleted);
            report.TotalProducts = await _repository.GetCountAsync(includeDeleted);
            
            if (includeDeleted)
            {
                var activeCount = await _repository.GetCountAsync(false);
                report.DeletedProducts = report.TotalProducts - activeCount;
            }

            // Apply date filtering if needed
            IEnumerable<ProductEntity> products;
            if (dateFrom.HasValue || dateTo.HasValue)
            {
                products = await _repository.GetByCreatedDateRangeAsync(
                    dateFrom ?? DateTime.MinValue,
                    dateTo ?? DateTime.MaxValue,
                    includeDeleted);
            }
            else
            {
                products = await _repository.GetAllAsync(includeDeleted);
            }

            // Calculate additional statistics
            if (products.Any())
            {
                report.OldestProduct = products.Min(p => p.CreatedAt);
                report.NewestProduct = products.Max(p => p.CreatedAt);

                // Products by source
                var productsBySource = new Dictionary<string, int>();
                foreach (var product in products)
                {
                    var productSourceFile = product.GetDynamicProperty<string>("SourceFile");
                    if (!string.IsNullOrEmpty(productSourceFile))
                    {
                        productsBySource[productSourceFile] = productsBySource.GetValueOrDefault(productSourceFile, 0) + 1;
                    }
                }
                report.ProductsBySource = productsBySource;

                // Products by date
                var productsByDate = products
                    .GroupBy(p => p.CreatedAt.Date)
                    .ToDictionary(g => g.Key, g => g.Count());
                report.ProductsByDate = productsByDate;

                // Top dynamic properties
                var allDynamicProperties = new Dictionary<string, int>();
                foreach (var product in products)
                {
                    foreach (var key in product.GetDynamicPropertyKeys())
                    {
                        allDynamicProperties[key] = allDynamicProperties.GetValueOrDefault(key, 0) + 1;
                    }
                }
                report.TopDynamicProperties = allDynamicProperties
                    .OrderByDescending(kvp => kvp.Value)
                    .Take(10)
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            }

            return report;
        }

        #endregion

        #region Validation and Business Rules

        public async Task<ValidationResult> ValidateProductAsync(ProductEntity product)
        {
            var result = new ValidationResult { IsValid = true };

            // Required field validation
            if (string.IsNullOrWhiteSpace(product.Name))
            {
                result.Errors.Add("Product name is required");
                result.IsValid = false;
            }

            // Length validation
            if (product.Name?.Length > 255)
            {
                result.Errors.Add("Product name cannot exceed 255 characters");
                result.IsValid = false;
            }

            if (product.Description?.Length > 2000)
            {
                result.Errors.Add("Product description cannot exceed 2000 characters");
                result.IsValid = false;
            }

            if (product.SKU?.Length > 100)
            {
                result.Errors.Add("Product SKU cannot exceed 100 characters");
                result.IsValid = false;
            }

            // Business rule validations
            if (!string.IsNullOrWhiteSpace(product.SKU) && product.SKU.Contains(" "))
            {
                result.Warnings.Add("SKU contains spaces which may cause issues");
            }

            // Dynamic properties validation
            try
            {
                if (!string.IsNullOrEmpty(product.DynamicPropertiesJson))
                {
                    JsonSerializer.Deserialize<Dictionary<string, object>>(product.DynamicPropertiesJson);
                }
            }
            catch (JsonException)
            {
                result.Errors.Add("Dynamic properties JSON is malformed");
                result.IsValid = false;
            }

            return await Task.FromResult(result);
        }

        public async Task<bool> HasDuplicateSKUAsync(string sku, int? excludeId = null)
        {
            if (string.IsNullOrWhiteSpace(sku))
                return false;

            var existingProduct = await _repository.GetBySKUAsync(sku);
            return existingProduct != null && (excludeId == null || existingProduct.Id != excludeId);
        }

        public async Task<IEnumerable<ProductEntity>> GetSimilarProductsAsync(string productName, int maxResults = 5)
        {
            if (string.IsNullOrWhiteSpace(productName))
                return Enumerable.Empty<ProductEntity>();

            // Simple similarity search - can be enhanced with more sophisticated algorithms
            var searchTerms = productName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var allProducts = await _repository.GetAllAsync();

            var similarProducts = allProducts
                .Select(p => new
                {
                    Product = p,
                    Similarity = CalculateSimilarity(productName, p.Name)
                })
                .Where(x => x.Similarity > 0.3) // Minimum similarity threshold
                .OrderByDescending(x => x.Similarity)
                .Take(maxResults)
                .Select(x => x.Product);

            return similarProducts;
        }

        private static double CalculateSimilarity(string text1, string text2)
        {
            if (string.IsNullOrEmpty(text1) || string.IsNullOrEmpty(text2))
                return 0;

            // Simple Jaccard similarity based on words
            var words1 = text1.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
            var words2 = text2.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet();

            var intersection = words1.Intersect(words2).Count();
            var union = words1.Union(words2).Count();

            return union > 0 ? (double)intersection / union : 0;
        }

        #endregion
    }
}
