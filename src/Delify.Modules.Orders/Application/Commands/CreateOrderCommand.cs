using Delify.Modules.Orders.Domain;
using Delify.Modules.Orders.Infrastructure;
using Delify.Shared.Abstractions;
using Delify.Shared.Result;
using MediatR;

namespace Delify.Modules.Orders.Application.Commands;

public record OrderItemDto(Guid ProductId, string ProductName, int Quantity, decimal UnitPrice);

public record CreateOrderCommand(
    Guid EstablishmentId,
    List<OrderItemDto> Items,
    string? CustomerNote = null) : IRequest<Result<Guid>>;

internal sealed class CreateOrderCommandHandler(
    OrdersDbContext db,
    ITenantContext tenant) : IRequestHandler<CreateOrderCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        if (request.Items.Count == 0)
            return Result.Failure<Guid>(Error.Validation("Order must have at least one item."));

        var order = Order.Create(tenant.TenantId, request.EstablishmentId);

        foreach (var item in request.Items)
            order.AddItem(item.ProductId, item.ProductName, item.Quantity, item.UnitPrice);

        order.CustomerNote = request.CustomerNote;

        db.Orders.Add(order);
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(order.Id);
    }
}
