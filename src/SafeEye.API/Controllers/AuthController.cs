using SafeEye.Application.Auth.Commands;
using SafeEye.API.Filters;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SafeEye.API.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(ISender sender) : ControllerBase
{
    /// <summary>Register a new guardian account.</summary>
    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest req, CancellationToken ct)
    {
        var result = await sender.Send(new RegisterCommand(req.Name, req.Email, req.Password), ct);
        return CreatedAtAction(nameof(Register), result);
    }

    /// <summary>Authenticate and receive tokens.</summary>
    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest req, CancellationToken ct)
        => Ok(await sender.Send(new LoginCommand(req.Email, req.Password), ct));

    /// <summary>Exchange a refresh token for a new access token.</summary>
    [HttpPost("refresh")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest req, CancellationToken ct)
        => Ok(await sender.Send(new RefreshTokenCommand(req.RefreshToken), ct));

    /// <summary>Invalidate token(s).</summary>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest req, CancellationToken ct)
    {
        await sender.Send(new LogoutCommand(req.RefreshToken, req.AllDevices, HttpContext.GetUserId()), ct);
        return NoContent();
    }
}

public record RegisterRequest(string Name, string Email, string Password);
public record LoginRequest(string Email, string Password);
public record RefreshRequest(string RefreshToken);
public record LogoutRequest(string? RefreshToken, bool AllDevices = false);