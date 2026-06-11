using Delify.Modules.Orders.Infrastructure;
using Delify.Shared.Abstractions;
using Delify.Shared.IntegrationEvents;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Delify.Modules.Orders.Application.Consumers;

public sealed class PaymentConfirmedConsumer(
    OrdersDbContext db,
    IOrderTrackingNotifier trackingNotifier) : IConsumer<PaymentConfirmedIntegrationEvent>
{
    public async Task Consume(ConsumeContext<PaymentConfirmedIntegrationEvent> context)
    {
        var order = await db.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == context.Message.OrderId, context.CancellationToken);

        if (order is null) return;

        try { order.Confirm(); }
        catch (InvalidOperationException) { return; }

        await db.SaveChangesAsync(context.CancellationToken);

        trackingNotifier.Notify(order.Id, "Confirmed", "Pagamento confirmado");
    }
}
