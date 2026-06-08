namespace Delify.Modules.Orders.Domain;

public enum OrderStatus
{
    PendingPayment = 0,
    AwaitingConfirmation = 1,
    InPreparation = 2,
    InDelivery = 3,
    Delivered = 4,
    Cancelled = 5
}
