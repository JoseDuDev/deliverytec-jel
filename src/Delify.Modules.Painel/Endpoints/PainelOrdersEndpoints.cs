using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Delify.Modules.Orders.Domain;
using Delify.Modules.Orders.Infrastructure;
using Delify.Modules.Painel.Services;
using Delify.Shared.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Delify.Modules.Painel.Endpoints;

internal static class PainelOrdersEndpoints
{
    private static readonly JsonSerializerOptions JsonOpts =
        new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    internal static IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/painel/pedidos")
            .RequireAuthorization()
            .WithTags("Painel");

        // ── Lista de pedidos ──────────────────────────────────────────────
        group.MapGet("/", async (
            ITenantContext tenant,
            OrdersDbContext db,
            string? status) =>
        {
            var today = new DateTimeOffset(DateTime.UtcNow.Date, TimeSpan.Zero);

            var query = db.Orders
                .AsNoTracking()
                .Include(o => o.Items)
                .Where(o => o.TenantId == tenant.TenantId);

            query = status switch
            {
                "today" => query.Where(o => o.CreatedAt >= today),
                _ => query.Where(o =>
                    o.Status == OrderStatus.AwaitingConfirmation ||
                    o.Status == OrderStatus.InPreparation ||
                    o.Status == OrderStatus.InDelivery)
            };

            var orders = await query
                .OrderBy(o => o.CreatedAt)
                .ToListAsync();

            return Results.Ok(orders.Select(o => MapOrder(o)));
        });

        // ── SSE stream ────────────────────────────────────────────────────
        app.MapGet("/painel/pedidos/stream", async (
            string? token,
            PainelDashboardNotifier notifier,
            IConfiguration config,
            HttpContext http,
            CancellationToken ct) =>
        {
            var tenantId = ValidateToken(token, config);
            if (tenantId is null) return Results.Unauthorized();

            http.Response.ContentType = "text/event-stream";
            http.Response.Headers.CacheControl = "no-cache";
            http.Response.Headers.Append("X-Accel-Buffering", "no");

            var reader = notifier.Subscribe(tenantId.Value);

            try
            {
                // Heartbeat loop — keeps connection alive and drains events
                while (!ct.IsCancellationRequested)
                {
                    var waitTask = reader.WaitToReadAsync(ct).AsTask();
                    var delay = Task.Delay(20_000, ct);

                    await Task.WhenAny(waitTask, delay);

                    if (ct.IsCancellationRequested) break;

                    if (waitTask.IsCompletedSuccessfully && reader.TryRead(out var evt))
                    {
                        var json = JsonSerializer.Serialize(evt, JsonOpts);
                        await http.Response.WriteAsync($"event: order-update\ndata: {json}\n\n", ct);
                    }
                    else
                    {
                        await http.Response.WriteAsync(": heartbeat\n\n", ct);
                    }

                    await http.Response.Body.FlushAsync(ct);
                }
            }
            catch (OperationCanceledException) { }

            return Results.Empty;
        })
        .AllowAnonymous()
        .WithTags("Painel");

        // ── Ações de status ───────────────────────────────────────────────
        group.MapPatch("/{id:guid}/accept", async (
            Guid id, ITenantContext tenant, OrdersDbContext db,
            IOrderTrackingNotifier trackingNotifier, IPainelDashboardNotifier painelNotifier) =>
            await UpdateStatus(id, tenant.TenantId, OrderAction.Accept, db, trackingNotifier, painelNotifier));

        group.MapPatch("/{id:guid}/start-delivery", async (
            Guid id, ITenantContext tenant, OrdersDbContext db,
            IOrderTrackingNotifier trackingNotifier, IPainelDashboardNotifier painelNotifier) =>
            await UpdateStatus(id, tenant.TenantId, OrderAction.StartDelivery, db, trackingNotifier, painelNotifier));

        group.MapPatch("/{id:guid}/complete", async (
            Guid id, ITenantContext tenant, OrdersDbContext db,
            IOrderTrackingNotifier trackingNotifier, IPainelDashboardNotifier painelNotifier) =>
            await UpdateStatus(id, tenant.TenantId, OrderAction.Complete, db, trackingNotifier, painelNotifier));

        group.MapPatch("/{id:guid}/cancel", async (
            Guid id, ITenantContext tenant, OrdersDbContext db,
            IOrderTrackingNotifier trackingNotifier, IPainelDashboardNotifier painelNotifier) =>
            await UpdateStatus(id, tenant.TenantId, OrderAction.Cancel, db, trackingNotifier, painelNotifier));

        return app;
    }

    private static async Task<IResult> UpdateStatus(
        Guid orderId, Guid tenantId, OrderAction action,
        OrdersDbContext db,
        IOrderTrackingNotifier trackingNotifier,
        IPainelDashboardNotifier painelNotifier)
    {
        var order = await db.Orders.Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.TenantId == tenantId);

        if (order is null) return Results.NotFound();

        try
        {
            switch (action)
            {
                case OrderAction.Accept:        order.Accept();        break;
                case OrderAction.StartDelivery: order.StartDelivery(); break;
                case OrderAction.Complete:      order.Complete();      break;
                case OrderAction.Cancel:        order.Cancel();        break;
            }
        }
        catch (InvalidOperationException ex)
        {
            return Results.Conflict(new { error = ex.Message });
        }

        await db.SaveChangesAsync();

        var (statusKey, label) = order.Status switch
        {
            OrderStatus.InPreparation => ("Preparing",      "Preparando seu pedido"),
            OrderStatus.InDelivery    => ("OutForDelivery", "Saiu para entrega"),
            OrderStatus.Delivered     => ("Delivered",      "Pedido entregue!"),
            OrderStatus.Cancelled     => ("Cancelled",      "Pedido cancelado"),
            _                         => (order.Status.ToString(), order.Status.ToString())
        };

        trackingNotifier.Notify(orderId, statusKey, label);
        painelNotifier.Notify(tenantId, new PainelOrderEvent(orderId, order.Status.ToString(), DateTimeOffset.UtcNow));

        return Results.Ok(MapOrder(order));
    }

    private static OrderResponse MapOrder(Order o) =>
        new(o.Id, o.Status.ToString(), o.Total, o.CreatedAt, o.CustomerNote,
            o.Items.Select(i => new OrderItemResponse(i.ProductName, i.Quantity, i.UnitPrice)));

    private static Guid? ValidateToken(string? token, IConfiguration config)
    {
        if (string.IsNullOrWhiteSpace(token)) return null;

        var jwtKey = config["Jwt:Key"];
        if (jwtKey is null) return null;

        var handler = new JwtSecurityTokenHandler();
        try
        {
            var principal = handler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                ValidateIssuer = false,
                ValidateAudience = false,
                ClockSkew = TimeSpan.Zero
            }, out _);

            var tenantIdClaim = principal.FindFirst("tenant_id")?.Value;
            return Guid.TryParse(tenantIdClaim, out var id) ? id : null;
        }
        catch { return null; }
    }
}

internal record OrderItemResponse(string ProductName, int Quantity, decimal UnitPrice);
internal record OrderResponse(
    Guid Id, string Status, decimal Total, DateTimeOffset CreatedAt,
    string? CustomerNote, IEnumerable<OrderItemResponse> Items);

internal enum OrderAction { Accept, StartDelivery, Complete, Cancel }
