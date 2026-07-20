using Delify.Modules.Payments.Domain;
using Delify.Modules.Payments.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Delify.Modules.Payments.Application;

public static class SessionSettlement
{
    /// <summary>
    /// A comanda está quitada quando não sobra nenhuma cobrança pendente nela.
    ///
    /// Serve para os dois casos sem ramificar: cobrança única (1 linha — confirmou,
    /// quitou) e conta dividida (N linhas — só quita na última). É o que impede o
    /// SessionPaidIntegrationEvent de ser publicado na primeira parte paga, que
    /// fecharia a sessão e liberaria a mesa com gente ainda devendo.
    ///
    /// Cobranças invalidadas (Failed) não contam — ver <see cref="Payment.Void"/>.
    /// </summary>
    public static async Task<bool> IsFullySettledAsync(
        PaymentsDbContext db, Guid tableSessionId, CancellationToken ct = default)
    {
        return !await db.Payments
            .AsNoTracking()
            .AnyAsync(p => p.TableSessionId == tableSessionId
                        && p.Status == PaymentStatus.Pending, ct);
    }
}
