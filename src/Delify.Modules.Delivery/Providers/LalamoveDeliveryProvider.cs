using Delify.Modules.Delivery.Abstractions;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Json;
using System.Text.Json;

namespace Delify.Modules.Delivery.Providers;

internal sealed class LalamoveDeliveryProvider(HttpClient httpClient, IConfiguration config) : IDeliveryProvider
{
    public string ProviderName => "Lalamove";

    public async Task<DeliveryQuote> GetQuoteAsync(Address pickup, Address dropoff, CancellationToken ct = default)
    {
        var body = new
        {
            data = new
            {
                serviceType = "MOTORCYCLE",
                language = "pt_BR",
                stops = new[]
                {
                    new { coordinates = new { lat = pickup.Latitude.ToString(), lng = pickup.Longitude.ToString() }, address = $"{pickup.Street}, {pickup.Number}" },
                    new { coordinates = new { lat = dropoff.Latitude.ToString(), lng = dropoff.Longitude.ToString() }, address = $"{dropoff.Street}, {dropoff.Number}" }
                }
            }
        };

        var response = await httpClient.PostAsJsonAsync("/v3/quotations", body, ct);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
        var price = decimal.Parse(json.GetProperty("data").GetProperty("priceBreakdown").GetProperty("total").GetString()!);
        var minutes = json.GetProperty("data").GetProperty("estimatedTimeline").GetProperty("pickup").GetInt32();

        return new DeliveryQuote(ProviderName, price / 100, minutes);
    }

    public async Task<DispatchResult> DispatchAsync(Address pickup, Address dropoff, string contactPhone, CancellationToken ct = default)
    {
        var body = new
        {
            data = new
            {
                serviceType = "MOTORCYCLE",
                language = "pt_BR",
                stops = new[]
                {
                    new
                    {
                        coordinates = new { lat = pickup.Latitude.ToString(), lng = pickup.Longitude.ToString() },
                        address = $"{pickup.Street}, {pickup.Number}",
                        contact = new { name = "Estabelecimento", phone = contactPhone }
                    },
                    new
                    {
                        coordinates = new { lat = dropoff.Latitude.ToString(), lng = dropoff.Longitude.ToString() },
                        address = $"{dropoff.Street}, {dropoff.Number}",
                        contact = new { name = "Cliente", phone = contactPhone }
                    }
                }
            }
        };

        var response = await httpClient.PostAsJsonAsync("/v3/orders", body, ct);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
        var orderId = json.GetProperty("data").GetProperty("orderId").GetString()!;

        return new DispatchResult(orderId, TrackingUrl: null);
    }
}
