namespace RepairShop.Application.Auth;

/// <summary>Checks credentials and issues tokens. Implemented in Infrastructure (hashing, JWT).</summary>
public interface IAuthService
{
    /// <summary>A token for valid credentials; null otherwise — never says which part was wrong.</summary>
    Task<TokenResponseDto?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
}
