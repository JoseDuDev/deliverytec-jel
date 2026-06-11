namespace Delify.Modules.Bff.Models;

public record MenuResponse(
    Guid EstablishmentId,
    string Name,
    string Slug,
    IReadOnlyList<MenuCategoryDto> Categories);

public record MenuCategoryDto(
    Guid Id,
    string Name,
    int Order,
    IReadOnlyList<MenuProductDto> Products);

public record MenuProductDto(
    Guid Id,
    string Name,
    decimal Price,
    string? Description,
    string? ImageUrl,
    IReadOnlyList<MenuComplementDto> Complements);

public record MenuComplementDto(Guid Id, string Name, decimal Price);
