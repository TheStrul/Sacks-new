using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using SacksDataLayer.Entities;

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
        public DbSet<SupplierOfferAnnex> SupplierOffers { get; set; }

        /// <summary>
        /// Offer products junction table
        /// </summary>
        public DbSet<OfferProductAnnex> OfferProducts { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            ArgumentNullException.ThrowIfNull(modelBuilder);
            
            base.OnModelCreating(modelBuilder);

            // Configure ProductEntity
            modelBuilder.Entity<ProductEntity>(entity =>
            {
                // Primary key
                entity.HasKey(e => e.Id);

                // Index configurations for common queries
                entity.HasIndex(e => e.EAN)
                      .IsUnique(true)  // Make EAN unique as it should be a unique product identifier
                      .HasDatabaseName("IX_Products_EAN")
                      .HasFilter("EAN IS NOT NULL AND EAN != ''");  // Only enforce uniqueness for non-empty EANs

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

                entity.Property(e => e.EAN)
                      .HasMaxLength(100);

                // Configure DynamicPropertiesJson as JSON column (SQL Server compatible)
                entity.Property(e => e.DynamicPropertiesJson)
                      .HasColumnType("nvarchar(max)")
                      .HasColumnName("DynamicProperties");

                // Configure the DynamicProperties to be ignored by EF (since it's handled by JSON property)
                entity.Ignore(e => e.DynamicProperties);

                // Configure timestamps with default values (SQL Server compatible)
                entity.Property(e => e.CreatedAt)
                      .HasDefaultValueSql("GETUTCDATE()");

                entity.Property(e => e.ModifiedAt)
                      .HasDefaultValueSql("GETUTCDATE()");
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
            });

            // Configure SupplierOfferAnnex
            modelBuilder.Entity<SupplierOfferAnnex>(entity =>
            {
                // Primary key
                entity.HasKey(e => e.Id);

                // Index configurations
                entity.HasIndex(e => e.SupplierId)
                      .HasDatabaseName("IX_SupplierOffers_Supplier");

                // Property configurations
                entity.Property(e => e.OfferName)
                      .HasMaxLength(255);

                entity.Property(e => e.Description)
                      .HasMaxLength(500);

                entity.Property(e => e.Currency)
                      .HasMaxLength(20);

                // Foreign key relationships
                entity.HasOne(e => e.Supplier)
                      .WithMany(s => s.Offers)
                      .HasForeignKey(e => e.SupplierId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure OfferProductAnnex (Junction Table)
            modelBuilder.Entity<OfferProductAnnex>(entity =>
            {
                // Primary key
                entity.HasKey(e => e.Id);

                // Index configurations
                entity.HasIndex(e => new { e.OfferId, e.ProductId })
                      .HasDatabaseName("IX_OfferProducts_Offer_Product");

                // Property configurations
                entity.Property(e => e.Price)
                      .HasColumnType("decimal(18,2)");

                entity.Property(e => e.Description)
                      .HasMaxLength(2000);

                // Configure OfferPropertiesJson as JSON column (SQL Server compatible)
                entity.Property(e => e.OfferPropertiesJson)
                      .HasColumnType("nvarchar(max)")
                      .HasColumnName("OfferProperties");

                // Configure the OfferProperties to be ignored by EF (since it's handled by JSON property)
                entity.Ignore(e => e.OfferProperties);

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

            // Handle OfferProductAnnex serialization before saving
            var offerProductEntries = ChangeTracker.Entries<OfferProductAnnex>()
                .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

            foreach (var entry in offerProductEntries)
            {
                entry.Entity.SerializeOfferProperties();
            }
        }
    }
}
