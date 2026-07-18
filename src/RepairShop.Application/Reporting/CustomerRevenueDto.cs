namespace RepairShop.Application.Reporting;

/// <summary>One row of the revenue-per-customer report.</summary>
/// <param name="CustomerId">Customer primary key.</param>
/// <param name="CustomerName">Customer name at the time of the query.</param>
/// <param name="OrderCount">Number of invoiced orders in the period.</param>
/// <param name="LabourRevenue">Net revenue from labour lines.</param>
/// <param name="PartsRevenue">Net revenue from spare-part lines.</param>
/// <param name="TotalRevenue">Sum of labour and parts.</param>
public record CustomerRevenueDto(
    int CustomerId,
    string CustomerName,
    int OrderCount,
    decimal LabourRevenue,
    decimal PartsRevenue,
    decimal TotalRevenue);
