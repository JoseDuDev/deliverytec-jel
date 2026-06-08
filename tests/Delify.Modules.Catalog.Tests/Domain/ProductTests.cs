using Delify.Modules.Catalog.Domain;
using FluentAssertions;

namespace Delify.Modules.Catalog.Tests.Domain;

public class ProductTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid CategoryId = Guid.NewGuid();

    [Fact]
    public void Create_WithValidData_SetsProperties()
    {
        var product = Product.Create(TenantId, CategoryId, "X-Burguer", 29.90m);

        product.Name.Should().Be("X-Burguer");
        product.Price.Should().Be(29.90m);
        product.TenantId.Should().Be(TenantId);
        product.CategoryId.Should().Be(CategoryId);
        product.IsAvailable.Should().BeTrue();
        product.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void Create_WithEmptyName_ThrowsArgumentException()
    {
        var act = () => Product.Create(TenantId, CategoryId, "", 29.90m);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithNegativePrice_ThrowsArgumentOutOfRangeException()
    {
        var act = () => Product.Create(TenantId, CategoryId, "Produto", -1m);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Update_ChangesNameAndPrice()
    {
        var product = Product.Create(TenantId, CategoryId, "X-Burguer", 29.90m);
        product.Update("X-Burguer Duplo", "Com bacon", 45.00m, null, true);

        product.Name.Should().Be("X-Burguer Duplo");
        product.Price.Should().Be(45.00m);
        product.UpdatedAt.Should().NotBeNull();
    }
}
