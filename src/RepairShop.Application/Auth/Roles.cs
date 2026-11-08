namespace RepairShop.Application.Auth;

/// <summary>
/// The two roles of the workshop. Strings because they travel in the JWT's role
/// claim and in <c>[Authorize(Roles = …)]</c>.
/// </summary>
public static class Roles
{
    /// <summary>Workshop floor: reads orders, books lines, moves work up to "Completed".</summary>
    public const string Mechanic = "Mechanic";

    /// <summary>Front office: everything a mechanic may, plus master data, deleting, invoicing, reports.</summary>
    public const string Office = "Office";

    public static readonly IReadOnlyList<string> All = [Mechanic, Office];
}
