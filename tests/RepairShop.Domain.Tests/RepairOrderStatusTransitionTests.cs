using RepairShop.Domain.Common;
using RepairShop.Domain.Entities;

namespace RepairShop.Domain.Tests;

/// <summary>
/// The heart of the test suite. These run without a database, which is exactly
/// why the transition rules live in the domain and not in a controller.
/// </summary>
[TestFixture]
public class RepairOrderStatusTransitionTests
{
    private static readonly DateTime Now = new(2026, 3, 1, 9, 0, 0, DateTimeKind.Utc);

    [Test]
    public void NewOrder_StartsAsReceived()
    {
        var order = NewOrder();

        Assert.Multiple(() =>
        {
            Assert.That(order.Status, Is.EqualTo(RepairOrderStatus.Received));
            Assert.That(order.CompletedAtUtc, Is.Null);
            Assert.That(order.Items, Is.Empty);
        });
    }

    [TestCase(RepairOrderStatus.Received, RepairOrderStatus.InProgress)]
    [TestCase(RepairOrderStatus.InProgress, RepairOrderStatus.WaitingForParts)]
    [TestCase(RepairOrderStatus.InProgress, RepairOrderStatus.Completed)]
    [TestCase(RepairOrderStatus.WaitingForParts, RepairOrderStatus.InProgress)]
    [TestCase(RepairOrderStatus.Completed, RepairOrderStatus.Invoiced)]
    public void TransitionTo_AllowedTransition_ChangesStatus(RepairOrderStatus from, RepairOrderStatus to)
    {
        var order = OrderInStatus(from);

        order.TransitionTo(to, Now);

        Assert.That(order.Status, Is.EqualTo(to));
    }

    [TestCase(RepairOrderStatus.Received, RepairOrderStatus.Completed)]
    [TestCase(RepairOrderStatus.Received, RepairOrderStatus.WaitingForParts)]
    [TestCase(RepairOrderStatus.Received, RepairOrderStatus.Invoiced)]
    [TestCase(RepairOrderStatus.InProgress, RepairOrderStatus.Received)]
    [TestCase(RepairOrderStatus.InProgress, RepairOrderStatus.Invoiced)]
    [TestCase(RepairOrderStatus.WaitingForParts, RepairOrderStatus.Completed)]
    [TestCase(RepairOrderStatus.WaitingForParts, RepairOrderStatus.Invoiced)]
    [TestCase(RepairOrderStatus.Completed, RepairOrderStatus.InProgress)]
    [TestCase(RepairOrderStatus.Completed, RepairOrderStatus.Received)]
    public void TransitionTo_ForbiddenTransition_Throws(RepairOrderStatus from, RepairOrderStatus to)
    {
        var order = OrderInStatus(from);

        Assert.Throws<DomainException>(() => order.TransitionTo(to, Now));
        Assert.That(order.Status, Is.EqualTo(from), "a rejected transition must not change the order");
    }

    /// <summary>
    /// The rule from the brief: an invoiced order is final. This is the
    /// regression test that keeps someone from "just" adding a back edge later.
    /// </summary>
    [TestCase(RepairOrderStatus.Received)]
    [TestCase(RepairOrderStatus.InProgress)]
    [TestCase(RepairOrderStatus.WaitingForParts)]
    [TestCase(RepairOrderStatus.Completed)]
    public void TransitionTo_FromInvoiced_IsAlwaysRejected(RepairOrderStatus target)
    {
        var order = OrderInStatus(RepairOrderStatus.Invoiced);

        var ex = Assert.Throws<DomainException>(() => order.TransitionTo(target, Now));

        Assert.Multiple(() =>
        {
            Assert.That(order.Status, Is.EqualTo(RepairOrderStatus.Invoiced));
            Assert.That(ex!.Message, Does.Contain("Invoiced"));
        });
    }

    [Test]
    public void TransitionTo_SameStatus_Throws()
    {
        var order = OrderInStatus(RepairOrderStatus.InProgress);

        var ex = Assert.Throws<DomainException>(() => order.TransitionTo(RepairOrderStatus.InProgress, Now));

        Assert.That(ex!.Message, Does.Contain("already"));
    }

    [Test]
    public void TransitionTo_Completed_StampsCompletionTime()
    {
        var order = OrderInStatus(RepairOrderStatus.InProgress);

        order.TransitionTo(RepairOrderStatus.Completed, Now);

        Assert.That(order.CompletedAtUtc, Is.EqualTo(Now));
    }

    [Test]
    public void TransitionTo_Invoiced_KeepsOriginalCompletionTime()
    {
        var order = OrderInStatus(RepairOrderStatus.InProgress);
        order.TransitionTo(RepairOrderStatus.Completed, Now);

        order.TransitionTo(RepairOrderStatus.Invoiced, Now.AddDays(2));

        Assert.That(order.CompletedAtUtc, Is.EqualTo(Now), "invoicing must not rewrite when the work finished");
    }

    [Test]
    public void CanTransitionTo_ReportsTheSameAnswerAsTransitionTo()
    {
        var order = OrderInStatus(RepairOrderStatus.WaitingForParts);

        Assert.Multiple(() =>
        {
            Assert.That(order.CanTransitionTo(RepairOrderStatus.InProgress), Is.True);
            Assert.That(order.CanTransitionTo(RepairOrderStatus.Completed), Is.False);
        });
    }

    [Test]
    public void FullHappyPath_ReachesInvoiced()
    {
        var order = NewOrder();

        order.TransitionTo(RepairOrderStatus.InProgress, Now);
        order.TransitionTo(RepairOrderStatus.WaitingForParts, Now.AddHours(1));
        order.TransitionTo(RepairOrderStatus.InProgress, Now.AddDays(1));
        order.TransitionTo(RepairOrderStatus.Completed, Now.AddDays(2));
        order.TransitionTo(RepairOrderStatus.Invoiced, Now.AddDays(3));

        Assert.That(order.Status, Is.EqualTo(RepairOrderStatus.Invoiced));
    }

    private static RepairOrder NewOrder() =>
        new(vehicleId: 1, description: "Bremsen quietschen", receivedAtUtc: Now.AddDays(-1));

    /// <summary>Walks an order to <paramref name="status"/> along legal transitions only.</summary>
    private static RepairOrder OrderInStatus(RepairOrderStatus status)
    {
        var order = NewOrder();

        if (status == RepairOrderStatus.Received)
        {
            return order;
        }

        order.TransitionTo(RepairOrderStatus.InProgress, Now);

        switch (status)
        {
            case RepairOrderStatus.InProgress:
                return order;

            case RepairOrderStatus.WaitingForParts:
                order.TransitionTo(RepairOrderStatus.WaitingForParts, Now);
                return order;

            case RepairOrderStatus.Completed:
                order.TransitionTo(RepairOrderStatus.Completed, Now);
                return order;

            case RepairOrderStatus.Invoiced:
                order.TransitionTo(RepairOrderStatus.Completed, Now);
                order.TransitionTo(RepairOrderStatus.Invoiced, Now);
                return order;

            default:
                throw new ArgumentOutOfRangeException(nameof(status), status, "Unhandled status");
        }
    }
}
