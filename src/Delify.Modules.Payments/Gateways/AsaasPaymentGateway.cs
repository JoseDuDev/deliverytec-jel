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
        var customerId = await GetOrCreateCustomerAsync(request.CustomerCpf, request.CustomerName, ct);

        var body = new
        {
            customer = customerId,
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

    private async Task<string> GetOrCreateCustomerAsync(string cpf, string name, CancellationToken ct)
    {
        // Busca cliente existente pelo CPF
        var search = await httpClient.GetFromJsonAsync<JsonElement>(
            $"/api/v3/customers?cpfCnpj={cpf}&limit=1", ct);

        var data = search.GetProperty("data");
        if (data.GetArrayLength() > 0)
            return data[0].GetProperty("id").GetString()!;

        // Cria novo cliente
        var create = await httpClient.PostAsJsonAsync("/api/v3/customers", new { name, cpfCnpj = cpf }, ct);
        create.EnsureSuccessStatusCode();

        var created = await create.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
        return created.GetProperty("id").GetString()!;
    }
}
