using Delify.Modules.Orders.Domain;
using Delify.Modules.Orders.Infrastructure;
using Delify.Shared.Abstractions;
using Delify.Shared.IntegrationEvents;
using Delify.Shared.Result;
using MassTransit;
using MediatR;

namespace Delify.Modules.Orders.Application.Commands;

public record OrderItemDto(Guid ProductId, string ProductName, int Quantity, decimal UnitPrice);

public record CreateOrderCommand(
    Guid EstablishmentId,
    List<OrderItemDto> Items,
    string CustomerCpf,
    string CustomerName,
    string? CustomerNote = null) : IRequest<Result<Guid>>;

internal sealed class CreateOrderCommandHandler(
    OrdersDbContext db,
    ITenantContext tenant,
    IBus bus) : IRequestHandler<CreateOrderCommand, Result<Guid>>
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

        await bus.Publish(new OrderCreatedIntegrationEvent(
            order.Id, order.TenantId, order.Total,
            request.CustomerCpf, request.CustomerName), cancellationToken);

        return Result.Success(order.Id);
    }
}
