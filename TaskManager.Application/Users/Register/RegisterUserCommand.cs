using TaskManager.Application.Abstractions.Messaging;

namespace TaskManager.Application.Users.Register;

public sealed record RegisterUserCommand(
    string Email,
    string DisplayName,
    string Password)
    : ICommand<RegisterUserResult>;