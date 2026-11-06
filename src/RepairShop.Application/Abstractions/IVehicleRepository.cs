using RepairShop.Domain.Entities;
using RepairShop.Domain.ValueObjects;

namespace RepairShop.Application.Abstractions;

public interface IVehicleRepository
{
    Task<Vehicle?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Vehicle>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// True if another vehicle already carries <paramref name="plate"/>.
    /// <paramref name="excludingVehicleId"/> lets an update keep its own plate.
    /// </summary>
    Task<bool> PlateExistsAsync(
        LicensePlate plate,
        int? excludingVehicleId = null,
        CancellationToken cancellationToken = default);

    void Add(Vehicle vehicle);
}
