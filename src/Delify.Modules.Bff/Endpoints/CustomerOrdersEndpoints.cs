using System.Security.Claims;
using Delify.Modules.Catalog.Infrastructure;
using Delify.Modules.Orders.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Delify.Modules.Bff.Endpoints;

internal static class CustomerOrdersEndpoints
{
    internal static IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/bff/orders/mine", async (
            ClaimsPrincipal user,
            OrdersDbContext ordersDb,
            CatalogDbContext catalogDb,
            CancellationToken ct) =>
        {
            var sub = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                   ?? user.FindFirst("sub")?.Value;

            if (!Guid.TryParse(sub, out var customerId))
                return Results.Unauthorized();

            var orders = await ordersDb.Orders
                .AsNoTracking()
                .Include(o => o.Items)
                .Where(o => o.CustomerId == customerId)
                .OrderByDescending(o => o.CreatedAt)
                .Take(20)
                .ToListAsync(ct);

            var estIds = orders.Select(o => o.EstablishmentId).Distinct().ToList();
            var establishments = await catalogDb.Establishments
                .AsNoTracking()
                .Where(e => estIds.Contains(e.Id))
                .ToDictionaryAsync(e => e.Id, ct);

            var result = orders.Select(o =>
            {
                var est = establishments.GetValueOrDefault(o.EstablishmentId);
                return new
                {
                    id = o.Id,
                    status = o.Status.ToString(),
                    total = o.Total,
                    deliveryFee = o.DeliveryFee,
                    createdAt = o.CreatedAt,
                    establishment = est is null ? null : new { name = est.Name, slug = est.Slug },
                    items = o.Items.Select(i => new { i.ProductName, i.Quantity }),
                };
            });

            return Results.Ok(result);
        })
        .WithName("BffMyOrders")
        .WithTags("BFF")
        .RequireAuthorization();

        return app;
    }
}
