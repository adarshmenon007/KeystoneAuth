using Keystone.Application.Identity.Commands.LoginUser;
using Keystone.Application.Identity.Commands.Logout;
using Keystone.Application.Identity.Commands.RefreshToken;
using Keystone.Application.Identity.Commands.RegisterUser;
using Keystone.Application.Identity.Common;
using Keystone.Contracts.Identity;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Keystone.Api.Controllers;

[ApiController]
[AllowAnonymous]
[EnableRateLimiting("auth")]
[Route("auth")]
public class AuthController : ApiControllerBase
{
    private readonly ISender _sender;
    private readonly ILogger<AuthController> _logger;

    public AuthController(ISender sender, ILogger<AuthController> logger)
    {
        _sender = sender;
        _logger = logger;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var result = await _sender.Send(
            new RegisterUserCommand(request.Email, request.Password, request.DisplayName));

        return result.MatchFirst(
            auth =>
            {
                _logger.LogInformation("Account registered for {Email}.", request.Email);
                return Ok(ToResponse(auth));
            },
            error =>
            {
                _logger.LogWarning("Registration failed for {Email}: {ErrorCode}.", request.Email, error.Code);
                return Problem(error);
            });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await _sender.Send(new LoginUserCommand(request.Email, request.Password));

        return result.MatchFirst(
            auth =>
            {
                _logger.LogInformation("Login succeeded for {Email}.", request.Email);
                return Ok(ToResponse(auth));
            },
            error =>
            {
                _logger.LogWarning("Login failed for {Email}: {ErrorCode}.", request.Email, error.Code);
                return Problem(error);
            });
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request)
    {
        var result = await _sender.Send(new RefreshTokenCommand(request.RefreshToken));

        return result.MatchFirst(
            auth => Ok(ToResponse(auth)),
            error =>
            {
                _logger.LogWarning("Refresh rejected: {ErrorCode}.", error.Code);
                return Problem(error);
            });
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] RefreshRequest request)
    {
        var result = await _sender.Send(new LogoutCommand(request.RefreshToken));

        return result.MatchFirst<IActionResult>(
            _ =>
            {
                _logger.LogInformation("Logout processed.");
                return NoContent();
            },
            error => Problem(error));
    }

    private static AuthResponse ToResponse(AuthResult auth) => new(
        AccessToken: auth.AccessToken,
        RefreshToken: auth.RefreshToken,
        ExpiresAtUtc: auth.ExpiresAtUtc,
        UserId: auth.UserId,
        DisplayName: auth.DisplayName);
}
