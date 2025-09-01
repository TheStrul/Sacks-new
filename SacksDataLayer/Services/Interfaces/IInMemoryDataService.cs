using SacksDataLayer.Entities;

namespace SacksDataLayer.Services.Interfaces
{
    /// <summary>
    /// 🚀 PERFORMANCE: Interface for thread-safe in-memory data cache service
    /// </summary>
    public interface IInMemoryDataService : IDisposable
    {
        /// <summary>
        /// Loads all data from database into thread-safe in-memory collections
        /// </summary>
        Task LoadAllDataAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a product by ID from in-memory cache
        /// </summary>
        ProductEntity? GetProductById(int id);

        /// <summary>
        /// Gets a product by EAN from in-memory cache
        /// </summary>
        ProductEntity? GetProductByEAN(string ean);

        /// <summary>
        /// Gets multiple products by EANs from in-memory cache
        /// </summary>
        Dictionary<string, ProductEntity> GetProductsByEANs(IEnumerable<string> eans);

        /// <summary>
        /// Gets all products from in-memory cache
        /// </summary>
        IEnumerable<ProductEntity> GetAllProducts();

        /// <summary>
        /// Gets a supplier by ID from in-memory cache
        /// </summary>
        SupplierEntity? GetSupplierById(int id);

        /// <summary>
        /// Gets a supplier by name from in-memory cache
        /// </summary>
        SupplierEntity? GetSupplierByName(string name);

        /// <summary>
        /// Gets all suppliers from in-memory cache
        /// </summary>
        IEnumerable<SupplierEntity> GetAllSuppliers();

        /// <summary>
        /// Gets an offer by ID from in-memory cache
        /// </summary>
        SupplierOfferEntity? GetOfferById(int id);

        /// <summary>
        /// Gets all offers from in-memory cache
        /// </summary>
        IEnumerable<SupplierOfferEntity> GetAllOffers();

        /// <summary>
        /// Gets offer products by offer ID from in-memory cache
        /// </summary>
        List<OfferProductEntity> GetOfferProductsByOfferId(int offerId);

        /// <summary>
        /// Gets all offer products from in-memory cache
        /// </summary>
        IEnumerable<OfferProductEntity> GetAllOfferProducts();

        /// <summary>
        /// Forces a refresh of the in-memory data
        /// </summary>
        Task RefreshDataAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if the in-memory data is loaded and fresh
        /// </summary>
        bool IsDataLoaded { get; }

        /// <summary>
        /// Checks if the in-memory data is expired and needs refresh
        /// </summary>
        bool IsDataExpired { get; }

        /// <summary>
        /// Gets cache statistics
        /// </summary>
        (int Products, int Suppliers, int Offers, int OfferProducts, DateTime LastLoaded) GetCacheStats();

        #region In-Memory Modifications (No Database Save)

        /// <summary>
        /// Adds or updates a product in memory only (no database save)
        /// </summary>
        void AddOrUpdateProductInMemory(ProductEntity product);

        /// <summary>
        /// Adds or updates a supplier in memory only (no database save)
        /// </summary>
        void AddOrUpdateSupplierInMemory(SupplierEntity supplier);

        /// <summary>
        /// Adds or updates an offer in memory only (no database save)
        /// </summary>
        void AddOrUpdateOfferInMemory(SupplierOfferEntity offer);

        /// <summary>
        /// Adds or updates an offer product in memory only (no database save)
        /// </summary>
        void AddOrUpdateOfferProductInMemory(OfferProductEntity offerProduct);

        /// <summary>
        /// Gets all entities that have been modified in memory
        /// </summary>
        (List<ProductEntity> Products, List<SupplierEntity> Suppliers, List<SupplierOfferEntity> Offers, List<OfferProductEntity> OfferProducts) GetModifiedEntities();

        /// <summary>
        /// Saves all in-memory changes to the database in a single transaction
        /// </summary>
        Task<(int ProductsCreated, int ProductsUpdated, int SuppliersCreated, int OffersCreated, int OfferProductsCreated, int OfferProductsUpdated)> SaveAllChangesToDatabaseAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Clears all pending in-memory changes without saving
        /// </summary>
        void ClearPendingChanges();

        /// <summary>
        /// Gets count of pending changes
        /// </summary>
        (int Products, int Suppliers, int Offers, int OfferProducts) GetPendingChangesCount();

        #endregion
    }
}
