namespace RepairShop.Infrastructure.Identity;

/// <summary>
/// Passwords for the two demo logins, from configuration ("DemoUsers" section;
/// DEMO_MECHANIC_PASSWORD / DEMO_OFFICE_PASSWORD in .env under docker compose).
/// Never in the repository. Without them no users are seeded, and nobody can log in.
/// </summary>
public sealed class DemoUserOptions
{
    public const string SectionName = "DemoUsers";

    /// <summary>Login "werkstatt", role Mechanic.</summary>
    public string? MechanicPassword { get; init; }

    /// <summary>Login "buero", role Office.</summary>
    public string? OfficePassword { get; init; }
}
