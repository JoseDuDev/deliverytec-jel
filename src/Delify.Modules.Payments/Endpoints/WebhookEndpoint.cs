using Delify.Modules.Payments.Abstractions;
using Delify.Modules.Payments.Application;
using Delify.Modules.Payments.Domain;
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

            if (result.Outcome == WebhookOutcome.Ignored) return Results.Ok();

            // No checkout hospedado a cobrança é criada só quando o pagador conclui,
            // então o payment.id do callback é desconhecido até aqui — quem resolve
            // a correlação é o externalReference, que carrega o nosso Payment.Id.
            Payment? payment = null;

            if (!string.IsNullOrEmpty(result.GatewayPaymentId))
                payment = await db.Payments
                    .FirstOrDefaultAsync(p => p.GatewayPaymentId == result.GatewayPaymentId);

            if (payment is null && Guid.TryParse(result.ExternalReference, out var ourId))
                payment = await db.Payments.FirstOrDefaultAsync(p => p.Id == ourId);

            if (payment is null) return Results.Ok();

            if (result.Outcome == WebhookOutcome.Refused)
            {
                // Cartão recusado/estornado: a parte volta a ser devida para nova tentativa.
                if (payment.Status != PaymentStatus.Confirmed)
                {
                    payment.MarkRetryable();
                    await db.SaveChangesAsync();
                }
                return Results.Ok();
            }

            // O gateway reenvia callbacks. Confirmar de novo republicaria o evento
            // e fecharia uma comanda que talvez já tenha sido reaberta.
            if (payment.Status == PaymentStatus.Confirmed) return Results.Ok();

            payment.ConfirmPayment(
                string.IsNullOrEmpty(result.GatewayPaymentId)
                    ? payment.GatewayPaymentId ?? payment.Id.ToString()
                    : result.GatewayPaymentId);
            await db.SaveChangesAsync();

            if (payment.TableSessionId is Guid sessionId)
            {
                // Conta dividida: só avisa quando a última parte quitar, senão a
                // mesa seria liberada com gente ainda devendo.
                if (await SessionSettlement.IsFullySettledAsync(db, sessionId))
                    await bus.Publish(new SessionPaidIntegrationEvent(sessionId, payment.TenantId));
            }
            else if (payment.OrderId is Guid orderId)
            {
                await bus.Publish(new PaymentConfirmedIntegrationEvent(orderId, payment.TenantId));
            }

            return Results.Ok();
        })
        .WithName("AsaasWebhook")
        .WithTags("Webhooks")
        .AllowAnonymous();

        return app;
    }
}
