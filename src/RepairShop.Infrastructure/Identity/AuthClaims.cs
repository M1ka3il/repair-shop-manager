using Microsoft.IdentityModel.JsonWebTokens;

namespace RepairShop.Infrastructure.Identity;

/// <summary>
/// Claim names shared by the issuer (AuthService) and the validator (API). One
/// place, so the two cannot drift apart and silently break every role check.
/// </summary>
public static class AuthClaims
{
    public const string Name = JwtRegisteredClaimNames.UniqueName;
    public const string Role = "role";
}
