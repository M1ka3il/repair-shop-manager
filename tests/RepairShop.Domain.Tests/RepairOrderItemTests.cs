using RepairShop.Domain.Common;
using RepairShop.Domain.Entities;

namespace RepairShop.Domain.Tests;

/// <summary>
/// Line items are only reachable through the aggregate root, so the rules about
/// when they may change are tested here too.
/// </summary>
[TestFixture]
public class RepairOrderItemTests
{
    private static readonly DateTime Now = new(2026, 3, 1, 9, 0, 0, DateTimeKind.Utc);

    [Test]
    public void TotalPrice_SumsLabourAndParts()
    {
        var order = InProgressOrder();

        order.AddItem(RepairItemType.Labour, "Bremsbeläge wechseln", unitPrice: 89.00m, quantity: 1.5m);
        order.AddItem(RepairItemType.Part, "Bremsbelagsatz", unitPrice: 74.90m, quantity: 2m);

        // 133.50 labour + 149.80 parts
        Assert.That(order.TotalPrice, Is.EqualTo(283.30m));
    }

    [Test]
    public void TotalPrice_OfEmptyOrder_IsZero()
    {
        Assert.That(InProgressOrder().TotalPrice, Is.Zero);
    }

    [TestCase(RepairOrderStatus.Completed)]
    [TestCase(RepairOrderStatus.Invoiced)]
    public void AddItem_AfterWorkIsFinished_Throws(RepairOrderStatus status)
    {
        var order = InProgressOrder();
        order.TransitionTo(RepairOrderStatus.Completed, Now);

        if (status == RepairOrderStatus.Invoiced)
        {
            order.TransitionTo(RepairOrderStatus.Invoiced, Now);
        }

        Assert.Throws<DomainException>(() => order.AddItem(RepairItemType.Part, "Nachträglich", 10m, 1m));
    }

    [Test]
    public void RemoveItem_OnInvoicedOrder_Throws()
    {
        var order = InProgressOrder();
        var item = order.AddItem(RepairItemType.Labour, "Diagnose", 89m, 1m);
        order.TransitionTo(RepairOrderStatus.Completed, Now);
        order.TransitionTo(RepairOrderStatus.Invoiced, Now);

        Assert.Throws<DomainException>(() => order.RemoveItem(item.Id));
        Assert.That(order.Items, Has.Count.EqualTo(1));
    }

    [Test]
    public void RemoveItem_UnknownId_Throws()
    {
        var order = InProgressOrder();

        Assert.Throws<DomainException>(() => order.RemoveItem(itemId: 999));
    }

    [TestCase(-1)]
    public void AddItem_NegativeUnitPrice_Throws(decimal unitPrice)
    {
        var order = InProgressOrder();

        Assert.Throws<DomainException>(() => order.AddItem(RepairItemType.Part, "Teil", unitPrice, 1m));
    }

    [TestCase(0)]
    [TestCase(-2)]
    public void AddItem_NonPositiveQuantity_Throws(decimal quantity)
    {
        var order = InProgressOrder();

        Assert.Throws<DomainException>(() => order.AddItem(RepairItemType.Part, "Teil", 10m, quantity));
    }

    [Test]
    public void AddItem_BlankDesignation_Throws()
    {
        var order = InProgressOrder();

        Assert.Throws<DomainException>(() => order.AddItem(RepairItemType.Labour, "   ", 10m, 1m));
    }

    [Test]
    public void UpdateDescription_OnInvoicedOrder_Throws()
    {
        var order = InProgressOrder();
        order.TransitionTo(RepairOrderStatus.Completed, Now);
        order.TransitionTo(RepairOrderStatus.Invoiced, Now);

        Assert.Throws<DomainException>(() => order.UpdateDescription("Doch etwas anderes"));
    }

    [TestCase(RepairOrderStatus.Received)]
    [TestCase(RepairOrderStatus.InProgress)]
    [TestCase(RepairOrderStatus.WaitingForParts)]
    public void EnsureCanBeDeleted_WhileWorkIsOpen_DoesNotThrow(RepairOrderStatus status)
    {
        var order = OrderIn(status);

        Assert.DoesNotThrow(order.EnsureCanBeDeleted);
    }

    /// <summary>
    /// Deleting billable work is worse than editing it, and editing is already
    /// forbidden — so deletion must be blocked at least as strictly.
    /// </summary>
    [TestCase(RepairOrderStatus.Completed)]
    [TestCase(RepairOrderStatus.Invoiced)]
    public void EnsureCanBeDeleted_AfterWorkIsFinished_Throws(RepairOrderStatus status)
    {
        var order = OrderIn(status);

        var ex = Assert.Throws<DomainException>(order.EnsureCanBeDeleted);
        Assert.That(ex.Message, Does.Contain(status.ToString()));
    }

    [Test]
    public void Constructor_BlankDescription_Throws()
    {
        Assert.Throws<DomainException>(() => new RepairOrder(1, "  ", Now));
    }

    private static RepairOrder OrderIn(RepairOrderStatus status)
    {
        var order = new RepairOrder(1, "Bremsen quietschen", Now.AddDays(-1));
        RepairOrderStatus[] path = status switch
        {
            RepairOrderStatus.Received => [],
            RepairOrderStatus.InProgress => [RepairOrderStatus.InProgress],
            RepairOrderStatus.WaitingForParts => [RepairOrderStatus.InProgress, RepairOrderStatus.WaitingForParts],
            RepairOrderStatus.Completed => [RepairOrderStatus.InProgress, RepairOrderStatus.Completed],
            RepairOrderStatus.Invoiced =>
                [RepairOrderStatus.InProgress, RepairOrderStatus.Completed, RepairOrderStatus.Invoiced],
            _ => throw new ArgumentOutOfRangeException(nameof(status))
        };

        foreach (var step in path)
        {
            order.TransitionTo(step, Now);
        }

        return order;
    }

    private static RepairOrder InProgressOrder()
    {
        var order = new RepairOrder(1, "Bremsen quietschen", Now.AddDays(-1));
        order.TransitionTo(RepairOrderStatus.InProgress, Now);
        return order;
    }
}
