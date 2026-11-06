using Microsoft.AspNetCore.Mvc;
using RepairShop.Application.Reporting;

namespace RepairShop.Api.Controllers;

/// <summary>Aggregated evaluations. Backed by T-SQL rather than LINQ — see the README.</summary>
[ApiController]
[Route("api/reports")]
[Produces("application/json")]
public class ReportsController(IRevenueReportQuery revenueReport) : ControllerBase
{
    /// <summary>
    /// Revenue per customer over invoiced orders, split into labour and parts,
    /// highest revenue first.
    /// </summary>
    /// <param name="from">Start of the period (inclusive). ISO 8601; with an offset it is converted to UTC, without one it is taken as UTC. Defaults to one year before <paramref name="to"/>.</param>
    /// <param name="to">End of the period (inclusive, a point in time — send the end of the last day, not just its date). Same format as <paramref name="from"/>. Defaults to now.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">One row per customer with revenue in the period.</response>
    /// <response code="400"><paramref name="from"/> is after <paramref name="to"/>.</response>
    [HttpGet("revenue-by-customer")]
    [ProducesResponseType(typeof(IReadOnlyList<CustomerRevenueDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<CustomerRevenueDto>>> RevenueByCustomer(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken cancellationToken)
    {
        var toUtc = to is { } t ? AsUtc(t) : DateTime.UtcNow;
        var fromUtc = from is { } f ? AsUtc(f) : toUtc.AddYears(-1);

        if (fromUtc > toUtc)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid period",
                Detail = "'from' must not be later than 'to'."
            });
        }

        return Ok(await revenueReport.GetRevenueByCustomerAsync(fromUtc, toUtc, cancellationToken));
    }

    /// <summary>
    /// Model binding turns "…Z" or "…+01:00" into a Local DateTime in the server's
    /// time zone; the stored procedure compares raw values against UTC columns.
    /// Local is converted back; Unspecified (no offset given) is taken as UTC,
    /// the same convention the persistence layer uses.
    /// </summary>
    private static DateTime AsUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };
}
