using Delify.Modules.Payments.Abstractions;

namespace Delify.Modules.Payments.Gateways;

internal sealed class StubPaymentGateway : IPaymentGateway
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

    public Task<WebhookResult> ProcessWebhookAsync(string payload, string signature, CancellationToken ct = default)
        => Task.FromResult(new WebhookResult(string.Empty, false));
}
