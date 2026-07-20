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

    /// <summary>
    /// Checkout hospedado: uma página do Asaas com PIX e cartão. O cartão é digitado
    /// lá, então nenhum dado sensível passa por aqui.
    /// </summary>
    public async Task<CheckoutResult> CreateCheckoutAsync(CheckoutRequest request, CancellationToken ct = default)
    {
        var callbackBase = config["Asaas:CheckoutCallbackBaseUrl"]?.TrimEnd('/') ?? "";

        var body = new
        {
            billingTypes = new[] { "PIX", "CREDIT_CARD" },
            chargeTypes = new[] { "DETACHED" },
            minutesToExpire = request.MinutesToExpire,
            // É por aqui que o webhook volta a encontrar este Payment: a resposta do
            // checkout traz o id da sessão, não o da cobrança que será criada depois.
            externalReference = request.PaymentId.ToString(),
            callback = new
            {
                successUrl = $"{callbackBase}/pagamento/sucesso",
                cancelUrl = $"{callbackBase}/pagamento/cancelado",
                expiredUrl = $"{callbackBase}/pagamento/expirado"
            },
            items = new[]
            {
                new { name = request.Description, description = request.Description, quantity = 1, value = request.Amount }
            },
            customerData = new
            {
                name = request.CustomerName,
                cpfCnpj = request.CustomerCpf
            }
        };

        var response = await httpClient.PostAsJsonAsync("/api/v3/checkouts", body, ct);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);

        return new CheckoutResult(
            GatewayId: json.GetProperty("id").GetString()!,
            Url: json.GetProperty("link").GetString()!,
            ExpiresAt: DateTimeOffset.UtcNow.AddMinutes(request.MinutesToExpire));
    }

    public Task<WebhookResult> ProcessWebhookAsync(string payload, string signature, CancellationToken ct = default)
    {
        var webhookToken = config["Asaas:WebhookToken"];

        // Token em branco aceitaria qualquer requisição sem header — o callback é
        // anônimo e público, então sem token configurado nada é processado.
        if (string.IsNullOrWhiteSpace(webhookToken) || signature != webhookToken)
            return Task.FromResult(Ignored);

        var json = JsonDocument.Parse(payload);
        if (!json.RootElement.TryGetProperty("event", out var evt)) return Task.FromResult(Ignored);
        if (!json.RootElement.TryGetProperty("payment", out var pay)) return Task.FromResult(Ignored);

        var paymentId = pay.TryGetProperty("id", out var pid) ? pid.GetString() ?? "" : "";
        var externalRef = pay.TryGetProperty("externalReference", out var xr) ? xr.GetString() : null;

        var outcome = evt.GetString() switch
        {
            // Cartão confirma primeiro e só libera fundos até 32 dias depois
            // (PAYMENT_RECEIVED). Esperar o repasse deixaria a mesa ocupada — então
            // o que libera a comanda é a confirmação.
            "PAYMENT_CONFIRMED" => WebhookOutcome.Confirmed,
            "PAYMENT_RECEIVED" => WebhookOutcome.Confirmed,

            // Recusado ou desfeito: a parte volta a ser devida.
            "PAYMENT_CREDIT_CARD_CAPTURE_REFUSED" => WebhookOutcome.Refused,
            "PAYMENT_REPROVED_BY_RISK_ANALYSIS" => WebhookOutcome.Refused,
            "PAYMENT_REFUNDED" => WebhookOutcome.Refused,
            "PAYMENT_CHARGEBACK_REQUESTED" => WebhookOutcome.Refused,

            // PAYMENT_CREATED, _UPDATED, _OVERDUE, _AUTHORIZED (só autorizado, ainda
            // não capturado), _AWAITING_RISK_ANALYSIS e os demais não mudam nada aqui.
            _ => WebhookOutcome.Ignored
        };

        return Task.FromResult(new WebhookResult(paymentId, externalRef, outcome));
    }

    private static readonly WebhookResult Ignored = new(string.Empty, null, WebhookOutcome.Ignored);

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
