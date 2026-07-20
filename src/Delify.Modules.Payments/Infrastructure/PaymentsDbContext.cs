using Delify.Modules.Payments.Domain;
using Microsoft.EntityFrameworkCore;

namespace Delify.Modules.Payments.Infrastructure;

public sealed class PaymentsDbContext(DbContextOptions<PaymentsDbContext> options) : DbContext(options)
{
    public DbSet<Payment> Payments => Set<Payment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("payments");
        modelBuilder.Entity<Payment>(e =>
        {
            e.HasKey(p => p.Id);
            e.HasIndex(p => p.OrderId);
            e.HasIndex(p => p.TableSessionId);
            e.Property(p => p.Amount).HasColumnType("numeric(10,2)");
            e.Property(p => p.Status).HasConversion<string>();
        });
    }
}
