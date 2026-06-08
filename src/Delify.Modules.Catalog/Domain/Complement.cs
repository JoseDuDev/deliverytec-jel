using Delify.Shared.Domain;

namespace Delify.Modules.Catalog.Domain;

public sealed class Complement : Entity
{
    public Guid ProductId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public decimal AdditionalPrice { get; private set; }
    public bool IsAvailable { get; private set; } = true;

    private Complement() { }

    public static Complement Create(Guid tenantId, Guid productId, string name, decimal additionalPrice = 0)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new Complement { TenantId = tenantId, ProductId = productId, Name = name, AdditionalPrice = additionalPrice };
    }
}
