using RepairShop.Application.Auth;
using RepairShop.Domain.Entities;

namespace RepairShop.Application.Tests;

/// <summary>The full role × target matrix, so a change to the table is a visible test change.</summary>
[TestFixture]
public class StatusChangePolicyTests
{
    [TestCase(RepairOrderStatus.InProgress, true)]
    [TestCase(RepairOrderStatus.WaitingForParts, true)]
    [TestCase(RepairOrderStatus.Completed, true)]
    [TestCase(RepairOrderStatus.Invoiced, false)]
    [TestCase(RepairOrderStatus.Received, false)]
    public void Mechanic_MovesWorkAlong_ButDoesNotBill(RepairOrderStatus target, bool expected)
    {
        Assert.That(StatusChangePolicy.MayMoveTo(new FakeCurrentUser(Roles.Mechanic), target), Is.EqualTo(expected));
    }

    [Test]
    public void Office_MayMakeEveryMove()
    {
        var office = new FakeCurrentUser(Roles.Office);

        Assert.That(Enum.GetValues<RepairOrderStatus>().All(t => StatusChangePolicy.MayMoveTo(office, t)), Is.True);
    }

    [Test]
    public void NoRole_MayMakeNoMove()
    {
        var nobody = new FakeCurrentUser();

        Assert.That(Enum.GetValues<RepairOrderStatus>().Any(t => StatusChangePolicy.MayMoveTo(nobody, t)), Is.False);
    }
}
