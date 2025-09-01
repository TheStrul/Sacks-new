using Microsoft.EntityFrameworkCore;
using SacksDataLayer.Data;
using SacksDataLayer.Entities;
using SacksDataLayer.Services.Interfaces;
using System.Collections.Concurrent;

namespace SacksDataLayer.Services.Implementations
{
    /// <summary>
    /// 🚀 PERFORMANCE: Thread-safe in-memory data cache service for high-performance operations
    /// Loads all data into memory to avoid DbContext concurrency issues during parallel processing
    /// </summary>
    public class InMemoryDataService : IInMemoryDataService
    {
        private readonly SacksDbContext _context;
        private readonly SemaphoreSlim _loadingSemaphore = new(1, 1);
        
        // Thread-safe collections for in-memory data
        private readonly ConcurrentDictionary<int, ProductEntity> _productsById = new();
        private readonly ConcurrentDictionary<string, ProductEntity> _productsByEAN = new();
        private readonly ConcurrentDictionary<int, SupplierEntity> _suppliersById = new();
        private readonly ConcurrentDictionary<string, SupplierEntity> _suppliersByName = new();
        private readonly ConcurrentDictionary<int, SupplierOfferEntity> _offersById = new();
        private readonly ConcurrentDictionary<int, List<OfferProductEntity>> _offerProductsByOfferId = new();
        
        // 🚀 NEW: Collections for tracking pending changes (not yet saved to database)
        private readonly ConcurrentDictionary<int, ProductEntity> _pendingProducts = new();
        private readonly ConcurrentDictionary<int, SupplierEntity> _pendingSuppliers = new();
        private readonly ConcurrentDictionary<int, SupplierOfferEntity> _pendingOffers = new();
        private readonly ConcurrentDictionary<int, OfferProductEntity> _pendingOfferProducts = new();
        
        private DateTime _lastLoadTime = DateTime.MinValue;
        private readonly TimeSpan _cacheExpiry = TimeSpan.FromMinutes(30); // Cache expires after 30 minutes
        
        public InMemoryDataService(SacksDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <summary>
        /// 🔄 Loads all data from database into thread-safe in-memory collections
        /// </summary>
        public async Task LoadAllDataAsync(CancellationToken cancellationToken = default)
        {
            await _loadingSemaphore.WaitAsync(cancellationToken);
            try
            {
                Console.WriteLine("🔄 Loading all data into memory for thread-safe access...");
                var startTime = DateTime.UtcNow;

                // Clear existing data
                _productsById.Clear();
                _productsByEAN.Clear();
                _suppliersById.Clear();
                _suppliersByName.Clear();
                _offersById.Clear();
                _offerProductsByOfferId.Clear();

                // Load Products with optimized query
                var products = await _context.Products
                    .AsNoTracking()
                    .ToListAsync(cancellationToken);

                foreach (var product in products)
                {
                    _productsById.TryAdd(product.Id, product);
                    if (!string.IsNullOrEmpty(product.EAN))
                    {
                        _productsByEAN.TryAdd(product.EAN, product);
                    }
                }

                // Load Suppliers
                var suppliers = await _context.Suppliers
                    .AsNoTracking()
                    .ToListAsync(cancellationToken);

                foreach (var supplier in suppliers)
                {
                    _suppliersById.TryAdd(supplier.Id, supplier);
                    _suppliersByName.TryAdd(supplier.Name, supplier);
                }

                // Load Supplier Offers
                var offers = await _context.SupplierOffers
                    .AsNoTracking()
                    .ToListAsync(cancellationToken);

                foreach (var offer in offers)
                {
                    _offersById.TryAdd(offer.Id, offer);
                }

                // Load Offer Products grouped by OfferId
                var offerProducts = await _context.OfferProducts
                    .AsNoTracking()
                    .ToListAsync(cancellationToken);

                var groupedOfferProducts = offerProducts
                    .GroupBy(op => op.OfferId)
                    .ToDictionary(g => g.Key, g => g.ToList());

                foreach (var kvp in groupedOfferProducts)
                {
                    _offerProductsByOfferId.TryAdd(kvp.Key, kvp.Value);
                }

                _lastLoadTime = DateTime.UtcNow;
                var loadTime = DateTime.UtcNow - startTime;

                Console.WriteLine($"✅ In-memory data loaded successfully in {loadTime.TotalMilliseconds:F0}ms:");
                Console.WriteLine($"   📦 Products: {_productsById.Count:N0}");
                Console.WriteLine($"   🏢 Suppliers: {_suppliersById.Count:N0}");
                Console.WriteLine($"   📋 Offers: {_offersById.Count:N0}");
                Console.WriteLine($"   🔗 Offer-Products: {offerProducts.Count:N0}");
            }
            finally
            {
                _loadingSemaphore.Release();
            }
        }

        /// <summary>
        /// 🔍 Gets a product by ID from in-memory cache
        /// </summary>
        public ProductEntity? GetProductById(int id)
        {
            EnsureDataIsLoaded();
            return _productsById.TryGetValue(id, out var product) ? product : null;
        }

        /// <summary>
        /// 🔍 Gets a product by EAN from in-memory cache
        /// </summary>
        public ProductEntity? GetProductByEAN(string ean)
        {
            if (string.IsNullOrEmpty(ean))
                return null;

            EnsureDataIsLoaded();
            return _productsByEAN.TryGetValue(ean, out var product) ? product : null;
        }

        /// <summary>
        /// 🚀 PERFORMANCE: Gets multiple products by EANs from in-memory cache
        /// </summary>
        public Dictionary<string, ProductEntity> GetProductsByEANs(IEnumerable<string> eans)
        {
            EnsureDataIsLoaded();
            var result = new Dictionary<string, ProductEntity>();

            foreach (var ean in eans.Where(e => !string.IsNullOrEmpty(e)))
            {
                if (_productsByEAN.TryGetValue(ean, out var product))
                {
                    result[ean] = product;
                }
            }

            return result;
        }

        /// <summary>
        /// 🔍 Gets all products from in-memory cache
        /// </summary>
        public IEnumerable<ProductEntity> GetAllProducts()
        {
            EnsureDataIsLoaded();
            return _productsById.Values.ToList(); // Return a copy to avoid modification
        }

        /// <summary>
        /// 🔍 Gets a supplier by ID from in-memory cache
        /// </summary>
        public SupplierEntity? GetSupplierById(int id)
        {
            EnsureDataIsLoaded();
            return _suppliersById.TryGetValue(id, out var supplier) ? supplier : null;
        }

        /// <summary>
        /// 🔍 Gets a supplier by name from in-memory cache
        /// </summary>
        public SupplierEntity? GetSupplierByName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return null;

            EnsureDataIsLoaded();
            return _suppliersByName.TryGetValue(name, out var supplier) ? supplier : null;
        }

        /// <summary>
        /// 🔍 Gets all suppliers from in-memory cache
        /// </summary>
        public IEnumerable<SupplierEntity> GetAllSuppliers()
        {
            EnsureDataIsLoaded();
            return _suppliersById.Values.ToList();
        }

        /// <summary>
        /// 🔍 Gets an offer by ID from in-memory cache
        /// </summary>
        public SupplierOfferEntity? GetOfferById(int id)
        {
            EnsureDataIsLoaded();
            return _offersById.TryGetValue(id, out var offer) ? offer : null;
        }

        /// <summary>
        /// 🔍 Gets all offers from in-memory cache
        /// </summary>
        public IEnumerable<SupplierOfferEntity> GetAllOffers()
        {
            EnsureDataIsLoaded();
            return _offersById.Values.ToList();
        }

        /// <summary>
        /// 🔍 Gets offer products by offer ID from in-memory cache
        /// </summary>
        public List<OfferProductEntity> GetOfferProductsByOfferId(int offerId)
        {
            EnsureDataIsLoaded();
            return _offerProductsByOfferId.TryGetValue(offerId, out var offerProducts) 
                ? offerProducts.ToList() // Return a copy
                : new List<OfferProductEntity>();
        }

        /// <summary>
        /// 🔍 Gets all offer products from in-memory cache
        /// </summary>
        public IEnumerable<OfferProductEntity> GetAllOfferProducts()
        {
            EnsureDataIsLoaded();
            return _offerProductsByOfferId.Values.SelectMany(x => x).ToList();
        }

        /// <summary>
        /// 🔄 Forces a refresh of the in-memory data
        /// </summary>
        public async Task RefreshDataAsync(CancellationToken cancellationToken = default)
        {
            await LoadAllDataAsync(cancellationToken);
        }

        /// <summary>
        /// ✅ Checks if the in-memory data is loaded and fresh
        /// </summary>
        public bool IsDataLoaded => _lastLoadTime != DateTime.MinValue;

        /// <summary>
        /// ✅ Checks if the in-memory data is expired and needs refresh
        /// </summary>
        public bool IsDataExpired => DateTime.UtcNow - _lastLoadTime > _cacheExpiry;

        /// <summary>
        /// 📊 Gets cache statistics
        /// </summary>
        public (int Products, int Suppliers, int Offers, int OfferProducts, DateTime LastLoaded) GetCacheStats()
        {
            return (
                _productsById.Count,
                _suppliersById.Count,
                _offersById.Count,
                _offerProductsByOfferId.Values.Sum(x => x.Count),
                _lastLoadTime
            );
        }

        /// <summary>
        /// Ensures data is loaded and fresh
        /// </summary>
        private void EnsureDataIsLoaded()
        {
            if (!IsDataLoaded)
            {
                throw new InvalidOperationException(
                    "In-memory data is not loaded. Call LoadAllDataAsync() first.");
            }

            if (IsDataExpired)
            {
                Console.WriteLine("⚠️ In-memory data cache has expired. Consider calling RefreshDataAsync().");
            }
        }

        #region In-Memory Modifications (No Database Save)

        /// <summary>
        /// 🚀 Adds or updates a product in memory only (no database save)
        /// </summary>
        public void AddOrUpdateProductInMemory(ProductEntity product)
        {
            if (product == null) throw new ArgumentNullException(nameof(product));
            
            EnsureDataIsLoaded();
            
            // Add to main cache
            _productsById.AddOrUpdate(product.Id, product, (key, existing) => product);
            if (!string.IsNullOrWhiteSpace(product.EAN))
            {
                _productsByEAN.AddOrUpdate(product.EAN, product, (key, existing) => product);
            }
            
            // Track as pending change
            _pendingProducts.AddOrUpdate(product.Id, product, (key, existing) => product);
        }

        /// <summary>
        /// 🚀 Adds or updates a supplier in memory only (no database save)
        /// </summary>
        public void AddOrUpdateSupplierInMemory(SupplierEntity supplier)
        {
            if (supplier == null) throw new ArgumentNullException(nameof(supplier));
            
            EnsureDataIsLoaded();
            
            // Add to main cache
            _suppliersById.AddOrUpdate(supplier.Id, supplier, (key, existing) => supplier);
            if (!string.IsNullOrWhiteSpace(supplier.Name))
            {
                _suppliersByName.AddOrUpdate(supplier.Name, supplier, (key, existing) => supplier);
            }
            
            // Track as pending change
            _pendingSuppliers.AddOrUpdate(supplier.Id, supplier, (key, existing) => supplier);
        }

        /// <summary>
        /// 🚀 Adds or updates an offer in memory only (no database save)
        /// </summary>
        public void AddOrUpdateOfferInMemory(SupplierOfferEntity offer)
        {
            if (offer == null) throw new ArgumentNullException(nameof(offer));
            
            EnsureDataIsLoaded();
            
            // Add to main cache
            _offersById.AddOrUpdate(offer.Id, offer, (key, existing) => offer);
            
            // Track as pending change
            _pendingOffers.AddOrUpdate(offer.Id, offer, (key, existing) => offer);
        }

        /// <summary>
        /// 🚀 Adds or updates an offer product in memory only (no database save)
        /// </summary>
        public void AddOrUpdateOfferProductInMemory(OfferProductEntity offerProduct)
        {
            if (offerProduct == null) throw new ArgumentNullException(nameof(offerProduct));
            
            EnsureDataIsLoaded();
            
            // Add to main cache - update the list for the offer
            _offerProductsByOfferId.AddOrUpdate(
                offerProduct.OfferId,
                new List<OfferProductEntity> { offerProduct },
                (key, existingList) =>
                {
                    var existing = existingList.FirstOrDefault(op => op.Id == offerProduct.Id);
                    if (existing != null)
                    {
                        existingList.Remove(existing);
                    }
                    existingList.Add(offerProduct);
                    return existingList;
                });
            
            // Track as pending change
            _pendingOfferProducts.AddOrUpdate(offerProduct.Id, offerProduct, (key, existing) => offerProduct);
        }

        /// <summary>
        /// 📊 Gets all entities that have been modified in memory
        /// </summary>
        public (List<ProductEntity> Products, List<SupplierEntity> Suppliers, List<SupplierOfferEntity> Offers, List<OfferProductEntity> OfferProducts) GetModifiedEntities()
        {
            return (
                _pendingProducts.Values.ToList(),
                _pendingSuppliers.Values.ToList(),
                _pendingOffers.Values.ToList(),
                _pendingOfferProducts.Values.ToList()
            );
        }

        /// <summary>
        /// 💾 Saves all in-memory changes to the database in a single transaction
        /// </summary>
        public async Task<(int ProductsCreated, int ProductsUpdated, int SuppliersCreated, int OffersCreated, int OfferProductsCreated, int OfferProductsUpdated)> SaveAllChangesToDatabaseAsync(CancellationToken cancellationToken = default)
        {
            // Use execution strategy to handle retries properly with transactions
            var executionStrategy = _context.Database.CreateExecutionStrategy();
            
            return await executionStrategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
                try
                {
                    int productsCreated = 0, productsUpdated = 0;
                    int suppliersCreated = 0, offersCreated = 0;
                    int offerProductsCreated = 0, offerProductsUpdated = 0;

                    // Save suppliers first (they may be referenced by offers)
                    foreach (var supplier in _pendingSuppliers.Values)
                    {
                        var existing = await _context.Suppliers.FirstOrDefaultAsync(s => s.Id == supplier.Id, cancellationToken);
                        if (existing == null)
                        {
                            _context.Suppliers.Add(supplier);
                            suppliersCreated++;
                        }
                        else
                        {
                            _context.Entry(existing).CurrentValues.SetValues(supplier);
                        }
                    }

                    // Save offers next (they may be referenced by offer products)
                    foreach (var offer in _pendingOffers.Values)
                    {
                        var existing = await _context.SupplierOffers.FirstOrDefaultAsync(o => o.Id == offer.Id, cancellationToken);
                        if (existing == null)
                        {
                            _context.SupplierOffers.Add(offer);
                            offersCreated++;
                        }
                        else
                        {
                            _context.Entry(existing).CurrentValues.SetValues(offer);
                        }
                    }

                    // Save products
                    foreach (var product in _pendingProducts.Values)
                    {
                        var existing = await _context.Products.FirstOrDefaultAsync(p => p.Id == product.Id, cancellationToken);
                        if (existing == null)
                        {
                            _context.Products.Add(product);
                            productsCreated++;
                        }
                        else
                        {
                            _context.Entry(existing).CurrentValues.SetValues(product);
                            productsUpdated++;
                        }
                    }

                    // Save offer products last
                    foreach (var offerProduct in _pendingOfferProducts.Values)
                    {
                        var existing = await _context.OfferProducts.FirstOrDefaultAsync(op => op.Id == offerProduct.Id, cancellationToken);
                        if (existing == null)
                        {
                            _context.OfferProducts.Add(offerProduct);
                            offerProductsCreated++;
                        }
                        else
                        {
                            _context.Entry(existing).CurrentValues.SetValues(offerProduct);
                            offerProductsUpdated++;
                        }
                    }

                    // Save all changes in single transaction
                    await _context.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);

                    // Clear pending changes after successful save
                    ClearPendingChanges();

                    return (productsCreated, productsUpdated, suppliersCreated, offersCreated, offerProductsCreated, offerProductsUpdated);
                }
                catch
                {
                    await transaction.RollbackAsync(cancellationToken);
                    throw;
                }
            });
        }

        /// <summary>
        /// 🧹 Clears all pending in-memory changes without saving
        /// </summary>
        public void ClearPendingChanges()
        {
            _pendingProducts.Clear();
            _pendingSuppliers.Clear();
            _pendingOffers.Clear();
            _pendingOfferProducts.Clear();
        }

        /// <summary>
        /// 📊 Gets count of pending changes
        /// </summary>
        public (int Products, int Suppliers, int Offers, int OfferProducts) GetPendingChangesCount()
        {
            return (
                _pendingProducts.Count,
                _pendingSuppliers.Count,
                _pendingOffers.Count,
                _pendingOfferProducts.Count
            );
        }

        #endregion

        /// <summary>
        /// Disposes resources
        /// </summary>
        public void Dispose()
        {
            _loadingSemaphore?.Dispose();
        }
    }
}
