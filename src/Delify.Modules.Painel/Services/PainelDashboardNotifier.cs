using System.Collections.Concurrent;
using System.Threading.Channels;
using Delify.Shared.Abstractions;

namespace Delify.Modules.Painel.Services;

public sealed class PainelDashboardNotifier : IPainelDashboardNotifier
{
    private readonly ConcurrentDictionary<Guid, List<ChannelWriter<PainelOrderEvent>>> _writers = new();

    public ChannelReader<PainelOrderEvent> Subscribe(Guid tenantId)
    {
        var channel = Channel.CreateBounded<PainelOrderEvent>(
            new BoundedChannelOptions(100) { FullMode = BoundedChannelFullMode.DropOldest });

        _writers.AddOrUpdate(tenantId,
            _ => [channel.Writer],
            (_, list) => { list.Add(channel.Writer); return list; });

        return channel.Reader;
    }

    public void Unsubscribe(Guid tenantId, ChannelWriter<PainelOrderEvent> writer)
    {
        if (_writers.TryGetValue(tenantId, out var list))
            list.Remove(writer);
    }

    public void Notify(Guid tenantId, PainelOrderEvent orderEvent)
    {
        if (!_writers.TryGetValue(tenantId, out var writers)) return;

        foreach (var writer in writers.ToList())
            writer.TryWrite(orderEvent);
    }
}
