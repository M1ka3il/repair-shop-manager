using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using RepairShop.Application.Auth;

namespace RepairShop.IntegrationTests;

/// <summary>
/// Signed-in clients for the integration tests. Tokens come from the real
/// POST /api/auth/token — no hand-forged JWTs — and are cached per role.
/// </summary>
public static class TestAuth
{
    // Generated per test run instead of written into the code: they only ever
    // protect the throwaway Testcontainers database, but a literal password in
    // the repository is a bad pattern and trips secret scanners (GitGuardian).
    public static readonly string SigningKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
    public static readonly string MechanicPassword = Convert.ToBase64String(RandomNumberGenerator.GetBytes(18));
    public static readonly string OfficePassword = Convert.ToBase64String(RandomNumberGenerator.GetBytes(18));

    private static readonly ConcurrentDictionary<string, string> Tokens = new();

    /// <summary>A client signed in as the office (may do everything) unless another role is asked for.</summary>
    public static async Task<HttpClient> CreateClientAsync(string role = Roles.Office)
    {
        var client = IntegrationTestContext.Factory.CreateClient();
        var token = Tokens.TryGetValue(role, out var cached) ? cached : await SignInAsync(client, role);
        Tokens[role] = token;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private static async Task<string> SignInAsync(HttpClient client, string role)
    {
        var (user, password) = role == Roles.Mechanic
            ? ("werkstatt", MechanicPassword)
            : ("buero", OfficePassword);

        var response = await client.PostAsJsonAsync("/api/auth/token", new { userName = user, password });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<TokenResponseDto>()
            ?? throw new InvalidOperationException("Empty token response.");
        return body.AccessToken;
    }
}
