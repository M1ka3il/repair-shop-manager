using RepairShop.Domain.Common;
using RepairShop.Domain.Entities;
using RepairShop.Domain.ValueObjects;

namespace RepairShop.Domain.Tests;

/// <summary>Validation of customers and vehicles — the data the UI will now write.</summary>
[TestFixture]
public class MasterDataTests
{
    [Test]
    public void Customer_BlankContactDetails_AreStoredAsUnknown()
    {
        var customer = new Customer("  Anna Becker ", email: "  ", phone: "");

        Assert.Multiple(() =>
        {
            Assert.That(customer.Name, Is.EqualTo("Anna Becker"));
            Assert.That(customer.Email, Is.Null);
            Assert.That(customer.Phone, Is.Null);
        });
    }

    [Test]
    public void Customer_Rename_Blank_Throws()
    {
        var customer = new Customer("Anna Becker", null, null);

        Assert.Throws<DomainException>(() => customer.Rename(" "));
        Assert.That(customer.Name, Is.EqualTo("Anna Becker"));
    }

    [Test]
    public void Vehicle_Update_ReplacesAllMasterData()
    {
        var vehicle = Golf();

        vehicle.Update(LicensePlate.Create("b-xy 99e"), " Volkswagen ", " ID.3 ", 2023);

        Assert.Multiple(() =>
        {
            Assert.That(vehicle.LicensePlate.Value, Is.EqualTo("B-XY 99E"));
            Assert.That(vehicle.Make, Is.EqualTo("Volkswagen"));
            Assert.That(vehicle.Model, Is.EqualTo("ID.3"));
            Assert.That(vehicle.Year, Is.EqualTo(2023));
            Assert.That(vehicle.CustomerId, Is.EqualTo(1), "the owner is not part of an update");
        });
    }

    [TestCase("", "Golf", 2020)]
    [TestCase("VW", " ", 2020)]
    [TestCase("VW", "Golf", 1885)]
    public void Vehicle_Update_InvalidData_ThrowsAndLeavesVehicleUnchanged(string make, string model, int year)
    {
        var vehicle = Golf();

        Assert.Throws<DomainException>(() => vehicle.Update(LicensePlate.Create("M-ZZ 1"), make, model, year));

        Assert.Multiple(() =>
        {
            Assert.That(vehicle.LicensePlate.Value, Is.EqualTo("M-AB 1234"));
            Assert.That(vehicle.Make, Is.EqualTo("VW"));
        });
    }

    [Test]
    public void Vehicle_YearInTheFarFuture_Throws()
    {
        Assert.Throws<DomainException>(() =>
            new Vehicle(1, LicensePlate.Create("M-AB 1234"), "VW", "Golf", DateTime.UtcNow.Year + 2));
    }

    private static Vehicle Golf() => new(1, LicensePlate.Create("M-AB 1234"), "VW", "Golf", 2020);
}
