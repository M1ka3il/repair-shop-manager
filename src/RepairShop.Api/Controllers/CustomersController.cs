using Microsoft.AspNetCore.Mvc;
using RepairShop.Application.Customers;

namespace RepairShop.Api.Controllers;

/// <summary>
/// Customers and their vehicles. Read-only — see the README section
/// "What was deliberately left out".
/// </summary>
[ApiController]
[Route("api")]
[Produces("application/json")]
public class CustomersController(ICustomerService service) : ControllerBase
{
    /// <summary>Lists all customers including their vehicles.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">The customers.</response>
    [HttpGet("customers")]
    [ProducesResponseType(typeof(IReadOnlyList<CustomerDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CustomerDto>>> ListCustomers(CancellationToken cancellationToken)
        => Ok(await service.ListCustomersAsync(cancellationToken));

    /// <summary>Gets one customer including their vehicles.</summary>
    /// <param name="id">Customer id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">The customer.</response>
    /// <response code="404">No customer with that id.</response>
    [HttpGet("customers/{id:int}")]
    [ProducesResponseType(typeof(CustomerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CustomerDto>> GetCustomer(int id, CancellationToken cancellationToken)
        => Ok(await service.GetCustomerAsync(id, cancellationToken));

    /// <summary>Lists all vehicles across customers — useful for picking a VehicleId when creating an order.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">The vehicles.</response>
    [HttpGet("vehicles")]
    [ProducesResponseType(typeof(IReadOnlyList<VehicleDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<VehicleDto>>> ListVehicles(CancellationToken cancellationToken)
        => Ok(await service.ListVehiclesAsync(cancellationToken));
}
