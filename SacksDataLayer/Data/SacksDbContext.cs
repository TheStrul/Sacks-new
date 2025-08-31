using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace SacksDataLayer.Data
{
    /// <summary>
    /// Main database context for the Sacks application
    /// </summary>
    public class SacksDbContext : DbContext
    {
        public SacksDbContext(DbContextOptions<SacksDbContext> options) : base(options)
        {
        }

        /// <summary>
        /// Products table
        /// </summary>
        public DbSet<ProductEntity> Products { get; set; }

        /// <summary>
        /// Suppliers table
        /// </summary>
        public DbSet<SupplierEntity> Suppliers { get; set; }

        /// <summary>
        /// Supplier offers table
        /// </summary>
        public DbSet<SupplierOfferEntity> SupplierOffers { get; set; }

        /// <summary>
        /// Offer products junction table
        /// </summary>
        public DbSet<OfferProductEntity> OfferProducts { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure ProductEntity
            modelBuilder.Entity<ProductEntity>(entity =>
            {
                // Primary key
                entity.HasKey(e => e.Id);

                // Index configurations for common queries
                entity.HasIndex(e => e.SKU)
                      .IsUnique(false)
                      .HasDatabaseName("IX_Products_SKU");

                entity.HasIndex(e => e.Name)
                      .HasDatabaseName("IX_Products_Name");

                entity.HasIndex(e => e.CreatedAt)
                      .HasDatabaseName("IX_Products_CreatedAt");

                entity.HasIndex(e => e.ModifiedAt)
                      .HasDatabaseName("IX_Products_ModifiedAt");

                // Property configurations
                entity.Property(e => e.Name)
                      .IsRequired()
                      .HasMaxLength(255);

                entity.Property(e => e.Description)
                      .HasMaxLength(2000);

                entity.Property(e => e.SKU)
                      .HasMaxLength(100);

                // Configure DynamicPropertiesJson as JSON column (core product attributes)
                entity.Property(e => e.DynamicPropertiesJson)
                      .HasColumnType("JSON")
                      .HasColumnName("DynamicProperties");

                // Configure the DynamicProperties to be ignored by EF (since it's handled by JSON property)
                entity.Ignore(e => e.DynamicProperties);

                // Configure timestamps with default values (MySQL compatible)
                entity.Property(e => e.CreatedAt)
                      .HasDefaultValueSql("UTC_TIMESTAMP()");

                entity.Property(e => e.ModifiedAt)
                      .HasDefaultValueSql("UTC_TIMESTAMP()");
            });

            // Configure SupplierEntity
            modelBuilder.Entity<SupplierEntity>(entity =>
            {
                // Primary key
                entity.HasKey(e => e.Id);

                // Index configurations
                entity.HasIndex(e => e.Name)
                      .IsUnique()
                      .HasDatabaseName("IX_Suppliers_Name");

                // Property configurations
                entity.Property(e => e.Name)
                      .IsRequired()
                      .HasMaxLength(100);

                entity.Property(e => e.Description)
                      .HasMaxLength(500);

                entity.Property(e => e.Industry)
                      .HasMaxLength(100);

                entity.Property(e => e.Region)
                      .HasMaxLength(100);

                entity.Property(e => e.ContactName)
                      .HasMaxLength(255);

                entity.Property(e => e.ContactEmail)
                      .HasMaxLength(255);

                entity.Property(e => e.Company)
                      .HasMaxLength(255);

                entity.Property(e => e.FileFrequency)
                      .HasMaxLength(50);

                entity.Property(e => e.Notes)
                      .HasColumnType("TEXT");
            });

            // Configure SupplierOfferEntity
            modelBuilder.Entity<SupplierOfferEntity>(entity =>
            {
                // Primary key
                entity.HasKey(e => e.Id);

                // Index configurations
                entity.HasIndex(e => e.SupplierId)
                      .HasDatabaseName("IX_SupplierOffers_Supplier");

                entity.HasIndex(e => e.IsActive)
                      .HasDatabaseName("IX_SupplierOffers_IsActive");

                entity.HasIndex(e => e.ValidFrom)
                      .HasDatabaseName("IX_SupplierOffers_ValidFrom");

                entity.HasIndex(e => e.ValidTo)
                      .HasDatabaseName("IX_SupplierOffers_ValidTo");

                // Property configurations
                entity.Property(e => e.OfferName)
                      .HasMaxLength(255);

                entity.Property(e => e.Description)
                      .HasMaxLength(500);

                entity.Property(e => e.Currency)
                      .HasMaxLength(20);

                entity.Property(e => e.OfferType)
                      .HasMaxLength(100);

                entity.Property(e => e.Version)
                      .HasMaxLength(50);

                // Foreign key relationships
                entity.HasOne(e => e.Supplier)
                      .WithMany(s => s.Offers)
                      .HasForeignKey(e => e.SupplierId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure OfferProductEntity (Junction Table)
            modelBuilder.Entity<OfferProductEntity>(entity =>
            {
                // Primary key
                entity.HasKey(e => e.Id);

                // Index configurations
                entity.HasIndex(e => new { e.OfferId, e.ProductId })
                      .HasDatabaseName("IX_OfferProducts_Offer_Product");

                entity.HasIndex(e => e.IsAvailable)
                      .HasDatabaseName("IX_OfferProducts_IsAvailable");

                // Property configurations
                entity.Property(e => e.Price)
                      .HasColumnType("decimal(18,2)");

                entity.Property(e => e.Discount)
                      .HasColumnType("decimal(18,4)");

                entity.Property(e => e.ListPrice)
                      .HasColumnType("decimal(18,2)");

                entity.Property(e => e.Capacity)
                      .HasMaxLength(50);

                entity.Property(e => e.UnitOfMeasure)
                      .HasMaxLength(50);

                entity.Property(e => e.Notes)
                      .HasMaxLength(255);

                // Configure ProductPropertiesJson as JSON column
                entity.Property(e => e.ProductPropertiesJson)
                      .HasColumnType("JSON")
                      .HasColumnName("ProductProperties");

                // Configure the ProductProperties to be ignored by EF (since it's handled by JSON property)
                entity.Ignore(e => e.ProductProperties);

                // Foreign key relationships
                entity.HasOne(e => e.Offer)
                      .WithMany(o => o.OfferProducts)
                      .HasForeignKey(e => e.OfferId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Product)
                      .WithMany(p => p.OfferProducts)
                      .HasForeignKey(e => e.ProductId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }

        /// <summary>
        /// Override SaveChanges to automatically update audit fields
        /// </summary>
        public override int SaveChanges()
        {
            UpdateAuditFields();
            return base.SaveChanges();
        }

        /// <summary>
        /// Override SaveChangesAsync to automatically update audit fields
        /// </summary>
        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            UpdateAuditFields();
            return await base.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// Updates audit fields for entities being saved
        /// </summary>
        private void UpdateAuditFields()
        {
            var productEntries = ChangeTracker.Entries<ProductEntity>()
                .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

            foreach (var entry in productEntries)
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Entity.CreatedAt = DateTime.UtcNow;
                    entry.Entity.ModifiedAt = DateTime.UtcNow;
                }
                else if (entry.State == EntityState.Modified)
                {
                    entry.Entity.ModifiedAt = DateTime.UtcNow;
                }
            }

            // Handle OfferProductEntity serialization before saving
            var offerProductEntries = ChangeTracker.Entries<OfferProductEntity>()
                .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

            foreach (var entry in offerProductEntries)
            {
                entry.Entity.SerializeProductProperties();
            }
        }
    }
}
