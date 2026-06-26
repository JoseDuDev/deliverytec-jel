using Delify.Modules.Catalog.Infrastructure;
using Delify.Modules.Identity.Infrastructure;
using Delify.Modules.Orders.Infrastructure;
using Delify.Modules.Payments.Domain;
using Delify.Modules.Payments.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Delify.Modules.Admin.Application;

internal sealed class AdminQueryService(
    IdentityDbContext identityDb,
    CatalogDbContext catalogDb,
    OrdersDbContext ordersDb,
    PaymentsDbContext paymentsDb)
{
    public async Task<List<EstabelecimentoSummary>> GetEstabelecimentosAsync()
    {
        var tenants = await identityDb.Tenants
            .AsNoTracking()
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

        var orderCounts = await ordersDb.Orders
            .GroupBy(o => o.TenantId)
            .Select(g => new { TenantId = g.Key, Count = g.Count() })
            .ToListAsync();

        var revenues = await paymentsDb.Payments
            .Where(p => p.Status == PaymentStatus.Confirmed)
            .GroupBy(p => p.TenantId)
            .Select(g => new { TenantId = g.Key, Total = g.Sum(p => p.Amount) })
            .ToListAsync();

        return tenants.Select(t => new EstabelecimentoSummary(
            t.Id, t.Name, t.Slug, t.IsActive, t.CreatedAt,
            orderCounts.FirstOrDefault(x => x.TenantId == t.Id)?.Count ?? 0,
            revenues.FirstOrDefault(x => x.TenantId == t.Id)?.Total ?? 0m
        )).ToList();
    }

    public async Task<EstabelecimentoDetail?> GetEstabelecimentoDetailAsync(Guid tenantId)
    {
        var tenant = await identityDb.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == tenantId);

        if (tenant is null) return null;

        var establishment = await catalogDb.Establishments
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.TenantId == tenantId);

        var monthAgo = DateTimeOffset.UtcNow.AddDays(-30);

        var totalOrders  = await ordersDb.Orders.CountAsync(o => o.TenantId == tenantId);
        var recentOrders = await ordersDb.Orders
            .Include(o => o.Items)
            .Where(o => o.TenantId == tenantId)
            .OrderByDescending(o => o.CreatedAt)
            .Take(10)
            .AsNoTracking()
            .ToListAsync();
        var totalRevenue = await paymentsDb.Payments
            .Where(p => p.TenantId == tenantId && p.Status == PaymentStatus.Confirmed)
            .SumAsync(p => (decimal?)p.Amount);
        var monthRevenue = await paymentsDb.Payments
            .Where(p => p.TenantId == tenantId && p.Status == PaymentStatus.Confirmed
                     && p.CreatedAt >= monthAgo)
            .SumAsync(p => (decimal?)p.Amount);

        var catalogInfo = establishment is null ? null : new EstabelecimentoCatalogInfo(
            establishment.Id, establishment.Name, establishment.Description,
            establishment.LogoUrl, establishment.IsOpen);

        var orders = recentOrders.Select(o => new OrderSummary(
            o.Id, o.Status.ToString(), o.Total, o.Items.Count, o.CreatedAt)).ToList();

        return new EstabelecimentoDetail(
            tenant.Id, tenant.Name, tenant.Slug, tenant.IsActive, tenant.CreatedAt,
            catalogInfo,
            new EstabelecimentoStats(
                totalOrders,
                totalRevenue ?? 0m,
                monthRevenue ?? 0m),
            orders);
    }
}

internal record EstabelecimentoSummary(
    Guid Id, string Name, string Slug, bool IsActive,
    DateTimeOffset CreatedAt, int TotalOrders, decimal TotalRevenue);

internal record EstabelecimentoDetail(
    Guid Id, string Name, string Slug, bool IsActive, DateTimeOffset CreatedAt,
    EstabelecimentoCatalogInfo? Catalog,
    EstabelecimentoStats Stats,
    List<OrderSummary> RecentOrders);

internal record EstabelecimentoCatalogInfo(
    Guid Id, string Name, string? Description, string? LogoUrl, bool IsOpen);

internal record EstabelecimentoStats(
    int TotalOrders, decimal TotalRevenue, decimal RevenueLastMonth);

internal record OrderSummary(
    Guid Id, string Status, decimal Total, int ItemCount, DateTimeOffset CreatedAt);
