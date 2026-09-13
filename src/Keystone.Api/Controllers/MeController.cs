using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Keystone.Contracts.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Keystone.Api.Controllers;

/// <summary>
/// A single protected endpoint that exists purely to demonstrate a JWT round-trip — this
/// repo has no business domain of its own to guard. Reads straight off the validated
/// token's claims; no database lookup needed, since the token already carries everything.
/// </summary>
[ApiController]
[Authorize]
[Route("me")]
public class MeController : ApiControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        var userId = Guid.Parse(User.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
        var email = User.FindFirstValue(JwtRegisteredClaimNames.Email)!;
        var displayName = User.FindFirstValue(JwtRegisteredClaimNames.Name)!;

        return Ok(new MeResponse(userId, email, displayName));
    }
}
