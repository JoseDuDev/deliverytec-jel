using Delify.Modules.Catalog.Domain;
using Microsoft.EntityFrameworkCore;

namespace Delify.Modules.Catalog.Infrastructure;

public sealed class CatalogDbContext(DbContextOptions<CatalogDbContext> options) : DbContext(options)
{
    public DbSet<Establishment> Establishments => Set<Establishment>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Complement> Complements => Set<Complement>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("catalog");

        modelBuilder.Entity<Establishment>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Slug).IsUnique();
            e.Property(x => x.Slug).HasMaxLength(100);
            e.Property(x => x.Name).HasMaxLength(200);
            e.Property(x => x.DeliveryFee).HasColumnType("numeric(10,2)").HasDefaultValue(0m);
            e.Property(x => x.ServiceFeeEnabled).HasDefaultValue(true);
            e.Property(x => x.ServiceFeePercent).HasColumnType("numeric(5,2)").HasDefaultValue(10m);
            e.HasMany(x => x.Categories).WithOne().HasForeignKey(c => c.EstablishmentId);
        });

        modelBuilder.Entity<Category>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(100);
            e.HasMany(x => x.Products).WithOne().HasForeignKey(p => p.CategoryId);
        });

        modelBuilder.Entity<Product>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Price).HasColumnType("numeric(10,2)");
            e.Property(x => x.Name).HasMaxLength(200);
            // Default explícito para a migration preencher as linhas existentes.
            e.Property(x => x.IsFeatured).HasDefaultValue(false);
            e.Property(x => x.FeaturedOrder).HasDefaultValue(0);
            e.HasMany(x => x.Complements).WithOne().HasForeignKey(c => c.ProductId);
        });

        modelBuilder.Entity<Complement>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.AdditionalPrice).HasColumnType("numeric(10,2)");
            e.Property(x => x.Name).HasMaxLength(100);
        });
    }
}
