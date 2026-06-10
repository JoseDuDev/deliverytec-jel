using Delify.Modules.Orders.Application.Commands;
using Delify.Modules.Orders.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Delify.Modules.Orders.Endpoints;

internal static class OrderEndpoints
{
    internal static IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/orders").WithTags("Orders").RequireAuthorization();

        group.MapPost("/", async (CreateOrderCommand command, IMediator mediator) =>
        {
            var result = await mediator.Send(command);
            return result.IsSuccess
                ? Results.Created($"/api/orders/{result.Value}", new { id = result.Value })
                : Results.BadRequest(result.Error);
        })
        .WithName("CreateOrder");

        group.MapGet("/establishments/{establishmentId:guid}", async (
            Guid establishmentId,
            IMediator mediator,
            int page = 1,
            int pageSize = 20) =>
        {
            var result = await mediator.Send(new GetOrdersByEstablishmentQuery(establishmentId, page, pageSize));
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        })
        .WithName("GetOrdersByEstablishment");

        group.MapPatch("/{orderId:guid}/accept",
            (Guid orderId, IMediator mediator) => HandleStatusUpdate(orderId, OrderAction.Accept, mediator))
            .WithName("AcceptOrder");

        group.MapPatch("/{orderId:guid}/start-delivery",
            (Guid orderId, IMediator mediator) => HandleStatusUpdate(orderId, OrderAction.StartDelivery, mediator))
            .WithName("StartOrderDelivery");

        group.MapPatch("/{orderId:guid}/complete",
            (Guid orderId, IMediator mediator) => HandleStatusUpdate(orderId, OrderAction.Complete, mediator))
            .WithName("CompleteOrder");

        group.MapPatch("/{orderId:guid}/cancel",
            (Guid orderId, IMediator mediator) => HandleStatusUpdate(orderId, OrderAction.Cancel, mediator))
            .WithName("CancelOrder");

        return app;
    }

    private static async Task<IResult> HandleStatusUpdate(Guid orderId, OrderAction action, IMediator mediator)
    {
        var result = await mediator.Send(new UpdateOrderStatusCommand(orderId, action));
        return result.IsSuccess ? Results.NoContent() : Results.BadRequest(result.Error);
    }
}
