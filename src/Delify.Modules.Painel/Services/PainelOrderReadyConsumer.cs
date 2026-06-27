using Delify.Shared.Abstractions;
using Delify.Shared.IntegrationEvents;
using MassTransit;

namespace Delify.Modules.Painel.Services;

public sealed class PainelOrderReadyConsumer(IPainelDashboardNotifier notifier)
    : IConsumer<PaymentConfirmedIntegrationEvent>
{
    public Task Consume(ConsumeContext<PaymentConfirmedIntegrationEvent> context)
    {
        notifier.Notify(context.Message.TenantId, new PainelOrderEvent(
            context.Message.OrderId,
            "AwaitingConfirmation",
            DateTimeOffset.UtcNow));

        return Task.CompletedTask;
    }
}
