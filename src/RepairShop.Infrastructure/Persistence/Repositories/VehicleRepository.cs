using Microsoft.EntityFrameworkCore;
using RepairShop.Application.Abstractions;
using RepairShop.Domain.Entities;
using RepairShop.Domain.ValueObjects;

namespace RepairShop.Infrastructure.Persistence.Repositories;

/// <inheritdoc cref="IVehicleRepository"/>
public class VehicleRepository(RepairShopDbContext db) : IVehicleRepository
{
    // Tracked: the same load serves reads and updates, as in RepairOrderRepository.
    public Task<Vehicle?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        db.Vehicles
            .Include(v => v.Customer)
            .SingleOrDefaultAsync(v => v.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Vehicle>> ListAsync(CancellationToken cancellationToken = default) =>
        await db.Vehicles
            .Include(v => v.Customer)
            .AsNoTracking()
            .OrderBy(v => v.Id)
            .ToListAsync(cancellationToken);

    // The comparison goes through the LicensePlate value conversion, so it is
    // translated to SQL against the normalised column value.
    public Task<bool> PlateExistsAsync(
        LicensePlate plate,
        int? excludingVehicleId = null,
        CancellationToken cancellationToken = default) =>
        db.Vehicles.AnyAsync(
            v => v.LicensePlate == plate && (excludingVehicleId == null || v.Id != excludingVehicleId),
            cancellationToken);

    public void Add(Vehicle vehicle) => db.Vehicles.Add(vehicle);
}
