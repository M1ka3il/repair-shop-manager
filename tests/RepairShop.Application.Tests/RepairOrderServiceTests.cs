using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using RepairShop.Application.Abstractions;
using RepairShop.Application.Common;
using RepairShop.Application.RepairOrders;
using RepairShop.Domain.Common;
using RepairShop.Domain.Entities;
using RepairShop.Domain.ValueObjects;

namespace RepairShop.Application.Tests;

/// <summary>
/// Use-case level tests with mocked repositories: no database, no EF Core.
/// What is verified here is orchestration — that the right thing is loaded, that
/// a save actually happens, and that failures do not save.
/// </summary>
[TestFixture]
public class RepairOrderServiceTests
{
    private static readonly DateTime Now = new(2026, 3, 1, 9, 0, 0, DateTimeKind.Utc);

    private Mock<IRepairOrderRepository> _orders = null!;
    private Mock<IVehicleRepository> _vehicles = null!;
    private Mock<IUnitOfWork> _unitOfWork = null!;
    private RepairOrderService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _orders = new Mock<IRepairOrderRepository>(MockBehavior.Strict);
        _vehicles = new Mock<IVehicleRepository>(MockBehavior.Strict);
        _unitOfWork = new Mock<IUnitOfWork>();

        var clock = new Mock<IClock>();
        clock.SetupGet(c => c.UtcNow).Returns(Now);

        _service = new RepairOrderService(
            _orders.Object,
            _vehicles.Object,
            _unitOfWork.Object,
            clock.Object,
            NullLogger<RepairOrderService>.Instance);
    }

    [Test]
    public void GetAsync_UnknownId_ThrowsNotFound()
    {
        _orders.Setup(r => r.GetByIdAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RepairOrder?)null);

        Assert.ThrowsAsync<NotFoundException>(() => _service.GetAsync(42));
    }

    [Test]
    public async Task CreateAsync_ValidVehicle_AddsOrderAndSavesOnce()
    {
        _vehicles.Setup(r => r.GetByIdAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Vehicle());
        _orders.Setup(r => r.Add(It.IsAny<RepairOrder>()));

        var result = await _service.CreateAsync(new CreateRepairOrderRequest
        {
            VehicleId = 7,
            Description = "Bremsen quietschen"
        });

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(RepairOrderStatus.Received));
            Assert.That(result.ReceivedAtUtc, Is.EqualTo(Now), "the injected clock must be the only time source");
            Assert.That(result.Description, Is.EqualTo("Bremsen quietschen"));
        });

        _orders.Verify(r => r.Add(It.IsAny<RepairOrder>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// A dangling vehicle id must surface as 404 rather than reaching the
    /// database and coming back as a foreign key violation.
    /// </summary>
    [Test]
    public void CreateAsync_UnknownVehicle_ThrowsNotFoundAndDoesNotSave()
    {
        _vehicles.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Vehicle?)null);

        Assert.ThrowsAsync<NotFoundException>(() => _service.CreateAsync(new CreateRepairOrderRequest
        {
            VehicleId = 99,
            Description = "Irgendwas"
        }));

        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task ChangeStatusAsync_AllowedTransition_SavesAndReturnsNewStatus()
    {
        var order = OrderInStatus(RepairOrderStatus.Received);
        _orders.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(order);

        var result = await _service.ChangeStatusAsync(1, new ChangeStatusRequest
        {
            TargetStatus = RepairOrderStatus.InProgress
        });

        Assert.That(result.Status, Is.EqualTo(RepairOrderStatus.InProgress));
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// The important negative case: the domain rejects the transition, and the
    /// service must not persist anything on the way out.
    /// </summary>
    [Test]
    public void ChangeStatusAsync_ForbiddenTransition_ThrowsDomainExceptionAndDoesNotSave()
    {
        var order = OrderInStatus(RepairOrderStatus.Invoiced);
        _orders.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(order);

        Assert.ThrowsAsync<DomainException>(() => _service.ChangeStatusAsync(1, new ChangeStatusRequest
        {
            TargetStatus = RepairOrderStatus.InProgress
        }));

        Assert.That(order.Status, Is.EqualTo(RepairOrderStatus.Invoiced));
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task AddItemAsync_ReturnsOrderWithRecalculatedTotal()
    {
        var order = OrderInStatus(RepairOrderStatus.InProgress);
        _orders.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(order);

        var result = await _service.AddItemAsync(1, new AddRepairItemRequest
        {
            Type = RepairItemType.Labour,
            Designation = "Diagnose",
            UnitPrice = 89.00m,
            Quantity = 2m
        });

        Assert.Multiple(() =>
        {
            Assert.That(result.Items, Has.Count.EqualTo(1));
            Assert.That(result.TotalPrice, Is.EqualTo(178.00m));
        });
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task ListAsync_PassesStatusFilterThrough()
    {
        _orders.Setup(r => r.ListAsync(RepairOrderStatus.WaitingForParts, It.IsAny<CancellationToken>()))
            .ReturnsAsync([OrderInStatus(RepairOrderStatus.WaitingForParts)]);

        var result = await _service.ListAsync(RepairOrderStatus.WaitingForParts);

        Assert.That(result, Has.Count.EqualTo(1));
        _orders.VerifyAll();
    }

    [Test]
    public async Task DeleteAsync_RemovesAndSaves()
    {
        var order = OrderInStatus(RepairOrderStatus.Received);
        _orders.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        _orders.Setup(r => r.Remove(order));

        await _service.DeleteAsync(1);

        _orders.Verify(r => r.Remove(order), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    private static Vehicle Vehicle() =>
        new(customerId: 1, LicensePlate.Create("M-AB 1234"), "Opel", "Corsa F", 2021);

    private static RepairOrder OrderInStatus(RepairOrderStatus status)
    {
        var order = new RepairOrder(7, "Bremsen quietschen", Now.AddDays(-1));

        if (status == RepairOrderStatus.Received)
        {
            return order;
        }

        order.TransitionTo(RepairOrderStatus.InProgress, Now);

        switch (status)
        {
            case RepairOrderStatus.InProgress:
                break;
            case RepairOrderStatus.WaitingForParts:
                order.TransitionTo(RepairOrderStatus.WaitingForParts, Now);
                break;
            case RepairOrderStatus.Completed:
                order.TransitionTo(RepairOrderStatus.Completed, Now);
                break;
            case RepairOrderStatus.Invoiced:
                order.TransitionTo(RepairOrderStatus.Completed, Now);
                order.TransitionTo(RepairOrderStatus.Invoiced, Now);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(status), status, "Unhandled status");
        }

        return order;
    }
}
