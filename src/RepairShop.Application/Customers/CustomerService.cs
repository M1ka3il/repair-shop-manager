using RepairShop.Application.Abstractions;
using RepairShop.Application.Common;
using RepairShop.Domain.Entities;

namespace RepairShop.Application.Customers;

/// <inheritdoc cref="ICustomerService"/>
public class CustomerService(ICustomerRepository customers, IVehicleRepository vehicles) : ICustomerService
{
    public async Task<IReadOnlyList<CustomerDto>> ListCustomersAsync(CancellationToken cancellationToken = default)
    {
        var result = await customers.ListAsync(cancellationToken);
        return [.. result.Select(ToDto)];
    }

    public async Task<CustomerDto> GetCustomerAsync(int id, CancellationToken cancellationToken = default)
    {
        var customer = await customers.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(Customer), id);

        return ToDto(customer);
    }

    public async Task<IReadOnlyList<VehicleDto>> ListVehiclesAsync(CancellationToken cancellationToken = default)
    {
        var result = await vehicles.ListAsync(cancellationToken);
        return [.. result.Select(ToDto)];
    }

    private static CustomerDto ToDto(Customer customer) => new(
        customer.Id,
        customer.Name,
        customer.Email,
        customer.Phone,
        [.. customer.Vehicles.Select(ToDto)]);

    private static VehicleDto ToDto(Vehicle vehicle) => new(
        vehicle.Id,
        vehicle.CustomerId,
        vehicle.LicensePlate.Value,
        vehicle.Make,
        vehicle.Model,
        vehicle.Year);
}
