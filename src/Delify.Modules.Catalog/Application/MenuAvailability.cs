using Delify.Modules.Catalog.Domain;
using Delify.Modules.Catalog.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Delify.Modules.Catalog.Application;

public static class MenuAvailability
{
    /// <summary>
    /// Nomes dos produtos que não podem ser pedidos agora — pausados pelo lojista
    /// ou pertencentes a uma categoria desativada. Lista vazia = tudo liberado.
    ///
    /// Existe porque esconder na tela não basta: o cardápio é público e o pedido é
    /// um POST direto, então a recusa precisa vir do servidor. Vale para os três
    /// canais (delivery, mesa e app do garçom).
    ///
    /// Categoria inativa conta como indisponível: desativá-la é o lojista dizendo
    /// "não venda isso", e a seção inteira some do cardápio do cliente.
    /// </summary>
    public static async Task<IReadOnlyList<string>> FindUnorderableAsync(
        CatalogDbContext db, IReadOnlyCollection<Product> products, CancellationToken ct = default)
    {
        if (products.Count == 0) return [];

        var categoryIds = products.Select(p => p.CategoryId).Distinct().ToList();
        var inactiveCategories = await db.Categories
            .AsNoTracking()
            .Where(c => categoryIds.Contains(c.Id) && !c.IsActive)
            .Select(c => c.Id)
            .ToListAsync(ct);

        return products
            .Where(p => !p.IsAvailable || inactiveCategories.Contains(p.CategoryId))
            .Select(p => p.Name)
            .ToList();
    }
}
