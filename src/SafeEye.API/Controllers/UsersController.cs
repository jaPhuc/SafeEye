using SafeEye.Application.Users.Commands;
using SafeEye.Application.Users.Queries;
using SafeEye.API.Filters;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SafeEye.API.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public sealed class UsersController(ISender sender) : ControllerBase
{
    /// <summary>Get the current guardian's profile.</summary>
    [HttpGet("me")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMe(CancellationToken ct)
        => Ok(await sender.Send(new GetCurrentUserQuery(HttpContext.GetUserId()), ct));

    /// <summary>Update name and/or password.</summary>
    [HttpPut("me")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> UpdateMe([FromBody] UpdateMeRequest req, CancellationToken ct)
        => Ok(await sender.Send(
            new UpdateUserCommand(HttpContext.GetUserId(), req.Name, req.CurrentPassword, req.NewPassword), ct));

    /// <summary>Register or refresh FCM token for push notifications.</summary>
    [HttpPut("me/fcm-token")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateFcmToken([FromBody] UpdateFcmTokenRequest req, CancellationToken ct)
    {
        await sender.Send(new UpdateFcmTokenCommand(HttpContext.GetUserId(), req.FcmToken), ct);
        return NoContent();
    }
}

public record UpdateMeRequest(string Name, string? CurrentPassword = null, string? NewPassword = null);
public record UpdateFcmTokenRequest(string FcmToken);