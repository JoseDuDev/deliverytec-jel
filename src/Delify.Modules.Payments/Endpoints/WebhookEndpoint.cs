using Delify.Modules.Payments.Abstractions;
using Delify.Modules.Payments.Infrastructure;
using Delify.Shared.IntegrationEvents;
using MassTransit;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Delify.Modules.Payments.Endpoints;

internal static class WebhookEndpoint
{
    internal static IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/webhooks/asaas", async (
            HttpRequest req,
            IPaymentGateway gateway,
            PaymentsDbContext db,
            IBus bus) =>
        {
            var body = await new StreamReader(req.Body).ReadToEndAsync();
            var signature = req.Headers["asaas-access-token"].ToString();

            var result = await gateway.ProcessWebhookAsync(body, signature);

            if (!result.IsConfirmed) return Results.Ok();

            var payment = await db.Payments
                .FirstOrDefaultAsync(p => p.GatewayPaymentId == result.GatewayPaymentId);

            if (payment is not null)
            {
                payment.ConfirmPayment(result.GatewayPaymentId);
                await db.SaveChangesAsync();
                await bus.Publish(new PaymentConfirmedIntegrationEvent(payment.OrderId, payment.TenantId));
            }

            return Results.Ok();
        })
        .WithName("AsaasWebhook")
        .WithTags("Webhooks")
        .AllowAnonymous();

        return app;
    }
}
