using Microsoft.Extensions.DependencyInjection;
using RepairShop.Application.Customers;
using RepairShop.Application.RepairOrders;

namespace RepairShop.Application;

/// <summary>
/// Registration for everything this layer owns. Each layer exposes exactly one
/// of these, so <c>Program.cs</c> stays a list of layers rather than a list of
/// services.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IRepairOrderService, RepairOrderService>();
        services.AddScoped<ICustomerService, CustomerService>();
        return services;
    }
}
