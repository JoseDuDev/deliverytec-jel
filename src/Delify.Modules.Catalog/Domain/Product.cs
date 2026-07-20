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

    // Carro-chefe: sai numa seção "Destaques" no topo do cardápio, ANTES das
    // categorias, e continua aparecendo na categoria de origem. A posição é
    // definida pelo lojista (merchandising), não pelo nome.
    public bool IsFeatured { get; private set; }
    public int FeaturedOrder { get; private set; }

    public ICollection<Complement> Complements { get; private set; } = [];

    private Product() { }

    public static Product Create(
        Guid tenantId, Guid categoryId, string name, decimal price,
        string? description = null, string? photoUrl = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (price < 0) throw new ArgumentOutOfRangeException(nameof(price));
        return new Product
        {
            TenantId = tenantId,
            CategoryId = categoryId,
            Name = name,
            Price = price,
            Description = description,
            PhotoUrl = string.IsNullOrWhiteSpace(photoUrl) ? null : photoUrl
        };
    }

    public void Update(
        string name, string? description, decimal price, string? photoUrl, bool isAvailable,
        bool isFeatured = false, int featuredOrder = 0)
    {
        Name = name;
        Description = description;
        Price = price;
        PhotoUrl = photoUrl;
        IsAvailable = isAvailable;
        IsFeatured = isFeatured;
        FeaturedOrder = isFeatured ? featuredOrder : 0;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
