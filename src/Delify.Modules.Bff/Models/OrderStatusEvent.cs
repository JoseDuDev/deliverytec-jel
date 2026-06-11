namespace Delify.Modules.Bff.Models;

public record OrderStatusEvent(Guid OrderId, string Status, string Label, DateTimeOffset At);
