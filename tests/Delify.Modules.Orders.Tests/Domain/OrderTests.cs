using Delify.Modules.Orders.Domain;
using Delify.Modules.Orders.Domain.Events;
using FluentAssertions;

namespace Delify.Modules.Orders.Tests.Domain;

public class OrderTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid EstablishmentId = Guid.NewGuid();
    private static readonly Guid ProductId = Guid.NewGuid();

    [Fact]
    public void Create_NewOrder_HasPendingPaymentStatus()
    {
        var order = Order.Create(TenantId, EstablishmentId);

        order.Status.Should().Be(OrderStatus.PendingPayment);
        order.Items.Should().BeEmpty();
        order.Total.Should().Be(0);
    }

    [Fact]
    public void AddItem_IncreasesTotalCorrectly()
    {
        var order = Order.Create(TenantId, EstablishmentId);
        order.AddItem(ProductId, "X-Burguer", 2, 29.90m);

        order.Items.Should().HaveCount(1);
        order.Total.Should().Be(59.80m);
    }

    [Fact]
    public void Confirm_TransitionsToAwaitingConfirmation()
    {
        var order = Order.Create(TenantId, EstablishmentId);
        order.AddItem(ProductId, "X-Burguer", 1, 29.90m);
        order.Confirm();

        order.Status.Should().Be(OrderStatus.AwaitingConfirmation);
    }

    [Fact]
    public void Confirm_RaisesOrderCreatedEvent()
    {
        var order = Order.Create(TenantId, EstablishmentId);
        order.AddItem(ProductId, "X-Burguer", 1, 29.90m);
        order.Confirm();

        order.DomainEvents.Should().HaveCount(1);
        order.DomainEvents[0].Should().BeOfType<OrderCreatedEvent>();
    }

    [Fact]
    public void Accept_FromWrongState_ThrowsInvalidOperationException()
    {
        var order = Order.Create(TenantId, EstablishmentId);

        var act = () => order.Accept();
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void FullHappyPath_TransitionsCorrectly()
    {
        var order = Order.Create(TenantId, EstablishmentId);
        order.AddItem(ProductId, "X-Burguer", 1, 29.90m);

        order.Confirm();
        order.Status.Should().Be(OrderStatus.AwaitingConfirmation);

        order.Accept();
        order.Status.Should().Be(OrderStatus.InPreparation);

        order.StartDelivery();
        order.Status.Should().Be(OrderStatus.InDelivery);

        order.Complete();
        order.Status.Should().Be(OrderStatus.Delivered);
    }
}
