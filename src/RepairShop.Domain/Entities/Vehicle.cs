using RepairShop.Domain.Common;
using RepairShop.Domain.ValueObjects;

namespace RepairShop.Domain.Entities;

/// <summary>A car brought in for repair, owned by exactly one <see cref="Customer"/>.</summary>
public class Vehicle
{
    private readonly List<RepairOrder> _repairOrders = [];

    private Vehicle()
    {
        // EF Core materialisation.
        LicensePlate = null!;
        Make = null!;
        Model = null!;
    }

    public Vehicle(int customerId, LicensePlate licensePlate, string make, string model, int year)
    {
        if (string.IsNullOrWhiteSpace(make))
        {
            throw new DomainException("Vehicle make must not be empty.");
        }

        if (string.IsNullOrWhiteSpace(model))
        {
            throw new DomainException("Vehicle model must not be empty.");
        }

        // 1886: Benz Patent-Motorwagen. Anything earlier is a data entry error.
        if (year < 1886 || year > DateTime.UtcNow.Year + 1)
        {
            throw new DomainException($"Year {year} is out of range.");
        }

        CustomerId = customerId;
        LicensePlate = licensePlate;
        Make = make.Trim();
        Model = model.Trim();
        Year = year;
    }

    public int Id { get; private set; }

    public int CustomerId { get; private set; }

    public Customer? Customer { get; private set; }

    public LicensePlate LicensePlate { get; private set; }

    public string Make { get; private set; }

    public string Model { get; private set; }

    public int Year { get; private set; }

    public IReadOnlyCollection<RepairOrder> RepairOrders => _repairOrders.AsReadOnly();
}
