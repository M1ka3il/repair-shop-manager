using RepairShop.Application.Auth;

namespace RepairShop.Api.Auth;

/// <summary><see cref="ICurrentUser"/> from the validated JWT of the current request.</summary>
public sealed class HttpCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    public string? UserName => accessor.HttpContext?.User.Identity?.Name;

    public bool IsInRole(string role) => accessor.HttpContext?.User.IsInRole(role) ?? false;
}
