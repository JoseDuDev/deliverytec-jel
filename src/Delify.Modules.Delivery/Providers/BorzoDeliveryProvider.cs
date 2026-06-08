using Delify.Modules.Delivery.Abstractions;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Json;
using System.Text.Json;

namespace Delify.Modules.Delivery.Providers;

internal sealed class BorzoDeliveryProvider(HttpClient httpClient, IConfiguration config) : IDeliveryProvider
{
    public string ProviderName => "Borzo";

    public async Task<DeliveryQuote> GetQuoteAsync(Address pickup, Address dropoff, CancellationToken ct = default)
    {
        var body = new
        {
            matter = "Pedido delivery",
            vehicle_type_id = 8,
            points = new[]
            {
                new { address = $"{pickup.Street}, {pickup.Number}", latitude = pickup.Latitude, longitude = pickup.Longitude },
                new { address = $"{dropoff.Street}, {dropoff.Number}", latitude = dropoff.Latitude, longitude = dropoff.Longitude }
            }
        };

        var response = await httpClient.PostAsJsonAsync("/api/business/1.1/calculate-order", body, ct);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
        var price = json.GetProperty("order").GetProperty("payment_amount").GetDecimal();
        var minutes = json.GetProperty("order").GetProperty("delivery_fee_amount").GetInt32();

        return new DeliveryQuote(ProviderName, price, minutes);
    }

    public async Task<DispatchResult> DispatchAsync(Address pickup, Address dropoff, string contactPhone, CancellationToken ct = default)
    {
        var body = new
        {
            matter = "Pedido delivery",
            vehicle_type_id = 8,
            points = new[]
            {
                new { address = $"{pickup.Street}, {pickup.Number}", latitude = pickup.Latitude, longitude = pickup.Longitude, contact_person = new { name = "Estabelecimento", phone = contactPhone } },
                new { address = $"{dropoff.Street}, {dropoff.Number}", latitude = dropoff.Latitude, longitude = dropoff.Longitude, contact_person = new { name = "Cliente", phone = contactPhone } }
            }
        };

        var response = await httpClient.PostAsJsonAsync("/api/business/1.1/create-order", body, ct);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
        var orderId = json.GetProperty("order").GetProperty("id").GetInt64().ToString();

        return new DispatchResult(orderId, TrackingUrl: null);
    }
}
