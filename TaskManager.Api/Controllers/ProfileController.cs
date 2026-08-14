using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;

namespace TaskManager.Api.Controllers;

[ApiController]
[Route("api/profile")]
[Authorize]
public sealed class ProfileController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        var userId =
            User.FindFirst(
                JwtRegisteredClaimNames.Sub)?.Value;

        var email =
            User.FindFirst(
                JwtRegisteredClaimNames.Email)?.Value;

        var displayName =
            User.FindFirst(
                "display_name")?.Value;

        return Ok(new
        {
            UserId = userId,
            Email = email,
            DisplayName = displayName
        });
    }
}