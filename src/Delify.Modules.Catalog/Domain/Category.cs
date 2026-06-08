using Delify.Shared.Domain;

namespace Delify.Modules.Catalog.Domain;

public sealed class Category : Entity
{
    public Guid EstablishmentId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public int Order { get; private set; }
    public bool IsActive { get; private set; } = true;
    public ICollection<Product> Products { get; private set; } = [];

    private Category() { }

    public static Category Create(Guid tenantId, Guid establishmentId, string name, int order = 0)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new Category { TenantId = tenantId, EstablishmentId = establishmentId, Name = name, Order = order };
    }
}
