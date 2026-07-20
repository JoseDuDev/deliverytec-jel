using Delify.Modules.Dinein.Domain;
using Delify.Modules.Dinein.Infrastructure;
using Delify.Modules.Dinein.Services;
using Delify.Shared.IntegrationEvents;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Delify.Modules.Dinein.Application;

// Pagamento da comanda confirmado (PIX) → fecha a sessão e libera a mesa.
public sealed class SessionPaidConsumer(DineinDbContext db, MesaNotifier notifier)
    : IConsumer<SessionPaidIntegrationEvent>
{
    public async Task Consume(ConsumeContext<SessionPaidIntegrationEvent> context)
    {
        var ct = context.CancellationToken;
        var session = await db.Sessions
            .FirstOrDefaultAsync(s => s.Id == context.Message.TableSessionId, ct);
        if (session is null || session.Status == SessionStatus.Closed) return;

        session.Close();

        var table = await db.Tables.FirstOrDefaultAsync(t => t.Id == session.TableId, ct);
        table?.Vacate();

        var calls = await db.WaiterCalls
            .Where(w => w.TableId == session.TableId && w.Status == WaiterCallStatus.Pending)
            .ToListAsync(ct);
        foreach (var c in calls) c.Acknowledge();

        await db.SaveChangesAsync(ct);

        if (table is not null)
            notifier.Notify(session.TenantId,
                new MesaEvent("session-paid", table.Id, table.Number, null, null, DateTimeOffset.UtcNow));
    }
}
