using Delify.Modules.Orders.Domain;
using Delify.Modules.Orders.Infrastructure;
using Delify.Shared.Abstractions;
using Delify.Shared.Result;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Delify.Modules.Orders.Application.Queries;

public record GetOrdersByEstablishmentQuery(Guid EstablishmentId, int Page = 1, int PageSize = 20)
    : IRequest<Result<List<Order>>>;

internal sealed class GetOrdersByEstablishmentQueryHandler(
    OrdersDbContext db,
    ITenantContext tenant) : IRequestHandler<GetOrdersByEstablishmentQuery, Result<List<Order>>>
{
    public async Task<Result<List<Order>>> Handle(GetOrdersByEstablishmentQuery request, CancellationToken cancellationToken)
    {
        var orders = await db.Orders
            .Include(o => o.Items)
            .Where(o => o.TenantId == tenant.TenantId && o.EstablishmentId == request.EstablishmentId)
            .OrderByDescending(o => o.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return Result.Success(orders);
    }
}
