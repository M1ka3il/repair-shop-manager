using Microsoft.EntityFrameworkCore;
using RepairShop.Application.Abstractions;
using RepairShop.Domain.Entities;

namespace RepairShop.Infrastructure.Persistence.Repositories;

/// <inheritdoc cref="IRepairOrderRepository"/>
public class RepairOrderRepository(RepairShopDbContext db) : IRepairOrderRepository
{
    public Task<RepairOrder?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        db.RepairOrders
            .Include(o => o.Items)
            .Include(o => o.Vehicle!)
                .ThenInclude(v => v.Customer)
            .SingleOrDefaultAsync(o => o.Id == id, cancellationToken);

    public async Task<IReadOnlyList<RepairOrder>> ListAsync(
        RepairOrderStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var query = db.RepairOrders
            .Include(o => o.Items)
            .Include(o => o.Vehicle!)
                .ThenInclude(v => v.Customer)
            // Read-only path: no change tracking needed, and skipping it avoids
            // paying for snapshots of every loaded graph.
            .AsNoTracking()
            .AsQueryable();

        if (status is not null)
        {
            query = query.Where(o => o.Status == status);
        }

        return await query
            .OrderByDescending(o => o.ReceivedAtUtc)
            .ThenByDescending(o => o.Id)
            .ToListAsync(cancellationToken);
    }

    public void Add(RepairOrder order) => db.RepairOrders.Add(order);

    public void Remove(RepairOrder order) => db.RepairOrders.Remove(order);
}
