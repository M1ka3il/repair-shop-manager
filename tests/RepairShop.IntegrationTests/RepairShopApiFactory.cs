using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.MsSql;

namespace RepairShop.IntegrationTests;

/// <summary>
/// Boots the real API pipeline — controllers, middleware, EF Core — against a
/// throwaway SQL Server started by Testcontainers.
/// <para>
/// Deliberately not an in-memory provider: the parts most likely to break are
/// exactly the ones an in-memory provider fakes away (the value-object column
/// conversion, the unique index, the stored procedure). A test that cannot fail
/// on those is not worth running.
/// </para>
/// </summary>
public class RepairShopApiFactory : WebApplicationFactory<Program>, IAsyncDisposable
{
    // Same image as docker-compose.yml, so the tests exercise the version that
    // actually ships.
    private readonly MsSqlContainer _database =
        new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();

    public async Task InitialiseAsync()
    {
        await _database.StartAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:RepairShopDb", _database.GetConnectionString());

        // Test-only secrets: a signing key and the two demo passwords.
        builder.UseSetting("Jwt:SigningKey", TestAuth.SigningKey);
        builder.UseSetting("DemoUsers:MechanicPassword", TestAuth.MechanicPassword);
        builder.UseSetting("DemoUsers:OfficePassword", TestAuth.OfficePassword);
        // The suite signs in a handful of times; the throttle has its own test.
        builder.UseSetting("Auth:LoginAttemptsPerMinute", "1000");

        // The API's own startup migration runs; the seed gives the tests a known
        // fixture without duplicating it here.
        builder.UseSetting("Database:AutoMigrate", "true");
        builder.UseEnvironment("Development");
    }

    /// <summary>Connection string of the SQL Server container (default database).</summary>
    public string ConnectionString => _database.GetConnectionString();

    /// <summary>Opens a context against the test database for assertions that bypass the API.</summary>
    public IServiceScope CreateScope() => Services.CreateScope();

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await _database.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Shared fixture: one container for the whole assembly rather than one per test
/// — starting SQL Server costs far more than the isolation is worth here.
/// </summary>
[SetUpFixture]
public class IntegrationTestContext
{
    public static RepairShopApiFactory Factory { get; private set; } = null!;

    /// <summary>True when Docker was unavailable and the tests were skipped.</summary>
    public static bool DockerUnavailable { get; private set; }

    [OneTimeSetUp]
    public async Task StartAsync()
    {
        RepairShopApiFactory? factory = null;

        try
        {
            // Constructing the factory already touches the Docker endpoint, so it
            // has to happen inside the try.
            factory = new RepairShopApiFactory();
            await factory.InitialiseAsync();
        }
        catch (Exception ex)
        {
            // No Docker on this machine (common on a dev box, never in CI).
            // Skipping beats a red suite that says nothing about the code.
            DockerUnavailable = true;
            await Console.Error.WriteLineAsync($"Skipping integration tests, Docker unavailable: {ex.Message}");

            if (factory is not null)
            {
                await factory.DisposeAsync();
            }

            return;
        }

        Factory = factory;
    }

    [OneTimeTearDown]
    public async Task StopAsync()
    {
        if (!DockerUnavailable)
        {
            await Factory.DisposeAsync();
        }
    }

    /// <summary>Call at the start of every test so a Docker-less machine skips instead of failing.</summary>
    public static void EnsureDockerAvailable()
    {
        if (DockerUnavailable)
        {
            Assert.Ignore("Docker is not available on this machine.");
        }
    }
}
