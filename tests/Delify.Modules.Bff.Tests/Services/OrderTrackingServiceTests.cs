using Delify.Modules.Bff.Services;
using Delify.Modules.Bff.Models;

namespace Delify.Modules.Bff.Tests.Services;

public class OrderTrackingServiceTests
{
    [Fact]
    public async Task Subscribe_ThenNotify_DeliversEventToReader()
    {
        var svc = new OrderTrackingService();
        var orderId = Guid.NewGuid();

        var reader = svc.Subscribe(orderId);
        svc.Notify(orderId, "Confirmed", "Pagamento confirmado");

        var evt = await reader.ReadAsync();

        Assert.Equal(orderId, evt.OrderId);
        Assert.Equal("Confirmed", evt.Status);
        Assert.Equal("Pagamento confirmado", evt.Label);
    }

    [Fact]
    public void Notify_WhenNoSubscriber_DoesNotThrow()
    {
        var svc = new OrderTrackingService();
        var ex = Record.Exception(() => svc.Notify(Guid.NewGuid(), "Confirmed", "label"));
        Assert.Null(ex);
    }

    [Fact]
    public async Task Notify_WhenDelivered_CompletesChannel()
    {
        var svc = new OrderTrackingService();
        var orderId = Guid.NewGuid();

        var reader = svc.Subscribe(orderId);
        svc.Notify(orderId, "Delivered", "Pedido entregue!");

        await reader.ReadAsync(); // consome o evento
        Assert.True(reader.Completion.IsCompleted);
    }
}
