using Delify.Shared.Domain;

namespace Delify.Modules.Payments.Domain;

public sealed class Payment : Entity
{
    // Pagamento de um pedido (delivery) OU de uma sessão de mesa (comanda). Só um é preenchido.
    public Guid? OrderId { get; private set; }
    public Guid? TableSessionId { get; private set; }
    public string Gateway { get; private set; } = string.Empty;
    public string? GatewayPaymentId { get; private set; }
    public PaymentStatus Status { get; private set; } = PaymentStatus.Pending;
    public decimal Amount { get; private set; }
    public string Method { get; private set; } = string.Empty;
    public string? PixQrCode { get; private set; }
    public string? PixCopyPaste { get; private set; }

    // Conta dividida: cada parte é um Payment próprio (1..ShareCount).
    // Nulos = cobrança única da comanda inteira (ou pagamento de delivery).
    public int? ShareIndex { get; private set; }
    public int? ShareCount { get; private set; }

    private Payment() { }

    public static Payment CreatePix(Guid tenantId, Guid orderId, decimal amount)
    {
        return new Payment
        {
            TenantId = tenantId,
            OrderId = orderId,
            Amount = amount,
            Gateway = "Asaas",
            Method = "PIX"
        };
    }

    public static Payment CreateForSession(Guid tenantId, Guid tableSessionId, decimal amount)
    {
        return new Payment
        {
            TenantId = tenantId,
            TableSessionId = tableSessionId,
            Amount = amount,
            Gateway = "Asaas",
            Method = "PIX"
        };
    }

    /// <summary>Uma das partes de uma comanda dividida. Índice de 1 a <paramref name="shareCount"/>.</summary>
    public static Payment CreateSessionShare(
        Guid tenantId, Guid tableSessionId, decimal amount, int shareIndex, int shareCount)
    {
        if (shareCount < 2) throw new ArgumentOutOfRangeException(nameof(shareCount), "Divisão exige ao menos 2 partes.");
        if (shareIndex < 1 || shareIndex > shareCount)
            throw new ArgumentOutOfRangeException(nameof(shareIndex), "Índice fora do intervalo das partes.");

        return new Payment
        {
            TenantId = tenantId,
            TableSessionId = tableSessionId,
            Amount = amount,
            Gateway = "Asaas",
            Method = "PIX",
            ShareIndex = shareIndex,
            ShareCount = shareCount
        };
    }

    public void ConfirmPayment(string gatewayPaymentId)
    {
        GatewayPaymentId = gatewayPaymentId;
        Status = PaymentStatus.Confirmed;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Invalida uma cobrança que não vai mais ser paga — ex.: a mesa gerou o PIX da
    /// conta inteira e depois optou por dividir. Sai do caminho sem sumir do histórico,
    /// e deixa de contar como pendente no fechamento da comanda.
    /// </summary>
    public void Void()
    {
        if (Status == PaymentStatus.Confirmed)
            throw new InvalidOperationException("Não é possível invalidar um pagamento já confirmado.");
        Status = PaymentStatus.Failed;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SetPixData(string gatewayId, string qrCode, string copyPaste)
    {
        GatewayPaymentId = gatewayId;
        PixQrCode = qrCode;
        PixCopyPaste = copyPaste;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
