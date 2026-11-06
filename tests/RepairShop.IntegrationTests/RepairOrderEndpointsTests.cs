using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using RepairShop.Application.Customers;
using RepairShop.Application.RepairOrders;
using RepairShop.Application.Reporting;
using RepairShop.Domain.Entities;

namespace RepairShop.IntegrationTests;

/// <summary>
/// End-to-end over HTTP against a real SQL Server. These cover the seams the unit
/// tests cannot: routing, model binding, the exception middleware's status codes,
/// EF Core mapping, and the stored procedure.
/// </summary>
[TestFixture]
public class RepairOrderEndpointsTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private HttpClient _client = null!;

    [SetUp]
    public void SetUp()
    {
        IntegrationTestContext.EnsureDockerAvailable();
        _client = IntegrationTestContext.Factory.CreateClient();
    }

    [TearDown]
    public void TearDown() => _client?.Dispose();

    [Test]
    public async Task Health_ReturnsOk()
    {
        var response = await _client.GetAsync("/health");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task GetRepairOrders_ReturnsSeededData()
    {
        var orders = await GetAsync<List<RepairOrderDto>>("/api/repair-orders");

        Assert.That(orders, Is.Not.Empty, "the seed should make the API useful on first start");
    }

    [Test]
    public async Task GetRepairOrders_FilteredByStatus_ReturnsOnlyThatStatus()
    {
        var orders = await GetAsync<List<RepairOrderDto>>("/api/repair-orders?status=Invoiced");

        Assert.That(orders, Is.Not.Empty);
        Assert.That(orders, Has.All.Matches<RepairOrderDto>(o => o.Status == RepairOrderStatus.Invoiced));
    }

    [Test]
    public async Task GetRepairOrder_UnknownId_Returns404()
    {
        var response = await _client.GetAsync("/api/repair-orders/999999");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    /// <summary>
    /// The full lifecycle over HTTP: create, book work, walk the status flow,
    /// and confirm the terminal state really is terminal.
    /// </summary>
    [Test]
    public async Task RepairOrderLifecycle_FromCreationToInvoiced()
    {
        var vehicles = await GetAsync<List<VehicleDto>>("/api/vehicles");
        var vehicleId = vehicles[0].Id;

        // Create → 201 with a usable Location header.
        var createResponse = await _client.PostAsJsonAsync("/api/repair-orders", new
        {
            vehicleId,
            description = "Kupplung rutscht bei Volllast"
        });

        Assert.That(createResponse.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        Assert.That(createResponse.Headers.Location, Is.Not.Null);

        var created = await ReadAsync<RepairOrderDto>(createResponse);
        Assert.That(created.Status, Is.EqualTo(RepairOrderStatus.Received));

        // Book labour and a part.
        var withItems = await PatchOrPostAsync<RepairOrderDto>(
            $"/api/repair-orders/{created.Id}/items",
            new { type = "Labour", designation = "Kupplung wechseln", unitPrice = 89.00m, quantity = 6m });

        Assert.That(withItems.TotalPrice, Is.EqualTo(534.00m));

        // Received → InProgress → Completed → Invoiced.
        await ChangeStatusAsync(created.Id, RepairOrderStatus.InProgress);
        await ChangeStatusAsync(created.Id, RepairOrderStatus.Completed);
        var invoiced = await ChangeStatusAsync(created.Id, RepairOrderStatus.Invoiced);

        Assert.Multiple(() =>
        {
            Assert.That(invoiced.Status, Is.EqualTo(RepairOrderStatus.Invoiced));
            Assert.That(invoiced.CompletedAtUtc, Is.Not.Null);
        });

        // And the rule that matters: no way back out of Invoiced — 400, not 500.
        var rejected = await _client.PatchAsJsonAsync(
            $"/api/repair-orders/{created.Id}/status",
            new { targetStatus = "InProgress" });

        Assert.That(rejected.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

        var problem = await rejected.Content.ReadAsStringAsync();
        Assert.That(problem, Does.Contain("Invoiced"), "the response should say which rule was violated");
    }

    [Test]
    public async Task DeleteRepairOrder_OpenOrder_Returns204AndIsGone()
    {
        var created = await CreateOrderAsync("Wird storniert");

        var response = await _client.DeleteAsync($"/api/repair-orders/{created.Id}");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));

        var lookup = await _client.GetAsync($"/api/repair-orders/{created.Id}");
        Assert.That(lookup.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    /// <summary>
    /// An invoiced order is part of the billing record: DELETE is a business-rule
    /// violation (400, not 500), and the order must still be there afterwards.
    /// </summary>
    [Test]
    public async Task DeleteRepairOrder_Invoiced_Returns400AndKeepsOrder()
    {
        var created = await CreateOrderAsync("Abgerechnet, darf nicht verschwinden");
        await ChangeStatusAsync(created.Id, RepairOrderStatus.InProgress);
        await ChangeStatusAsync(created.Id, RepairOrderStatus.Completed);
        await ChangeStatusAsync(created.Id, RepairOrderStatus.Invoiced);

        var response = await _client.DeleteAsync($"/api/repair-orders/{created.Id}");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(await response.Content.ReadAsStringAsync(), Does.Contain("Invoiced"));

        var stillThere = await GetAsync<RepairOrderDto>($"/api/repair-orders/{created.Id}");
        Assert.That(stillThere.Status, Is.EqualTo(RepairOrderStatus.Invoiced));
    }

    [Test]
    public async Task CreateRepairOrder_UnknownVehicle_Returns404()
    {
        var response = await _client.PostAsJsonAsync("/api/repair-orders", new
        {
            vehicleId = 999999,
            description = "Fahrzeug gibt es nicht"
        });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task CreateRepairOrder_BlankDescription_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/repair-orders", new
        {
            vehicleId = 1,
            description = ""
        });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    /// <summary>Exercises the stored procedure against a real SQL Server.</summary>
    [Test]
    public async Task RevenueByCustomer_AggregatesInvoicedOrders()
    {
        var report = await GetAsync<List<CustomerRevenueDto>>(
            "/api/reports/revenue-by-customer?from=2000-01-01&to=2100-01-01");

        Assert.That(report, Is.Not.Empty, "the seed contains invoiced orders");

        Assert.Multiple(() =>
        {
            Assert.That(
                report.Select(r => r.TotalRevenue),
                Is.Ordered.Descending,
                "the procedure orders by revenue");

            Assert.That(
                report,
                Has.All.Matches<CustomerRevenueDto>(
                    r => r.TotalRevenue == r.LabourRevenue + r.PartsRevenue),
                "labour plus parts must equal the total");
        });
    }

    [Test]
    public async Task RevenueByCustomer_InvertedPeriod_Returns400()
    {
        var response = await _client.GetAsync(
            "/api/reports/revenue-by-customer?from=2100-01-01&to=2000-01-01");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task Swagger_DocumentIsServed()
    {
        var response = await _client.GetAsync("/swagger/v1/swagger.json");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(await response.Content.ReadAsStringAsync(), Does.Contain("/api/repair-orders"));
    }

    private async Task<RepairOrderDto> CreateOrderAsync(string description)
    {
        var vehicles = await GetAsync<List<VehicleDto>>("/api/vehicles");
        var response = await _client.PostAsJsonAsync("/api/repair-orders", new
        {
            vehicleId = vehicles[0].Id,
            description
        });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        return await ReadAsync<RepairOrderDto>(response);
    }

    private async Task<RepairOrderDto> ChangeStatusAsync(int id, RepairOrderStatus target)
    {
        var response = await _client.PatchAsJsonAsync(
            $"/api/repair-orders/{id}/status",
            new { targetStatus = target.ToString() });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), $"transition to {target} should succeed");
        return await ReadAsync<RepairOrderDto>(response);
    }

    private async Task<T> GetAsync<T>(string url)
    {
        var response = await _client.GetAsync(url);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), $"GET {url}");
        return await ReadAsync<T>(response);
    }

    private async Task<T> PatchOrPostAsync<T>(string url, object payload)
    {
        var response = await _client.PostAsJsonAsync(url, payload);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), $"POST {url}");
        return await ReadAsync<T>(response);
    }

    private static async Task<T> ReadAsync<T>(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<T>(JsonOptions)
        ?? throw new InvalidOperationException($"Response body was null for {response.RequestMessage?.RequestUri}");
}
