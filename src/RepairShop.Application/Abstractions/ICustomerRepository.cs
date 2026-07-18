using RepairShop.Domain.Entities;

namespace RepairShop.Application.Abstractions;

public interface ICustomerRepository
{
    Task<Customer?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Customer>> ListAsync(CancellationToken cancellationToken = default);
}
