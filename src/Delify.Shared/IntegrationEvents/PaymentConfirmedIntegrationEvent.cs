namespace Delify.Shared.IntegrationEvents;

public record PaymentConfirmedIntegrationEvent(
    Guid OrderId,
    Guid TenantId);
