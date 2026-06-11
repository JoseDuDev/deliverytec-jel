namespace Delify.Modules.Bff.Models;

public record PlaceOrderRequest(
    Guid EstablishmentId,
    List<OrderItemRequest> Items,
    CustomerRequest Customer,
    AddressRequest Address,
    string? Note = null);

public record OrderItemRequest(Guid ProductId, int Quantity, List<Guid>? ComplementIds = null);

public record CustomerRequest(string Name, string Phone, string Cpf);

public record AddressRequest(
    string Street,
    string Number,
    string Neighborhood,
    string City,
    string? Complement = null);
