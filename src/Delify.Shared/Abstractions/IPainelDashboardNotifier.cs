namespace Delify.Shared.Abstractions;

public record PainelOrderEvent(Guid OrderId, string Status, DateTimeOffset At);

public interface IPainelDashboardNotifier
{
    void Notify(Guid tenantId, PainelOrderEvent orderEvent);
}

public sealed class NullPainelDashboardNotifier : IPainelDashboardNotifier
{
    public void Notify(Guid tenantId, PainelOrderEvent orderEvent) { }
}
