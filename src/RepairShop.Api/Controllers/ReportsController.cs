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
    /// <param name="from">Start of the period (inclusive, UTC). Defaults to one year ago.</param>
    /// <param name="to">End of the period (inclusive, UTC). Defaults to now.</param>
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
        var toUtc = to ?? DateTime.UtcNow;
        var fromUtc = from ?? toUtc.AddYears(-1);

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
}
