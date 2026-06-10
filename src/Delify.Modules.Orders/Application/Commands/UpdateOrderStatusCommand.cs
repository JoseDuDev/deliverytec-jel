using Delify.Modules.Orders.Infrastructure;
using Delify.Shared.Result;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Delify.Modules.Orders.Application.Commands;

public enum OrderAction { Accept, StartDelivery, Complete, Cancel }

public record UpdateOrderStatusCommand(Guid OrderId, OrderAction Action) : IRequest<Result>;

internal sealed class UpdateOrderStatusCommandHandler(OrdersDbContext db)
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
        return Result.Success();
    }
}
