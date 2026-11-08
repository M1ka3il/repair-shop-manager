namespace RepairShop.Application.Customers;

/// <summary>
/// Master data: customers and their vehicles. Intentionally rudimentary — create,
/// read and correct, but no delete: a vehicle with repair history must not
/// vanish, for the same reason an invoiced order cannot be deleted.
/// </summary>
public interface ICustomerService
{
    Task<IReadOnlyList<CustomerDto>> ListCustomersAsync(CancellationToken cancellationToken = default);

    Task<CustomerDto> GetCustomerAsync(int id, CancellationToken cancellationToken = default);

    Task<CustomerDto> CreateCustomerAsync(SaveCustomerRequest request, CancellationToken cancellationToken = default);

    Task<CustomerDto> UpdateCustomerAsync(int id, SaveCustomerRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<VehicleDto>> ListVehiclesAsync(CancellationToken cancellationToken = default);

    Task<VehicleDto> GetVehicleAsync(int id, CancellationToken cancellationToken = default);

    /// <exception cref="Common.NotFoundException">The customer does not exist.</exception>
    /// <exception cref="Common.ConflictException">The plate is already registered.</exception>
    Task<VehicleDto> AddVehicleAsync(int customerId, SaveVehicleRequest request, CancellationToken cancellationToken = default);

    /// <exception cref="Common.ConflictException">The new plate belongs to another vehicle.</exception>
    Task<VehicleDto> UpdateVehicleAsync(int id, SaveVehicleRequest request, CancellationToken cancellationToken = default);
}
