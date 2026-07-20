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

        // Página de checkout falsa: em dev o app redireciona pra cá exatamente como
        // redirecionaria pra página do Asaas, e é aqui que se escolhe aprovar ou
        // recusar o cartão — inclusive o caminho da recusa, que é o que devolve a
        // parte pro estado "a pagar".
        app.MapGet("/bff/dev/checkout/{paymentId:guid}", async (
            Guid paymentId, PaymentsDbContext db, IHostEnvironment env) =>
        {
            if (!env.IsDevelopment()) return Results.NotFound();

            var payment = await db.Payments.AsNoTracking().FirstOrDefaultAsync(p => p.Id == paymentId);
            if (payment is null) return Results.NotFound();

            var valor = payment.Amount.ToString("N2", new System.Globalization.CultureInfo("pt-BR"));
            var pago = payment.Status == PaymentStatus.Confirmed;

            var html = $$"""
            <!doctype html><html lang="pt-BR"><head><meta charset="utf-8">
            <meta name="viewport" content="width=device-width,initial-scale=1">
            <title>Checkout (simulado)</title>
            <style>
              body{font-family:system-ui,sans-serif;background:#f4f4f5;margin:0;
                   display:flex;align-items:center;justify-content:center;min-height:100vh}
              .card{background:#fff;border-radius:16px;padding:28px;max-width:360px;width:90%;
                    box-shadow:0 8px 30px rgba(0,0,0,.08);text-align:center}
              .tag{display:inline-block;background:#fef3c7;color:#92400e;font-size:12px;
                   font-weight:700;padding:4px 10px;border-radius:999px;margin-bottom:16px}
              .valor{font-size:32px;font-weight:800;margin:8px 0 4px}
              .sub{color:#71717a;font-size:14px;margin-bottom:22px}
              button{width:100%;border:0;border-radius:999px;padding:13px;font-size:15px;
                     font-weight:700;cursor:pointer;margin-bottom:10px}
              .ok{background:#16a34a;color:#fff}.no{background:#fff;color:#dc2626;border:1px solid #fecaca}
              .done{color:#16a34a;font-weight:700;font-size:18px}
            </style></head><body><div class="card">
              <div class="tag">⚡ CHECKOUT SIMULADO (DEV)</div>
              <div class="valor">R$ {{valor}}</div>
              <div class="sub">Página que em produção seria hospedada pelo Asaas</div>
              {{(pago
                ? "<p class=\"done\">✅ Pagamento já confirmado</p>"
                : $$"""
                  <button class="ok" onclick="go('aprovar')">Pagar com cartão</button>
                  <button class="no" onclick="go('recusar')">Simular recusa do cartão</button>
                  <script>
                    async function go(acao){
                      await fetch('/bff/dev/checkout/{{paymentId}}/'+acao,{method:'POST'});
                      document.querySelector('.card').innerHTML =
                        acao==='aprovar'
                          ? '<p class="done">✅ Pagamento aprovado</p><p class="sub">Pode voltar para a mesa.</p>'
                          : '<p style="color:#dc2626;font-weight:700">❌ Cartão recusado</p><p class="sub">A parte volta a ficar em aberto.</p>';
                    }
                  </script>
                  """)}}
            </div></body></html>
            """;

            return Results.Content(html, "text/html");
        })
        .AllowAnonymous()
        .WithTags("Dev");

        // Desfecho do checkout simulado: aprovar confirma (e liquida a comanda se
        // for a última parte); recusar devolve a cobrança pro estado "a pagar".
        app.MapPost("/bff/dev/checkout/{paymentId:guid}/{acao}", async (
            Guid paymentId, string acao,
            PaymentsDbContext db, IBus bus, IHostEnvironment env) =>
        {
            if (!env.IsDevelopment()) return Results.NotFound();

            var payment = await db.Payments.FirstOrDefaultAsync(p => p.Id == paymentId);
            if (payment is null) return Results.NotFound();

            if (acao == "recusar")
            {
                if (payment.Status != PaymentStatus.Confirmed)
                {
                    payment.MarkRetryable();
                    await db.SaveChangesAsync();
                }
                return Results.Ok(new { message = "Cartão recusado (simulado).", retryable = true });
            }

            if (payment.Status != PaymentStatus.Confirmed)
            {
                payment.ConfirmPayment(payment.GatewayPaymentId ?? $"simulated_{payment.Id}");
                await db.SaveChangesAsync();

                if (payment.TableSessionId is Guid sid)
                {
                    if (await SessionSettlement.IsFullySettledAsync(db, sid))
                        await bus.Publish(new SessionPaidIntegrationEvent(sid, payment.TenantId));
                }
                else if (payment.OrderId is Guid oid)
                {
                    await bus.Publish(new PaymentConfirmedIntegrationEvent(oid, payment.TenantId));
                }
            }

            return Results.Ok(new { message = "Pagamento aprovado (simulado).", paymentId });
        })
        .AllowAnonymous()
        .WithTags("Dev");

        return app;
    }
}
