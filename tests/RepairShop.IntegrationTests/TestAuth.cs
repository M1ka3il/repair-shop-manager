using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using RepairShop.Application.Auth;

namespace RepairShop.IntegrationTests;

/// <summary>
/// Signed-in clients for the integration tests. Tokens come from the real
/// POST /api/auth/token — no hand-forged JWTs — and are cached per role.
/// </summary>
public static class TestAuth
{
    public const string SigningKey = "integration-tests-signing-key-0123456789abcdef";
    public const string MechanicPassword = "werkstatt-test-pw";
    public const string OfficePassword = "buero-test-pw";

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
