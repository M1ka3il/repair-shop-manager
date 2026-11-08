using System.ComponentModel.DataAnnotations;
using System.Text;

namespace RepairShop.Infrastructure.Identity;

/// <summary>
/// Token settings, bound from the "Jwt" configuration section. The signing key
/// never lives in the repository: docker compose takes it from .env, local
/// development from user secrets.
/// </summary>
public sealed class JwtOptions : IValidatableObject
{
    public const string SectionName = "Jwt";

    [Required]
    public string Issuer { get; init; } = "repair-shop";

    [Required]
    public string Audience { get; init; } = "repair-shop";

    /// <summary>HMAC-SHA256 key; at least 32 bytes (256 bits) as UTF-8.</summary>
    [Required]
    public string SigningKey { get; init; } = string.Empty;

    /// <summary>One workshop shift by default: there is no refresh token, so shorter means re-login mid-shift.</summary>
    [Range(5, 24 * 60)]
    public int LifetimeMinutes { get; init; } = 480;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Encoding.UTF8.GetByteCount(SigningKey) < 32)
        {
            yield return new ValidationResult(
                "Jwt:SigningKey must be at least 32 bytes. Set JWT_SIGNING_KEY in .env (docker compose) " +
                "or 'dotnet user-secrets set Jwt:SigningKey …' (local).",
                [nameof(SigningKey)]);
        }
    }
}
