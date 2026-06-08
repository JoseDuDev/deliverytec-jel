namespace Delify.Modules.Identity.Domain;

public sealed class Tenant
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Slug { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;

    private Tenant() { }

    public static Tenant Create(string slug, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(slug);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new Tenant { Slug = slug.ToLowerInvariant(), Name = name };
    }
}
