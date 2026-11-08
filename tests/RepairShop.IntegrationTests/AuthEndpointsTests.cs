using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;
using RepairShop.Application.Auth;
using RepairShop.Application.RepairOrders;
using RepairShop.Domain.Entities;

namespace RepairShop.IntegrationTests;

/// <summary>
/// Sign-in and roles over real HTTP: closed by default, the role split between
/// workshop and office, and the edges (wrong password, tampered token,
/// throttling, misconfiguration).
/// </summary>
[TestFixture]
public class AuthEndpointsTests
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    [SetUp]
    public void SetUp() => IntegrationTestContext.EnsureDockerAvailable();

    [Test]
    public async Task WithoutToken_ApiAnswers401AsProblemDetails_HealthAndSwaggerStayOpen()
    {
        using var anonymous = IntegrationTestContext.Factory.CreateClient();

        var orders = await anonymous.GetAsync("/api/repair-orders");
        Assert.That(orders.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        Assert.That(orders.Content.Headers.ContentType?.MediaType, Is.EqualTo("application/problem+json"));

        Assert.That((await anonymous.GetAsync("/health")).StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That((await anonymous.GetAsync("/swagger/v1/swagger.json")).StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task SignIn_ReturnsTokenWithRole_UserNameIgnoresCase()
    {
        using var client = IntegrationTestContext.Factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/token",
            new { userName = " BUERO ", password = TestAuth.OfficePassword });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var token = await response.Content.ReadFromJsonAsync<TokenResponseDto>();
        Assert.Multiple(() =>
        {
            Assert.That(token!.Role, Is.EqualTo(Roles.Office));
            Assert.That(token.UserName, Is.EqualTo("buero"));
            Assert.That(token.ExpiresAtUtc, Is.GreaterThan(DateTime.UtcNow.AddHours(1)));
        });
    }

    /// <summary>Same answer for a wrong password and an unknown user: no hint which logins exist.</summary>
    [TestCase("buero", "falsch")]
    [TestCase("gibt-es-nicht", "egal")]
    public async Task SignIn_WithWrongCredentials_Returns401WithoutSayingWhichPartWasWrong(string user, string password)
    {
        using var client = IntegrationTestContext.Factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/token", new { userName = user, password });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        Assert.That(await response.Content.ReadAsStringAsync(), Does.Contain("User name or password is wrong."));
    }

    [Test]
    public async Task TamperedToken_IsRejected()
    {
        using var office = await TestAuth.CreateClientAsync();
        var token = office.DefaultRequestHeaders.Authorization!.Parameter!;
        using var client = IntegrationTestContext.Factory.CreateClient();
        // Flip a character of the signature.
        var tampered = token[..^2] + (token[^2] == 'A' ? 'B' : 'A') + token[^1];
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tampered);

        Assert.That((await client.GetAsync("/api/repair-orders")).StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    /// <summary>
    /// The role split end to end: the mechanic works an order up to "Completed",
    /// may not bill it, and is not even offered to; the office bills it.
    /// </summary>
    [Test]
    public async Task Mechanic_FinishesWork_OnlyTheOfficeInvoices()
    {
        using var office = await TestAuth.CreateClientAsync(Roles.Office);
        using var mechanic = await TestAuth.CreateClientAsync(Roles.Mechanic);

        var vehicles = await office.GetFromJsonAsync<List<JsonElement>>("/api/vehicles", Json);
        var created = await Read<RepairOrderDto>(await office.PostAsJsonAsync("/api/repair-orders",
            new { vehicleId = vehicles![0].GetProperty("id").GetInt32(), description = "Rollentest" }));

        await Expect(mechanic, created.Id, RepairOrderStatus.InProgress, HttpStatusCode.OK);
        var completed = await Expect(mechanic, created.Id, RepairOrderStatus.Completed, HttpStatusCode.OK);
        Assert.That(completed!.AllowedTransitions, Is.Empty, "the mechanic is not offered to invoice");

        var asOffice = await office.GetFromJsonAsync<RepairOrderDto>($"/api/repair-orders/{created.Id}", Json);
        Assert.That(asOffice!.AllowedTransitions, Is.EqualTo(new[] { RepairOrderStatus.Invoiced }));

        await Expect(mechanic, created.Id, RepairOrderStatus.Invoiced, HttpStatusCode.Forbidden);
        await Expect(office, created.Id, RepairOrderStatus.Invoiced, HttpStatusCode.OK);
    }

    [TestCase("GET", "/api/reports/revenue-by-customer")]
    [TestCase("POST", "/api/customers")]
    [TestCase("POST", "/api/repair-orders")]
    [TestCase("DELETE", "/api/repair-orders/999999")]
    public async Task Mechanic_IsKeptOutOfOfficeEndpoints(string method, string url)
    {
        using var mechanic = await TestAuth.CreateClientAsync(Roles.Mechanic);

        var request = new HttpRequestMessage(new HttpMethod(method), url);
        if (method == "POST")
        {
            request.Content = JsonContent.Create(new { name = "x", vehicleId = 1, description = "xyz" });
        }

        var response = await mechanic.SendAsync(request);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public async Task Mechanic_ReadsOrdersAndMasterData()
    {
        using var mechanic = await TestAuth.CreateClientAsync(Roles.Mechanic);

        Assert.Multiple(async () =>
        {
            Assert.That((await mechanic.GetAsync("/api/repair-orders")).StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That((await mechanic.GetAsync("/api/vehicles")).StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That((await mechanic.GetAsync("/api/customers")).StatusCode, Is.EqualTo(HttpStatusCode.OK));
        });
    }

    /// <summary>Own app instance with a tiny limit, so the shared one is not throttled.</summary>
    [Test]
    public async Task SignIn_IsThrottledPerClient()
    {
        await using var app = IntegrationTestContext.Factory.WithWebHostBuilder(b =>
            b.UseSetting("Auth:LoginAttemptsPerMinute", "2"));
        using var client = app.CreateClient();

        var statuses = new List<HttpStatusCode>();
        for (var i = 0; i < 3; i++)
        {
            var response = await client.PostAsJsonAsync("/api/auth/token", new { userName = "buero", password = "falsch" });
            statuses.Add(response.StatusCode);
        }

        Assert.That(statuses, Is.EqualTo(new[]
        {
            HttpStatusCode.Unauthorized, HttpStatusCode.Unauthorized, HttpStatusCode.TooManyRequests
        }));
    }

    /// <summary>A missing or short signing key stops the app at startup, not at the first login.</summary>
    [Test]
    public void StartupFails_WhenTheSigningKeyIsTooShort()
    {
        using var app = IntegrationTestContext.Factory.WithWebHostBuilder(b =>
            b.UseSetting("Jwt:SigningKey", "too-short"));

        var ex = Assert.Catch(() => app.CreateClient());
        Assert.That(ex, Is.InstanceOf<OptionsValidationException>().Or.InnerException.InstanceOf<OptionsValidationException>());
        Assert.That(ex!.ToString(), Does.Contain("at least 32 bytes"));
    }

    private static async Task<RepairOrderDto?> Expect(
        HttpClient client, int id, RepairOrderStatus target, HttpStatusCode expected)
    {
        var response = await client.PatchAsJsonAsync($"/api/repair-orders/{id}/status", new { targetStatus = target.ToString() });
        Assert.That(response.StatusCode, Is.EqualTo(expected), $"→ {target}");
        return expected == HttpStatusCode.OK ? await Read<RepairOrderDto>(response) : null;
    }

    private static async Task<T> Read<T>(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<T>(Json) ?? throw new InvalidOperationException("empty body");
}
