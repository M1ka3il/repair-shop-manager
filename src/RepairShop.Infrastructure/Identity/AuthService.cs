using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using RepairShop.Application.Abstractions;
using RepairShop.Application.Auth;
using RepairShop.Infrastructure.Persistence;

namespace RepairShop.Infrastructure.Identity;

/// <inheritdoc cref="IAuthService"/>
public sealed class AuthService(
    RepairShopDbContext db,
    IPasswordHasher<UserAccount> hasher,
    IOptions<JwtOptions> options,
    IClock clock,
    ILogger<AuthService> logger) : IAuthService
{
    /// <summary>
    /// Verified against when the user does not exist, so a wrong user name costs
    /// as much time as a wrong password and response times do not reveal which
    /// logins exist.
    /// </summary>
    private static readonly string DummyHash =
        new PasswordHasher<UserAccount>().HashPassword(new UserAccount("dummy", Roles.Mechanic), "not-a-password");

    public async Task<TokenResponseDto?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var userName = request.UserName.Trim().ToLowerInvariant();
        var user = await db.UserAccounts.SingleOrDefaultAsync(u => u.UserName == userName, cancellationToken);

        var result = hasher.VerifyHashedPassword(
            user ?? new UserAccount(userName, Roles.Mechanic),
            user?.PasswordHash ?? DummyHash,
            request.Password);

        if (user is null || result == PasswordVerificationResult.Failed)
        {
            logger.LogWarning("Failed login for {UserName}", userName);
            return null;
        }

        if (result == PasswordVerificationResult.SuccessRehashNeeded)
        {
            // Hash parameters were raised since this password was set: upgrade it now.
            user.SetPasswordHash(hasher.HashPassword(user, request.Password));
            await db.SaveChangesAsync(cancellationToken);
        }

        logger.LogInformation("User {UserName} signed in as {Role}", user.UserName, user.Role);
        return IssueToken(user);
    }

    private TokenResponseDto IssueToken(UserAccount user)
    {
        var settings = options.Value;
        var now = clock.UtcNow;
        var expires = now.AddMinutes(settings.LifetimeMinutes);

        var token = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = settings.Issuer,
            Audience = settings.Audience,
            IssuedAt = now,
            NotBefore = now,
            Expires = expires,
            Subject = new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.UniqueName, user.UserName),
                // Short claim names on purpose; the API validates with
                // NameClaimType/RoleClaimType set to exactly these (see AuthClaims).
                new Claim(AuthClaims.Role, user.Role)
            ]),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.SigningKey)),
                SecurityAlgorithms.HmacSha256)
        });

        return new TokenResponseDto(token, expires, user.UserName, user.Role);
    }
}
