// src/Delify.Modules.Bff/Endpoints/TrackingEndpoints.cs
using System.Text.Json;
using Delify.Modules.Bff.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Delify.Modules.Bff.Endpoints;

internal static class TrackingEndpoints
{
    internal static IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/bff/orders/{orderId:guid}/track", async (
            Guid orderId,
            OrderTrackingService tracker,
            HttpResponse response,
            CancellationToken ct) =>
        {
            response.ContentType = "text/event-stream";
            response.Headers.CacheControl = "no-cache";
            response.Headers.Connection = "keep-alive";

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
