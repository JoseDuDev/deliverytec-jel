using System.Text.Json;
using Delify.Modules.Bff.Services;
using Delify.Modules.Orders.Domain;
using Delify.Modules.Orders.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Delify.Modules.Bff.Endpoints;

internal static class TrackingEndpoints
{
    private static readonly Dictionary<OrderStatus, (string Key, string Label)> StatusMap = new()
    {
        [OrderStatus.PendingPayment]      = ("Pending",        "Aguardando pagamento"),
        [OrderStatus.AwaitingConfirmation]= ("Confirmed",      "Pagamento confirmado"),
        [OrderStatus.InPreparation]       = ("Preparing",      "Preparando seu pedido"),
        [OrderStatus.InDelivery]          = ("OutForDelivery", "Saiu para entrega"),
        [OrderStatus.Delivered]           = ("Delivered",      "Pedido entregue!"),
        [OrderStatus.Cancelled]           = ("Cancelled",      "Pedido cancelado"),
    };

    internal static IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        // Current status — REST fetch for initial state on page load
        app.MapGet("/bff/orders/{orderId:guid}/status", async (
            Guid orderId,
            OrdersDbContext db,
            CancellationToken ct) =>
        {
            var order = await db.Orders
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.Id == orderId, ct);

            if (order is null) return Results.NotFound();

            var (key, label) = StatusMap.GetValueOrDefault(order.Status, (order.Status.ToString(), order.Status.ToString()));

            return Results.Ok(new { status = key, label, at = order.UpdatedAt ?? order.CreatedAt });
        })
        .WithName("BffOrderStatus")
        .WithTags("BFF")
        .AllowAnonymous();

        app.MapGet("/bff/orders/{orderId:guid}/track", async (
            Guid orderId,
            OrderTrackingService tracker,
            HttpResponse response,
            CancellationToken ct) =>
        {
            response.ContentType = "text/event-stream";
            response.Headers.CacheControl = "no-cache";
            response.Headers.Connection = "keep-alive";
            await response.Body.FlushAsync(ct);

            var reader = tracker.Subscribe(orderId);

            await foreach (var evt in reader.ReadAllAsync(ct))
            {
                var json = JsonSerializer.Serialize(evt, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                await response.WriteAsync($"event: status-changed\ndata: {json}\n\n", ct);
                await response.Body.FlushAsync(ct);

                if (evt.Status is "Delivered" or "Cancelled")
                    break;
            }
        })
        .WithName("BffTrackOrder")
        .WithTags("BFF")
        .AllowAnonymous();

        return app;
    }
}
