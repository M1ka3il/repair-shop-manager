namespace RepairShop.Application.Auth;

/// <summary>
/// Who is calling. A port, like the repositories: the API implements it from the
/// JWT, the use cases never see HTTP.
/// </summary>
public interface ICurrentUser
{
    /// <summary>Login name, for logs; null if unauthenticated.</summary>
    string? UserName { get; }

    bool IsInRole(string role);
}
