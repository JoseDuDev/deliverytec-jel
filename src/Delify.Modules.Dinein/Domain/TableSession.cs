using Delify.Shared.Domain;

namespace Delify.Modules.Dinein.Domain;

public sealed class TableSession : Entity
{
    public Guid EstablishmentId { get; private set; }
    public Guid TableId { get; private set; }
    public SessionStatus Status { get; private set; } = SessionStatus.Open;
    public DateTimeOffset OpenedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ClosedAt { get; private set; }

    private TableSession() { }

    public static TableSession Open(Guid tenantId, Guid establishmentId, Guid tableId)
    {
        return new TableSession
        {
            TenantId = tenantId,
            EstablishmentId = establishmentId,
            TableId = tableId
        };
    }

    public void Close()
    {
        Status = SessionStatus.Closed;
        ClosedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
