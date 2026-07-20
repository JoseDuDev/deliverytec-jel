using System.Security.Claims;
using System.Text.Json;
using Delify.Modules.Catalog.Infrastructure;
using Delify.Modules.Dinein.Domain;
using Delify.Modules.Dinein.Infrastructure;
using Delify.Modules.Dinein.Services;
using Delify.Modules.Orders.Domain;
using Delify.Modules.Orders.Infrastructure;
using Delify.Shared.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Delify.Modules.Dinein.Endpoints;

// App do garçom: abrir/gerenciar comandas, lançar pedidos e ver todas as mesas.
// Compartilha a mesma TableSession do app do cliente — a conta da mesa é unificada.
internal static class GarcomEndpoints
{
    private static readonly JsonSerializerOptions JsonOpts =
        new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    internal static IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/garcom")
            .RequireAuthorization()
            .WithTags("Garcom");

        // ── Mapa de mesas (todas), marcando as minhas ───────────────────────
        group.MapGet("/mesas", async (
            ClaimsPrincipal user, ITenantContext tenant, DineinDbContext db, OrdersDbContext ordersDb) =>
        {
            var waiterId = WaiterId(user);

            var tables = await db.Tables.AsNoTracking()
                .Where(t => t.TenantId == tenant.TenantId)
                .OrderBy(t => t.Number)
                .ToListAsync();

            var openSessions = await db.Sessions.AsNoTracking()
                .Where(s => s.TenantId == tenant.TenantId && s.Status == SessionStatus.Open)
                .ToListAsync();
            var sessionByTable = openSessions.ToDictionary(s => s.TableId);
            var sessionIds = openSessions.Select(s => s.Id).ToList();

            var pendingCallTableIds = (await db.WaiterCalls.AsNoTracking()
                .Where(w => w.TenantId == tenant.TenantId && w.Status == WaiterCallStatus.Pending)
                .Select(w => w.TableId)
                .ToListAsync()).ToHashSet();

            var orderAgg = await ordersDb.Orders.AsNoTracking()
                .Where(o => o.TableSessionId != null
                    && sessionIds.Contains(o.TableSessionId.Value)
                    && o.Status != OrderStatus.Cancelled)
                .Include(o => o.Items)
                .ToListAsync();
            var aggBySession = orderAgg
                .GroupBy(o => o.TableSessionId!.Value)
                .ToDictionary(g => g.Key, g => (Total: g.Sum(o => o.Items.Sum(i => i.Total)), Orders: g.Count()));

            var result = tables.Select(t =>
            {
                sessionByTable.TryGetValue(t.Id, out var s);
                decimal total = 0;
                int orders = 0;
                if (s is not null && aggBySession.TryGetValue(s.Id, out var agg))
                    (total, orders) = agg;

                var isMine = s?.OpenedByWaiterId is Guid ow && ow == waiterId && waiterId != Guid.Empty;

                return new GarcomTableDto(
                    t.Id, t.Number, t.Status.ToString(),
                    s?.Id, s?.OpenedAt, s?.OpenedByWaiterId, s?.OpenedByName, isMine,
                    orders, total, pendingCallTableIds.Contains(t.Id));
            });

            return Results.Ok(result);
        });

        // ── Cardápio (para lançar pedidos) ──────────────────────────────────
        group.MapGet("/cardapio", async (ITenantContext tenant, CatalogDbContext catalogDb) =>
        {
            var est = await catalogDb.Establishments
                .Include(e => e.Categories.OrderBy(c => c.Order))
                    .ThenInclude(c => c.Products)
                        .ThenInclude(p => p.Complements)
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.TenantId == tenant.TenantId);
            if (est is null) return Results.NotFound(new { error = "Estabelecimento não encontrado." });

            var categories = est.Categories
                .Select(c => new MesaCategoryDto(
                    c.Id, c.Name, c.Order,
                    c.Products.Select(p => new MesaProductDto(
                        p.Id, p.Name, p.Price, p.Description, p.PhotoUrl,
                        p.Complements.Select(cp => new MesaComplementDto(cp.Id, cp.Name, cp.AdditionalPrice)).ToList()))
                        .ToList()))
                .ToList();

            return Results.Ok(new GarcomMenuDto(est.Id, est.Name, est.IsOpen, categories));
        });

        // ── Abrir comanda (mesa livre) ──────────────────────────────────────
        group.MapPost("/mesas/{tableId:guid}/abrir", async (
            Guid tableId, ClaimsPrincipal user, ITenantContext tenant, DineinDbContext db, MesaNotifier mesaNotifier) =>
        {
            var table = await db.Tables.FirstOrDefaultAsync(t => t.Id == tableId && t.TenantId == tenant.TenantId);
            if (table is null) return Results.NotFound(new { error = "Mesa não encontrada." });

            var existing = await db.Sessions
                .FirstOrDefaultAsync(s => s.TableId == tableId && s.Status == SessionStatus.Open);
            if (existing is not null)
                return Results.Ok(new OpenComandaResponse(existing.Id, false));

            var (id, name) = Waiter(user);
            var session = TableSession.Open(table.TenantId, table.EstablishmentId, table.Id, id, name);
            db.Sessions.Add(session);
            table.Occupy();
            await db.SaveChangesAsync();

            mesaNotifier.Notify(tenant.TenantId,
                new MesaEvent("table-update", table.Id, table.Number, null, null, DateTimeOffset.UtcNow));
            return Results.Ok(new OpenComandaResponse(session.Id, true));
        });

        // ── Lançar pedido na comanda ────────────────────────────────────────
        group.MapPost("/mesas/{tableId:guid}/pedido", async (
            Guid tableId, PlaceGarcomOrderReq req, ClaimsPrincipal user, ITenantContext tenant,
            DineinDbContext db, CatalogDbContext catalogDb, OrdersDbContext ordersDb,
            IPainelDashboardNotifier painelNotifier, MesaNotifier mesaNotifier) =>
        {
            if (req.Items is null || req.Items.Count == 0)
                return Results.BadRequest(new { error = "Adicione ao menos um item ao pedido." });

            var table = await db.Tables.FirstOrDefaultAsync(t => t.Id == tableId && t.TenantId == tenant.TenantId);
            if (table is null) return Results.NotFound(new { error = "Mesa não encontrada." });

            var establishment = await catalogDb.Establishments.AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == table.EstablishmentId);
            if (establishment is null) return Results.NotFound(new { error = "Estabelecimento não encontrado." });

            var productIds = req.Items.Select(i => i.ProductId).ToList();
            var products = await catalogDb.Products.Include(p => p.Complements)
                .Where(p => productIds.Contains(p.Id)).ToListAsync();
            var missing = productIds.Except(products.Select(p => p.Id)).ToList();
            if (missing.Count > 0)
                return Results.BadRequest(new { error = "Um ou mais produtos não foram encontrados.", missingIds = missing });

            // Abre a comanda se ainda não houver (atribuída a quem lançou).
            var session = await db.Sessions
                .FirstOrDefaultAsync(s => s.TableId == table.Id && s.Status == SessionStatus.Open);
            if (session is null)
            {
                var (wid, wname) = Waiter(user);
                session = TableSession.Open(table.TenantId, table.EstablishmentId, table.Id, wid, wname);
                db.Sessions.Add(session);
                table.Occupy();
                await db.SaveChangesAsync();
            }

            var order = Order.CreateDinein(establishment.TenantId, table.EstablishmentId, session.Id, req.Note);
            foreach (var itemReq in req.Items)
            {
                var product = products.First(p => p.Id == itemReq.ProductId);
                var complementsTotal = itemReq.ComplementIds is { Count: > 0 }
                    ? product.Complements.Where(c => itemReq.ComplementIds.Contains(c.Id)).Sum(c => c.AdditionalPrice)
                    : 0m;
                order.AddItem(product.Id, product.Name, itemReq.Quantity, product.Price + complementsTotal);
            }

            ordersDb.Orders.Add(order);
            await ordersDb.SaveChangesAsync();

            painelNotifier.Notify(order.TenantId,
                new PainelOrderEvent(order.Id, "AwaitingConfirmation", DateTimeOffset.UtcNow));
            mesaNotifier.Notify(order.TenantId,
                new MesaEvent("table-update", table.Id, table.Number, null, null, DateTimeOffset.UtcNow));

            var comanda = await BuildComandaAsync(session, ordersDb);
            return Results.Ok(new GarcomOrderResponse(order.Id, session.Id, order.Total, comanda));
        });

        // ── Ver comanda de uma mesa ─────────────────────────────────────────
        group.MapGet("/mesas/{tableId:guid}/comanda", async (
            Guid tableId, ITenantContext tenant, DineinDbContext db, OrdersDbContext ordersDb) =>
        {
            var table = await db.Tables.AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == tableId && t.TenantId == tenant.TenantId);
            if (table is null) return Results.NotFound(new { error = "Mesa não encontrada." });

            var session = await db.Sessions.AsNoTracking()
                .FirstOrDefaultAsync(s => s.TableId == tableId && s.Status == SessionStatus.Open);
            var comanda = await BuildComandaAsync(session, ordersDb);

            return Results.Ok(new GarcomComandaDto(table.Id, table.Number, session?.OpenedByName, comanda));
        });

        // ── Liberar mesa (encerra comanda + chamadas) ───────────────────────
        group.MapPost("/mesas/{tableId:guid}/liberar", async (
            Guid tableId, ITenantContext tenant, DineinDbContext db, MesaNotifier mesaNotifier) =>
        {
            var table = await db.Tables.FirstOrDefaultAsync(t => t.Id == tableId && t.TenantId == tenant.TenantId);
            if (table is null) return Results.NotFound();

            var sessions = await db.Sessions
                .Where(s => s.TableId == tableId && s.Status == SessionStatus.Open).ToListAsync();
            foreach (var s in sessions) s.Close();

            var calls = await db.WaiterCalls
                .Where(w => w.TableId == tableId && w.Status == WaiterCallStatus.Pending).ToListAsync();
            foreach (var c in calls) c.Acknowledge();

            table.Vacate();
            await db.SaveChangesAsync();

            mesaNotifier.Notify(tenant.TenantId,
                new MesaEvent("table-update", table.Id, table.Number, null, null, DateTimeOffset.UtcNow));
            return Results.NoContent();
        });

        // ── Chamadas de garçom pendentes ────────────────────────────────────
        group.MapGet("/chamadas", async (ITenantContext tenant, DineinDbContext db) =>
        {
            var calls = await db.WaiterCalls.AsNoTracking()
                .Where(w => w.TenantId == tenant.TenantId && w.Status == WaiterCallStatus.Pending)
                .OrderBy(w => w.CreatedAt)
                .Select(w => new GarcomChamadaDto(w.Id, w.TableId, w.TableNumber, w.Reason, w.CreatedAt))
                .ToListAsync();
            return Results.Ok(calls);
        });

        group.MapPost("/chamadas/{id:guid}/atender", async (
            Guid id, ITenantContext tenant, DineinDbContext db, MesaNotifier mesaNotifier) =>
        {
            var call = await db.WaiterCalls.FirstOrDefaultAsync(w => w.Id == id && w.TenantId == tenant.TenantId);
            if (call is null) return Results.NotFound();

            if (call.Status == WaiterCallStatus.Pending)
            {
                call.Acknowledge();
                await db.SaveChangesAsync();
                mesaNotifier.Notify(tenant.TenantId,
                    new MesaEvent("call-resolved", call.TableId, call.TableNumber, call.Id, null, DateTimeOffset.UtcNow));
            }
            return Results.NoContent();
        });

        // ── SSE do app do garçom ────────────────────────────────────────────
        app.MapGet("/garcom/stream", async (
            string? token, MesaNotifier notifier, IConfiguration config, HttpContext http, CancellationToken ct) =>
        {
            var tenantId = DineinAuth.ValidateTenant(token, config);
            if (tenantId is null) return Results.Unauthorized();

            http.Response.ContentType = "text/event-stream";
            http.Response.Headers.CacheControl = "no-cache";
            http.Response.Headers.Append("X-Accel-Buffering", "no");

            var reader = notifier.Subscribe(tenantId.Value);
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    var waitTask = reader.WaitToReadAsync(ct).AsTask();
                    var delay = Task.Delay(20_000, ct);
                    await Task.WhenAny(waitTask, delay);
                    if (ct.IsCancellationRequested) break;

                    if (waitTask.IsCompletedSuccessfully && reader.TryRead(out var evt))
                        await http.Response.WriteAsync($"event: mesa-update\ndata: {JsonSerializer.Serialize(evt, JsonOpts)}\n\n", ct);
                    else
                        await http.Response.WriteAsync(": heartbeat\n\n", ct);

                    await http.Response.Body.FlushAsync(ct);
                }
            }
            catch (OperationCanceledException) { }

            return Results.Empty;
        })
        .AllowAnonymous()
        .WithTags("Garcom");

        return app;
    }

    private static Guid WaiterId(ClaimsPrincipal u) => Waiter(u).Id;

    private static (Guid Id, string Name) Waiter(ClaimsPrincipal u)
    {
        var sub = u.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? u.FindFirst("sub")?.Value;
        Guid.TryParse(sub, out var id);
        var name = u.FindFirst("full_name")?.Value ?? "Garçom";
        return (id, name);
    }

    private static async Task<ComandaDto> BuildComandaAsync(TableSession? session, OrdersDbContext ordersDb)
    {
        if (session is null) return new ComandaDto(null, null, [], 0);

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

internal record GarcomTableDto(
    Guid Id, string Number, string Status,
    Guid? SessionId, DateTimeOffset? OpenedAt, Guid? OpenedByWaiterId, string? OpenedByName, bool IsMine,
    int OrderCount, decimal SessionTotal, bool HasPendingCall);

internal record GarcomMenuDto(Guid EstablishmentId, string EstablishmentName, bool IsOpen, IReadOnlyList<MesaCategoryDto> Categories);
internal record OpenComandaResponse(Guid SessionId, bool Created);
internal record PlaceGarcomOrderReq(IReadOnlyList<MesaOrderItemReq> Items, string? Note);
internal record GarcomOrderResponse(Guid OrderId, Guid SessionId, decimal OrderTotal, ComandaDto Comanda);
internal record GarcomComandaDto(Guid TableId, string TableNumber, string? OpenedByName, ComandaDto Comanda);
internal record GarcomChamadaDto(Guid Id, Guid TableId, string TableNumber, string? Reason, DateTimeOffset CreatedAt);
