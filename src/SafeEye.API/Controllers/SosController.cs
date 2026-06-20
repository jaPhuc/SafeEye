using SafeEye.Application.Sos.Commands;
using SafeEye.Application.Sos.Queries;
using SafeEye.API.Filters;
using SafeEye.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SafeEye.API.Controllers;

[ApiController]
[Route("api/sos")]
[Authorize]
public sealed class SosController(ISender sender) : ControllerBase
{
    /// <summary>
    /// List SOS events from all watched devices.
    /// Optional: ?status=Active or ?status=Resolved
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] SosStatus? status, CancellationToken ct)
        => Ok(await sender.Send(new GetSosEventsQuery(HttpContext.GetUserId(), status), ct));

    /// <summary>Get a single SOS event by ID.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        => Ok(await sender.Send(new GetSosEventQuery(HttpContext.GetUserId(), id), ct));

    /// <summary>Mark a SOS event as resolved.</summary>
    [HttpPut("{id:guid}/resolve")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Resolve(Guid id, CancellationToken ct)
    {
        await sender.Send(new ResolveSosEventCommand(HttpContext.GetUserId(), id), ct);
        return NoContent();
    }

    /// <summary>Resolve a Firebase SOS request by push ID.</summary>
    [HttpPut("firebase/{pushId}/resolve")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ResolveFirebase(string pushId, CancellationToken ct)
    {
        await sender.Send(new ResolveFirebaseSosCommand(pushId, HttpContext.GetUserId()), ct);
        return NoContent();
    }
}