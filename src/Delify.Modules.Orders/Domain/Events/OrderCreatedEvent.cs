using Delify.Shared.Domain;

namespace Delify.Modules.Orders.Domain.Events;

public sealed record OrderCreatedEvent(
    Guid Id,
    DateTimeOffset OccurredAt,
    Guid OrderId,
    Guid TenantId,
    decimal Total) : IDomainEvent;
