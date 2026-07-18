namespace RepairShop.Application.Customers;

/// <summary>
/// Read-only access to customers and vehicles. Deliberately not a full CRUD
/// surface — the project's depth is in the repair-order lifecycle, and master
/// data management would add volume without adding signal.
/// </summary>
public interface ICustomerService
{
    Task<IReadOnlyList<CustomerDto>> ListCustomersAsync(CancellationToken cancellationToken = default);

    Task<CustomerDto> GetCustomerAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<VehicleDto>> ListVehiclesAsync(CancellationToken cancellationToken = default);
}
