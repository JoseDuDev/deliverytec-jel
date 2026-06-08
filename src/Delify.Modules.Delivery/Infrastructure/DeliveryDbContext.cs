using Delify.Modules.Delivery.Domain;
using Microsoft.EntityFrameworkCore;

namespace Delify.Modules.Delivery.Infrastructure;

public sealed class DeliveryDbContext(DbContextOptions<DeliveryDbContext> options) : DbContext(options)
{
    public DbSet<DeliveryOrder> DeliveryOrders => Set<DeliveryOrder>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("delivery");
        modelBuilder.Entity<DeliveryOrder>(e =>
        {
            e.HasKey(d => d.Id);
            e.HasIndex(d => d.OrderId);
            e.Property(d => d.QuotedPrice).HasColumnType("numeric(10,2)");
            e.Property(d => d.Status).HasConversion<string>();
        });
    }
}
