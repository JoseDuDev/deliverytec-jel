using Delify.Modules.Bff.Models;
using Delify.Modules.Catalog.Infrastructure;
using Delify.Modules.Orders.Domain;
using Delify.Modules.Orders.Infrastructure;
using Delify.Modules.Payments.Abstractions;
using Delify.Modules.Payments.Domain;
using Delify.Modules.Payments.Infrastructure;
using Delify.Shared.Abstractions;
using Delify.Shared.IntegrationEvents;
using MassTransit;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Delify.Modules.Bff.Endpoints;

internal static class OrderEndpoints
{
    internal static IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/bff/orders", async (
            PlaceOrderRequest req,
            CatalogDbContext catalogDb,
            OrdersDbContext ordersDb,
            PaymentsDbContext paymentsDb,
            IPaymentGateway gateway,
            IOrderTrackingNotifier trackingNotifier,
            IBus bus) =>
        {
            // 1. Buscar o Establishment
            var establishment = await catalogDb.Establishments
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == req.EstablishmentId);

            if (establishment is null)
                return Results.NotFound(new { error = "Establishment not found." });

            // 2. Buscar os Products dos itens do pedido
            var productIds = req.Items.Select(i => i.ProductId).ToList();
            var products = await catalogDb.Products
                .Include(p => p.Complements)
                .Where(p => productIds.Contains(p.Id))
                .ToListAsync();

            var missingProducts = productIds.Except(products.Select(p => p.Id)).ToList();
            if (missingProducts.Count > 0)
                return Results.BadRequest(new { error = "One or more products not found.", missingIds = missingProducts });

            // 3. Criar o Order
            // CustomerNote has internal set — only settable from within the Orders module assembly.
            // The note is passed via OrderCreatedIntegrationEvent so the Orders handler can apply it.
            var order = Order.Create(establishment.TenantId, req.EstablishmentId);

            foreach (var itemReq in req.Items)
            {
                var product = products.First(p => p.Id == itemReq.ProductId);

                var complementsTotal = itemReq.ComplementIds is { Count: > 0 }
                    ? product.Complements
                        .Where(c => itemReq.ComplementIds.Contains(c.Id))
                        .Sum(c => c.AdditionalPrice)
                    : 0m;

                var unitPrice = product.Price + complementsTotal;
                order.AddItem(product.Id, product.Name, itemReq.Quantity, unitPrice);
            }

            // 4. Salvar o pedido
            ordersDb.Orders.Add(order);
            await ordersDb.SaveChangesAsync();

            // 5. Publicar OrderCreatedIntegrationEvent
            await bus.Publish(new OrderCreatedIntegrationEvent(
                order.Id,
                order.TenantId,
                order.Total,
                req.Customer.Cpf,
                req.Customer.Name));

            // 6. Criar PIX via gateway
            var pixRequest = new PixPaymentRequest(order.Id, order.Total, req.Customer.Cpf, req.Customer.Name);
            var pixResult = await gateway.CreatePixAsync(pixRequest);

            // 7. Criar e salvar Payment
            var payment = Payment.CreatePix(order.TenantId, order.Id, order.Total);
            payment.SetPixData(pixResult.GatewayId, pixResult.QrCode, pixResult.CopyPaste);
            paymentsDb.Payments.Add(payment);
            await paymentsDb.SaveChangesAsync();

            // 8. Notificar tracking
            trackingNotifier.Notify(order.Id, "Pending", "Aguardando pagamento");

            // 9. Retornar resposta
            var response = new PlaceOrderResponse(
                order.Id,
                order.Total,
                new PixResponse(pixResult.QrCode, pixResult.CopyPaste, pixResult.ExpiresAt));

            return Results.Ok(response);
        })
        .WithName("BffPlaceOrder")
        .WithTags("BFF")
        .RequireAuthorization();

        return app;
    }
}
