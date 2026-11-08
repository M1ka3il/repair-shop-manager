using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RepairShop.Infrastructure.Persistence;

namespace RepairShop.IntegrationTests;

/// <summary>
/// The AddInvoicedAtUtc migration rewrites data that already exists in
/// production, so it is tested the way it will run there: on a database at the
/// previous schema version that already holds an invoiced order.
/// Uses its own database on the shared container, so the API's data is untouched.
/// </summary>
[TestFixture]
public class InvoiceDateMigrationTests
{
    private const string PreviousMigration = "20260718191028_AddRevenueByCustomerProcedure";

    [Test]
    public async Task ExistingInvoicedOrders_AreBackfilledAndReportedByThatDate()
    {
        IntegrationTestContext.EnsureDockerAvailable();

        var connectionString = new SqlConnectionStringBuilder(IntegrationTestContext.Factory.ConnectionString)
        {
            InitialCatalog = $"MigrationTest_{Guid.NewGuid():N}"
        }.ConnectionString;

        await using var db = new RepairShopDbContext(
            new DbContextOptionsBuilder<RepairShopDbContext>().UseSqlServer(connectionString).Options);
        var migrator = db.GetService<IMigrator>();

        // 1. Schema as it was before: no InvoicedAtUtc column.
        await migrator.MigrateAsync(PreviousMigration);

        // 2. Legacy data: invoiced, completed on 10 March, received on 1 March.
        await db.Database.ExecuteSqlRawAsync("""
            INSERT INTO dbo.Customers (Name) VALUES (N'Altkunde');
            INSERT INTO dbo.Vehicles (CustomerId, LicensePlate, Make, Model, Year)
                VALUES (SCOPE_IDENTITY(), N'M-AL 1', N'VW', N'Golf', 2015);
            INSERT INTO dbo.RepairOrders (VehicleId, Status, ReceivedAtUtc, CompletedAtUtc, Description)
                VALUES (SCOPE_IDENTITY(), 5, '2026-03-01T08:00:00', '2026-03-10T16:00:00', N'Altauftrag');
            INSERT INTO dbo.RepairItems (RepairOrderId, Type, Designation, UnitPrice, Quantity)
                VALUES (SCOPE_IDENTITY(), 1, N'Inspektion', 100.00, 1.00);
            """);

        // 3. Upgrade.
        await migrator.MigrateAsync();

        var invoicedAt = await db.Database
            .SqlQueryRaw<DateTime?>("SELECT InvoicedAtUtc AS Value FROM dbo.RepairOrders")
            .SingleAsync();
        Assert.That(invoicedAt, Is.EqualTo(new DateTime(2026, 3, 10, 16, 0, 0)),
            "backfilled from CompletedAtUtc, the closest known date");

        // The procedure now reports by invoice date: found in a window around
        // 10 March, but no longer in one around the intake date of 1 March.
        Assert.Multiple(async () =>
        {
            Assert.That(await RevenueRowsAsync(db, "2026-03-10T00:00:00", "2026-03-10T23:59:59"), Is.EqualTo(1));
            Assert.That(await RevenueRowsAsync(db, "2026-03-01T00:00:00", "2026-03-01T23:59:59"), Is.Zero);
        });

        await db.Database.EnsureDeletedAsync();
    }

    private static async Task<int> RevenueRowsAsync(RepairShopDbContext db, string from, string to)
    {
        var connection = db.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync();
        }

        await using var command = connection.CreateCommand();
        command.CommandText = "dbo.usp_GetRevenueByCustomer";
        command.CommandType = System.Data.CommandType.StoredProcedure;
        command.Parameters.Add(new SqlParameter("@FromUtc", DateTime.Parse(from)));
        command.Parameters.Add(new SqlParameter("@ToUtc", DateTime.Parse(to)));

        await using var reader = await command.ExecuteReaderAsync();
        var rows = 0;
        while (await reader.ReadAsync())
        {
            rows++;
        }
        return rows;
    }
}
