using Delify.Modules.Catalog.Infrastructure;
using Delify.Modules.Dinein.Domain;
using Delify.Modules.Dinein.Infrastructure;
using Delify.Modules.Orders.Domain;
using Delify.Modules.Orders.Infrastructure;
using Delify.Shared.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Delify.Modules.Dinein.Endpoints;

internal static class DineinMesaEndpoints
{
    internal static IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        // ── Cardápio da mesa + comanda corrente ─────────────────────────────
        app.MapGet("/bff/mesa/{token}", async (
            string token,
            DineinDbContext db,
            CatalogDbContext catalogDb,
            OrdersDbContext ordersDb) =>
        {
            var table = await db.Tables.AsNoTracking()
                .FirstOrDefaultAsync(t => t.QrToken == token);
            if (table is null)
                return Results.NotFound(new { error = "Mesa não encontrada." });

            var establishment = await catalogDb.Establishments
                .Include(e => e.Categories.OrderBy(c => c.Order))
                    .ThenInclude(c => c.Products)
                        .ThenInclude(p => p.Complements)
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == table.EstablishmentId);
            if (establishment is null)
                return Results.NotFound(new { error = "Estabelecimento não encontrado." });

            var session = await db.Sessions.AsNoTracking()
                .FirstOrDefaultAsync(s => s.TableId == table.Id && s.Status == SessionStatus.Open);

            var comanda = await BuildComandaAsync(session, ordersDb);

            var categories = establishment.Categories
                .Select(c => new MesaCategoryDto(
                    c.Id, c.Name, c.Order,
                    c.Products
                        .Select(p => new MesaProductDto(
                            p.Id, p.Name, p.Price, p.Description, p.PhotoUrl,
                            p.Complements
                                .Select(cp => new MesaComplementDto(cp.Id, cp.Name, cp.AdditionalPrice))
                                .ToList()))
                        .ToList()))
                .ToList();

            return Results.Ok(new MesaResponse(
                table.Id, table.Number,
                establishment.Id, establishment.Name, establishment.Slug, establishment.IsOpen,
                categories, comanda));
        })
        .WithName("MesaGetMenu")
        .WithTags("Mesa")
        .AllowAnonymous();

        // ── Enviar rodada de pedido ─────────────────────────────────────────
        app.MapPost("/bff/mesa/{token}/pedido", async (
            string token,
            PlaceMesaOrderReq req,
            DineinDbContext db,
            CatalogDbContext catalogDb,
            OrdersDbContext ordersDb,
            IPainelDashboardNotifier painelNotifier) =>
        {
            if (req.Items is null || req.Items.Count == 0)
                return Results.BadRequest(new { error = "Adicione ao menos um item ao pedido." });

            var table = await db.Tables
                .FirstOrDefaultAsync(t => t.QrToken == token);
            if (table is null)
                return Results.NotFound(new { error = "Mesa não encontrada." });

            var establishment = await catalogDb.Establishments.AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == table.EstablishmentId);
            if (establishment is null)
                return Results.NotFound(new { error = "Estabelecimento não encontrado." });
            if (!establishment.IsOpen)
                return Results.Conflict(new { error = "O estabelecimento está fechado no momento." });

            var productIds = req.Items.Select(i => i.ProductId).ToList();
            var products = await catalogDb.Products
                .Include(p => p.Complements)
                .Where(p => productIds.Contains(p.Id))
                .ToListAsync();

            var missing = productIds.Except(products.Select(p => p.Id)).ToList();
            if (missing.Count > 0)
                return Results.BadRequest(new { error = "Um ou mais produtos não foram encontrados.", missingIds = missing });

            // Abre a comanda de forma lazy: a mesa só fica ocupada no 1º pedido.
            var session = await db.Sessions
                .FirstOrDefaultAsync(s => s.TableId == table.Id && s.Status == SessionStatus.Open);
            if (session is null)
            {
                session = TableSession.Open(table.TenantId, table.EstablishmentId, table.Id);
                db.Sessions.Add(session);
                table.Occupy();
                await db.SaveChangesAsync();
            }

            var order = Order.CreateDinein(establishment.TenantId, table.EstablishmentId, session.Id, req.Note);
            foreach (var itemReq in req.Items)
            {
                var product = products.First(p => p.Id == itemReq.ProductId);
                var complementsTotal = itemReq.ComplementIds is { Count: > 0 }
                    ? product.Complements
                        .Where(c => itemReq.ComplementIds.Contains(c.Id))
                        .Sum(c => c.AdditionalPrice)
                    : 0m;
                var unitPrice = product.Price + complementsTotal;
                order.AddItem(product.Id, product.Name, itemReq.Quantity, unitPrice);
            }

            ordersDb.Orders.Add(order);
            await ordersDb.SaveChangesAsync();

            // Empurra pro dashboard da cozinha em tempo real.
            painelNotifier.Notify(order.TenantId,
                new PainelOrderEvent(order.Id, "AwaitingConfirmation", DateTimeOffset.UtcNow));

            var comanda = await BuildComandaAsync(session, ordersDb);
            return Results.Ok(new PlaceMesaOrderResponse(order.Id, session.Id, order.Total, comanda));
        })
        .WithName("MesaPlaceOrder")
        .WithTags("Mesa")
        .AllowAnonymous();

        return app;
    }

    private static async Task<ComandaDto> BuildComandaAsync(TableSession? session, OrdersDbContext ordersDb)
    {
        if (session is null)
            return new ComandaDto(null, null, [], 0);

        var orders = await ordersDb.Orders.AsNoTracking()
            .Include(o => o.Items)
            .Where(o => o.TableSessionId == session.Id && o.Status != OrderStatus.Cancelled)
            .OrderBy(o => o.CreatedAt)
            .ToListAsync();

        var items = orders
            .SelectMany(o => o.Items)
            .Select(i => new ComandaItemDto(i.ProductName, i.Quantity, i.UnitPrice, i.Total))
            .ToList();

        return new ComandaDto(session.Id, session.OpenedAt, items, items.Sum(i => i.Total));
    }
}

internal record MesaComplementDto(Guid Id, string Name, decimal Price);
internal record MesaProductDto(Guid Id, string Name, decimal Price, string? Description, string? ImageUrl, IReadOnlyList<MesaComplementDto> Complements);
internal record MesaCategoryDto(Guid Id, string Name, int Order, IReadOnlyList<MesaProductDto> Products);
internal record ComandaItemDto(string ProductName, int Quantity, decimal UnitPrice, decimal Total);
internal record ComandaDto(Guid? SessionId, DateTimeOffset? OpenedAt, IReadOnlyList<ComandaItemDto> Items, decimal Total);
internal record MesaResponse(
    Guid TableId, string TableNumber,
    Guid EstablishmentId, string EstablishmentName, string Slug, bool IsOpen,
    IReadOnlyList<MesaCategoryDto> Categories, ComandaDto Comanda);

internal record MesaOrderItemReq(Guid ProductId, int Quantity, IReadOnlyList<Guid>? ComplementIds);
internal record PlaceMesaOrderReq(IReadOnlyList<MesaOrderItemReq> Items, string? Note);
internal record PlaceMesaOrderResponse(Guid OrderId, Guid SessionId, decimal OrderTotal, ComandaDto Comanda);
