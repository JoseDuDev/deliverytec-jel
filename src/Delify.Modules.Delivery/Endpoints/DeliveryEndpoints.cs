using Delify.Modules.Delivery.Abstractions;
using Delify.Modules.Delivery.Domain;
using Delify.Modules.Delivery.Infrastructure;
using Delify.Shared.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Delify.Modules.Delivery.Endpoints;

internal static class DeliveryEndpoints
{
    private record QuoteRequest(Guid OrderId, Address Pickup, Address Dropoff, string ContactPhone);
    private record DispatchRequest(Guid OrderId, string Provider, Address Pickup, Address Dropoff,
        string ContactPhone, decimal QuotedPrice);

    internal static IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/delivery").WithTags("Delivery").RequireAuthorization();

        group.MapPost("/quote", async (
            QuoteRequest req,
            IEnumerable<IDeliveryProvider> providers) =>
        {
            var quotesTasks = providers.Select(p => p.GetQuoteAsync(req.Pickup, req.Dropoff));
            var quotes = await Task.WhenAll(quotesTasks);
            return Results.Ok(quotes.OrderBy(q => q.Price));
        })
        .WithName("QuoteDelivery");

        group.MapPost("/dispatch", async (
            DispatchRequest req,
            IEnumerable<IDeliveryProvider> providers,
            DeliveryDbContext db,
            ITenantContext tenant) =>
        {
            var provider = providers.FirstOrDefault(p => p.ProviderName == req.Provider);
            if (provider is null)
                return Results.BadRequest($"Provider '{req.Provider}' not available.");

            var dispatchResult = await provider.DispatchAsync(req.Pickup, req.Dropoff, req.ContactPhone);

            var deliveryOrder = DeliveryOrder.Create(
                tenant.TenantId, req.OrderId, req.Provider,
                $"{req.Pickup.Street}, {req.Pickup.Number}",
                $"{req.Dropoff.Street}, {req.Dropoff.Number}",
                req.QuotedPrice);

            deliveryOrder.Dispatch(dispatchResult.ProviderOrderId, dispatchResult.TrackingUrl);
            db.DeliveryOrders.Add(deliveryOrder);
            await db.SaveChangesAsync();

            return Results.Ok(new { dispatchResult.ProviderOrderId, dispatchResult.TrackingUrl });
        })
        .WithName("DispatchDelivery");

        return app;
    }
}
