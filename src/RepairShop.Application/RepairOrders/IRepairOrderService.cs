using RepairShop.Domain.Entities;

namespace RepairShop.Application.RepairOrders;

/// <summary>Use cases around repair orders. The API depends on this, never on EF Core.</summary>
public interface IRepairOrderService
{
    Task<IReadOnlyList<RepairOrderDto>> ListAsync(
        RepairOrderStatus? status, CancellationToken cancellationToken = default);

    Task<RepairOrderDto> GetAsync(int id, CancellationToken cancellationToken = default);

    Task<RepairOrderDto> CreateAsync(
        CreateRepairOrderRequest request, CancellationToken cancellationToken = default);

    Task<RepairOrderDto> UpdateAsync(
        int id, UpdateRepairOrderRequest request, CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, CancellationToken cancellationToken = default);

    Task<RepairOrderDto> ChangeStatusAsync(
        int id, ChangeStatusRequest request, CancellationToken cancellationToken = default);

    Task<RepairOrderDto> AddItemAsync(
        int id, AddRepairItemRequest request, CancellationToken cancellationToken = default);

    Task<RepairOrderDto> RemoveItemAsync(
        int id, int itemId, CancellationToken cancellationToken = default);
}
