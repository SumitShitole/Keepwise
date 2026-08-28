using Keepwise.Application.Identity;
using Keepwise.Application.Users;
using Keepwise.Api.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace Keepwise.Api.Controllers;

[ApiController]
[Route("v1/auth")]
public sealed class AuthController(
    UserService users,
    JwtTokenService tokens,
    IOptions<AuthOptions> authOptions) : ControllerBase
{
    [HttpPost("dev-login")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<AuthResponse>> DevLogin(DevLoginRequest request, CancellationToken cancellationToken)
    {
        if (!authOptions.Value.AllowDevLogin)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(request.Email) || !request.Email.Contains('@'))
        {
            return BadRequest(new { error = new { code = "invalid_email", message = "A valid email is required." } });
        }

        var email = request.Email.Trim().ToLowerInvariant();
        var uid = $"dev:{email}";
        var user = await users.ProvisionAsync(uid, email, request.DisplayName, cancellationToken);
        var jwt = tokens.CreateDevToken(user);
        return Ok(new AuthResponse(jwt, UserService.Map(user)));
    }

    [HttpPost("session")]
    [Authorize]
    public async Task<ActionResult<AuthResponse>> Session(CancellationToken cancellationToken)
    {
        var profile = await users.GetProfileAsync(cancellationToken);
        return Ok(new AuthResponse(string.Empty, profile));
    }

    [HttpPost("logout")]
    [Authorize]
    public IActionResult Logout() => NoContent();
}
