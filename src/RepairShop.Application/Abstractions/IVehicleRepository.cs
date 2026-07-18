using RepairShop.Domain.Entities;

namespace RepairShop.Application.Abstractions;

public interface IVehicleRepository
{
    Task<Vehicle?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Vehicle>> ListAsync(CancellationToken cancellationToken = default);
}
