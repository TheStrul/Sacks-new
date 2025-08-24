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

                entity.HasIndex(e => e.IsDeleted)
                      .HasDatabaseName("IX_Products_IsDeleted");

                // Composite index for soft delete queries
                entity.HasIndex(e => new { e.IsDeleted, e.CreatedAt })
                      .HasDatabaseName("IX_Products_IsDeleted_CreatedAt");

                // Property configurations
                entity.Property(e => e.Name)
                      .IsRequired()
                      .HasMaxLength(255);

                entity.Property(e => e.Description)
                      .HasMaxLength(2000);

                entity.Property(e => e.SKU)
                      .HasMaxLength(100);

                entity.Property(e => e.CreatedBy)
                      .HasMaxLength(100);

                entity.Property(e => e.ModifiedBy)
                      .HasMaxLength(100);

                entity.Property(e => e.DeletedBy)
                      .HasMaxLength(100);

                // Configure DynamicPropertiesJson as JSON column
                entity.Property(e => e.DynamicPropertiesJson)
                      .HasColumnType("nvarchar(max)")
                      .HasColumnName("DynamicProperties");

                // Configure the DynamicProperties to be ignored by EF (since it's handled by DynamicPropertiesJson)
                entity.Ignore(e => e.DynamicProperties);

                // Configure timestamps with default values
                entity.Property(e => e.CreatedAt)
                      .HasDefaultValueSql("GETUTCDATE()");

                entity.Property(e => e.IsDeleted)
                      .HasDefaultValue(false);
            });

            // Global query filter to exclude soft deleted entities by default
            modelBuilder.Entity<ProductEntity>().HasQueryFilter(e => !e.IsDeleted);
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
            var entries = ChangeTracker.Entries<Entity>()
                .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

            foreach (var entry in entries)
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Entity.CreatedAt = DateTime.UtcNow;
                }
                else if (entry.State == EntityState.Modified)
                {
                    entry.Entity.UpdateModified();
                }
            }
        }

        /// <summary>
        /// Gets products including soft deleted ones
        /// </summary>
        public IQueryable<ProductEntity> ProductsWithDeleted => Products.IgnoreQueryFilters();
    }
}
