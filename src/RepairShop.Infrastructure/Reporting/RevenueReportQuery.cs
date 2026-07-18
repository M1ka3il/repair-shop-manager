using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using RepairShop.Application.Reporting;
using RepairShop.Infrastructure.Persistence;

namespace RepairShop.Infrastructure.Reporting;

/// <summary>
/// Calls the stored procedure <c>dbo.usp_GetRevenueByCustomer</c>.
/// <para>
/// Why a procedure and not LINQ: this report aggregates across all four tables
/// and pivots labour against parts in the same pass. In LINQ that becomes a
/// group-join with two conditional sums whose generated SQL is neither readable
/// nor tunable, and the shape returned has nothing to do with the entity model.
/// As a procedure the execution plan is stable, a DBA can index for it, and the
/// query is reviewable as SQL. Everything transactional stays in EF Core —
/// this is the deliberate exception, not the house style.
/// </para>
/// </summary>
public class RevenueReportQuery(RepairShopDbContext db) : IRevenueReportQuery
{
    public async Task<IReadOnlyList<CustomerRevenueDto>> GetRevenueByCustomerAsync(
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken = default)
    {
        var results = new List<CustomerRevenueDto>();

        var connection = db.Database.GetDbConnection();
        await using var command = connection.CreateCommand();
        command.CommandText = "dbo.usp_GetRevenueByCustomer";
        command.CommandType = CommandType.StoredProcedure;
        command.Parameters.Add(new SqlParameter("@FromUtc", SqlDbType.DateTime2) { Value = fromUtc });
        command.Parameters.Add(new SqlParameter("@ToUtc", SqlDbType.DateTime2) { Value = toUtc });

        var wasClosed = connection.State == ConnectionState.Closed;
        if (wasClosed)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                results.Add(new CustomerRevenueDto(
                    reader.GetInt32(reader.GetOrdinal("CustomerId")),
                    reader.GetString(reader.GetOrdinal("CustomerName")),
                    reader.GetInt32(reader.GetOrdinal("OrderCount")),
                    reader.GetDecimal(reader.GetOrdinal("LabourRevenue")),
                    reader.GetDecimal(reader.GetOrdinal("PartsRevenue")),
                    reader.GetDecimal(reader.GetOrdinal("TotalRevenue"))));
            }
        }
        finally
        {
            // Only close what we opened — the context may still need the
            // connection for the rest of the request.
            if (wasClosed)
            {
                await connection.CloseAsync();
            }
        }

        return results;
    }
}
