using Delify.Modules.Payments.Abstractions;
using Delify.Modules.Payments.Domain;
using Delify.Modules.Payments.Infrastructure;
using Delify.Shared.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Delify.Modules.Payments.Endpoints;

internal static class CheckoutEndpoint
{
    private record PixCheckoutRequest(Guid OrderId, decimal Amount, string CustomerCpf, string CustomerName);

    internal static IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/payments/pix", async (
            PixCheckoutRequest req,
            IPaymentGateway gateway,
            PaymentsDbContext db,
            ITenantContext tenant) =>
        {
            var pixResult = await gateway.CreatePixAsync(
                new PixPaymentRequest(req.OrderId, req.Amount, req.CustomerCpf, req.CustomerName));

            var payment = Payment.CreatePix(tenant.TenantId, req.OrderId, req.Amount);
            payment.SetPixData(pixResult.GatewayId, pixResult.QrCode, pixResult.CopyPaste);
            db.Payments.Add(payment);
            await db.SaveChangesAsync();

            return Results.Ok(new { pixResult.QrCode, pixResult.CopyPaste, pixResult.ExpiresAt });
        })
        .WithName("CreatePixPayment")
        .WithTags("Payments")
        .RequireAuthorization();

        return app;
    }
}
