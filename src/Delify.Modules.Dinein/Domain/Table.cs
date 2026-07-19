using Delify.Shared.Domain;

namespace Delify.Modules.Dinein.Domain;

public sealed class Table : Entity
{
    public Guid EstablishmentId { get; private set; }
    public string Number { get; private set; } = string.Empty;
    public string QrToken { get; private set; } = string.Empty;
    public TableStatus Status { get; private set; } = TableStatus.Free;

    private Table() { }

    public static Table Create(Guid tenantId, Guid establishmentId, string number, string qrToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(number);
        ArgumentException.ThrowIfNullOrWhiteSpace(qrToken);
        return new Table
        {
            TenantId = tenantId,
            EstablishmentId = establishmentId,
            Number = number.Trim(),
            QrToken = qrToken
        };
    }

    public void Rename(string number)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(number);
        Number = number.Trim();
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void RegenerateToken(string qrToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(qrToken);
        QrToken = qrToken;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Occupy()
    {
        Status = TableStatus.Occupied;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Vacate()
    {
        Status = TableStatus.Free;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
