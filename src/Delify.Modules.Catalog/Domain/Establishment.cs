using Delify.Shared.Domain;

namespace Delify.Modules.Catalog.Domain;

public sealed class Establishment : Entity
{
    public string Slug { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? LogoUrl { get; private set; }
    public string? Description { get; private set; }
    public bool IsOpen { get; private set; }
    public decimal DeliveryFee { get; private set; }
    // Taxa de serviço (os 10% do garçom) aplicada na conta da mesa.
    public bool ServiceFeeEnabled { get; private set; } = true;
    public decimal ServiceFeePercent { get; private set; } = 10m;
    public ICollection<Category> Categories { get; private set; } = [];

    private Establishment() { }

    public static Establishment Create(Guid tenantId, string slug, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(slug);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new Establishment { TenantId = tenantId, Slug = slug.ToLowerInvariant(), Name = name };
    }

    public void Update(string name, string? description, string? logoUrl, bool isOpen, decimal deliveryFee)
    {
        Name = name;
        Description = description;
        LogoUrl = logoUrl;
        IsOpen = isOpen;
        DeliveryFee = deliveryFee >= 0 ? deliveryFee : 0;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SetServiceFee(bool enabled, decimal percent)
    {
        ServiceFeeEnabled = enabled;
        ServiceFeePercent = percent >= 0 ? percent : 0;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
