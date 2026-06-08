using SafeEye.Application.GuardianDevices.Commands;
using SafeEye.Application.GuardianDevices.Queries;
using SafeEye.API.Filters;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SafeEye.API.Controllers;

[ApiController]
[Route("api/guardian-devices")]
[Authorize]
public sealed class GuardianDevicesController(ISender sender) : ControllerBase
{
    /// <summary>Add a device to the guardian's watch list using its device key.</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Add([FromBody] AddDeviceRequest req, CancellationToken ct)
    {
        var result = await sender.Send(
            new AddGuardianDeviceCommand(HttpContext.GetUserId(), req.DeviceKey, req.Label), ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>List all watched devices with latest location and SOS status.</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
        => Ok(await sender.Send(new GetGuardianDevicesQuery(HttpContext.GetUserId()), ct));

    /// <summary>Get a single watched device by entry ID.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        => Ok(await sender.Send(new GetGuardianDeviceQuery(HttpContext.GetUserId(), id), ct));

    /// <summary>Update the display label for a watched device.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateLabel(Guid id, [FromBody] UpdateLabelRequest req, CancellationToken ct)
        => Ok(await sender.Send(
            new UpdateGuardianDeviceLabelCommand(HttpContext.GetUserId(), id, req.Label), ct));

    /// <summary>Stop watching a device.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Remove(Guid id, CancellationToken ct)
    {
        await sender.Send(new RemoveGuardianDeviceCommand(HttpContext.GetUserId(), id), ct);
        return NoContent();
    }
}

public record AddDeviceRequest(string DeviceKey, string Label);
public record UpdateLabelRequest(string Label);