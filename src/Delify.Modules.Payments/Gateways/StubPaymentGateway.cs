using Delify.Modules.Payments.Abstractions;
using Microsoft.Extensions.Configuration;

namespace Delify.Modules.Payments.Gateways;

internal sealed class StubPaymentGateway(IConfiguration config) : IPaymentGateway
{
    public string GatewayName => "Stub";

    public Task<PixPaymentResult> CreatePixAsync(PixPaymentRequest request, CancellationToken ct = default)
    {
        var fakeQr = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"fake-pix-qr:{request.OrderId}"));
        var fakeCopyPaste = $"00020126580014BR.GOV.BCB.PIX0136{request.OrderId}5204000053039865802BR5925DELIFY STUB6009SAO PAULO62070503***6304FAKE";

        return Task.FromResult(new PixPaymentResult(
            GatewayId: $"stub_{request.OrderId}",
            QrCode: fakeQr,
            CopyPaste: fakeCopyPaste,
            ExpiresAt: DateTimeOffset.UtcNow.AddHours(24)));
    }

    /// <summary>
    /// Devolve a URL de uma página de checkout falsa servida pela própria API
    /// (ver DevEndpoints). É uma URL de verdade, para a qual o app redireciona
    /// igual faria com a página do Asaas — assim o caminho exercitado em dev é o
    /// mesmo de produção, e não um atalho que só existe no ambiente local.
    /// </summary>
    public Task<CheckoutResult> CreateCheckoutAsync(CheckoutRequest request, CancellationToken ct = default)
    {
        var baseUrl = (config["Dev:PublicApiUrl"] ?? "http://localhost:7000").TrimEnd('/');

        return Task.FromResult(new CheckoutResult(
            GatewayId: $"stub_checkout_{request.PaymentId}",
            Url: $"{baseUrl}/bff/dev/checkout/{request.PaymentId}",
            ExpiresAt: DateTimeOffset.UtcNow.AddMinutes(request.MinutesToExpire)));
    }

    // Sem gateway real não chega callback: em dev a confirmação vem pelos endpoints
    // de simulação e pela página de checkout falsa.
    public Task<WebhookResult> ProcessWebhookAsync(string payload, string signature, CancellationToken ct = default)
        => Task.FromResult(new WebhookResult(string.Empty, null, WebhookOutcome.Ignored));
}
