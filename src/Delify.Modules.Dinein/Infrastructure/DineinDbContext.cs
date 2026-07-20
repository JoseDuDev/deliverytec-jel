using Delify.Modules.Dinein.Domain;
using Microsoft.EntityFrameworkCore;

namespace Delify.Modules.Dinein.Infrastructure;

public sealed class DineinDbContext(DbContextOptions<DineinDbContext> options) : DbContext(options)
{
    public DbSet<Table> Tables => Set<Table>();
    public DbSet<TableSession> Sessions => Set<TableSession>();
    public DbSet<WaiterCall> WaiterCalls => Set<WaiterCall>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("dinein");

        modelBuilder.Entity<Table>(e =>
        {
            e.HasKey(t => t.Id);
            e.Property(t => t.Number).HasMaxLength(40);
            e.Property(t => t.QrToken).HasMaxLength(64);
            e.HasIndex(t => t.QrToken).IsUnique();
            e.HasIndex(t => new { t.EstablishmentId, t.Number }).IsUnique();
            e.Property(t => t.Status).HasConversion<string>().HasMaxLength(20);
        });

        modelBuilder.Entity<TableSession>(e =>
        {
            e.HasKey(s => s.Id);
            e.Property(s => s.Status).HasConversion<string>().HasMaxLength(20);
            e.HasIndex(s => new { s.TableId, s.Status });
        });

        modelBuilder.Entity<WaiterCall>(e =>
        {
            e.HasKey(w => w.Id);
            e.Property(w => w.TableNumber).HasMaxLength(40);
            e.Property(w => w.Reason).HasMaxLength(120);
            e.Property(w => w.Status).HasConversion<string>().HasMaxLength(20);
            e.HasIndex(w => new { w.EstablishmentId, w.Status });
            e.HasIndex(w => new { w.TableId, w.Status });
        });
    }
}
