namespace Delify.Shared.Abstractions;

public interface ITenantContext
{
    Guid TenantId { get; }
}
