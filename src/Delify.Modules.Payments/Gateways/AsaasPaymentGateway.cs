using Delify.Modules.Payments.Abstractions;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Json;
using System.Text.Json;

namespace Delify.Modules.Payments.Gateways;

internal sealed class AsaasPaymentGateway(HttpClient httpClient, IConfiguration config) : IPaymentGateway
{
    public string GatewayName => "Asaas";

    public async Task<PixPaymentResult> CreatePixAsync(PixPaymentRequest request, CancellationToken ct = default)
    {
        var body = new
        {
            customer = request.CustomerCpf,
            billingType = "PIX",
            value = request.Amount,
            dueDate = DateTimeOffset.UtcNow.AddHours(24).ToString("yyyy-MM-dd"),
            externalReference = request.OrderId.ToString(),
            description = $"Pedido #{request.OrderId}"
        };

        var response = await httpClient.PostAsJsonAsync("/api/v3/payments", body, ct);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
        var paymentId = json.GetProperty("id").GetString()!;

        var pixResponse = await httpClient.GetFromJsonAsync<JsonElement>(
            $"/api/v3/payments/{paymentId}/pixQrCode", ct);

        return new PixPaymentResult(
            GatewayId: paymentId,
            QrCode: pixResponse.GetProperty("encodedImage").GetString()!,
            CopyPaste: pixResponse.GetProperty("payload").GetString()!,
            ExpiresAt: DateTimeOffset.UtcNow.AddHours(24));
    }

    public Task<WebhookResult> ProcessWebhookAsync(string payload, string signature, CancellationToken ct = default)
    {
        var webhookToken = config["Asaas:WebhookToken"];
        if (signature != webhookToken)
            return Task.FromResult(new WebhookResult(string.Empty, false));

        var json = JsonDocument.Parse(payload);
        var eventType = json.RootElement.GetProperty("event").GetString();
        var paymentId = json.RootElement.GetProperty("payment").GetProperty("id").GetString()!;

        return Task.FromResult(new WebhookResult(paymentId, eventType == "PAYMENT_CONFIRMED"));
    }
}
