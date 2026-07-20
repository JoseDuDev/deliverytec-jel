using Delify.Shared.Domain;

namespace Delify.Modules.Dinein.Domain;

public sealed class TableSession : Entity
{
    public Guid EstablishmentId { get; private set; }
    public Guid TableId { get; private set; }
    public SessionStatus Status { get; private set; } = SessionStatus.Open;
    public DateTimeOffset OpenedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ClosedAt { get; private set; }
    // Quem abriu a comanda: garçom (id + nome) ou null quando foi o cliente pelo app.
    public Guid? OpenedByWaiterId { get; private set; }
    public string? OpenedByName { get; private set; }

    private TableSession() { }

    public static TableSession Open(
        Guid tenantId, Guid establishmentId, Guid tableId,
        Guid? openedByWaiterId = null, string? openedByName = null)
    {
        return new TableSession
        {
            TenantId = tenantId,
            EstablishmentId = establishmentId,
            TableId = tableId,
            OpenedByWaiterId = openedByWaiterId,
            OpenedByName = openedByName,
        };
    }

    public void Close()
    {
        Status = SessionStatus.Closed;
        ClosedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
