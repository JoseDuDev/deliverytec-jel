namespace Delify.Modules.Payments.Abstractions;

public record PixPaymentRequest(Guid OrderId, decimal Amount, string CustomerCpf, string CustomerName);
public record PixPaymentResult(string GatewayId, string QrCode, string CopyPaste, DateTimeOffset ExpiresAt);

/// <summary>
/// Cobrança numa página hospedada pelo gateway, onde o pagador escolhe PIX ou
/// cartão e digita os dados do cartão fora da nossa aplicação — é o que mantém
/// o Delify fora do escopo PCI.
/// </summary>
/// <param name="PaymentId">Nosso <c>Payment.Id</c>. Vai como externalReference e é o que
/// correlaciona o webhook de volta: a resposta do checkout traz o id da SESSÃO, não o do
/// pagamento, então o <c>payment.id</c> que chega no callback é desconhecido até então.</param>
public record CheckoutRequest(
    Guid PaymentId, decimal Amount, string Description,
    string CustomerCpf, string CustomerName, int MinutesToExpire = 30);

public record CheckoutResult(string GatewayId, string Url, DateTimeOffset ExpiresAt);

public enum WebhookOutcome
{
    /// <summary>Evento irrelevante para nós (criação, atualização, vencimento…).</summary>
    Ignored,
    /// <summary>Pagamento processado. É aqui que a comanda pode ser dada como paga.</summary>
    Confirmed,
    /// <summary>Recusado/estornado — a cobrança volta a ser devida e pode ser retentada.</summary>
    Refused
}

/// <param name="ExternalReference">O que enviamos como externalReference (nosso Payment.Id).
/// Único caminho de correlação no fluxo de checkout hospedado.</param>
public record WebhookResult(
    string GatewayPaymentId, string? ExternalReference, WebhookOutcome Outcome)
{
    public bool IsConfirmed => Outcome == WebhookOutcome.Confirmed;
}

public interface IPaymentGateway
{
    string GatewayName { get; }
    Task<PixPaymentResult> CreatePixAsync(PixPaymentRequest request, CancellationToken ct = default);
    Task<CheckoutResult> CreateCheckoutAsync(CheckoutRequest request, CancellationToken ct = default);
    Task<WebhookResult> ProcessWebhookAsync(string payload, string signature, CancellationToken ct = default);
}
