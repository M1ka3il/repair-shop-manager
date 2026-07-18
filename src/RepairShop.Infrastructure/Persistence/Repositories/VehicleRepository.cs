using Microsoft.EntityFrameworkCore;
using RepairShop.Application.Abstractions;
using RepairShop.Domain.Entities;

namespace RepairShop.Infrastructure.Persistence.Repositories;

/// <inheritdoc cref="IVehicleRepository"/>
public class VehicleRepository(RepairShopDbContext db) : IVehicleRepository
{
    public Task<Vehicle?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        db.Vehicles
            .Include(v => v.Customer)
            .AsNoTracking()
            .SingleOrDefaultAsync(v => v.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Vehicle>> ListAsync(CancellationToken cancellationToken = default) =>
        await db.Vehicles
            .Include(v => v.Customer)
            .AsNoTracking()
            .OrderBy(v => v.Id)
            .ToListAsync(cancellationToken);
}
