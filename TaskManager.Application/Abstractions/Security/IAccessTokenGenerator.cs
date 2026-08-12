using TaskManager.Domain.Entities;

namespace TaskManager.Application.Abstractions.Security;

public interface IAccessTokenGenerator
{
    AccessToken Generate(
        User user,
        DateTimeOffset issuedAtUtc);
}

public sealed record AccessToken(
    string Value,
    DateTimeOffset ExpiresAtUtc);