using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using RepairShop.Application.Abstractions;
using RepairShop.Application.Common;
using RepairShop.Application.Customers;
using RepairShop.Domain.Common;
using RepairShop.Domain.Entities;
using RepairShop.Domain.ValueObjects;

namespace RepairShop.Application.Tests;

/// <summary>
/// Orchestration of the master-data writes: what is checked before saving, and
/// that a rejected request saves nothing.
/// </summary>
[TestFixture]
public class CustomerServiceTests
{
    private Mock<ICustomerRepository> _customers = null!;
    private Mock<IVehicleRepository> _vehicles = null!;
    private Mock<IUnitOfWork> _unitOfWork = null!;
    private CustomerService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _customers = new Mock<ICustomerRepository>(MockBehavior.Strict);
        _vehicles = new Mock<IVehicleRepository>(MockBehavior.Strict);
        _unitOfWork = new Mock<IUnitOfWork>();

        _service = new CustomerService(
            _customers.Object,
            _vehicles.Object,
            _unitOfWork.Object,
            NullLogger<CustomerService>.Instance);
    }

    [Test]
    public async Task CreateCustomerAsync_AddsAndSavesOnce()
    {
        _customers.Setup(r => r.Add(It.IsAny<Customer>()));

        var result = await _service.CreateCustomerAsync(new SaveCustomerRequest
        {
            Name = "Anna Becker",
            Email = "anna@example.com"
        });

        Assert.That(result.Name, Is.EqualTo("Anna Becker"));
        _customers.Verify(r => r.Add(It.IsAny<Customer>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public void AddVehicleAsync_UnknownCustomer_ThrowsNotFoundAndDoesNotSave()
    {
        _customers.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null);

        Assert.ThrowsAsync<NotFoundException>(() => _service.AddVehicleAsync(99, ValidVehicle()));

        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public void AddVehicleAsync_PlateTaken_ThrowsConflictAndDoesNotSave()
    {
        _customers.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Customer("Anna Becker", null, null));
        _vehicles.Setup(r => r.PlateExistsAsync(LicensePlate.Create("M-AB 1234"), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var ex = Assert.ThrowsAsync<ConflictException>(() => _service.AddVehicleAsync(1, ValidVehicle()));

        Assert.That(ex.Message, Does.Contain("M-AB 1234"));
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public void AddVehicleAsync_MalformedPlate_ThrowsDomainExceptionBeforeAnyLookup()
    {
        _customers.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Customer("Anna Becker", null, null));

        Assert.ThrowsAsync<DomainException>(() =>
            _service.AddVehicleAsync(1, new SaveVehicleRequest
            {
                LicensePlate = "kein kennzeichen",
                Make = "VW",
                Model = "Golf",
                Year = 2020
            }));

        // Strict mock: PlateExistsAsync has no setup, so reaching it would fail the test.
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>A vehicle keeping its own plate is not a conflict with itself.</summary>
    [Test]
    public async Task UpdateVehicleAsync_ExcludesItselfFromThePlateCheck()
    {
        var vehicle = new Vehicle(1, LicensePlate.Create("M-AB 1234"), "VW", "Golf", 2020);
        _vehicles.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(vehicle);
        _vehicles.Setup(r => r.PlateExistsAsync(LicensePlate.Create("M-AB 1234"), 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _service.UpdateVehicleAsync(5, new SaveVehicleRequest
        {
            LicensePlate = "M-AB 1234",
            Make = "VW",
            Model = "Golf Variant",
            Year = 2020
        });

        Assert.That(result.Model, Is.EqualTo("Golf Variant"));
        _vehicles.VerifyAll();
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    private static SaveVehicleRequest ValidVehicle() => new()
    {
        LicensePlate = "m-ab 1234",
        Make = "VW",
        Model = "Golf",
        Year = 2020
    };
}
