namespace RepairShop.Domain.Entities;

/// <summary>
/// Lifecycle of a repair order. Values are explicit so that reordering the enum
/// later cannot silently rewrite persisted data.
/// </summary>
public enum RepairOrderStatus
{
    /// <summary>Angenommen — vehicle accepted, work not started.</summary>
    Received = 1,

    /// <summary>In Arbeit — a mechanic is working on it.</summary>
    InProgress = 2,

    /// <summary>Wartet auf Teil — blocked on a spare part.</summary>
    WaitingForParts = 3,

    /// <summary>Fertig — work finished, not yet billed.</summary>
    Completed = 4,

    /// <summary>Abgerechnet — invoiced. Terminal state.</summary>
    Invoiced = 5
}
