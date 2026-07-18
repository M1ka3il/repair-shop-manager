using RepairShop.Domain.Entities;

namespace RepairShop.Application.Abstractions;

/// <summary>
/// Persistence port for the <see cref="RepairOrder"/> aggregate. Declared here,
/// implemented in Infrastructure — that inversion is what keeps Application
/// free of any EF Core reference.
/// </summary>
public interface IRepairOrderRepository
{
    /// <summary>Loads an order including its items; null if it does not exist.</summary>
    Task<RepairOrder?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Lists orders, newest first, optionally filtered by status.</summary>
    Task<IReadOnlyList<RepairOrder>> ListAsync(
        RepairOrderStatus? status = null,
        CancellationToken cancellationToken = default);

    void Add(RepairOrder order);

    void Remove(RepairOrder order);
}
