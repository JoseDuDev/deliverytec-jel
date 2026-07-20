using Delify.Shared.Domain;

namespace Delify.Modules.Dinein.Domain;

public sealed class WaiterCall : Entity
{
    public Guid EstablishmentId { get; private set; }
    public Guid TableId { get; private set; }
    public Guid? TableSessionId { get; private set; }
    public string TableNumber { get; private set; } = string.Empty;
    public string? Reason { get; private set; }
    public WaiterCallStatus Status { get; private set; } = WaiterCallStatus.Pending;
    public DateTimeOffset? AcknowledgedAt { get; private set; }

    private WaiterCall() { }

    public static WaiterCall Create(
        Guid tenantId, Guid establishmentId, Guid tableId, string tableNumber,
        Guid? tableSessionId, string? reason)
    {
        return new WaiterCall
        {
            TenantId = tenantId,
            EstablishmentId = establishmentId,
            TableId = tableId,
            TableNumber = tableNumber,
            TableSessionId = tableSessionId,
            Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim(),
        };
    }

    public void Acknowledge()
    {
        Status = WaiterCallStatus.Acknowledged;
        AcknowledgedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
