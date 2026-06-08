using Delify.Shared.Domain;

namespace Delify.Modules.Delivery.Domain;

public sealed class DeliveryOrder : Entity
{
    public Guid OrderId { get; private set; }
    public string Provider { get; private set; } = string.Empty;
    public string? ProviderOrderId { get; private set; }
    public DeliveryStatus Status { get; private set; } = DeliveryStatus.Quoted;
    public decimal QuotedPrice { get; private set; }
    public string PickupAddress { get; private set; } = string.Empty;
    public string DropoffAddress { get; private set; } = string.Empty;
    public string? TrackingUrl { get; private set; }

    private DeliveryOrder() { }

    public static DeliveryOrder Create(Guid tenantId, Guid orderId, string provider,
        string pickupAddress, string dropoffAddress, decimal quotedPrice)
    {
        return new DeliveryOrder
        {
            TenantId = tenantId,
            OrderId = orderId,
            Provider = provider,
            PickupAddress = pickupAddress,
            DropoffAddress = dropoffAddress,
            QuotedPrice = quotedPrice
        };
    }

    public void Dispatch(string providerOrderId, string? trackingUrl)
    {
        ProviderOrderId = providerOrderId;
        TrackingUrl = trackingUrl;
        Status = DeliveryStatus.Dispatched;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
