using Delify.Modules.Payments.Abstractions;
using Delify.Modules.Payments.Domain;
using Delify.Modules.Payments.Infrastructure;
using Delify.Shared.IntegrationEvents;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Delify.Modules.Payments.Application.Consumers;

public sealed class OrderCreatedConsumer(
    IPaymentGateway gateway,
    PaymentsDbContext db) : IConsumer<OrderCreatedIntegrationEvent>
{
    public async Task Consume(ConsumeContext<OrderCreatedIntegrationEvent> context)
    {
        var msg = context.Message;

        var exists = await db.Payments.AnyAsync(
            p => p.OrderId == msg.OrderId, context.CancellationToken);

        if (exists) return;

        var pixResult = await gateway.CreatePixAsync(
            new PixPaymentRequest(msg.OrderId, msg.Total, msg.CustomerCpf, msg.CustomerName),
            context.CancellationToken);

        var payment = Payment.CreatePix(msg.TenantId, msg.OrderId, msg.Total);
        payment.SetPixData(pixResult.GatewayId, pixResult.QrCode, pixResult.CopyPaste);
        db.Payments.Add(payment);
        await db.SaveChangesAsync(context.CancellationToken);
    }
}
