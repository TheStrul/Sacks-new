using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SacksDataLayer.Data;
using SacksDataLayer.Services.Interfaces;
using SacksDataLayer.Repositories.Interfaces;
using SacksDataLayer.Entities;

namespace SacksDataLayer.Examples
{
    /// <summary>
    /// Examples demonstrating transaction-only operations using UnitOfWork pattern
    /// </summary>
    public class TransactionExamples
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IProductsRepository _productsRepository;
        private readonly ISuppliersRepository _suppliersRepository;
        private readonly ISupplierOffersRepository _supplierOffersRepository;

        public TransactionExamples(
            IUnitOfWork unitOfWork,
            IProductsRepository productsRepository,
            ISuppliersRepository suppliersRepository,
            ISupplierOffersRepository supplierOffersRepository)
        {
            _unitOfWork = unitOfWork;
            _productsRepository = productsRepository;
            _suppliersRepository = suppliersRepository;
            _supplierOffersRepository = supplierOffersRepository;
        }

        /// <summary>
        /// Example 1: Simple transaction for creating a single product
        /// </summary>
        public async Task<ProductEntity> CreateProductWithTransactionAsync(ProductEntity product, CancellationToken cancellationToken = default)
        {
            return await _unitOfWork.ExecuteInTransactionAsync(async (ct) =>
            {
                var createdProduct = await _productsRepository.CreateAsync(product, "System", ct);
                await _unitOfWork.SaveChangesAsync(ct);
                return createdProduct;
            }, cancellationToken);
        }

        /// <summary>
        /// Example 2: Complex transaction with multiple entity operations
        /// All operations succeed or all fail together
        /// </summary>
        public async Task CreateSupplierWithProductsAsync(
            SupplierEntity supplier, 
            List<ProductEntity> products, 
            CancellationToken cancellationToken = default)
        {
            await _unitOfWork.ExecuteInTransactionAsync(async (ct) =>
            {
                // Step 1: Create supplier
                var createdSupplier = await _suppliersRepository.CreateAsync(supplier, ct);

                // Step 2: Create all products
                var createdProducts = new List<ProductEntity>();
                foreach (var product in products)
                {
                    var createdProduct = await _productsRepository.CreateAsync(product, "System", ct);
                    createdProducts.Add(createdProduct);
                }

                // Step 3: Create supplier offer linking supplier to products
                var supplierOffer = new SupplierOfferEntity
                {
                    SupplierId = createdSupplier.Id,
                    OfferName = "Initial Product Offer",
                    IsActive = true,
                    ValidFrom = DateTime.UtcNow,
                    ValidTo = DateTime.UtcNow.AddMonths(12)
                };

                await _supplierOffersRepository.CreateAsync(supplierOffer, ct);

                // Step 4: Commit all changes
                await _unitOfWork.SaveChangesAsync(ct);

            }, cancellationToken);
        }

        /// <summary>
        /// Example 3: Multiple individual creates with transaction
        /// Efficient processing with single transaction
        /// </summary>
        public async Task<IEnumerable<ProductEntity>> CreateMultipleProductsAsync(
            IEnumerable<ProductEntity> products, 
            CancellationToken cancellationToken = default)
        {
            return await _unitOfWork.ExecuteInTransactionAsync(async (ct) =>
            {
                var createdProducts = new List<ProductEntity>();
                foreach (var product in products)
                {
                    var createdProduct = await _productsRepository.CreateAsync(product, "System", ct);
                    createdProducts.Add(createdProduct);
                }
                
                await _unitOfWork.SaveChangesAsync(ct);
                return createdProducts;
            }, cancellationToken);
        }

        /// <summary>
        /// Example 4: Manual transaction management for complex scenarios
        /// When you need finer control over transaction boundaries
        /// </summary>
        public async Task ComplexBusinessOperationAsync(CancellationToken cancellationToken = default)
        {
            using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
            
            try
            {
                // Step 1: Validate data
                var existingProducts = await _productsRepository.GetAllAsync(cancellationToken);
                if (existingProducts.Count() > 1000)
                {
                    throw new InvalidOperationException("Too many products already exist");
                }

                // Step 2: Create new products
                var newProduct = new ProductEntity
                {
                    Name = "Test Product",
                    EAN = "1234567890123",
                    Description = "Created in transaction"
                };

                await _productsRepository.CreateAsync(newProduct, "System", cancellationToken);

                // Step 3: Update existing products
                foreach (var product in existingProducts.Take(5)) // Just first 5 for example
                {
                    product.Description += " - Updated in batch";
                    await _productsRepository.UpdateAsync(product, "System", cancellationToken);
                }

                // Step 4: Save all changes
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                // Step 5: Commit transaction
                await _unitOfWork.CommitTransactionAsync(cancellationToken);
            }
            catch (Exception)
            {
                // Rollback on any error
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                throw;
            }
        }

        /// <summary>
        /// Example 5: Read-only operations (no transaction needed)
        /// Optimized for queries without modifications
        /// </summary>
        public async Task<IEnumerable<ProductEntity>> GetProductsWithOptimizationAsync(CancellationToken cancellationToken = default)
        {
            // No transaction needed for read-only operations
            return await _productsRepository.GetPagedAsync(0, 100, cancellationToken);
        }

        /// <summary>
        /// Example 6: Error handling and rollback demonstration
        /// Shows how transactions automatically rollback on exceptions
        /// </summary>
        public async Task<bool> DemonstrateRollbackAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                await _unitOfWork.ExecuteInTransactionAsync(async (ct) =>
                {
                    // Create a product
                    var product = new ProductEntity
                    {
                        Name = "Will be rolled back",
                        EAN = "0000000000000"
                    };

                    await _productsRepository.CreateAsync(product, "System", ct);

                    // This will cause the transaction to rollback
                    throw new Exception("Simulated error - this will rollback the product creation");

                }, cancellationToken);

                return true; // This won't be reached
            }
            catch (Exception)
            {
                // The product creation has been automatically rolled back
                return false;
            }
        }
    }
}
