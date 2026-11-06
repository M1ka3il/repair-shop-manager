using System.ComponentModel.DataAnnotations;
using RepairShop.Domain.Entities;

namespace RepairShop.Application.RepairOrders;

/// <summary>A repair order as returned by the API.</summary>
/// <param name="Id">Order primary key.</param>
/// <param name="VehicleId">Vehicle the order belongs to.</param>
/// <param name="LicensePlate">Plate of that vehicle, for readability.</param>
/// <param name="CustomerName">Owner of that vehicle, for readability.</param>
/// <param name="Status">Current lifecycle status.</param>
/// <param name="ReceivedAtUtc">When the vehicle was accepted.</param>
/// <param name="CompletedAtUtc">When the work was finished, if it was.</param>
/// <param name="Description">What the customer reported.</param>
/// <param name="TotalPrice">Sum of all line items, net.</param>
/// <param name="Items">Labour and parts booked on this order.</param>
/// <param name="AllowedTransitions">
/// Statuses this order may move to next, computed by the domain. Empty once
/// invoiced. Clients offer these instead of duplicating the lifecycle rules.
/// </param>
public record RepairOrderDto(
    int Id,
    int VehicleId,
    string? LicensePlate,
    string? CustomerName,
    RepairOrderStatus Status,
    DateTime ReceivedAtUtc,
    DateTime? CompletedAtUtc,
    string Description,
    decimal TotalPrice,
    IReadOnlyList<RepairItemDto> Items,
    IReadOnlyList<RepairOrderStatus> AllowedTransitions);

/// <summary>A single labour or parts line.</summary>
/// <param name="Id">Item primary key.</param>
/// <param name="Type">Labour or Part.</param>
/// <param name="Designation">Free-text description of the line.</param>
/// <param name="UnitPrice">Price per hour or per piece, net.</param>
/// <param name="Quantity">Hours for labour, pieces for parts.</param>
/// <param name="LineTotal">UnitPrice × Quantity.</param>
public record RepairItemDto(
    int Id,
    RepairItemType Type,
    string Designation,
    decimal UnitPrice,
    decimal Quantity,
    decimal LineTotal);

/// <summary>Payload for creating a repair order.</summary>
public class CreateRepairOrderRequest
{
    /// <summary>Id of an existing vehicle.</summary>
    /// <example>1</example>
    [Range(1, int.MaxValue)]
    public int VehicleId { get; init; }

    /// <summary>What the customer reported.</summary>
    /// <example>Bremsen quietschen beim Bremsen</example>
    [Required]
    [StringLength(1000, MinimumLength = 3)]
    public string Description { get; init; } = string.Empty;
}

/// <summary>Payload for editing the free-text part of an order.</summary>
public class UpdateRepairOrderRequest
{
    /// <summary>Corrected or extended problem description.</summary>
    /// <example>Bremsbeläge vorne verschlissen, Scheiben geprüft</example>
    [Required]
    [StringLength(1000, MinimumLength = 3)]
    public string Description { get; init; } = string.Empty;
}

/// <summary>Payload for the dedicated status-change endpoint.</summary>
public class ChangeStatusRequest
{
    /// <summary>Status to move the order to.</summary>
    /// <example>InProgress</example>
    [Required]
    public RepairOrderStatus TargetStatus { get; init; }
}

/// <summary>Payload for booking labour or a part onto an order.</summary>
public class AddRepairItemRequest
{
    /// <summary>Labour or Part.</summary>
    /// <example>Labour</example>
    [Required]
    public RepairItemType Type { get; init; }

    /// <summary>What was done or fitted.</summary>
    /// <example>Bremsbeläge vorne wechseln</example>
    [Required]
    [StringLength(200, MinimumLength = 2)]
    public string Designation { get; init; } = string.Empty;

    /// <summary>Net price per hour or per piece.</summary>
    /// <example>89.00</example>
    [Range(0, 1_000_000)]
    public decimal UnitPrice { get; init; }

    /// <summary>Hours for labour, pieces for parts.</summary>
    /// <example>1.5</example>
    [Range(0.01, 100_000)]
    public decimal Quantity { get; init; }
}
