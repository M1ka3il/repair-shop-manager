using RepairShop.Domain.Common;

namespace RepairShop.Domain.Entities;

/// <summary>
/// One line on a repair order — either labour or a spare part.
/// Created through <see cref="RepairOrder.AddItem"/> only, so that the order's
/// status rules cannot be bypassed.
/// </summary>
public class RepairItem
{
    private RepairItem()
    {
        // EF Core materialisation.
        Designation = null!;
    }

    internal RepairItem(int repairOrderId, RepairItemType type, string designation, decimal unitPrice, decimal quantity)
    {
        if (string.IsNullOrWhiteSpace(designation))
        {
            throw new DomainException("Repair item designation must not be empty.");
        }

        if (unitPrice < 0)
        {
            throw new DomainException("Unit price must not be negative.");
        }

        if (quantity <= 0)
        {
            throw new DomainException("Quantity must be greater than zero.");
        }

        RepairOrderId = repairOrderId;
        Type = type;
        Designation = designation.Trim();
        UnitPrice = unitPrice;
        Quantity = quantity;
    }

    public int Id { get; private set; }

    public int RepairOrderId { get; private set; }

    public RepairOrder? RepairOrder { get; private set; }

    public RepairItemType Type { get; private set; }

    public string Designation { get; private set; }

    /// <summary>Price per hour (labour) or per piece (part), net.</summary>
    public decimal UnitPrice { get; private set; }

    /// <summary>Hours for labour, pieces for parts.</summary>
    public decimal Quantity { get; private set; }

    public decimal LineTotal => UnitPrice * Quantity;
}
