using Microsoft.EntityFrameworkCore;
using RepairShop.Application.Abstractions;
using RepairShop.Domain.Entities;

namespace RepairShop.Infrastructure.Persistence.Repositories;

/// <inheritdoc cref="ICustomerRepository"/>
public class CustomerRepository(RepairShopDbContext db) : ICustomerRepository
{
    // Tracked: the same load serves reads and updates, as in RepairOrderRepository.
    public Task<Customer?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        db.Customers
            .Include(c => c.Vehicles)
            .SingleOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Customer>> ListAsync(CancellationToken cancellationToken = default) =>
        await db.Customers
            .Include(c => c.Vehicles)
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);

    public void Add(Customer customer) => db.Customers.Add(customer);
}
