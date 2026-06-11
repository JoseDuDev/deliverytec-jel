using Delify.Modules.Orders.Domain;
using Delify.Modules.Orders.Infrastructure;
using Delify.Shared.Abstractions;
using Delify.Shared.Result;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Delify.Modules.Orders.Application.Commands;

public enum OrderAction { Accept, StartDelivery, Complete, Cancel }

public record UpdateOrderStatusCommand(Guid OrderId, OrderAction Action) : IRequest<Result>;

internal sealed class UpdateOrderStatusCommandHandler(
    OrdersDbContext db,
    IOrderTrackingNotifier trackingNotifier)
    : IRequestHandler<UpdateOrderStatusCommand, Result>
{
    public async Task<Result> Handle(UpdateOrderStatusCommand request, CancellationToken cancellationToken)
    {
        var order = await db.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken);

        if (order is null)
            return Result.Failure(Error.NotFound("Order"));

        try
        {
            switch (request.Action)
            {
                case OrderAction.Accept: order.Accept(); break;
                case OrderAction.StartDelivery: order.StartDelivery(); break;
                case OrderAction.Complete: order.Complete(); break;
                case OrderAction.Cancel: order.Cancel(); break;
            }
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure(Error.Validation(ex.Message));
        }

        await db.SaveChangesAsync(cancellationToken);

        var (statusKey, label) = order.Status switch
        {
            OrderStatus.InPreparation => ("Preparing", "Preparando seu pedido"),
            OrderStatus.InDelivery    => ("OutForDelivery", "Saiu para entrega"),
            OrderStatus.Delivered     => ("Delivered", "Pedido entregue!"),
            OrderStatus.Cancelled     => ("Cancelled", "Pedido cancelado"),
            _                         => (order.Status.ToString(), order.Status.ToString())
        };

        trackingNotifier.Notify(order.Id, statusKey, label);

        return Result.Success();
    }
}
