namespace RepairShop.Application.Reporting;

/// <summary>
/// Read-side port for the revenue report.
/// Separate from the repositories on purpose: this is a reporting query, not an
/// aggregate load, and its implementation is raw T-SQL rather than LINQ
/// (see the "Design decisions" section of the README).
/// </summary>
public interface IRevenueReportQuery
{
    /// <summary>
    /// Revenue per customer over invoiced orders received in the given period,
    /// highest revenue first.
    /// </summary>
    Task<IReadOnlyList<CustomerRevenueDto>> GetRevenueByCustomerAsync(
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken = default);
}
