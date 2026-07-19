using Delify.Modules.Orders.Domain;
using Microsoft.EntityFrameworkCore;

namespace Delify.Modules.Orders.Infrastructure;

public sealed class OrdersDbContext(DbContextOptions<OrdersDbContext> options) : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("orders");

        modelBuilder.Entity<Order>(e =>
        {
            e.HasKey(o => o.Id);
            e.Property(o => o.Status).HasConversion<string>();
            e.Property(o => o.Type).HasConversion<string>().HasMaxLength(20).HasDefaultValue(OrderType.Delivery);
            e.HasIndex(o => o.TableSessionId);
            e.Property(o => o.DeliveryFee).HasColumnType("numeric(10,2)").HasDefaultValue(0m);
            e.Ignore(o => o.Total);
            e.HasMany(o => o.Items).WithOne().HasForeignKey(i => i.OrderId);
            e.Ignore(o => o.DomainEvents);
        });

        modelBuilder.Entity<OrderItem>(e =>
        {
            e.HasKey(i => i.Id);
            e.Property(i => i.UnitPrice).HasColumnType("numeric(10,2)");
            e.Ignore(i => i.Total);
        });
    }
}
