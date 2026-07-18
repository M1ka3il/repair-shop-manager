using Microsoft.Extensions.Logging;
using RepairShop.Application.Abstractions;
using RepairShop.Application.Common;
using RepairShop.Domain.Entities;

namespace RepairShop.Application.RepairOrders;

/// <summary>
/// Orchestrates repair-order use cases: load the aggregate, let the domain decide,
/// persist. Note what is *not* here — no status rules. Those live in
/// <see cref="RepairOrder"/> so they hold no matter which caller triggers them.
/// </summary>
public class RepairOrderService(
    IRepairOrderRepository orders,
    IVehicleRepository vehicles,
    IUnitOfWork unitOfWork,
    IClock clock,
    ILogger<RepairOrderService> logger) : IRepairOrderService
{
    public async Task<IReadOnlyList<RepairOrderDto>> ListAsync(
        RepairOrderStatus? status, CancellationToken cancellationToken = default)
    {
        var result = await orders.ListAsync(status, cancellationToken);
        return [.. result.Select(o => o.ToDto())];
    }

    public async Task<RepairOrderDto> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        var order = await LoadAsync(id, cancellationToken);
        return order.ToDto();
    }

    public async Task<RepairOrderDto> CreateAsync(
        CreateRepairOrderRequest request, CancellationToken cancellationToken = default)
    {
        // Existence check up front so a bad vehicle id becomes a 404, not an
        // FK violation surfacing as a 500.
        _ = await vehicles.GetByIdAsync(request.VehicleId, cancellationToken)
            ?? throw new NotFoundException(nameof(Vehicle), request.VehicleId);

        var order = new RepairOrder(request.VehicleId, request.Description, clock.UtcNow);
        orders.Add(order);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Created repair order {RepairOrderId} for vehicle {VehicleId}", order.Id, order.VehicleId);

        return order.ToDto();
    }

    public async Task<RepairOrderDto> UpdateAsync(
        int id, UpdateRepairOrderRequest request, CancellationToken cancellationToken = default)
    {
        var order = await LoadAsync(id, cancellationToken);
        order.UpdateDescription(request.Description);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return order.ToDto();
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var order = await LoadAsync(id, cancellationToken);
        orders.Remove(order);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Deleted repair order {RepairOrderId}", id);
    }

    public async Task<RepairOrderDto> ChangeStatusAsync(
        int id, ChangeStatusRequest request, CancellationToken cancellationToken = default)
    {
        var order = await LoadAsync(id, cancellationToken);
        var previous = order.Status;

        // Throws DomainException on an illegal transition; the middleware turns
        // that into a 400 with a problem detail.
        order.TransitionTo(request.TargetStatus, clock.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Repair order {RepairOrderId} moved from {PreviousStatus} to {NewStatus}",
            id, previous, order.Status);

        return order.ToDto();
    }

    public async Task<RepairOrderDto> AddItemAsync(
        int id, AddRepairItemRequest request, CancellationToken cancellationToken = default)
    {
        var order = await LoadAsync(id, cancellationToken);
        order.AddItem(request.Type, request.Designation, request.UnitPrice, request.Quantity);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return order.ToDto();
    }

    public async Task<RepairOrderDto> RemoveItemAsync(
        int id, int itemId, CancellationToken cancellationToken = default)
    {
        var order = await LoadAsync(id, cancellationToken);
        order.RemoveItem(itemId);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return order.ToDto();
    }

    private async Task<RepairOrder> LoadAsync(int id, CancellationToken cancellationToken) =>
        await orders.GetByIdAsync(id, cancellationToken)
        ?? throw new NotFoundException(nameof(RepairOrder), id);
}
