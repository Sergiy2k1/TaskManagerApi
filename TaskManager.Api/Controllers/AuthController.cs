using Microsoft.AspNetCore.Mvc;
using TaskManager.Api.Contracts.Auth;
using TaskManager.Application.Users.Login;
using TaskManager.Application.Users.Register;

namespace TaskManager.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly RegisterUserHandler _registerUserHandler;
    private readonly LoginUserHandler _loginUserHandler;

    public AuthController(
        RegisterUserHandler registerUserHandler,
        LoginUserHandler loginUserHandler)
    {
        _registerUserHandler = registerUserHandler;
        _loginUserHandler = loginUserHandler;
    }

    [HttpPost("register")]
    [ProducesResponseType(
        typeof(RegisterResponse),
        StatusCodes.Status201Created)]
    public async Task<ActionResult<RegisterResponse>> Register(
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var command = new RegisterUserCommand(
            Email: request.Email,
            DisplayName: request.DisplayName,
            Password: request.Password);

        var result =
            await _registerUserHandler.HandleAsync(
                command,
                cancellationToken);

        var response = new RegisterResponse(
            UserId: result.UserId,
            Email: result.Email,
            DisplayName: result.DisplayName,
            CreatedAtUtc: result.CreatedAtUtc);

        return StatusCode(
            StatusCodes.Status201Created,
            response);
    }

    [HttpPost("login")]
    [ProducesResponseType(
        typeof(LoginResponse),
        StatusCodes.Status200OK)]
    public async Task<ActionResult<LoginResponse>> Login(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        var command = new LoginUserCommand(
            Email: request.Email,
            Password: request.Password);

        var result =
            await _loginUserHandler.HandleAsync(
                command,
                cancellationToken);

        var response = new LoginResponse(
            UserId: result.UserId,
            Email: result.Email,
            DisplayName: result.DisplayName,
            AccessToken: result.AccessToken,
            AccessTokenExpiresAtUtc:
                result.AccessTokenExpiresAtUtc);

        return Ok(response);
    }
}