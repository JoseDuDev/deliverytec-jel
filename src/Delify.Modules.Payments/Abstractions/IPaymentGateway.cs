namespace Delify.Modules.Payments.Abstractions;

public record PixPaymentRequest(Guid OrderId, decimal Amount, string CustomerCpf, string CustomerName);
public record PixPaymentResult(string GatewayId, string QrCode, string CopyPaste, DateTimeOffset ExpiresAt);
public record WebhookResult(string GatewayPaymentId, bool IsConfirmed);

public interface IPaymentGateway
{
    string GatewayName { get; }
    Task<PixPaymentResult> CreatePixAsync(PixPaymentRequest request, CancellationToken ct = default);
    Task<WebhookResult> ProcessWebhookAsync(string payload, string signature, CancellationToken ct = default);
}
