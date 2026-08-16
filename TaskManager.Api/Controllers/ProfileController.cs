using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManager.Application.Abstractions.Authentication;

namespace TaskManager.Api.Controllers;

[ApiController]
[Route("api/profile")]
[Authorize]
public sealed class ProfileController : ControllerBase
{
    private readonly ICurrentUser _currentUser;

    public ProfileController(
        ICurrentUser currentUser)
    {
        _currentUser = currentUser;
    }

    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new
        {
            _currentUser.UserId,
            _currentUser.Email,
            _currentUser.DisplayName
        });
    }
}