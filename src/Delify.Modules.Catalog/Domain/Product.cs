using Delify.Shared.Domain;

namespace Delify.Modules.Catalog.Domain;

public sealed class Product : Entity
{
    public Guid CategoryId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public decimal Price { get; private set; }
    public string? PhotoUrl { get; private set; }
    public bool IsAvailable { get; private set; } = true;
    public ICollection<Complement> Complements { get; private set; } = [];

    private Product() { }

    public static Product Create(Guid tenantId, Guid categoryId, string name, decimal price)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (price < 0) throw new ArgumentOutOfRangeException(nameof(price));
        return new Product { TenantId = tenantId, CategoryId = categoryId, Name = name, Price = price };
    }

    public void Update(string name, string? description, decimal price, string? photoUrl, bool isAvailable)
    {
        Name = name;
        Description = description;
        Price = price;
        PhotoUrl = photoUrl;
        IsAvailable = isAvailable;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
