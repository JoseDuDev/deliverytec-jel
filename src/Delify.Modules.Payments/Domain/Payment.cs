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

    public void ConfirmPayment(string gatewayPaymentId)
    {
        GatewayPaymentId = gatewayPaymentId;
        Status = PaymentStatus.Confirmed;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SetPixData(string gatewayId, string qrCode, string copyPaste)
    {
        GatewayPaymentId = gatewayId;
        PixQrCode = qrCode;
        PixCopyPaste = copyPaste;
    }
}
