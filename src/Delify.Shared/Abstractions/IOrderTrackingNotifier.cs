namespace Delify.Shared.Abstractions;

public interface IOrderTrackingNotifier
{
    void Notify(Guid orderId, string status, string label);
}

public sealed class NullOrderTrackingNotifier : IOrderTrackingNotifier
{
    public void Notify(Guid orderId, string status, string label) { }
}
