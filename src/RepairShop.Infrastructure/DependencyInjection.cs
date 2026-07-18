using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RepairShop.Application.Abstractions;
using RepairShop.Application.Reporting;
using RepairShop.Infrastructure.Persistence;
using RepairShop.Infrastructure.Persistence.Repositories;
using RepairShop.Infrastructure.Reporting;

namespace RepairShop.Infrastructure;

/// <summary>Registration for the persistence and reporting adapters.</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<RepairShopDbContext>(options =>
            options.UseSqlServer(connectionString, sql =>
            {
                sql.MigrationsAssembly(typeof(RepairShopDbContext).Assembly.FullName);

                // The API container regularly wins the race against SQL Server's
                // startup; without this the first requests fail on a cold compose up.
                sql.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(10),
                    errorNumbersToAdd: null);
            }));

        // One DbContext instance serves both roles per request, so the
        // repositories and the unit of work share a change tracker.
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<RepairShopDbContext>());

        services.AddScoped<IRepairOrderRepository, RepairOrderRepository>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IVehicleRepository, VehicleRepository>();
        services.AddScoped<IRevenueReportQuery, RevenueReportQuery>();

        services.AddSingleton<IClock, SystemClock>();
        services.AddScoped<DatabaseInitializer>();

        return services;
    }
}
