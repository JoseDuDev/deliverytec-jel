using Delify.Modules.Orders.Domain;
using Delify.Modules.Orders.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Delify.Modules.Dinein.Application;

internal static class TableRelease
{
    /// <summary>
    /// Cancela os pedidos que ainda não saíram quando o staff libera a mesa.
    ///
    /// "Liberar" é override do staff — a mesa acabou sem passar pelo pagamento —,
    /// então o que estava na fila não vai mais ser servido e não pode ficar
    /// poluindo o quadro da cozinha. Diferente do pagamento: lá o cliente pagou
    /// pela comida e ela ainda precisa sair, então os pedidos seguem no quadro.
    ///
    /// Só mexe no que ainda não é terminal — pedido já entregue vira histórico.
    /// </summary>
    internal static async Task<int> CancelPendingOrdersAsync(
        OrdersDbContext ordersDb, IReadOnlyCollection<Guid> sessionIds, CancellationToken ct = default)
    {
        if (sessionIds.Count == 0) return 0;

        var pending = await ordersDb.Orders
            .Where(o => o.TableSessionId != null
                     && sessionIds.Contains(o.TableSessionId.Value)
                     && o.Status != OrderStatus.Delivered
                     && o.Status != OrderStatus.Cancelled)
            .ToListAsync(ct);

        foreach (var order in pending) order.Cancel();

        if (pending.Count > 0) await ordersDb.SaveChangesAsync(ct);
        return pending.Count;
    }
}
