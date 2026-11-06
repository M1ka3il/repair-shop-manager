using Microsoft.Extensions.Logging;
using RepairShop.Application.Abstractions;
using RepairShop.Application.Common;
using RepairShop.Domain.Entities;
using RepairShop.Domain.ValueObjects;

namespace RepairShop.Application.Customers;

/// <inheritdoc cref="ICustomerService"/>
public class CustomerService(
    ICustomerRepository customers,
    IVehicleRepository vehicles,
    IUnitOfWork unitOfWork,
    ILogger<CustomerService> logger) : ICustomerService
{
    public async Task<IReadOnlyList<CustomerDto>> ListCustomersAsync(CancellationToken cancellationToken = default)
    {
        var result = await customers.ListAsync(cancellationToken);
        return [.. result.Select(ToDto)];
    }

    public async Task<CustomerDto> GetCustomerAsync(int id, CancellationToken cancellationToken = default) =>
        ToDto(await LoadCustomerAsync(id, cancellationToken));

    public async Task<CustomerDto> CreateCustomerAsync(
        SaveCustomerRequest request, CancellationToken cancellationToken = default)
    {
        var customer = new Customer(request.Name, request.Email, request.Phone);
        customers.Add(customer);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Created customer {CustomerId}", customer.Id);
        return ToDto(customer);
    }

    public async Task<CustomerDto> UpdateCustomerAsync(
        int id, SaveCustomerRequest request, CancellationToken cancellationToken = default)
    {
        var customer = await LoadCustomerAsync(id, cancellationToken);
        customer.Rename(request.Name);
        customer.UpdateContact(request.Email, request.Phone);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ToDto(customer);
    }

    public async Task<IReadOnlyList<VehicleDto>> ListVehiclesAsync(CancellationToken cancellationToken = default)
    {
        var result = await vehicles.ListAsync(cancellationToken);
        return [.. result.Select(ToDto)];
    }

    public async Task<VehicleDto> GetVehicleAsync(int id, CancellationToken cancellationToken = default) =>
        ToDto(await LoadVehicleAsync(id, cancellationToken));

    public async Task<VehicleDto> AddVehicleAsync(
        int customerId, SaveVehicleRequest request, CancellationToken cancellationToken = default)
    {
        // Existence first: a 404 for a wrong customer is more useful than a
        // complaint about the plate of a vehicle that could never be saved.
        _ = await LoadCustomerAsync(customerId, cancellationToken);

        var plate = LicensePlate.Create(request.LicensePlate);
        await EnsurePlateIsFreeAsync(plate, excludingVehicleId: null, cancellationToken);

        var vehicle = new Vehicle(customerId, plate, request.Make, request.Model, request.Year);
        vehicles.Add(vehicle);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Registered vehicle {VehicleId} ({Plate}) for customer {CustomerId}",
            vehicle.Id, plate, customerId);
        return ToDto(vehicle);
    }

    public async Task<VehicleDto> UpdateVehicleAsync(
        int id, SaveVehicleRequest request, CancellationToken cancellationToken = default)
    {
        var vehicle = await LoadVehicleAsync(id, cancellationToken);

        var plate = LicensePlate.Create(request.LicensePlate);
        await EnsurePlateIsFreeAsync(plate, excludingVehicleId: id, cancellationToken);

        vehicle.Update(plate, request.Make, request.Model, request.Year);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ToDto(vehicle);
    }

    /// <summary>
    /// Checked up front so the caller gets a 409 naming the plate. The unique
    /// index stays the real guarantee: two concurrent requests can both pass this
    /// check, and the loser then fails on the index.
    /// </summary>
    private async Task EnsurePlateIsFreeAsync(
        LicensePlate plate, int? excludingVehicleId, CancellationToken cancellationToken)
    {
        if (await vehicles.PlateExistsAsync(plate, excludingVehicleId, cancellationToken))
        {
            throw new ConflictException($"License plate '{plate}' is already registered.");
        }
    }

    private async Task<Customer> LoadCustomerAsync(int id, CancellationToken cancellationToken) =>
        await customers.GetByIdAsync(id, cancellationToken)
        ?? throw new NotFoundException(nameof(Customer), id);

    private async Task<Vehicle> LoadVehicleAsync(int id, CancellationToken cancellationToken) =>
        await vehicles.GetByIdAsync(id, cancellationToken)
        ?? throw new NotFoundException(nameof(Vehicle), id);

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
