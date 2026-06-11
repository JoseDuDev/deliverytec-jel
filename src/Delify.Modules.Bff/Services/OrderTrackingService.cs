using System.Collections.Concurrent;
using System.Threading.Channels;
using Delify.Modules.Bff.Models;
using Delify.Shared.Abstractions;

namespace Delify.Modules.Bff.Services;

public sealed class OrderTrackingService : IOrderTrackingNotifier
{
    private readonly ConcurrentDictionary<Guid, Channel<OrderStatusEvent>> _channels = new();

    public ChannelReader<OrderStatusEvent> Subscribe(Guid orderId)
    {
        var channel = _channels.GetOrAdd(orderId, _ => Channel.CreateUnbounded<OrderStatusEvent>());
        return channel.Reader;
    }

    public void Notify(Guid orderId, string status, string label)
    {
        if (!_channels.TryGetValue(orderId, out var channel)) return;

        channel.Writer.TryWrite(new OrderStatusEvent(orderId, status, label, DateTimeOffset.UtcNow));

        if (status is "Delivered" or "Cancelled")
        {
            _channels.TryRemove(orderId, out _);
            channel.Writer.TryComplete();
        }
    }
}
