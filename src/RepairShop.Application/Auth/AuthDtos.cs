using System.ComponentModel.DataAnnotations;

namespace RepairShop.Application.Auth;

/// <summary>Credentials for <c>POST /api/auth/token</c>.</summary>
public class LoginRequest
{
    /// <example>buero</example>
    [Required]
    [StringLength(100)]
    public string UserName { get; init; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string Password { get; init; } = string.Empty;
}

/// <summary>A bearer token and what the client needs to know about it.</summary>
/// <param name="AccessToken">JWT to send as <c>Authorization: Bearer …</c>.</param>
/// <param name="ExpiresAtUtc">When the token stops being accepted.</param>
/// <param name="UserName">Login name.</param>
/// <param name="Role">Mechanic or Office.</param>
public record TokenResponseDto(string AccessToken, DateTime ExpiresAtUtc, string UserName, string Role);
