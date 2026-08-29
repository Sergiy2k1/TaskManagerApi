using TaskManager.Application.Abstractions.Messaging;

namespace TaskManager.Application.Users.Login;

public sealed record LoginUserCommand(
    string Email,
    string Password)
    : ICommand<LoginUserResult>;