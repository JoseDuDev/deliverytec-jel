using System.Collections.Concurrent;
using System.Threading.Channels;

namespace Delify.Modules.Dinein.Services;

// Evento em tempo real para o painel de mesas.
// Type: "waiter-call" | "call-resolved" | "table-update"
public record MesaEvent(
    string Type,
    Guid TableId,
    string TableNumber,
    Guid? CallId,
    string? Reason,
    DateTimeOffset At);

public sealed class MesaNotifier
{
    private readonly ConcurrentDictionary<Guid, List<ChannelWriter<MesaEvent>>> _writers = new();

    public ChannelReader<MesaEvent> Subscribe(Guid tenantId)
    {
        var channel = Channel.CreateBounded<MesaEvent>(
            new BoundedChannelOptions(100) { FullMode = BoundedChannelFullMode.DropOldest });

        _writers.AddOrUpdate(tenantId,
            _ => [channel.Writer],
            (_, list) => { list.Add(channel.Writer); return list; });

        return channel.Reader;
    }

    public void Unsubscribe(Guid tenantId, ChannelWriter<MesaEvent> writer)
    {
        if (_writers.TryGetValue(tenantId, out var list))
            list.Remove(writer);
    }

    public void Notify(Guid tenantId, MesaEvent mesaEvent)
    {
        if (!_writers.TryGetValue(tenantId, out var writers)) return;

        foreach (var writer in writers.ToList())
            writer.TryWrite(mesaEvent);
    }
}
