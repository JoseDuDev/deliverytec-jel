namespace Delify.Shared.IntegrationEvents;

public record SessionPaidIntegrationEvent(
    Guid TableSessionId,
    Guid TenantId);
