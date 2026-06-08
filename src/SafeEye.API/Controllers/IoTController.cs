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
        var result = await sender.Send(new RegisterIoTDeviceCommand(req.Label, req.FirebaseDeviceKey), ct);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>REST fallback — normally SOS is detected via Firebase RTDB listener.</summary>
    [HttpPost("sos")]
    [ServiceFilter(typeof(DeviceAuthFilter))]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> PostSos([FromBody] SosRequest req, CancellationToken ct)
    {
        var device = HttpContext.GetDevice();
        await sender.Send(new HandleSosTriggerCommand(device.Id, req.Latitude, req.Longitude), ct);
        return StatusCode(StatusCodes.Status201Created, new { message = "SOS signal sent." });
    }
    // POST /location removed — device writes directly to Firebase RTDB
}

public record RegisterDeviceRequest(string Label, string? FirebaseDeviceKey = null);
public record SosRequest(double? Latitude = null, double? Longitude = null);