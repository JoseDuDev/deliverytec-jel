using Delify.Modules.Orders.Domain.Events;
using Delify.Shared.Domain;

namespace Delify.Modules.Orders.Domain;

public sealed class Order : AggregateRoot
{
    public Guid EstablishmentId { get; private set; }
    public Guid? CustomerId { get; private set; }
    public OrderStatus Status { get; private set; } = OrderStatus.PendingPayment;
    public string? CustomerNote { get; internal set; }
    public ICollection<OrderItem> Items { get; private set; } = [];
    public decimal DeliveryFee { get; private set; }
    public decimal Total => Items.Sum(i => i.Total) + DeliveryFee;

    private Order() { }

    public static Order Create(Guid tenantId, Guid establishmentId, decimal deliveryFee = 0, Guid? customerId = null)
    {
        return new Order { TenantId = tenantId, EstablishmentId = establishmentId, DeliveryFee = deliveryFee >= 0 ? deliveryFee : 0, CustomerId = customerId };
    }

    public void AddItem(Guid productId, string productName, int quantity, decimal unitPrice)
    {
        var item = OrderItem.Create(TenantId, Id, productId, productName, quantity, unitPrice);
        Items.Add(item);
    }

    public void Confirm()
    {
        if (Status != OrderStatus.PendingPayment)
            throw new InvalidOperationException("Order must be in PendingPayment state to confirm.");
        Status = OrderStatus.AwaitingConfirmation;
        UpdatedAt = DateTimeOffset.UtcNow;
        Raise(new OrderCreatedEvent(Guid.NewGuid(), DateTimeOffset.UtcNow, Id, TenantId, Total));
    }

    public void Accept() => Transition(OrderStatus.AwaitingConfirmation, OrderStatus.InPreparation);
    public void StartDelivery() => Transition(OrderStatus.InPreparation, OrderStatus.InDelivery);
    public void Complete() => Transition(OrderStatus.InDelivery, OrderStatus.Delivered);
    public void Cancel() { Status = OrderStatus.Cancelled; UpdatedAt = DateTimeOffset.UtcNow; }

    private void Transition(OrderStatus from, OrderStatus to)
    {
        if (Status != from)
            throw new InvalidOperationException($"Cannot transition from {Status} to {to}. Expected {from}.");
        Status = to;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
