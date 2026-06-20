using SafeEye.Application.IoT.Commands;
using SafeEye.Application.IoT.Queries;
using SafeEye.API.Filters;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace SafeEye.API.Controllers;

[ApiController]
[Route("api/iot")]
public sealed class IoTController(ISender sender) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
        => Ok(await sender.Send(new GetAllDevicesQuery(), ct));

    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> Register([FromBody] RegisterDeviceRequest req, CancellationToken ct)
    {
        var result = await sender.Send(new RegisterIoTDeviceCommand(req.Label, req.FirebaseDeviceKey, req.FirebaseUserId), ct);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>REST fallback — normally SOS is detected via Firebase RTDB listener.</summary>
    [HttpPost("sos")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult PostSos()
    {
        HttpContext.GetUserId();
        return Ok(new { message = "SOS signal received." });
    }
    // POST /location removed — device writes directly to Firebase RTDB
}

public record RegisterDeviceRequest(string Label, string? FirebaseDeviceKey = null, string? FirebaseUserId = null);
public record SosRequest(double? Latitude = null, double? Longitude = null);