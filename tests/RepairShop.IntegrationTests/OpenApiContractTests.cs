using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace RepairShop.IntegrationTests;

/// <summary>
/// The Angular frontend generates its TypeScript types from a committed snapshot
/// of the OpenAPI document (<c>frontend/openapi/repair-shop-api.json</c>). This
/// test fails as soon as the API's contract drifts from that snapshot, so a
/// backend change cannot silently break the UI's types.
/// <para>
/// After an intended API change, refresh the snapshot and regenerate the types:
/// <code>
/// UPDATE_OPENAPI_SNAPSHOT=1 dotnet test tests/RepairShop.IntegrationTests --filter OpenApi
/// cd frontend &amp;&amp; npm run api:types
/// </code>
/// </para>
/// </summary>
[TestFixture]
public class OpenApiContractTests
{
    private static readonly JsonSerializerOptions Indented = new() { WriteIndented = true };

    [Test]
    public async Task OpenApiDocument_MatchesFrontendSnapshot()
    {
        IntegrationTestContext.EnsureDockerAvailable();
        using var client = IntegrationTestContext.Factory.CreateClient();

        var response = await client.GetAsync("/swagger/v1/swagger.json");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var actual = Normalise(await response.Content.ReadAsStringAsync());
        var snapshotPath = Path.Combine(FindRepositoryRoot(), "frontend", "openapi", "repair-shop-api.json");

        if (Environment.GetEnvironmentVariable("UPDATE_OPENAPI_SNAPSHOT") == "1")
        {
            Directory.CreateDirectory(Path.GetDirectoryName(snapshotPath)!);
            await File.WriteAllTextAsync(snapshotPath, actual);
            Assert.Pass($"Snapshot written to {snapshotPath}");
        }

        Assert.That(File.Exists(snapshotPath), Is.True, $"No snapshot at {snapshotPath}");
        var expected = Normalise(await File.ReadAllTextAsync(snapshotPath));

        Assert.That(
            actual,
            Is.EqualTo(expected),
            "The API contract changed. If intended: UPDATE_OPENAPI_SNAPSHOT=1 dotnet test --filter OpenApi, "
            + "then `npm run api:types` in frontend/ and commit both.");
    }

    /// <summary>Re-serialises so whitespace and line endings never cause a false alarm.</summary>
    private static string Normalise(string json) =>
        JsonNode.Parse(json)!.ToJsonString(Indented).ReplaceLineEndings("\n") + "\n";

    private static string FindRepositoryRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "RepairShop.slnx")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName
            ?? throw new InvalidOperationException("Repository root (RepairShop.slnx) not found above the test binaries.");
    }
}
