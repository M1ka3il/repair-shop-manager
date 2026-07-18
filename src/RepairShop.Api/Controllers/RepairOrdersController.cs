using Microsoft.AspNetCore.Mvc;
using RepairShop.Application.RepairOrders;
using RepairShop.Domain.Entities;

namespace RepairShop.Api.Controllers;

/// <summary>
/// Repair orders — the main entity of this API.
/// Every response is a DTO; domain entities never cross this boundary.
/// </summary>
[ApiController]
[Route("api/repair-orders")]
[Produces("application/json")]
public class RepairOrdersController(IRepairOrderService service) : ControllerBase
{
    /// <summary>Lists repair orders, newest first.</summary>
    /// <param name="status">Optional status filter, e.g. <c>InProgress</c>.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">The matching orders.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<RepairOrderDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<RepairOrderDto>>> List(
        [FromQuery] RepairOrderStatus? status,
        CancellationToken cancellationToken)
        => Ok(await service.ListAsync(status, cancellationToken));

    /// <summary>Gets one repair order including its labour and parts lines.</summary>
    /// <param name="id">Order id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">The order.</response>
    /// <response code="404">No order with that id.</response>
    [HttpGet("{id:int}", Name = nameof(GetRepairOrder))]
    [ProducesResponseType(typeof(RepairOrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RepairOrderDto>> GetRepairOrder(int id, CancellationToken cancellationToken)
        => Ok(await service.GetAsync(id, cancellationToken));

    /// <summary>Creates a repair order for an existing vehicle. It starts in status <c>Received</c>.</summary>
    /// <param name="request">Vehicle and problem description.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="201">Created; the Location header points at the new order.</response>
    /// <response code="400">The payload failed validation.</response>
    /// <response code="404">The referenced vehicle does not exist.</response>
    [HttpPost]
    [ProducesResponseType(typeof(RepairOrderDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RepairOrderDto>> Create(
        [FromBody] CreateRepairOrderRequest request,
        CancellationToken cancellationToken)
    {
        var created = await service.CreateAsync(request, cancellationToken);
        return CreatedAtRoute(nameof(GetRepairOrder), new { id = created.Id }, created);
    }

    /// <summary>Updates the problem description. Not usable for status changes — see <c>PATCH /status</c>.</summary>
    /// <param name="id">Order id.</param>
    /// <param name="request">New description.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">The updated order.</response>
    /// <response code="400">Validation failed, or the order is already invoiced.</response>
    /// <response code="404">No order with that id.</response>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(RepairOrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RepairOrderDto>> Update(
        int id,
        [FromBody] UpdateRepairOrderRequest request,
        CancellationToken cancellationToken)
        => Ok(await service.UpdateAsync(id, request, cancellationToken));

    /// <summary>Deletes a repair order and its line items.</summary>
    /// <param name="id">Order id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="204">Deleted.</response>
    /// <response code="404">No order with that id.</response>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await service.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Moves the order to another status.
    /// <para>
    /// A dedicated endpoint rather than a field on the generic PUT: the status is
    /// not an editable attribute but a transition that either is or is not allowed
    /// (Received → InProgress ⇄ WaitingForParts → Completed → Invoiced). An
    /// illegal move is a business error and answered with 400, never a 500.
    /// </para>
    /// </summary>
    /// <param name="id">Order id.</param>
    /// <param name="request">Target status.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">The order in its new status.</response>
    /// <response code="400">The transition is not allowed from the current status.</response>
    /// <response code="404">No order with that id.</response>
    [HttpPatch("{id:int}/status")]
    [ProducesResponseType(typeof(RepairOrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RepairOrderDto>> ChangeStatus(
        int id,
        [FromBody] ChangeStatusRequest request,
        CancellationToken cancellationToken)
        => Ok(await service.ChangeStatusAsync(id, request, cancellationToken));

    /// <summary>Books labour or a spare part onto the order.</summary>
    /// <param name="id">Order id.</param>
    /// <param name="request">The line to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">The order including the new line.</response>
    /// <response code="400">Validation failed, or the order is completed or invoiced.</response>
    /// <response code="404">No order with that id.</response>
    [HttpPost("{id:int}/items")]
    [ProducesResponseType(typeof(RepairOrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RepairOrderDto>> AddItem(
        int id,
        [FromBody] AddRepairItemRequest request,
        CancellationToken cancellationToken)
        => Ok(await service.AddItemAsync(id, request, cancellationToken));

    /// <summary>Removes a line item from the order.</summary>
    /// <param name="id">Order id.</param>
    /// <param name="itemId">Line item id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">The order without that line.</response>
    /// <response code="400">The order is completed or invoiced, or the item belongs to another order.</response>
    /// <response code="404">No order with that id.</response>
    [HttpDelete("{id:int}/items/{itemId:int}")]
    [ProducesResponseType(typeof(RepairOrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RepairOrderDto>> RemoveItem(
        int id,
        int itemId,
        CancellationToken cancellationToken)
        => Ok(await service.RemoveItemAsync(id, itemId, cancellationToken));
}
