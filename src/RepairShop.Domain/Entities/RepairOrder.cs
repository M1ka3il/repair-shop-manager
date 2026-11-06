using RepairShop.Domain.Common;

namespace RepairShop.Domain.Entities;

/// <summary>
/// A repair job on one vehicle. This is the aggregate root of the project:
/// line items are only ever added or removed through it, and the status
/// lifecycle is enforced here rather than in a controller or a service.
/// </summary>
public class RepairOrder
{
    /// <summary>
    /// The single source of truth for the lifecycle:
    /// Received → InProgress ⇄ WaitingForParts → Completed → Invoiced.
    /// Anything not listed is rejected — including every backwards move out of
    /// <see cref="RepairOrderStatus.Invoiced"/>, which is terminal.
    /// </summary>
    private static readonly Dictionary<RepairOrderStatus, RepairOrderStatus[]> AllowedTransitions = new()
    {
        [RepairOrderStatus.Received] = [RepairOrderStatus.InProgress],
        [RepairOrderStatus.InProgress] = [RepairOrderStatus.WaitingForParts, RepairOrderStatus.Completed],
        [RepairOrderStatus.WaitingForParts] = [RepairOrderStatus.InProgress],
        [RepairOrderStatus.Completed] = [RepairOrderStatus.Invoiced],
        [RepairOrderStatus.Invoiced] = []
    };

    private readonly List<RepairItem> _items = [];

    private RepairOrder()
    {
        // EF Core materialisation.
        Description = null!;
    }

    public RepairOrder(int vehicleId, string description, DateTime receivedAtUtc)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            throw new DomainException("Repair order description must not be empty.");
        }

        VehicleId = vehicleId;
        Description = description.Trim();
        ReceivedAtUtc = receivedAtUtc;
        Status = RepairOrderStatus.Received;
    }

    public int Id { get; private set; }

    public int VehicleId { get; private set; }

    public Vehicle? Vehicle { get; private set; }

    public RepairOrderStatus Status { get; private set; }

    public DateTime ReceivedAtUtc { get; private set; }

    public DateTime? CompletedAtUtc { get; private set; }

    public string Description { get; private set; }

    public IReadOnlyCollection<RepairItem> Items => _items.AsReadOnly();

    /// <summary>Sum of all line items. Derived, never stored.</summary>
    public decimal TotalPrice => _items.Sum(item => item.LineTotal);

    /// <summary>
    /// Work is done once the order is completed: from here on, what it costs and
    /// whether it exists are part of the billing record and no longer editable.
    /// </summary>
    private bool IsWorkFinished => Status is RepairOrderStatus.Completed or RepairOrderStatus.Invoiced;

    /// <summary>
    /// The statuses reachable from the current one, in lifecycle order. Exposed so
    /// a client can offer exactly these moves instead of keeping its own copy of
    /// the transition table; <see cref="TransitionTo"/> still enforces them.
    /// </summary>
    public IReadOnlyList<RepairOrderStatus> AllowedTargets => AllowedTransitions[Status];

    /// <summary>True if <paramref name="target"/> is reachable from the current status.</summary>
    public bool CanTransitionTo(RepairOrderStatus target) =>
        AllowedTransitions.TryGetValue(Status, out var allowed) && allowed.Contains(target);

    /// <summary>
    /// Moves the order to <paramref name="target"/>.
    /// </summary>
    /// <exception cref="DomainException">The transition is not permitted.</exception>
    public void TransitionTo(RepairOrderStatus target, DateTime nowUtc)
    {
        if (Status == target)
        {
            throw new DomainException($"Repair order is already in status '{Status}'.");
        }

        if (!CanTransitionTo(target))
        {
            throw new DomainException(
                $"Cannot change status from '{Status}' to '{target}'. " +
                $"Allowed from '{Status}': {DescribeAllowedTargets()}.");
        }

        Status = target;

        if (target == RepairOrderStatus.Completed)
        {
            CompletedAtUtc = nowUtc;
        }
    }

    /// <summary>
    /// Adds a line item. Blocked once the order is completed or invoiced —
    /// billing a job and then changing what it cost is exactly the bug this
    /// rule exists to prevent.
    /// </summary>
    public RepairItem AddItem(RepairItemType type, string designation, decimal unitPrice, decimal quantity)
    {
        if (IsWorkFinished)
        {
            throw new DomainException($"Cannot add items to an order in status '{Status}'.");
        }

        var item = new RepairItem(Id, type, designation, unitPrice, quantity);
        _items.Add(item);
        return item;
    }

    /// <summary>Removes a line item. Same restriction as <see cref="AddItem"/>.</summary>
    public void RemoveItem(int itemId)
    {
        if (IsWorkFinished)
        {
            throw new DomainException($"Cannot remove items from an order in status '{Status}'.");
        }

        var item = _items.SingleOrDefault(i => i.Id == itemId)
            ?? throw new DomainException($"Repair item {itemId} does not belong to this order.");

        _items.Remove(item);
    }

    /// <summary>
    /// Guards deletion. Same restriction as <see cref="AddItem"/>: a completed or
    /// invoiced order is billable work, and deleting it would make that work
    /// disappear without trace — worse than editing it, which is already forbidden.
    /// </summary>
    /// <exception cref="DomainException">The order is completed or invoiced.</exception>
    public void EnsureCanBeDeleted()
    {
        if (IsWorkFinished)
        {
            throw new DomainException($"Cannot delete an order in status '{Status}'.");
        }
    }

    public void UpdateDescription(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            throw new DomainException("Repair order description must not be empty.");
        }

        if (Status == RepairOrderStatus.Invoiced)
        {
            throw new DomainException("Cannot modify an invoiced repair order.");
        }

        Description = description.Trim();
    }

    private string DescribeAllowedTargets()
    {
        var allowed = AllowedTransitions[Status];
        return allowed.Length == 0 ? "nothing (terminal status)" : string.Join(", ", allowed);
    }
}
