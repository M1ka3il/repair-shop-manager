using RepairShop.Domain.Entities;

namespace RepairShop.Application.RepairOrders;

/// <summary>
/// Hand-written entity → DTO projection.
/// No AutoMapper on purpose: with four entities the mapping is shorter than the
/// configuration it would replace, and it stays greppable and compile-checked.
/// </summary>
internal static class RepairOrderMapping
{
    /// <param name="order">The aggregate.</param>
    /// <param name="mayMoveTo">Filters the domain's allowed targets down to what the caller may do.</param>
    public static RepairOrderDto ToDto(this RepairOrder order, Func<RepairOrderStatus, bool> mayMoveTo) => new(
        order.Id,
        order.VehicleId,
        order.Vehicle?.LicensePlate.Value,
        order.Vehicle?.Customer?.Name,
        order.Status,
        order.ReceivedAtUtc,
        order.CompletedAtUtc,
        order.InvoicedAtUtc,
        order.Description,
        order.TotalPrice,
        [.. order.Items.Select(ToDto)],
        [.. order.AllowedTargets.Where(mayMoveTo)]);

    public static RepairItemDto ToDto(this RepairItem item) => new(
        item.Id,
        item.Type,
        item.Designation,
        item.UnitPrice,
        item.Quantity,
        item.LineTotal);
}
