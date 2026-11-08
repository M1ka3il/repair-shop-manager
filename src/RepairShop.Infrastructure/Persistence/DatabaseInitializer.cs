using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RepairShop.Application.Auth;
using RepairShop.Infrastructure.Identity;
using RepairShop.Domain.Entities;
using RepairShop.Domain.ValueObjects;

namespace RepairShop.Infrastructure.Persistence;

/// <summary>
/// Applies migrations and seeds demo data on startup.
/// <para>
/// Running migrations from the app is a deliberate trade-off: it makes
/// <c>docker compose up</c> a single command, which is the point of this repo.
/// In a real multi-instance deployment this would move to a separate migration
/// step so that two instances cannot race on the same schema change.
/// </para>
/// </summary>
public class DatabaseInitializer(
    RepairShopDbContext db,
    IPasswordHasher<UserAccount> hasher,
    IOptions<DemoUserOptions> demoUsers,
    ILogger<DatabaseInitializer> logger)
{
    public async Task InitialiseAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Applying database migrations");
        await db.Database.MigrateAsync(cancellationToken);

        // Independent of the demo data below: a database seeded before logins
        // existed must still get its users after the upgrade.
        await SeedUsersAsync(cancellationToken);

        if (await db.Customers.AnyAsync(cancellationToken))
        {
            logger.LogInformation("Database already seeded, skipping");
            return;
        }

        logger.LogInformation("Seeding demo data");
        await SeedAsync(cancellationToken);
    }

    private async Task SeedUsersAsync(CancellationToken cancellationToken)
    {
        if (await db.UserAccounts.AnyAsync(cancellationToken))
        {
            return;
        }

        var passwords = demoUsers.Value;
        if (string.IsNullOrWhiteSpace(passwords.MechanicPassword) || string.IsNullOrWhiteSpace(passwords.OfficePassword))
        {
            logger.LogWarning(
                "No user accounts and no demo passwords configured — nobody can log in. " +
                "Set DEMO_MECHANIC_PASSWORD and DEMO_OFFICE_PASSWORD in .env (docker compose) " +
                "or DemoUsers:MechanicPassword / DemoUsers:OfficePassword in user secrets.");
            return;
        }

        db.UserAccounts.AddRange(
            CreateUser("werkstatt", Roles.Mechanic, passwords.MechanicPassword),
            CreateUser("buero", Roles.Office, passwords.OfficePassword));
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Seeded demo logins 'werkstatt' (Mechanic) and 'buero' (Office)");
    }

    private UserAccount CreateUser(string userName, string role, string password)
    {
        var user = new UserAccount(userName, role);
        user.SetPasswordHash(hasher.HashPassword(user, password));
        return user;
    }

    private async Task SeedAsync(CancellationToken cancellationToken)
    {
        // Relative to "now", so the demo data always looks recent: the default
        // report period ("this year") and the board show it on first start.
        // Note: around New Year, the invoiced orders fall into the previous year.
        var now = DateTime.UtcNow;

        var schmidt = new Customer("Kfz-Handel Schmidt GmbH", "buchhaltung@schmidt-kfz.example", "+49 89 1234567");
        var becker = new Customer("Anna Becker", "anna.becker@example.com", "+49 171 9876543");
        var yilmaz = new Customer("Mehmet Yılmaz", "m.yilmaz@example.com", null);

        db.Customers.AddRange(schmidt, becker, yilmaz);
        await db.SaveChangesAsync(cancellationToken);

        var golf = new Vehicle(schmidt.Id, LicensePlate.Create("M-SD 4711"), "Volkswagen", "Golf VII", 2016);
        var sprinter = new Vehicle(schmidt.Id, LicensePlate.Create("M-SD 815"), "Mercedes-Benz", "Sprinter 316", 2019);
        var corsa = new Vehicle(becker.Id, LicensePlate.Create("M-AB 1234"), "Opel", "Corsa F", 2021);
        var octavia = new Vehicle(yilmaz.Id, LicensePlate.Create("FS-MY 90"), "Škoda", "Octavia III", 2014);

        db.Vehicles.AddRange(golf, sprinter, corsa, octavia);
        await db.SaveChangesAsync(cancellationToken);

        // One order per status, so the API returns something meaningful on the
        // very first call and every branch of the lifecycle is visible.
        var received = new RepairOrder(corsa.Id, "Warnleuchte Reifendruck leuchtet dauerhaft", now.AddDays(-1));

        var inProgress = new RepairOrder(golf.Id, "Bremsen quietschen beim Bremsen, Belagwechsel vorne", now.AddDays(-3));
        inProgress.TransitionTo(RepairOrderStatus.InProgress, now.AddDays(-3).AddHours(2));
        inProgress.AddItem(RepairItemType.Labour, "Bremsbeläge vorne wechseln", 89.00m, 1.5m);
        inProgress.AddItem(RepairItemType.Part, "Bremsbelagsatz vorne", 74.90m, 1m);

        var waiting = new RepairOrder(sprinter.Id, "Anlasser dreht nicht durch", now.AddDays(-6));
        waiting.TransitionTo(RepairOrderStatus.InProgress, now.AddDays(-6).AddHours(3));
        waiting.TransitionTo(RepairOrderStatus.WaitingForParts, now.AddDays(-5));
        waiting.AddItem(RepairItemType.Labour, "Fehlerdiagnose Startsystem", 89.00m, 1m);

        var completed = new RepairOrder(octavia.Id, "Inspektion inkl. Ölwechsel", now.AddDays(-12));
        completed.TransitionTo(RepairOrderStatus.InProgress, now.AddDays(-12).AddHours(1));
        completed.AddItem(RepairItemType.Labour, "Inspektion nach Herstellervorgabe", 89.00m, 2m);
        completed.AddItem(RepairItemType.Part, "Motoröl 5W-30 (5 l)", 12.50m, 5m);
        completed.AddItem(RepairItemType.Part, "Ölfilter", 18.90m, 1m);
        completed.TransitionTo(RepairOrderStatus.Completed, now.AddDays(-11));

        var invoiced = new RepairOrder(golf.Id, "HU/AU-Vorbereitung, Achsvermessung", now.AddDays(-30));
        invoiced.TransitionTo(RepairOrderStatus.InProgress, now.AddDays(-30).AddHours(2));
        invoiced.AddItem(RepairItemType.Labour, "Achsvermessung", 89.00m, 1m);
        invoiced.AddItem(RepairItemType.Labour, "Spur einstellen", 89.00m, 0.75m);
        invoiced.AddItem(RepairItemType.Part, "Spurstangenkopf links", 42.30m, 1m);
        invoiced.TransitionTo(RepairOrderStatus.Completed, now.AddDays(-29));
        invoiced.TransitionTo(RepairOrderStatus.Invoiced, now.AddDays(-28));

        var invoicedBecker = new RepairOrder(corsa.Id, "Klimaanlage kühlt nicht", now.AddDays(-45));
        invoicedBecker.TransitionTo(RepairOrderStatus.InProgress, now.AddDays(-45).AddHours(4));
        invoicedBecker.AddItem(RepairItemType.Labour, "Klimaanlage prüfen und befüllen", 89.00m, 1.25m);
        invoicedBecker.AddItem(RepairItemType.Part, "Kältemittel R1234yf", 95.00m, 1m);
        invoicedBecker.TransitionTo(RepairOrderStatus.Completed, now.AddDays(-44));
        invoicedBecker.TransitionTo(RepairOrderStatus.Invoiced, now.AddDays(-43));

        db.RepairOrders.AddRange(received, inProgress, waiting, completed, invoiced, invoicedBecker);
        await db.SaveChangesAsync(cancellationToken);
    }
}
