using RepairShop.Domain.Entities;

namespace RepairShop.Application.Auth;

/// <summary>
/// Which status changes a role may trigger. Sits next to — not inside — the
/// domain's transition table: the domain says whether a move is <em>possible</em>,
/// this says whether <em>this person</em> may make it. An endpoint attribute can't,
/// because the target status is only known from the request body.
/// </summary>
public static class StatusChangePolicy
{
    private static readonly IReadOnlyDictionary<string, IReadOnlySet<RepairOrderStatus>> TargetsByRole =
        new Dictionary<string, IReadOnlySet<RepairOrderStatus>>
        {
            // The workshop floor moves work along until it is done …
            [Roles.Mechanic] = new HashSet<RepairOrderStatus>
            {
                RepairOrderStatus.InProgress,
                RepairOrderStatus.WaitingForParts,
                RepairOrderStatus.Completed
            },
            // … the office may do the same, and is the only one that bills.
            [Roles.Office] = new HashSet<RepairOrderStatus>(Enum.GetValues<RepairOrderStatus>())
        };

    /// <summary>True if any of the user's roles allows moving an order to <paramref name="target"/>.</summary>
    public static bool MayMoveTo(ICurrentUser user, RepairOrderStatus target) =>
        TargetsByRole.Any(entry => user.IsInRole(entry.Key) && entry.Value.Contains(target));
}
