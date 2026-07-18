using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace RepairShop.Infrastructure.Persistence;

/// <summary>
/// Used only by <c>dotnet ef migrations</c>. Without it the tooling would have to
/// boot the API — and therefore need a reachable database — just to read the model.
/// The connection string here is never used to connect; it only satisfies the
/// SQL Server provider so it can generate provider-specific DDL.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<RepairShopDbContext>
{
    public RepairShopDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("REPAIRSHOP_DESIGNTIME_CONNECTION")
            ?? "Server=localhost,1433;Database=RepairShop;User Id=sa;Password=placeholder;TrustServerCertificate=True";

        var options = new DbContextOptionsBuilder<RepairShopDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new RepairShopDbContext(options);
    }
}
