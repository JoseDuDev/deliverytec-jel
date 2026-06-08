namespace Delify.Modules.Delivery.Abstractions;

public record Address(string Street, string Number, string City, double Latitude, double Longitude);
public record DeliveryQuote(string Provider, decimal Price, int EstimatedMinutes);
public record DispatchResult(string ProviderOrderId, string? TrackingUrl);

public interface IDeliveryProvider
{
    string ProviderName { get; }
    Task<DeliveryQuote> GetQuoteAsync(Address pickup, Address dropoff, CancellationToken ct = default);
    Task<DispatchResult> DispatchAsync(Address pickup, Address dropoff, string contactPhone, CancellationToken ct = default);
}
