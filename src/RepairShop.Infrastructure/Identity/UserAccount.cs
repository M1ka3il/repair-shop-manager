namespace RepairShop.Infrastructure.Identity;

/// <summary>
/// A login. Deliberately not a domain entity: who may sign in is an
/// infrastructure concern, the workshop domain does not know about users.
/// </summary>
public class UserAccount
{
    private UserAccount()
    {
        // EF Core materialisation.
        UserName = null!;
        PasswordHash = null!;
        Role = null!;
    }

    public UserAccount(string userName, string role)
    {
        UserName = userName.Trim().ToLowerInvariant();
        Role = role;
        PasswordHash = string.Empty;
    }

    public int Id { get; private set; }

    /// <summary>Stored lower-case: "Buero" and "buero" are the same login.</summary>
    public string UserName { get; private set; }

    /// <summary>PBKDF2 hash in ASP.NET Core Identity's format (salt and parameters included).</summary>
    public string PasswordHash { get; private set; }

    public string Role { get; private set; }

    public void SetPasswordHash(string hash) => PasswordHash = hash;
}
