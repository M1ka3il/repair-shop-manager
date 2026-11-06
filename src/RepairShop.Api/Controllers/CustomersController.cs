using Microsoft.AspNetCore.Mvc;
using RepairShop.Application.Customers;

namespace RepairShop.Api.Controllers;

/// <summary>
/// Customers and their vehicles: create, read and correct. No delete — see the
/// README section "What was deliberately left out".
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
    [HttpGet("customers/{id:int}", Name = nameof(GetCustomer))]
    [ProducesResponseType(typeof(CustomerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CustomerDto>> GetCustomer(int id, CancellationToken cancellationToken)
        => Ok(await service.GetCustomerAsync(id, cancellationToken));

    /// <summary>Creates a customer without vehicles.</summary>
    /// <param name="request">Name and optional contact details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="201">The new customer, with a Location header.</response>
    /// <response code="400">Validation failed.</response>
    [HttpPost("customers")]
    [ProducesResponseType(typeof(CustomerDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CustomerDto>> CreateCustomer(
        [FromBody] SaveCustomerRequest request,
        CancellationToken cancellationToken)
    {
        var created = await service.CreateCustomerAsync(request, cancellationToken);
        return CreatedAtRoute(nameof(GetCustomer), new { id = created.Id }, created);
    }

    /// <summary>Corrects a customer's name and contact details.</summary>
    /// <param name="id">Customer id.</param>
    /// <param name="request">The complete new state; omitted contact fields are cleared.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">The updated customer.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="404">No customer with that id.</response>
    [HttpPut("customers/{id:int}")]
    [ProducesResponseType(typeof(CustomerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CustomerDto>> UpdateCustomer(
        int id,
        [FromBody] SaveCustomerRequest request,
        CancellationToken cancellationToken)
        => Ok(await service.UpdateCustomerAsync(id, request, cancellationToken));

    /// <summary>Registers a vehicle for an existing customer.</summary>
    /// <param name="id">Customer id.</param>
    /// <param name="request">Plate, make, model and year.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="201">The new vehicle, with a Location header.</response>
    /// <response code="400">Validation failed or the plate is malformed.</response>
    /// <response code="404">No customer with that id.</response>
    /// <response code="409">The plate is already registered.</response>
    [HttpPost("customers/{id:int}/vehicles")]
    [ProducesResponseType(typeof(VehicleDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<VehicleDto>> AddVehicle(
        int id,
        [FromBody] SaveVehicleRequest request,
        CancellationToken cancellationToken)
    {
        var created = await service.AddVehicleAsync(id, request, cancellationToken);
        return CreatedAtRoute(nameof(GetVehicle), new { id = created.Id }, created);
    }

    /// <summary>Gets one vehicle.</summary>
    /// <param name="id">Vehicle id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">The vehicle.</response>
    /// <response code="404">No vehicle with that id.</response>
    [HttpGet("vehicles/{id:int}", Name = nameof(GetVehicle))]
    [ProducesResponseType(typeof(VehicleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VehicleDto>> GetVehicle(int id, CancellationToken cancellationToken)
        => Ok(await service.GetVehicleAsync(id, cancellationToken));

    /// <summary>Corrects a vehicle's plate, make, model or year. The owner cannot be changed.</summary>
    /// <param name="id">Vehicle id.</param>
    /// <param name="request">The complete new state.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">The updated vehicle.</response>
    /// <response code="400">Validation failed or the plate is malformed.</response>
    /// <response code="404">No vehicle with that id.</response>
    /// <response code="409">The new plate belongs to another vehicle.</response>
    [HttpPut("vehicles/{id:int}")]
    [ProducesResponseType(typeof(VehicleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<VehicleDto>> UpdateVehicle(
        int id,
        [FromBody] SaveVehicleRequest request,
        CancellationToken cancellationToken)
        => Ok(await service.UpdateVehicleAsync(id, request, cancellationToken));

    /// <summary>Lists all vehicles across customers — useful for picking a VehicleId when creating an order.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">The vehicles.</response>
    [HttpGet("vehicles")]
    [ProducesResponseType(typeof(IReadOnlyList<VehicleDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<VehicleDto>>> ListVehicles(CancellationToken cancellationToken)
        => Ok(await service.ListVehiclesAsync(cancellationToken));
}
