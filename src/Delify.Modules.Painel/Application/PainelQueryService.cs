using Delify.Modules.Catalog.Infrastructure;
using Delify.Modules.Orders.Infrastructure;
using Delify.Modules.Payments.Domain;
using Delify.Modules.Payments.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Delify.Modules.Painel.Application;

internal sealed class PainelQueryService(
    CatalogDbContext catalogDb,
    OrdersDbContext ordersDb,
    PaymentsDbContext paymentsDb)
{
    public async Task<DashboardResponse?> GetDashboardAsync(Guid tenantId)
    {
        var establishment = await catalogDb.Establishments
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.TenantId == tenantId);

        if (establishment is null) return null;

        var todayStart = new DateTimeOffset(DateTime.UtcNow.Date, TimeSpan.Zero);

        var ordersToday = await ordersDb.Orders
            .CountAsync(o => o.TenantId == tenantId && o.CreatedAt >= todayStart);

        var revenueToday = await paymentsDb.Payments
            .Where(p => p.TenantId == tenantId
                     && p.Status == PaymentStatus.Confirmed
                     && p.CreatedAt >= todayStart)
            .SumAsync(p => (decimal?)p.Amount);

        return new DashboardResponse(
            establishment.Id,
            establishment.Name,
            establishment.Slug,
            establishment.IsOpen,
            establishment.Description,
            establishment.LogoUrl,
            establishment.DeliveryFee,
            ordersToday,
            revenueToday ?? 0m);
    }
}

internal record DashboardResponse(
    Guid Id,
    string Name,
    string Slug,
    bool IsOpen,
    string? Description,
    string? LogoUrl,
    decimal DeliveryFee,
    int OrdersToday,
    decimal RevenueToday);
