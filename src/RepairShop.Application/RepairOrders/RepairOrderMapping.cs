using RepairShop.Domain.Entities;

namespace RepairShop.Application.RepairOrders;

/// <summary>
/// Hand-written entity → DTO projection.
/// No AutoMapper on purpose: with four entities the mapping is shorter than the
/// configuration it would replace, and it stays greppable and compile-checked.
/// </summary>
internal static class RepairOrderMapping
{
    public static RepairOrderDto ToDto(this RepairOrder order) => new(
        order.Id,
        order.VehicleId,
        order.Vehicle?.LicensePlate.Value,
        order.Vehicle?.Customer?.Name,
        order.Status,
        order.ReceivedAtUtc,
        order.CompletedAtUtc,
        order.Description,
        order.TotalPrice,
        [.. order.Items.Select(ToDto)]);

    public static RepairItemDto ToDto(this RepairItem item) => new(
        item.Id,
        item.Type,
        item.Designation,
        item.UnitPrice,
        item.Quantity,
        item.LineTotal);
}
