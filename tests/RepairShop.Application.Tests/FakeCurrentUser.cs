using RepairShop.Application.Auth;

namespace RepairShop.Application.Tests;

/// <summary>A caller with fixed roles — no HTTP, no JWT.</summary>
public sealed class FakeCurrentUser(params string[] roles) : ICurrentUser
{
    public string? UserName => roles.Length > 0 ? roles[0].ToLowerInvariant() : null;

    public bool IsInRole(string role) => roles.Contains(role);
}
