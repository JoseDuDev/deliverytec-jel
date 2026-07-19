using System.Security.Cryptography;
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

internal static class DineinPainelEndpoints
{
    internal static IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/painel/mesas")
            .RequireAuthorization()
            .WithTags("Painel");

        // ── Lista de mesas com estado da comanda ────────────────────────────
        group.MapGet("/", async (
            ITenantContext tenant,
            DineinDbContext db,
            OrdersDbContext ordersDb) =>
        {
            var tables = await db.Tables
                .AsNoTracking()
                .Where(t => t.TenantId == tenant.TenantId)
                .OrderBy(t => t.Number)
                .ToListAsync();

            var openSessions = await db.Sessions
                .AsNoTracking()
                .Where(s => s.TenantId == tenant.TenantId && s.Status == SessionStatus.Open)
                .ToListAsync();

            var sessionByTable = openSessions.ToDictionary(s => s.TableId);
            var sessionIds = openSessions.Select(s => s.Id).ToList();

            var orderAgg = await ordersDb.Orders
                .AsNoTracking()
                .Where(o => o.TableSessionId != null
                    && sessionIds.Contains(o.TableSessionId.Value)
                    && o.Status != OrderStatus.Cancelled)
                .Include(o => o.Items)
                .ToListAsync();

            var aggBySession = orderAgg
                .GroupBy(o => o.TableSessionId!.Value)
                .ToDictionary(
                    g => g.Key,
                    g => (Total: g.Sum(o => o.Items.Sum(i => i.Total)), Orders: g.Count()));

            var result = tables.Select(t =>
            {
                sessionByTable.TryGetValue(t.Id, out var session);
                decimal total = 0;
                int orders = 0;
                if (session is not null && aggBySession.TryGetValue(session.Id, out var agg))
                    (total, orders) = agg;

                return new TableResponse(
                    t.Id, t.Number, t.QrToken, t.Status.ToString(),
                    session?.Id, session?.OpenedAt, orders, total);
            });

            return Results.Ok(result);
        });

        // ── Criar mesa ──────────────────────────────────────────────────────
        group.MapPost("/", async (
            ITenantContext tenant,
            DineinDbContext db,
            CatalogDbContext catalogDb,
            CreateTableReq req) =>
        {
            var establishmentId = await catalogDb.Establishments
                .AsNoTracking()
                .Where(e => e.TenantId == tenant.TenantId)
                .Select(e => e.Id)
                .FirstOrDefaultAsync();
            if (establishmentId == Guid.Empty)
                return Results.NotFound(new { error = "Estabelecimento não encontrado." });

            var exists = await db.Tables
                .AnyAsync(t => t.TenantId == tenant.TenantId && t.Number == req.Number.Trim());
            if (exists)
                return Results.Conflict(new { error = "Já existe uma mesa com esse número." });

            var table = Table.Create(tenant.TenantId, establishmentId, req.Number, GenerateToken());
            db.Tables.Add(table);
            await db.SaveChangesAsync();

            return Results.Created($"/painel/mesas/{table.Id}",
                new TableResponse(table.Id, table.Number, table.QrToken, table.Status.ToString(), null, null, 0, 0));
        });

        // ── Renomear mesa ───────────────────────────────────────────────────
        group.MapPatch("/{id:guid}", async (
            Guid id, ITenantContext tenant, DineinDbContext db, RenameTableReq req) =>
        {
            var table = await db.Tables.FirstOrDefaultAsync(t => t.Id == id && t.TenantId == tenant.TenantId);
            if (table is null) return Results.NotFound();

            var clash = await db.Tables.AnyAsync(t =>
                t.TenantId == tenant.TenantId && t.Number == req.Number.Trim() && t.Id != id);
            if (clash) return Results.Conflict(new { error = "Já existe uma mesa com esse número." });

            table.Rename(req.Number);
            await db.SaveChangesAsync();
            return Results.Ok(new TableResponse(table.Id, table.Number, table.QrToken, table.Status.ToString(), null, null, 0, 0));
        });

        // ── Regenerar QR token ──────────────────────────────────────────────
        group.MapPost("/{id:guid}/qr", async (
            Guid id, ITenantContext tenant, DineinDbContext db) =>
        {
            var table = await db.Tables.FirstOrDefaultAsync(t => t.Id == id && t.TenantId == tenant.TenantId);
            if (table is null) return Results.NotFound();

            table.RegenerateToken(GenerateToken());
            await db.SaveChangesAsync();
            return Results.Ok(new { table.Id, table.QrToken });
        });

        // ── Liberar mesa (fecha a comanda sem pagamento — override do staff) ─
        group.MapPost("/{id:guid}/liberar", async (
            Guid id, ITenantContext tenant, DineinDbContext db) =>
        {
            var table = await db.Tables.FirstOrDefaultAsync(t => t.Id == id && t.TenantId == tenant.TenantId);
            if (table is null) return Results.NotFound();

            var sessions = await db.Sessions
                .Where(s => s.TableId == id && s.Status == SessionStatus.Open)
                .ToListAsync();
            foreach (var s in sessions) s.Close();

            table.Vacate();
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        // ── Excluir mesa ────────────────────────────────────────────────────
        group.MapDelete("/{id:guid}", async (
            Guid id, ITenantContext tenant, DineinDbContext db) =>
        {
            var table = await db.Tables.FirstOrDefaultAsync(t => t.Id == id && t.TenantId == tenant.TenantId);
            if (table is null) return Results.NotFound();

            var hasOpen = await db.Sessions.AnyAsync(s => s.TableId == id && s.Status == SessionStatus.Open);
            if (hasOpen)
                return Results.Conflict(new { error = "Libere a mesa antes de excluí-la." });

            db.Tables.Remove(table);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        return app;
    }

    private static string GenerateToken() =>
        Convert.ToHexString(RandomNumberGenerator.GetBytes(8)).ToLowerInvariant();

    private record CreateTableReq(string Number);
    private record RenameTableReq(string Number);
}

internal record TableResponse(
    Guid Id,
    string Number,
    string QrToken,
    string Status,
    Guid? SessionId,
    DateTimeOffset? OpenedAt,
    int OrderCount,
    decimal SessionTotal);
