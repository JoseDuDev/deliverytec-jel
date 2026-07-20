using Delify.Modules.Payments.Application;
using Delify.Modules.Payments.Domain;
using Delify.Modules.Payments.Infrastructure;
using Delify.Shared.IntegrationEvents;
using MassTransit;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace Delify.Modules.Bff.Endpoints;

/// <summary>
/// Endpoints disponíveis apenas em ambiente de desenvolvimento.
/// Permitem simular eventos externos (ex: confirmação de pagamento) sem ngrok.
/// </summary>
internal static class DevEndpoints
{
    internal static IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/bff/dev/simulate-payment/{orderId:guid}", async (
            Guid orderId,
            PaymentsDbContext db,
            IBus bus,
            IHostEnvironment env) =>
        {
            if (!env.IsDevelopment())
                return Results.NotFound();

            var payment = await db.Payments
                .FirstOrDefaultAsync(p => p.OrderId == orderId);

            if (payment is null)
                return Results.NotFound(new { error = "Nenhum pagamento encontrado para este pedido." });

            if (payment.Status == PaymentStatus.Confirmed)
                return Results.Ok(new { message = "Pagamento já confirmado.", orderId });

            payment.ConfirmPayment(payment.GatewayPaymentId ?? $"simulated_{orderId}");
            await db.SaveChangesAsync();

            await bus.Publish(new PaymentConfirmedIntegrationEvent(orderId, payment.TenantId));

            return Results.Ok(new { message = "Pagamento simulado com sucesso.", orderId, status = "Confirmed" });
        })
        .AllowAnonymous()
        .WithTags("Dev");

        // Simula a confirmação do PIX da comanda de uma mesa.
        app.MapPost("/bff/dev/simulate-payment-session/{sessionId:guid}", async (
            Guid sessionId,
            PaymentsDbContext db,
            IBus bus,
            IHostEnvironment env) =>
        {
            if (!env.IsDevelopment())
                return Results.NotFound();

            var payment = await db.Payments
                .Where(p => p.TableSessionId == sessionId
                         && p.ShareIndex == null
                         && p.Status != PaymentStatus.Failed)
                .OrderByDescending(p => p.CreatedAt)
                .FirstOrDefaultAsync();

            if (payment is null)
                return Results.NotFound(new { error = "Nenhum pagamento encontrado para esta comanda. Se ela foi dividida, use simulate-payment-share." });

            if (payment.Status != PaymentStatus.Confirmed)
            {
                payment.ConfirmPayment(payment.GatewayPaymentId ?? $"simulated_{sessionId}");
                await db.SaveChangesAsync();
                await bus.Publish(new SessionPaidIntegrationEvent(sessionId, payment.TenantId));
            }

            return Results.Ok(new { message = "Pagamento da comanda simulado.", sessionId, status = "Confirmed" });
        })
        .AllowAnonymous()
        .WithTags("Dev");

        // Simula o pagamento de UMA parte de uma comanda dividida. A comanda só é
        // dada como paga quando a última parte cair — mesma regra do webhook real.
        app.MapPost("/bff/dev/simulate-payment-share/{sessionId:guid}/{index:int}", async (
            Guid sessionId,
            int index,
            PaymentsDbContext db,
            IBus bus,
            IHostEnvironment env) =>
        {
            if (!env.IsDevelopment())
                return Results.NotFound();

            var share = await db.Payments.FirstOrDefaultAsync(p =>
                p.TableSessionId == sessionId && p.ShareIndex == index && p.Status != PaymentStatus.Failed);

            if (share is null)
                return Results.NotFound(new { error = $"Parte {index} não encontrada nesta comanda." });

            if (share.Status != PaymentStatus.Confirmed)
            {
                share.ConfirmPayment(share.GatewayPaymentId ?? $"simulated_{share.Id}");
                await db.SaveChangesAsync();

                if (await SessionSettlement.IsFullySettledAsync(db, sessionId))
                    await bus.Publish(new SessionPaidIntegrationEvent(sessionId, share.TenantId));
            }

            var settled = await SessionSettlement.IsFullySettledAsync(db, sessionId);
            return Results.Ok(new
            {
                message = $"Parte {index} paga.",
                sessionId,
                index,
                amount = share.Amount,
                sessionSettled = settled
            });
        })
        .AllowAnonymous()
        .WithTags("Dev");

        return app;
    }
}
