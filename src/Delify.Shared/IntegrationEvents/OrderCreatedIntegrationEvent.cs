namespace Delify.Shared.IntegrationEvents;

public record OrderCreatedIntegrationEvent(
    Guid OrderId,
    Guid TenantId,
    decimal Total,
    string CustomerCpf,
    string CustomerName);
