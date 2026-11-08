using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RepairShop.Application.Auth;
using RepairShop.Infrastructure.Identity;
using RepairShop.Application.Abstractions;
using RepairShop.Application.Reporting;
using RepairShop.Infrastructure.Persistence;
using RepairShop.Infrastructure.Persistence.Repositories;
using RepairShop.Infrastructure.Reporting;

namespace RepairShop.Infrastructure;

/// <summary>Registration for the persistence, reporting and identity adapters.</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, string connectionString, IConfiguration configuration)
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

        // Fails at startup, not at first login, when the signing key is missing or short.
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddOptions<DemoUserOptions>().Bind(configuration.GetSection(DemoUserOptions.SectionName));
        services.AddSingleton<IPasswordHasher<UserAccount>, PasswordHasher<UserAccount>>();
        services.AddScoped<IAuthService, AuthService>();

        return services;
    }
}
