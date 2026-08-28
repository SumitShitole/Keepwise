using Keepwise.Application.Identity;
using Keepwise.Application.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Keepwise.Api.Controllers;

[ApiController]
[Authorize]
[Route("v1/users")]
public sealed class UsersController(UserService users) : ControllerBase
{
    [HttpGet("me")]
    public async Task<ActionResult<UserProfileDto>> Me(CancellationToken cancellationToken) =>
        Ok(await users.GetProfileAsync(cancellationToken));

    [HttpPut("me")]
    public async Task<ActionResult<UserProfileDto>> Update(UpdateProfileRequest request, CancellationToken cancellationToken) =>
        Ok(await users.UpdateProfileAsync(request, cancellationToken));

    [HttpPost("me/devices")]
    public async Task<IActionResult> Device(RegisterDeviceRequest request, CancellationToken cancellationToken)
    {
        await users.RegisterDeviceAsync(request, cancellationToken);
        return NoContent();
    }
}
